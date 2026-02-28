using System.Text.Json;
using Bipins.AI.Agents.Tools.CodeGen;
using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools.BuiltIn;

/// <summary>
/// Tool for generating C# client libraries from Swagger/OpenAPI specifications.
/// Generates clean models, async API clients, authentication handlers, and DI setup
/// following SOLID principles and .NET 8 best practices.
/// </summary>
public class SwaggerClientGeneratorTool : IToolExecutor
{
    private readonly IOpenApiParser _openApiParser;
    private readonly IModelGenerator _modelGenerator;
    private readonly IClientGenerator _clientGenerator;
    private readonly IAuthGenerator _authGenerator;
    private readonly IFileWriter _fileWriter;
    private readonly ILogger<SwaggerClientGeneratorTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerClientGeneratorTool"/> class.
    /// </summary>
    public SwaggerClientGeneratorTool(
        IOpenApiParser openApiParser,
        IModelGenerator modelGenerator,
        IClientGenerator clientGenerator,
        IAuthGenerator authGenerator,
        IFileWriter fileWriter,
        ILogger<SwaggerClientGeneratorTool> logger)
    {
        _openApiParser = openApiParser;
        _modelGenerator = modelGenerator;
        _clientGenerator = clientGenerator;
        _authGenerator = authGenerator;
        _fileWriter = fileWriter;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "swagger_client_generator";

    /// <inheritdoc />
    public string Description =>
        "Generates a complete C# client library from a Swagger/OpenAPI specification. " +
        "Creates clean models, async API clients, authentication handlers, and DI setup " +
        "following SOLID principles and .NET 8 best practices. Supports OpenAPI 2.0 and 3.0 specifications.";

    /// <inheritdoc />
    public JsonElement ParametersSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            swaggerUrl = new
            {
                type = "string",
                description = "URL to OpenAPI/Swagger JSON or YAML specification (e.g., https://api.example.com/swagger.json)"
            },
            @namespace = new
            {
                type = "string",
                description = "Root namespace for generated code (e.g., 'MyCompany.ApiClient'). Will create Models and Clients subnamespaces."
            },
            outputPath = new
            {
                type = "string",
                description = "File system path where client library will be generated (e.g., 'C:\\Projects\\MyApp\\ApiClient')"
            },
            options = new
            {
                type = "object",
                description = "Optional code generation settings",
                properties = new
                {
                    generateModels = new
                    {
                        type = "boolean",
                        description = "Generate model/DTO classes (default: true)",
                        @default = true
                    },
                    generateClients = new
                    {
                        type = "boolean",
                        description = "Generate API client classes (default: true)",
                        @default = true
                    },
                    includeXmlDocs = new
                    {
                        type = "boolean",
                        description = "Include XML documentation comments (default: true)",
                        @default = true
                    },
                    useNullableReferenceTypes = new
                    {
                        type = "boolean",
                        description = "Use nullable reference types (default: true)",
                        @default = true
                    },
                    asyncSuffix = new
                    {
                        type = "boolean",
                        description = "Add 'Async' suffix to async methods (default: true)",
                        @default = true
                    },
                    generateInterfaces = new
                    {
                        type = "boolean",
                        description = "Generate interfaces for clients (default: true)",
                        @default = true
                    },
                    generateAuthentication = new
                    {
                        type = "boolean",
                        description = "Generate authentication handlers (default: true)",
                        @default = true
                    },
                    includeResiliencePolicies = new
                    {
                        type = "boolean",
                        description = "Include retry and circuit breaker policies (default: true)",
                        @default = true
                    }
                }
            }
        },
        required = new[] { "swaggerUrl", "namespace", "outputPath" }
    });

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate arguments
            if (toolCall.Arguments.ValueKind != JsonValueKind.Object)
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: "Invalid arguments format. Expected JSON object.");
            }

            // Extract required parameters
            var swaggerUrl = toolCall.Arguments.TryGetProperty("swaggerUrl", out var urlProp)
                ? urlProp.GetString()
                : null;

            var namespaceName = toolCall.Arguments.TryGetProperty("namespace", out var nsProp)
                ? nsProp.GetString()
                : null;

            var outputPath = toolCall.Arguments.TryGetProperty("outputPath", out var pathProp)
                ? pathProp.GetString()
                : null;

            // Validate required parameters
            if (string.IsNullOrWhiteSpace(swaggerUrl))
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: "Parameter 'swaggerUrl' is required.");
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: "Parameter 'namespace' is required.");
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: "Parameter 'outputPath' is required.");
            }

            // Extract options
            var options = ExtractOptions(toolCall.Arguments);

            _logger.LogInformation(
                "Generating C# client library from {Url} to namespace {Namespace} at {Path}",
                swaggerUrl, namespaceName, outputPath);

            // Parse OpenAPI document
            var document = await _openApiParser.ParseAsync(swaggerUrl, cancellationToken);
            var pathCount = document.Paths?.Count ?? 0;
            var schemaCount = document.Components?.Schemas?.Count ?? 0;

            _logger.LogInformation(
                "Parsed OpenAPI specification with {PathCount} paths and {SchemaCount} schemas",
                pathCount, schemaCount);

            // Generate all files
            var generatedFiles = new List<GeneratedFile>();

            // Generate models
            if (options.GenerateModels)
            {
                var modelFiles = await _modelGenerator.GenerateAsync(document, namespaceName, options, cancellationToken);
                generatedFiles.AddRange(modelFiles);
                _logger.LogInformation("Generated {Count} model files", modelFiles.Count);
            }

            // Generate clients
            if (options.GenerateClients)
            {
                var clientFiles = await _clientGenerator.GenerateAsync(document, namespaceName, options, cancellationToken);
                generatedFiles.AddRange(clientFiles);
                _logger.LogInformation("Generated {Count} client files", clientFiles.Count);
            }

            // Generate authentication handlers
            if (options.GenerateAuthentication && document.Components?.SecuritySchemes?.Count > 0)
            {
                var authFiles = await _authGenerator.GenerateAsync(document, namespaceName, options, cancellationToken);
                generatedFiles.AddRange(authFiles);
                _logger.LogInformation("Generated {Count} auth handler files", authFiles.Count);
            }

            // Write all files to disk
            var writtenPaths = await _fileWriter.WriteAllAsync(outputPath, generatedFiles, cancellationToken);

            _logger.LogInformation(
                "Successfully generated {Count} files to {Path}",
                writtenPaths.Count, outputPath);

            // Return success result
            var result = new
            {
                filesGenerated = writtenPaths.Count,
                outputPath,
                files = writtenPaths.Select(p => Path.GetRelativePath(outputPath, p)).ToList(),
                statistics = new
                {
                    models = generatedFiles.Count(f => f.Path.StartsWith("Models/")),
                    clients = generatedFiles.Count(f => f.Path.StartsWith("Clients/")),
                    authHandlers = generatedFiles.Count(f => f.Path.StartsWith("Auth/")),
                    totalPaths = pathCount,
                    totalSchemas = schemaCount
                }
            };

            _logger.LogInformation(
                "Client library generation complete for namespace {Namespace}",
                namespaceName);

            return new ToolExecutionResult(
                Success: true,
                Result: result,
                Metadata: new Dictionary<string, object>
                {
                    ["swaggerUrl"] = swaggerUrl,
                    ["namespace"] = namespaceName,
                    ["outputPath"] = outputPath
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client library from Swagger specification");
            return new ToolExecutionResult(
                Success: false,
                Error: $"Failed to generate client library: {ex.Message}");
        }
    }

    private GeneratorOptions ExtractOptions(JsonElement args)
    {
        var options = new GeneratorOptions();

        if (args.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Object)
        {
            if (optionsElement.TryGetProperty("generateModels", out var gm))
                options.GenerateModels = gm.GetBoolean();

            if (optionsElement.TryGetProperty("generateClients", out var gc))
                options.GenerateClients = gc.GetBoolean();

            if (optionsElement.TryGetProperty("includeXmlDocs", out var xml))
                options.IncludeXmlDocs = xml.GetBoolean();

            if (optionsElement.TryGetProperty("useNullableReferenceTypes", out var nullable))
                options.UseNullableReferenceTypes = nullable.GetBoolean();

            if (optionsElement.TryGetProperty("asyncSuffix", out var asyncSuffix))
                options.AsyncSuffix = asyncSuffix.GetBoolean();

            if (optionsElement.TryGetProperty("generateInterfaces", out var interfaces))
                options.GenerateInterfaces = interfaces.GetBoolean();

            if (optionsElement.TryGetProperty("generateAuthentication", out var auth))
                options.GenerateAuthentication = auth.GetBoolean();

            if (optionsElement.TryGetProperty("includeResiliencePolicies", out var resilience))
                options.IncludeResiliencePolicies = resilience.GetBoolean();
        }

        return options;
    }
}
