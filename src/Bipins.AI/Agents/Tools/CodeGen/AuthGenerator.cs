using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Generates authentication handler classes from OpenAPI security schemes.
/// </summary>
public class AuthGenerator : IAuthGenerator
{
    private readonly ILogger<AuthGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthGenerator"/> class.
    /// </summary>
    public AuthGenerator(ILogger<AuthGenerator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<GeneratedFile>> GenerateAsync(
        OpenApiDocument document,
        string namespaceName,
        GeneratorOptions options,
        CancellationToken cancellationToken = default)
    {
        var files = new List<GeneratedFile>();

        if (document.Components?.SecuritySchemes == null || document.Components.SecuritySchemes.Count == 0)
        {
            _logger.LogInformation("No security schemes found in OpenAPI document");
            return Task.FromResult(files);
        }

        _logger.LogInformation("Generating authentication handlers for {Count} security schemes", 
            document.Components.SecuritySchemes.Count);

        foreach (var securityScheme in document.Components.SecuritySchemes)
        {
            try
            {
                var code = GenerateAuthHandler(securityScheme.Key, securityScheme.Value, namespaceName, options);
                
                if (!string.IsNullOrEmpty(code))
                {
                    files.Add(new GeneratedFile(
                        Path: $"Auth/{securityScheme.Key}AuthenticationHandler.cs",
                        Content: code,
                        Description: $"Authentication handler for {securityScheme.Key}"));

                    _logger.LogDebug("Generated auth handler for {SchemeName}", securityScheme.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating auth handler for {SchemeName}", securityScheme.Key);
            }
        }

        _logger.LogInformation("Successfully generated {Count} auth handler files", files.Count);
        return Task.FromResult(files);
    }

    private string GenerateAuthHandler(
        string schemeName,
        OpenApiSecurityScheme scheme,
        string namespaceName,
        GeneratorOptions options)
    {
        return scheme.Type switch
        {
            SecuritySchemeType.Http when scheme.Scheme?.ToLower() == "bearer" 
                => GenerateBearerAuthHandler(schemeName, namespaceName, options),
            SecuritySchemeType.ApiKey 
                => GenerateApiKeyAuthHandler(schemeName, scheme, namespaceName, options),
            _ => string.Empty
        };
    }

    private string GenerateBearerAuthHandler(string schemeName, string namespaceName, GeneratorOptions options)
    {
        var sb = new StringBuilder();

        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName}.Auth;");
        sb.AppendLine();
        sb.AppendLine("using System.Net.Http.Headers;");
        sb.AppendLine();

        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Provides bearer token for API requests.");
            sb.AppendLine("/// </summary>");
        }
        sb.AppendLine("public interface ITokenProvider");
        sb.AppendLine("{");
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Gets the bearer token for authentication.");
            sb.AppendLine("    /// </summary>");
        }
        sb.AppendLine("    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);");
        sb.AppendLine("}");
        sb.AppendLine();

        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Delegating handler that adds bearer token authentication to HTTP requests.");
            sb.AppendLine("/// </summary>");
        }
        sb.AppendLine("public class BearerAuthenticationHandler : DelegatingHandler");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ITokenProvider _tokenProvider;");
        sb.AppendLine();
        
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Initializes a new instance of the <see cref=\"BearerAuthenticationHandler\"/> class.");
            sb.AppendLine("    /// </summary>");
        }
        sb.AppendLine("    public BearerAuthenticationHandler(ITokenProvider tokenProvider)");
        sb.AppendLine("    {");
        sb.AppendLine("        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("    /// <inheritdoc />");
        }
        sb.AppendLine("    protected override async Task<HttpResponseMessage> SendAsync(");
        sb.AppendLine("        HttpRequestMessage request,");
        sb.AppendLine("        CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        var token = await _tokenProvider.GetTokenAsync(cancellationToken);");
        sb.AppendLine("        request.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", token);");
        sb.AppendLine();
        sb.AppendLine("        return await base.SendAsync(request, cancellationToken);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateApiKeyAuthHandler(
        string schemeName,
        OpenApiSecurityScheme scheme,
        string namespaceName,
        GeneratorOptions options)
    {
        var sb = new StringBuilder();

        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName}.Auth;");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.Options;");
        sb.AppendLine();

        // Options class
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Options for API key authentication.");
            sb.AppendLine("/// </summary>");
        }
        sb.AppendLine("public class ApiKeyOptions");
        sb.AppendLine("{");
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Gets or sets the API key.");
            sb.AppendLine("    /// </summary>");
        }
        sb.AppendLine("    public string ApiKey { get; set; } = string.Empty;");
        sb.AppendLine("}");
        sb.AppendLine();

        // Handler class
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Delegating handler that adds API key authentication to HTTP requests.");
            sb.AppendLine("/// </summary>");
        }
        sb.AppendLine("public class ApiKeyAuthenticationHandler : DelegatingHandler");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ApiKeyOptions _options;");
        sb.AppendLine($"    private const string ApiKeyName = \"{scheme.Name}\";");
        sb.AppendLine();
        
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Initializes a new instance of the <see cref=\"ApiKeyAuthenticationHandler\"/> class.");
            sb.AppendLine("    /// </summary>");
        }
        sb.AppendLine("    public ApiKeyAuthenticationHandler(IOptions<ApiKeyOptions> options)");
        sb.AppendLine("    {");
        sb.AppendLine("        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("    /// <inheritdoc />");
        }
        sb.AppendLine("    protected override Task<HttpResponseMessage> SendAsync(");
        sb.AppendLine("        HttpRequestMessage request,");
        sb.AppendLine("        CancellationToken cancellationToken)");
        sb.AppendLine("    {");

        if (scheme.In == ParameterLocation.Header)
        {
            sb.AppendLine("        request.Headers.Add(ApiKeyName, _options.ApiKey);");
        }
        else if (scheme.In == ParameterLocation.Query)
        {
            sb.AppendLine("        var uriBuilder = new UriBuilder(request.RequestUri!);");
            sb.AppendLine("        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);");
            sb.AppendLine("        query[ApiKeyName] = _options.ApiKey;");
            sb.AppendLine("        uriBuilder.Query = query.ToString();");
            sb.AppendLine("        request.RequestUri = uriBuilder.Uri;");
        }

        sb.AppendLine();
        sb.AppendLine("        return base.SendAsync(request, cancellationToken);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void AppendFileHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine($"// Generated by Bipins.AI Swagger Client Generator at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("// Do not modify this file manually");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
    }
}
