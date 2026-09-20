using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Bipins.AI.Api.Authentication;
using Bipins.AI.Core.Ingestion;
using Bipins.AI.Runtime.Routing;
using Bipins.AI.Vector;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Api;

public class SecurityRegressionTests
{
    private const string SigningKey = "regression-test-signing-key-32-characters-long";

    private static WebApplicationFactory<global::Program> CreateFactory() =>
        new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = SigningKey,
                ["Authentication:ApiKeys:reader:Key"] = "configured-reader-key",
                ["Authentication:ApiKeys:reader:TenantId"] = "tenant-a",
                ["Authentication:ApiKeys:reader:Roles:0"] = "User",
                ["Authentication:ApiKeys:admin:Key"] = "configured-admin-key",
                ["Authentication:ApiKeys:admin:TenantId"] = "tenant-a",
                ["Authentication:ApiKeys:admin:Roles:0"] = "Admin"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IVectorStore>();
                services.AddSingleton(Mock.Of<IVectorStore>());
                services.RemoveAll<IModelRouter>();
                services.AddSingleton(Mock.Of<IModelRouter>());
                services.RemoveAll<ITenantQuotaEnforcer>();
                var quota = new Mock<ITenantQuotaEnforcer>();
                quota.Setup(q => q.CanIngestDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
                services.AddSingleton(quota.Object);
            });
        });

    [Theory]
    [InlineData(null)]
    [InlineData("Basic dXNlcjphbnl0aGluZw==")]
    [InlineData("Bearer invalid")]
    public async Task MissingOrInvalidCredentials_AreRejected(string? authorization)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-a");
        if (authorization != null) client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
        var response = await client.GetAsync("/v1/costs/tenant-a");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("test-api-key", HttpStatusCode.Unauthorized)]
    [InlineData("CONFIGURED-READER-KEY", HttpStatusCode.Unauthorized)]
    [InlineData("configured-reader-key", HttpStatusCode.OK)]
    public async Task OnlyExplicitlyConfiguredCaseSensitiveKeysWork(string key, HttpStatusCode expected)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", key);
        Assert.Equal(expected, (await client.GetAsync("/v1/costs/tenant-a")).StatusCode);
    }

    [Theory]
    [InlineData("/v1/costs/tenant-b")]
    [InlineData("/v1/costs/tenant-b/records")]
    public async Task Costs_RequireTenantOwnership(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "configured-reader-key");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-b");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Admin_CanReadAnotherTenantsCosts()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "configured-admin-key");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/costs/tenant-b")).StatusCode);
    }

    [Theory]
    [InlineData("/v1/ingest/text")]
    [InlineData("/v1/ingest/batch")]
    public async Task Ingestion_CannotOverrideAuthenticatedTenant(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "configured-reader-key");
        var response = await client.PostAsJsonAsync(path, new { tenantId = "tenant-b", text = "hello", texts = new[] { "hello" } });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("../../secrets.json")]
    public async Task Batch_RejectsServerPaths(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "configured-reader-key");
        var response = await client.PostAsJsonAsync("/v1/ingest/batch", new { sourceUris = new[] { path } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(true, false, HttpStatusCode.OK)]
    [InlineData(false, false, HttpStatusCode.Unauthorized)]
    [InlineData(true, true, HttpStatusCode.Unauthorized)]
    public async Task Jwt_RequiresTenantAndValidLifetime(bool tenantClaim, bool expired, HttpStatusCode expected)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var claims = tenantClaim ? new[] { new Claim("tenantId", "tenant-a") } : Array.Empty<Claim>();
        var jwt = new JwtSecurityToken("Bipins.AI", "Bipins.AI", claims,
            DateTime.UtcNow.AddHours(-2), expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        Assert.Equal(expected, (await client.GetAsync("/v1/costs/tenant-a")).StatusCode);
    }

    [Fact]
    public async Task EmptyKeyConfiguration_DeniesAllKeys()
    {
        var validator = new ConfigurationApiKeyValidator(new ConfigurationBuilder().Build());
        Assert.False((await validator.ValidateAsync("test-api-key")).IsValid);
    }
}
