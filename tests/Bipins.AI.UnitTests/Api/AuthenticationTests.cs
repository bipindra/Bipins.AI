using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Bipins.AI.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Api;

public class AuthenticationTests
{
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<IApiKeyValidator> _apiKeyValidator;
    private readonly Mock<IAuditLogger> _auditLogger;
    private readonly UrlEncoder _urlEncoder;

    public AuthenticationTests()
    {
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        _apiKeyValidator = new Mock<IApiKeyValidator>();
        _auditLogger = new Mock<IAuditLogger>();
        _urlEncoder = UrlEncoder.Default;
    }

    [Fact]
    public void ApiKeyAuthenticationHandler_CanBeInstantiated()
    {
        var handler = new ApiKeyAuthenticationHandler(
            new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>().Object,
            _loggerFactory.Object,
            _urlEncoder,
            _apiKeyValidator.Object,
            _auditLogger.Object);

        Assert.NotNull(handler);
    }

    [Fact]
    public void BasicAuthenticationHandler_CanBeInstantiated()
    {
        var handler = new BasicAuthenticationHandler(
            new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>().Object,
            _loggerFactory.Object,
            _urlEncoder,
            _auditLogger.Object);

        Assert.NotNull(handler);
    }

    [Fact]
    public void JwtAuthenticationHandler_CanBeInstantiated()
    {
        var options = new Mock<IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions());
        
        var handler = new JwtAuthenticationHandler(
            options.Object,
            _loggerFactory.Object,
            _urlEncoder);

        // Note: JWT handler testing is complex as it requires actual JWT token validation
        // This is a simplified test that verifies the handler can be instantiated
        Assert.NotNull(handler);
    }

    [Fact]
    public void InMemoryApiKeyValidator_ValidateAsync_ValidKey_ReturnsSuccess()
    {
        var logger = new Mock<ILogger<InMemoryApiKeyValidator>>();
        var validator = new InMemoryApiKeyValidator(logger.Object);

        var result = validator.ValidateAsync("test-api-key").Result;

        Assert.True(result.IsValid);
        Assert.Equal("default", result.TenantId);
        Assert.Equal("test-key-1", result.ApiKeyId);
    }

    [Fact]
    public void InMemoryApiKeyValidator_ValidateAsync_InvalidKey_ReturnsFailure()
    {
        var logger = new Mock<ILogger<InMemoryApiKeyValidator>>();
        var validator = new InMemoryApiKeyValidator(logger.Object);

        var result = validator.ValidateAsync("invalid-key").Result;

        Assert.False(result.IsValid);
    }

    [Fact]
    public void InMemoryApiKeyValidator_AddApiKey_AddsKey()
    {
        var logger = new Mock<ILogger<InMemoryApiKeyValidator>>();
        var validator = new InMemoryApiKeyValidator(logger.Object);

        validator.AddApiKey("custom-key", "tenant1", "key1", new[] { "Admin" });

        var result = validator.ValidateAsync("custom-key").Result;

        Assert.True(result.IsValid);
        Assert.Equal("tenant1", result.TenantId);
        Assert.Equal("key1", result.ApiKeyId);
        Assert.Contains("Admin", result.Roles ?? Array.Empty<string>());
    }
}
