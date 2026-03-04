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
                var schemeFiles = GenerateAuthHandlerFiles(securityScheme.Key, securityScheme.Value, namespaceName, options);
                foreach (var f in schemeFiles)
                {
                    files.Add(f);
                    _logger.LogDebug("Generated auth file {Path} for {SchemeName}", f.Path, securityScheme.Key);
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

    private List<GeneratedFile> GenerateAuthHandlerFiles(
        string schemeName,
        OpenApiSecurityScheme scheme,
        string namespaceName,
        GeneratorOptions options)
    {
        if (scheme.Type == SecuritySchemeType.Http && scheme.Scheme?.ToLower() == "bearer")
            return GenerateBearerAuthHandlerFiles(schemeName, namespaceName, options);
        if (scheme.Type == SecuritySchemeType.ApiKey)
            return GenerateApiKeyAuthHandlerFiles(schemeName, scheme, namespaceName, options);
        return new List<GeneratedFile>();
    }

    private List<GeneratedFile> GenerateBearerAuthHandlerFiles(string schemeName, string namespaceName, GeneratorOptions options)
    {
        var files = new List<GeneratedFile>();
        var sb = new StringBuilder();
        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName}.Auth;");
        sb.AppendLine();
        sb.AppendLine("using System.Net.Http.Headers;");
        sb.AppendLine();
        if (options.IncludeXmlDocs)
            sb.AppendLine("/// <summary>Provides bearer token for API requests.</summary>");
        sb.AppendLine("public interface ITokenProvider");
        sb.AppendLine("{");
        sb.AppendLine("    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);");
        sb.AppendLine("}");
        files.Add(new GeneratedFile(Path: "Auth/ITokenProvider.cs", Content: sb.ToString(), Description: "Token provider interface"));
        sb = new StringBuilder();
        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName}.Auth;");
        sb.AppendLine();
        sb.AppendLine("using System.Net.Http.Headers;");
        sb.AppendLine();
        if (options.IncludeXmlDocs)
            sb.AppendLine("/// <summary>Delegating handler that adds bearer token authentication to HTTP requests.</summary>");
        sb.AppendLine("public class BearerAuthenticationHandler : DelegatingHandler");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ITokenProvider _tokenProvider;");
        sb.AppendLine();
        sb.AppendLine("    public BearerAuthenticationHandler(ITokenProvider tokenProvider)");
        sb.AppendLine("    {");
        sb.AppendLine("        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    protected override async Task<HttpResponseMessage> SendAsync(");
        sb.AppendLine("        HttpRequestMessage request,");
        sb.AppendLine("        CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        var token = await _tokenProvider.GetTokenAsync(cancellationToken);");
        sb.AppendLine("        request.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", token);");
        sb.AppendLine("        return await base.SendAsync(request, cancellationToken);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        files.Add(new GeneratedFile(Path: "Auth/BearerAuthenticationHandler.cs", Content: sb.ToString(), Description: "Bearer auth handler"));
        return files;
    }

    private List<GeneratedFile> GenerateApiKeyAuthHandlerFiles(
        string schemeName,
        OpenApiSecurityScheme scheme,
        string namespaceName,
        GeneratorOptions options)
    {
        var prefix = TypeMapper.ToPascalCase(schemeName);
        var optionsClassName = prefix + "Options";
        var handlerClassName = prefix + "AuthenticationHandler";
        var files = new List<GeneratedFile>();
        var sb = new StringBuilder();
        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName}.Auth;");
        sb.AppendLine();
        if (options.IncludeXmlDocs)
            sb.AppendLine("/// <summary>Options for API key authentication.</summary>");
        sb.AppendLine($"public class {optionsClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    public string ApiKey { get; set; } = string.Empty;");
        sb.AppendLine("}");
        files.Add(new GeneratedFile(Path: $"Auth/{optionsClassName}.cs", Content: sb.ToString(), Description: "API key options"));
        sb = new StringBuilder();
        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName}.Auth;");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.Options;");
        sb.AppendLine();
        if (options.IncludeXmlDocs)
            sb.AppendLine("/// <summary>Delegating handler that adds API key authentication to HTTP requests.</summary>");
        sb.AppendLine($"public class {handlerClassName} : DelegatingHandler");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {optionsClassName} _options;");
        sb.AppendLine($"    private const string ApiKeyName = \"{scheme.Name}\";");
        sb.AppendLine();
        sb.AppendLine($"    public {handlerClassName}(IOptions<{optionsClassName}> options)");
        sb.AppendLine("    {");
        sb.AppendLine("        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    protected override Task<HttpResponseMessage> SendAsync(");
        sb.AppendLine("        HttpRequestMessage request,");
        sb.AppendLine("        CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        if (scheme.In == ParameterLocation.Header)
            sb.AppendLine("        request.Headers.Add(ApiKeyName, _options.ApiKey);");
        else if (scheme.In == ParameterLocation.Query)
        {
            sb.AppendLine("        var uriBuilder = new UriBuilder(request.RequestUri!);");
            sb.AppendLine("        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);");
            sb.AppendLine("        query[ApiKeyName] = _options.ApiKey;");
            sb.AppendLine("        uriBuilder.Query = query.ToString();");
            sb.AppendLine("        request.RequestUri = uriBuilder.Uri;");
        }
        sb.AppendLine("        return base.SendAsync(request, cancellationToken);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        files.Add(new GeneratedFile(Path: $"Auth/{handlerClassName}.cs", Content: sb.ToString(), Description: "API key auth handler"));
        return files;
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
