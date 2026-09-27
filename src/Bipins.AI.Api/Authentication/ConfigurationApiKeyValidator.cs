using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bipins.AI.Api.Authentication;

/// <summary>Validates explicitly provisioned keys; an empty configuration denies all keys.</summary>
public sealed class ConfigurationApiKeyValidator : IApiKeyValidator
{
    private readonly IConfiguration _configuration;

    public ConfigurationApiKeyValidator(IConfiguration configuration) => _configuration = configuration;

    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
            foreach (var entry in _configuration.GetSection("Authentication:ApiKeys").GetChildren())
            {
                var key = entry["Key"];
                var tenant = entry["TenantId"];
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(tenant)) continue;
                if (CryptographicOperations.FixedTimeEquals(suppliedHash, SHA256.HashData(Encoding.UTF8.GetBytes(key))))
                    return Task.FromResult(new ApiKeyValidationResult
                    {
                        IsValid = true,
                        ApiKeyId = entry.Key,
                        TenantId = tenant,
                        Roles = entry.GetSection("Roles").GetChildren()
                            .Select(role => role.Value).OfType<string>().ToArray()
                    });
            }
        }
        return Task.FromResult(new ApiKeyValidationResult { IsValid = false });
    }
}
