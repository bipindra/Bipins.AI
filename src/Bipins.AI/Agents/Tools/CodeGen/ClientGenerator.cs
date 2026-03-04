using System.Collections.Generic;
using System.Linq;
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
                var clientName = TypeMapper.ToPascalCase(tagGroup.Key) + "Client";

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
            var pathItemParams = path.Value.Parameters ?? new List<OpenApiParameter>();
            foreach (var operation in path.Value.Operations)
            {
                var tag = operation.Value.Tags?.FirstOrDefault()?.Name ?? "Default";
                var opParams = operation.Value.Parameters ?? new List<OpenApiParameter>();
                var merged = pathItemParams
                    .Where(pp => opParams.All(op => op.Name != pp.Name))
                    .Concat(opParams)
                    .ToList();
                if (!groups.ContainsKey(tag))
                    groups[tag] = new List<OperationInfo>();

                groups[tag].Add(new OperationInfo
                {
                    Path = path.Key,
                    Method = operation.Key,
                    Operation = operation.Value,
                    MergedParameters = merged
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
        sb.AppendLine("#nullable enable");
        sb.AppendLine($"namespace {namespaceName}.Clients;");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
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
        sb.AppendLine("#nullable enable");
        sb.AppendLine($"namespace {namespaceName}.Clients;");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
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
        var hasJsonBody = GetRequestBodyType(op) != null;
        var hasMultipart = HasMultipartFileUpload(op);
        var allParams = GetOperationParams(op);
        
        sb.AppendLine($"    public async Task<{returnType}> {methodName}({parameters}CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        foreach (var line in GetRequestUriAndQueryLines(op.Path, allParams))
            sb.AppendLine(line);
        sb.AppendLine();
        foreach (var line in GetHttpCallLines(op, hasJsonBody, hasMultipart, allParams))
            sb.AppendLine(line);
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

    private IEnumerable<string> GetRequestUriAndQueryLines(string pathTemplate, List<OperationParamInfo> allParams)
    {
        var pathParams = allParams.Where(p => p.Location == ParamLocation.Path).ToList();
        var queryParams = allParams.Where(p => p.Location == ParamLocation.Query).ToList();
        if (pathParams.Count == 0 && queryParams.Count == 0)
        {
            yield return "            var requestUri = \"" + EscapePathForCode(pathTemplate) + "\";";
            yield break;
        }
        yield return "            var requestUri = \"" + EscapePathForCode(pathTemplate) + "\";";
        foreach (var p in pathParams)
            yield return $"            requestUri = requestUri.Replace(\"{{{p.ApiName}}}\", System.Uri.EscapeDataString({p.CSharpName}.ToString()));";
        if (queryParams.Count > 0)
        {
            yield return "            var queryParts = new System.Collections.Generic.List<string>();";
            foreach (var p in queryParams)
            {
                var isCollection = p.CSharpType.StartsWith("List<", StringComparison.Ordinal) || p.CSharpType.StartsWith("IEnumerable<", StringComparison.Ordinal);
                if (isCollection)
                {
                    if (!p.Required)
                        yield return $"            if ({p.CSharpName} != null)";
                    yield return $"                foreach (var __v in {p.CSharpName}) queryParts.Add(\"{p.ApiName}=\" + System.Uri.EscapeDataString(__v?.ToString() ?? \"\"));";
                }
                else
                {
                    var addExpr = p.Required
                        ? $"queryParts.Add(\"{p.ApiName}=\" + System.Uri.EscapeDataString({p.CSharpName}.ToString()));"
                        : $"if ({p.CSharpName} != null) queryParts.Add(\"{p.ApiName}=\" + System.Uri.EscapeDataString({p.CSharpName}.ToString() ?? \"\"));";
                    yield return "            " + addExpr;
                }
            }
            yield return "            if (queryParts.Count > 0) requestUri += \"?\" + string.Join(\"&\", queryParts);";
        }
    }

    private static string EscapePathForCode(string path)
    {
        return path.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private IEnumerable<string> GetHttpCallLines(OperationInfo op, bool hasJsonBody, bool hasMultipart, List<OperationParamInfo> allParams)
    {
        var method = op.Method;
        var methodName = GetHttpMethod(method);
        var headerParams = allParams.Where(p => p.Location == ParamLocation.Header).ToList();
        var useRequestMessage = headerParams.Count > 0;

        if (useRequestMessage)
        {
            yield return "            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod." + methodName + ", requestUri);";
            foreach (var p in headerParams)
                yield return $"            if ({p.CSharpName} != null) request.Headers.TryAddWithoutValidation(\"{p.ApiName}\", {p.CSharpName});";
        }

        if (hasMultipart && (method == OperationType.Post || method == OperationType.Put || method == OperationType.Patch))
        {
            yield return "            using var content = new System.Net.Http.MultipartFormDataContent();";
            yield return "            if (fileContent != null) content.Add(new System.Net.Http.StreamContent(fileContent), \"file\", \"file\");";
            yield return "            if (additionalMetadata != null) content.Add(new System.Net.Http.StringContent(additionalMetadata), \"additionalMetadata\");";
            if (useRequestMessage)
            {
                yield return "            request.Content = content;";
                yield return "            using var response = await _httpClient.SendAsync(request, cancellationToken);";
            }
            else
                yield return "            using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);";
            yield break;
        }
        if (hasJsonBody && (method == OperationType.Post || method == OperationType.Put || method == OperationType.Patch))
        {
            if (useRequestMessage)
            {
                yield return "            request.Content = System.Net.Http.Json.JsonContent.Create(body);";
                yield return "            using var response = await _httpClient.SendAsync(request, cancellationToken);";
            }
            else
            {
                var sendMethod = method == OperationType.Post ? "PostAsJsonAsync" : method == OperationType.Put ? "PutAsJsonAsync" : "PostAsJsonAsync";
                if (method == OperationType.Patch) sendMethod = "PatchAsJsonAsync";
                yield return $"            using var response = await _httpClient.{sendMethod}(requestUri, body, cancellationToken);";
            }
            yield break;
        }
        if (method == OperationType.Post || method == OperationType.Put || method == OperationType.Patch)
        {
            if (useRequestMessage)
            {
                yield return "            request.Content = new System.Net.Http.StringContent(string.Empty);";
                yield return "            using var response = await _httpClient.SendAsync(request, cancellationToken);";
            }
            else
                yield return "            using var response = await _httpClient." + methodName + "Async(requestUri, new System.Net.Http.StringContent(string.Empty), cancellationToken);";
            yield break;
        }
        if (useRequestMessage)
            yield return "            using var response = await _httpClient.SendAsync(request, cancellationToken);";
        else
            yield return "            using var response = await _httpClient." + methodName + "Async(requestUri, cancellationToken);";
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
            var clientName = TypeMapper.ToPascalCase(tag) + "Client";
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

    private bool HasMultipartFileUpload(OperationInfo op)
    {
        var body = op.Operation.RequestBody;
        if (body?.Content != null)
        {
            var multipart = body.Content.FirstOrDefault(c =>
                c.Key.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase));
            if (multipart.Value?.Schema != null)
                return true;
        }
        // Swagger 2.0: file upload is in Parameters (formData, type: file); parameter name is often "file"
        if (op.Operation.Parameters != null)
        {
            foreach (var p in op.Operation.Parameters)
            {
                if (p.Schema?.Format == "binary" || (p.Schema?.Type == "string" && p.Schema?.Format == "binary"))
                    return true;
                if (string.Equals(p.Name, "file", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private string? GetRequestBodyType(OperationInfo op)
    {
        if (HasMultipartFileUpload(op))
            return null;
        var body = op.Operation.RequestBody;
        if (body?.Content == null || body.Content.Count == 0)
            return null;
        var jsonContent = body.Content.FirstOrDefault(c =>
            c.Key.Equals("application/json", StringComparison.OrdinalIgnoreCase));
        if (jsonContent.Value?.Schema == null)
            return null;
        var schema = jsonContent.Value.Schema;
        if (schema.Reference != null)
            return TypeMapper.GetTypeNameFromReference(schema.Reference.Id);
        return TypeMapper.MapToCSharpType(schema);
    }

    private string GetParameters(OperationInfo op)
    {
        var sb = new StringBuilder();
        var allParams = GetOperationParams(op);
        foreach (var p in allParams.Where(x => x.Location == ParamLocation.Path))
            sb.Append(GetParameterCSharpDeclaration(p));
        foreach (var p in allParams.Where(x => x.Location == ParamLocation.Query))
            sb.Append(GetParameterCSharpDeclaration(p));
        foreach (var p in allParams.Where(x => x.Location == ParamLocation.Header))
            sb.Append(GetParameterCSharpDeclaration(p));
        if (HasMultipartFileUpload(op))
            sb.Append("Stream? fileContent = null, string? additionalMetadata = null, ");
        else if (GetRequestBodyType(op) is { } bodyType)
            sb.Append($"{bodyType} body, ");
        return sb.ToString();
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
        public List<OpenApiParameter> MergedParameters { get; set; } = new();
    }

    private enum ParamLocation { Path, Query, Header }

    private record OperationParamInfo(string ApiName, string CSharpName, string CSharpType, ParamLocation Location, bool Required);

    private List<OperationParamInfo> GetOperationParams(OperationInfo op)
    {
        var list = new List<OperationParamInfo>();
        var pathTemplate = op.Path;
        var parameters = op.MergedParameters;
        if (parameters != null)
        {
            foreach (var p in parameters)
            {
                if (string.IsNullOrEmpty(p.Name)) continue;
                ParamLocation? loc = p.In switch
                {
                    ParameterLocation.Path => ParamLocation.Path,
                    ParameterLocation.Query => ParamLocation.Query,
                    ParameterLocation.Header => ParamLocation.Header,
                    _ => pathTemplate.Contains("{" + p.Name + "}", StringComparison.Ordinal) ? ParamLocation.Path : null
                };
                if (loc == null) continue;
                var schema = p.Schema;
                var csharpType = schema != null ? TypeMapper.MapToCSharpType(schema) : "string";
                var required = p.Required;
                var csharpName = TypeMapper.ToPascalCase(p.Name);
                if (csharpName == p.Name && p.Name.Length > 0 && char.IsLower(p.Name[0]))
                    csharpName = char.ToUpperInvariant(p.Name[0]) + p.Name.Substring(1);
                list.Add(new OperationParamInfo(p.Name, csharpName, csharpType, loc.Value, required));
            }
        }
        var inPath = new HashSet<string>(list.Where(x => x.Location == ParamLocation.Path).Select(x => x.ApiName));
        if (pathTemplate.IndexOf('{') >= 0)
        {
            var pathParamNames = new HashSet<string>();
            var parts = pathTemplate.Split('{', '}');
            for (var i = 1; i < parts.Length; i += 2)
                pathParamNames.Add(parts[i].Trim());
            foreach (var name in pathParamNames.Where(n => !string.IsNullOrEmpty(n) && !inPath.Contains(n)))
            {
                var csharpName = TypeMapper.ToPascalCase(name);
                var inferredType = name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ? "long" : "string";
                list.Add(new OperationParamInfo(name, csharpName, inferredType, ParamLocation.Path, true));
            }
        }
        return list;
    }

    private string GetParameterCSharpDeclaration(OperationParamInfo p)
    {
        var optional = !p.Required && p.Location != ParamLocation.Path;
        var type = optional && p.CSharpType != "string" && !p.CSharpType.EndsWith("?") ? p.CSharpType + "?" : p.CSharpType;
        if (type == "string" && optional) type = "string?";
        var def = optional ? " = null" : "";
        return $"{type} {p.CSharpName}{def}, ";
    }
}

