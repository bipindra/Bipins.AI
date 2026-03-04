using System.Text;
using System.Text.Json;
using Bipins.AI.Agents.Tools.CodeGen;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools.BuiltIn;

/// <summary>
/// Agentic tool that creates a runnable console app for a generated API client:
/// adds a client library project, a sample console project, and uses the LLM to generate
/// Program.cs that correctly uses the generated client and demonstrates the API.
/// </summary>
public class ApiClientSampleAppGeneratorTool : IToolExecutor
{
    private readonly IOpenApiParser _openApiParser;
    private readonly ILLMProvider _llmProvider;
    private readonly IFileWriter _fileWriter;
    private readonly ILogger<ApiClientSampleAppGeneratorTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientSampleAppGeneratorTool"/> class.
    /// </summary>
    public ApiClientSampleAppGeneratorTool(
        IOpenApiParser openApiParser,
        ILLMProvider llmProvider,
        IFileWriter fileWriter,
        ILogger<ApiClientSampleAppGeneratorTool> logger)
    {
        _openApiParser = openApiParser;
        _llmProvider = llmProvider;
        _fileWriter = fileWriter;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "api_client_sample_app_generator";

    /// <inheritdoc />
    public string Description =>
        "Creates a runnable console application for an existing generated API client. " +
        "Call this after swagger_client_generator. It adds a client library .csproj (if missing), " +
        "creates a sample console project with correct project reference, and uses the LLM to generate " +
        "Program.cs that correctly instantiates and uses the generated API clients to demonstrate the API. " +
        "Requires clientOutputPath (where the client was generated), baseUrl (API base URL), and namespace. " +
        "Optionally provide swaggerUrl so the LLM can understand API behavior and plan the sample.";

    /// <inheritdoc />
    public JsonElement ParametersSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            clientOutputPath = new
            {
                type = "string",
                description = "File system path where the API client was generated (e.g. by swagger_client_generator)"
            },
            sampleAppName = new
            {
                type = "string",
                description = "Name for the sample console app folder and project (e.g. 'SampleApp'). Default: 'SampleApp'"
            },
            baseUrl = new
            {
                type = "string",
                description = "Base URL of the API (e.g. 'https://api.example.com') used by the generated client"
            },
            namespaceName = new
            {
                type = "string",
                description = "Root namespace of the generated client (e.g. 'MyCompany.ApiClient')"
            },
            swaggerUrl = new
            {
                type = "string",
                description = "Optional. URL to OpenAPI/Swagger spec so the LLM can understand API behavior and plan the sample Program.cs"
            }
        },
        required = new[] { "clientOutputPath", "baseUrl", "namespaceName" }
    });

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (toolCall.Arguments.ValueKind != JsonValueKind.Object)
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: "Invalid arguments format. Expected JSON object.");
            }

            var clientOutputPath = toolCall.Arguments.TryGetProperty("clientOutputPath", out var pathProp)
                ? pathProp.GetString()
                : null;
            var sampleAppName = toolCall.Arguments.TryGetProperty("sampleAppName", out var nameProp)
                ? nameProp.GetString()
                : "SampleApp";
            var baseUrl = toolCall.Arguments.TryGetProperty("baseUrl", out var urlProp)
                ? urlProp.GetString()
                : null;
            var namespaceName = toolCall.Arguments.TryGetProperty("namespaceName", out var nsProp)
                ? nsProp.GetString()
                : null;
            var swaggerUrl = toolCall.Arguments.TryGetProperty("swaggerUrl", out var swaggerProp)
                ? swaggerProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(clientOutputPath))
            {
                return new ToolExecutionResult(Success: false, Error: "Parameter 'clientOutputPath' is required.");
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return new ToolExecutionResult(Success: false, Error: "Parameter 'baseUrl' is required.");
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return new ToolExecutionResult(Success: false, Error: "Parameter 'namespaceName' is required.");
            }

            if (string.IsNullOrWhiteSpace(sampleAppName))
            {
                sampleAppName = "SampleApp";
            }

            clientOutputPath = Path.GetFullPath(clientOutputPath);
            if (!Directory.Exists(clientOutputPath))
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: $"Client output path does not exist: {clientOutputPath}. Run swagger_client_generator first.");
            }

            _logger.LogInformation(
                "Creating sample app for client at {Path}, namespace {Namespace}, baseUrl {BaseUrl}",
                clientOutputPath, namespaceName, baseUrl);

            // 1) Ensure client folder has a library .csproj
            var clientProjectName = new DirectoryInfo(clientOutputPath).Name;
            var clientCsprojPath = Path.Combine(clientOutputPath, $"{clientProjectName}.csproj");
            if (!File.Exists(clientCsprojPath))
            {
                var clientCsproj = BuildClientLibraryCsproj(clientOutputPath, clientProjectName);
                await _fileWriter.WriteAsync(clientOutputPath,
                    new GeneratedFile($"{clientProjectName}.csproj", clientCsproj, "xml"),
                    cancellationToken);
                _logger.LogInformation("Created client library project {Csproj}", clientCsprojPath);
            }

            // 2) Sample app directory (sibling of client output)
            var parentDir = Path.GetDirectoryName(clientOutputPath) ?? clientOutputPath;
            var sampleAppPath = Path.Combine(parentDir, sampleAppName);
            if (!Directory.Exists(sampleAppPath))
            {
                Directory.CreateDirectory(sampleAppPath);
            }

            // 3) Build context for LLM: OpenAPI summary and/or generated client code
            var apiContext = await BuildApiContextAsync(clientOutputPath, namespaceName, swaggerUrl, cancellationToken);

            // 4) Generate Program.cs via LLM
            var programCs = await GenerateProgramCsViaLlmAsync(
                namespaceName,
                baseUrl,
                apiContext,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(programCs))
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: "LLM did not return valid Program.cs content.");
            }

            // 5) Write sample app .csproj and Program.cs
            var sampleCsproj = BuildSampleConsoleCsproj(clientProjectName, clientOutputPath, sampleAppPath, sampleAppName);
            await _fileWriter.WriteAsync(sampleAppPath,
                new GeneratedFile($"{sampleAppName}.csproj", sampleCsproj, "xml"),
                cancellationToken);
            await _fileWriter.WriteAsync(sampleAppPath,
                new GeneratedFile("Program.cs", programCs, "csharp"),
                cancellationToken);

            _logger.LogInformation(
                "Sample app created at {Path} with Program.cs and project reference to client",
                sampleAppPath);

            var result = new
            {
                sampleAppPath,
                clientProjectPath = clientOutputPath,
                clientCsproj = Path.Combine(clientOutputPath, $"{clientProjectName}.csproj"),
                sampleCsproj = Path.Combine(sampleAppPath, $"{sampleAppName}.csproj"),
                programCsPath = Path.Combine(sampleAppPath, "Program.cs"),
                instructions = "Run: dotnet run --project " + Path.Combine(sampleAppPath, $"{sampleAppName}.csproj")
            };

            return new ToolExecutionResult(
                Success: true,
                Result: result,
                Metadata: new Dictionary<string, object>
                {
                    ["clientOutputPath"] = clientOutputPath,
                    ["sampleAppName"] = sampleAppName,
                    ["namespaceName"] = namespaceName,
                    ["baseUrl"] = baseUrl
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API client sample app");
            return new ToolExecutionResult(
                Success: false,
                Error: $"Failed to create sample app: {ex.Message}");
        }
    }

    private async Task<string> BuildApiContextAsync(
        string clientOutputPath,
        string namespaceName,
        string? swaggerUrl,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(swaggerUrl))
        {
            try
            {
                var document = await _openApiParser.ParseAsync(swaggerUrl, cancellationToken);
                sb.AppendLine("## OpenAPI summary (use this to plan which endpoints to call)");
                sb.AppendLine();
                if (document.Info?.Title != null)
                    sb.AppendLine($"Title: {document.Info.Title}");
                if (document.Info?.Description != null)
                    sb.AppendLine($"Description: {document.Info.Description}");
                sb.AppendLine();
                if (document.Paths != null)
                {
                    foreach (var path in document.Paths)
                    {
                        foreach (var op in path.Value.Operations)
                        {
                            sb.AppendLine($"- {op.Key.ToString().ToUpperInvariant()} {path.Key}");
                            if (!string.IsNullOrEmpty(op.Value.Summary))
                                sb.AppendLine($"  Summary: {op.Value.Summary}");
                            if (!string.IsNullOrEmpty(op.Value.OperationId))
                                sb.AppendLine($"  OperationId: {op.Value.OperationId}");
                        }
                    }
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch OpenAPI from {Url}, using client code only", swaggerUrl);
                sb.AppendLine($"(OpenAPI fetch failed: {ex.Message}. Relying on generated client code below.)");
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Generated client code (C#) – use these types and methods in Program.cs");
        sb.AppendLine();

        var clientDir = Path.Combine(clientOutputPath, "Clients");
        if (Directory.Exists(clientDir))
        {
            foreach (var file in Directory.EnumerateFiles(clientDir, "*.cs"))
            {
                var content = await ReadFileContentAsync(file, cancellationToken);
                sb.AppendLine($"### {Path.GetFileName(file)}");
                sb.AppendLine("```csharp");
                sb.AppendLine(content);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        var extPath = Path.Combine(clientOutputPath, "ServiceCollectionExtensions.cs");
        if (File.Exists(extPath))
        {
            var content = await ReadFileContentAsync(extPath, cancellationToken);
            sb.AppendLine("### ServiceCollectionExtensions.cs (use AddApiClients)");
            sb.AppendLine("```csharp");
            sb.AppendLine(content);
            sb.AppendLine("```");
        }

        return sb.ToString();
    }

    private async Task<string> ReadFileContentAsync(string path, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_1
        await Task.Yield();
        return File.ReadAllText(path, Encoding.UTF8);
#else
        return await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
#endif
    }

    private async Task<string> GenerateProgramCsViaLlmAsync(
        string namespaceName,
        string baseUrl,
        string apiContext,
        CancellationToken cancellationToken)
    {
        var systemPrompt =
            "You are a C# expert. Generate a single, complete Program.cs file for a .NET 8 console app. " +
            "Output only valid C# code: no markdown fences, no explanations. " +
            "The app must: (1) use Host.CreateDefaultBuilder(args), (2) in ConfigureServices call " +
            "services.AddApiClients(baseUrl) from the generated client namespace (and add HttpClient/Logging if needed), " +
            "(3) resolve one or more generated API client interfaces from DI, (4) run a small demo that calls " +
            "at least one API method and prints the result. Use the provided API context to choose appropriate " +
            "methods and types. Handle async with async Main or GetAwaiter().GetResult() as appropriate. " +
            "Use the exact namespace and type names from the generated code. Do not invent endpoints or types.";

        var userContent = new StringBuilder();
        userContent.AppendLine("Namespace: " + namespaceName);
        userContent.AppendLine("Base URL: " + baseUrl);
        userContent.AppendLine();
        userContent.AppendLine(apiContext);
        userContent.AppendLine();
        userContent.AppendLine("Generate the complete Program.cs content only (no markdown):");

        var request = new ChatRequest(
            new List<Message>
            {
                new Message(MessageRole.System, systemPrompt),
                new Message(MessageRole.User, userContent.ToString())
            },
            MaxTokens: 4096,
            Temperature: 0.2f);

        var response = await _llmProvider.ChatAsync(request, cancellationToken);
        var content = response.Content?.Trim() ?? string.Empty;

        // Strip markdown code block if present
        if (content.StartsWith("```"))
        {
            var start = content.IndexOf('\n') + 1;
            var end = content.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                content = content.Substring(start, end - start).Trim();
            else
                content = content.Substring(start).Trim();
        }

        return content;
    }

    private static string BuildClientLibraryCsproj(string clientOutputPath, string projectName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net8.0</TargetFramework>");
        sb.AppendLine("    <RootNamespace>").Append(projectName).Append("</RootNamespace>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("    <GenerateDocumentationFile>false</GenerateDocumentationFile>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Extensions.Http\" Version=\"8.0.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Extensions.Logging.Abstractions\" Version=\"8.0.0\" />");
        sb.AppendLine("    <PackageReference Include=\"System.Net.Http.Json\" Version=\"8.0.0\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    private static string BuildSampleConsoleCsproj(string clientProjectName, string clientOutputPath, string sampleAppPath, string sampleAppName)
    {
        var clientCsprojFull = Path.Combine(clientOutputPath, $"{clientProjectName}.csproj");
        var relPath = Path.GetRelativePath(sampleAppPath, clientCsprojFull);
        relPath = relPath.Replace('\\', '/');
        if (!relPath.StartsWith("."))
            relPath = "./" + relPath;

        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <OutputType>Exe</OutputType>");
        sb.AppendLine("    <TargetFramework>net8.0</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine($"    <ProjectReference Include=\"{relPath}\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Extensions.Hosting\" Version=\"8.0.0\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("</Project>");
        return sb.ToString();
    }
}
