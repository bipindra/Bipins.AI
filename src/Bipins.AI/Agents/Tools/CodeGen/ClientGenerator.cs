using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Generates C# API client classes from OpenAPI operations.
/// </summary>
public class ClientGenerator : IClientGenerator
{
    private readonly ILogger<ClientGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientGenerator"/> class.
    /// </summary>
    public ClientGenerator(ILogger<ClientGenerator> logger)
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

        if (document.Paths == null || document.Paths.Count == 0)
        {
            _logger.LogWarning("No paths found in OpenAPI document");
            return Task.FromResult(files);
        }

        // Group operations by tag
        var operationsByTag = GroupOperationsByTag(document);

        _logger.LogInformation("Generating clients for {Count} tags", operationsByTag.Count);

        foreach (var tagGroup in operationsByTag)
        {
            try
            {
                var clientName = $"{tagGroup.Key}Client";

                // Generate interface
                if (options.GenerateInterfaces)
                {
                    var interfaceCode = GenerateClientInterface(
                        clientName,
                        tagGroup.Value,
                        namespaceName,
                        options);

                    files.Add(new GeneratedFile(
                        Path: $"Clients/I{clientName}.cs",
                        Content: interfaceCode,
                        Description: $"Interface for {clientName}"));
                }

                // Generate implementation
                var clientCode = GenerateClientImplementation(
                    clientName,
                    tagGroup.Value,
                    namespaceName,
                    options);

                files.Add(new GeneratedFile(
                    Path: $"Clients/{clientName}.cs",
                    Content: clientCode,
                    Description: $"Implementation of {clientName}"));

                _logger.LogDebug("Generated client for tag {Tag}", tagGroup.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating client for tag {Tag}", tagGroup.Key);
            }
        }

        // Generate DI extension method
        if (options.GenerateDependencyInjection)
        {
            var diCode = GenerateDependencyInjectionSetup(namespaceName, operationsByTag.Keys.ToList(), options);
            files.Add(new GeneratedFile(
                Path: "ServiceCollectionExtensions.cs",
                Content: diCode,
                Description: "Dependency injection setup"));
        }

        _logger.LogInformation("Successfully generated {Count} client files", files.Count);
        return Task.FromResult(files);
    }

    private Dictionary<string, List<OperationInfo>> GroupOperationsByTag(OpenApiDocument document)
    {
        var groups = new Dictionary<string, List<OperationInfo>>();

        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var tag = operation.Value.Tags?.FirstOrDefault()?.Name ?? "Default";
                
                if (!groups.ContainsKey(tag))
                    groups[tag] = new List<OperationInfo>();

                groups[tag].Add(new OperationInfo
                {
                    Path = path.Key,
                    Method = operation.Key,
                    Operation = operation.Value
                });
            }
        }

        return groups;
    }

    private string GenerateClientInterface(
        string clientName,
        List<OperationInfo> operations,
        string namespaceName,
        GeneratorOptions options)
    {
        var sb = new StringBuilder();
        
        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName}.Clients;");
        sb.AppendLine();
        sb.AppendLine($"using {namespaceName}.Models;");
        sb.AppendLine();
        
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Interface for {clientName}.");
            sb.AppendLine("/// </summary>");
        }
        
        sb.AppendLine($"public interface I{clientName}");
        sb.AppendLine("{");

        foreach (var op in operations)
        {
            var methodSignature = GenerateMethodSignature(op, options, interfaceMode: true);
            sb.AppendLine(methodSignature);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GenerateClientImplementation(
        string clientName,
        List<OperationInfo> operations,
        string namespaceName,
        GeneratorOptions options)
    {
        var sb = new StringBuilder();
        
        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName}.Clients;");
        sb.AppendLine();
        sb.AppendLine("using System.Net.Http.Json;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine($"using {namespaceName}.Models;");
        sb.AppendLine();
        
        if (options.IncludeXmlDocs)
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Implementation of {clientName}.");
            sb.AppendLine("/// </summary>");
        }
        
        var implementsClause = options.GenerateInterfaces ? $" : I{clientName}" : "";
        sb.AppendLine($"public class {clientName}{implementsClause}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly HttpClient _httpClient;");
        sb.AppendLine("    private readonly ILogger<" + clientName + "> _logger;");
        sb.AppendLine();
        
        // Constructor
        sb.AppendLine("    public " + clientName + "(HttpClient httpClient, ILogger<" + clientName + "> logger)");
        sb.AppendLine("    {");
        sb.AppendLine("        _httpClient = httpClient;");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Methods
        foreach (var op in operations)
        {
            var methodImpl = GenerateMethodImplementation(op, options);
            sb.AppendLine(methodImpl);
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GenerateMethodSignature(OperationInfo op, GeneratorOptions options, bool interfaceMode)
    {
        var methodName = GetMethodName(op, options);
        var returnType = GetReturnType(op);
        var parameters = GetParameters(op);
        
        var sb = new StringBuilder();
        
        if (options.IncludeXmlDocs && !interfaceMode)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {op.Operation.Summary ?? op.Operation.OperationId ?? "Executes the operation"}");
            sb.AppendLine("    /// </summary>");
        }
        
        var indent = interfaceMode ? "    " : "    ";
        sb.Append($"{indent}Task<{returnType}> {methodName}({parameters}CancellationToken cancellationToken = default)");
        
        if (interfaceMode)
            sb.Append(";");
        
        return sb.ToString();
    }

    private string GenerateMethodImplementation(OperationInfo op, GeneratorOptions options)
    {
        var sb = new StringBuilder();
        var methodName = GetMethodName(op, options);
        var returnType = GetReturnType(op);
        var parameters = GetParameters(op);
        
        sb.AppendLine($"    public async Task<{returnType}> {methodName}({parameters}CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine($"            var requestUri = \"{op.Path}\";");
        sb.AppendLine();
        sb.AppendLine($"            using var response = await _httpClient.{GetHttpMethod(op.Method)}Async(requestUri, cancellationToken);");
        sb.AppendLine("            response.EnsureSuccessStatusCode();");
        sb.AppendLine();
        
        if (returnType != "string")
        {
            sb.AppendLine($"            return await response.Content.ReadFromJsonAsync<{returnType}>(cancellationToken: cancellationToken)");
            sb.AppendLine("                ?? throw new InvalidOperationException(\"Response was null\");");
        }
        else
        {
#if NETSTANDARD2_1
            sb.AppendLine("            return await response.Content.ReadAsStringAsync();");
#else
            sb.AppendLine("            return await response.Content.ReadAsStringAsync(cancellationToken);");
#endif
        }
        
        sb.AppendLine("        }");
        sb.AppendLine("        catch (HttpRequestException ex)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _logger.LogError(ex, \"Error calling {methodName}\");");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        
        return sb.ToString();
    }

    private string GenerateDependencyInjectionSetup(
        string namespaceName,
        List<string> clientTags,
        GeneratorOptions options)
    {
        var sb = new StringBuilder();
        
        AppendFileHeader(sb);
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine($"using {namespaceName}.Clients;");
        sb.AppendLine();
        
        sb.AppendLine("public static class ServiceCollectionExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddApiClients(this IServiceCollection services, string baseUrl)");
        sb.AppendLine("    {");
        
        foreach (var tag in clientTags)
        {
            var clientName = $"{tag}Client";
            sb.AppendLine($"        services.AddHttpClient<I{clientName}, {clientName}>(client =>");
            sb.AppendLine("        {");
            sb.AppendLine("            client.BaseAddress = new Uri(baseUrl);");
            sb.AppendLine("        });");
            sb.AppendLine();
        }
        
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GetMethodName(OperationInfo op, GeneratorOptions options)
    {
        var name = op.Operation.OperationId ?? $"{op.Method}_{op.Path.Replace("/", "_").Replace("{", "").Replace("}", "")}";
        name = TypeMapper.ToPascalCase(name);
        
        if (options.AsyncSuffix && !name.EndsWith("Async"))
            name += "Async";
        
        return name;
    }

    private string GetReturnType(OperationInfo op)
    {
        var response = op.Operation.Responses?.FirstOrDefault(r => r.Key.StartsWith("2"));
        if (response?.Value?.Content == null)
            return "string";
        
        var content = response.Value.Value.Content.FirstOrDefault();
        if (content.Value?.Schema?.Reference != null)
        {
            return TypeMapper.GetTypeNameFromReference(content.Value.Schema.Reference.Id);
        }
        
        return "string";
    }

    private string GetParameters(OperationInfo op)
    {
        // Simplified - just return empty for now
        return "";
    }

    private string GetHttpMethod(OperationType method)
    {
        return method.ToString();
    }

    private void AppendFileHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine($"// Generated by Bipins.AI Swagger Client Generator at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("// Do not modify this file manually");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
    }

    private class OperationInfo
    {
        public string Path { get; set; } = string.Empty;
        public OperationType Method { get; set; }
        public OpenApiOperation Operation { get; set; } = new();
    }
}

