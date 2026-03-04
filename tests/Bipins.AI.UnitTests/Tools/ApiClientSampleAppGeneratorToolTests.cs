using System.Text.Json;
using Bipins.AI.Agents.Tools.BuiltIn;
using Bipins.AI.Agents.Tools.CodeGen;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Tools;

public class ApiClientSampleAppGeneratorToolTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly ILogger<ApiClientSampleAppGeneratorToolTests> _logger;

    public ApiClientSampleAppGeneratorToolTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ApiClientSampleAppGeneratorToolTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempRoot);

        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddDebug())
            .BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<ApiClientSampleAppGeneratorToolTests>>();
    }

    public void Dispose() => Dispose(true);
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        var tool = CreateTool();
        Assert.Equal("api_client_sample_app_generator", tool.Name);
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var tool = CreateTool();
        var description = tool.Description;
        Assert.NotEmpty(description);
        Assert.Contains("console", description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Program.cs", description);
    }

    [Fact]
    public void ParametersSchema_ContainsRequiredProperties()
    {
        var tool = CreateTool();
        var schema = tool.ParametersSchema;
        Assert.True(schema.TryGetProperty("required", out var required));
        var requiredArray = JsonSerializer.Deserialize<string[]>(required.GetRawText());
        Assert.NotNull(requiredArray);
        Assert.Contains("clientOutputPath", requiredArray);
        Assert.Contains("baseUrl", requiredArray);
        Assert.Contains("namespaceName", requiredArray);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidArgumentsFormat_ReturnsError()
    {
        var tool = CreateTool();
        var toolCall = new ToolCall(
            "call-1",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement("not-an-object"));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("Invalid arguments", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_MissingClientOutputPath_ReturnsError()
    {
        var tool = CreateTool();
        var toolCall = new ToolCall(
            "call-2",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement(new { baseUrl = "https://api.example.com", namespaceName = "Test.Client" }));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.False(result.Success);
        Assert.Contains("clientOutputPath", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_MissingBaseUrl_ReturnsError()
    {
        var tool = CreateTool();
        var clientPath = Path.Combine(_tempRoot, "Client");
        Directory.CreateDirectory(clientPath);

        var toolCall = new ToolCall(
            "call-3",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement(new { clientOutputPath = clientPath, namespaceName = "Test.Client" }));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.False(result.Success);
        Assert.Contains("baseUrl", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_MissingNamespaceName_ReturnsError()
    {
        var tool = CreateTool();
        var clientPath = Path.Combine(_tempRoot, "Client");
        Directory.CreateDirectory(clientPath);

        var toolCall = new ToolCall(
            "call-4",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement(new { clientOutputPath = clientPath, baseUrl = "https://api.example.com" }));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.False(result.Success);
        Assert.Contains("namespaceName", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ClientOutputPathDoesNotExist_ReturnsError()
    {
        var tool = CreateTool();
        var nonExistentPath = Path.Combine(_tempRoot, "DoesNotExist");

        var toolCall = new ToolCall(
            "call-5",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement(new
            {
                clientOutputPath = nonExistentPath,
                baseUrl = "https://api.example.com",
                namespaceName = "Test.Client"
            }));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("does not exist", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLlmReturnsEmpty_ReturnsError()
    {
        var clientPath = CreateClientOutputDirectory("EmptyLlmClient");
        var mockLlm = new Mock<ILLMProvider>();
        mockLlm
            .Setup(m => m.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(Content: "", Usage: null, FinishReason: null, ToolCalls: null, Safety: null));

        var tool = CreateTool(openApiParser: null, llmProvider: mockLlm.Object, fileWriter: new Mock<IFileWriter>().Object);

        var toolCall = new ToolCall(
            "call-6",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement(new
            {
                clientOutputPath = clientPath,
                baseUrl = "https://api.example.com",
                namespaceName = "Test.Client"
            }));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.False(result.Success);
        Assert.Contains("LLM", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_Success_CreatesClientCsprojAndSampleApp()
    {
        var clientDirName = "MyApiClient";
        var clientPath = CreateClientOutputDirectory(clientDirName);

        var writtenFiles = new List<(string OutputPath, GeneratedFile File)>();
        var mockFileWriter = new Mock<IFileWriter>();
        mockFileWriter
            .Setup(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<GeneratedFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string outputPath, GeneratedFile file, CancellationToken _) =>
            {
                writtenFiles.Add((outputPath, file));
                return Path.Combine(outputPath, file.Path);
            });

        var mockLlm = new Mock<ILLMProvider>();
        var expectedProgramContent = "// Generated Program.cs\nusing Microsoft.Extensions.Hosting;\nvar host = Host.CreateDefaultBuilder(args).Build();";
        mockLlm
            .Setup(m => m.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(Content: expectedProgramContent, Usage: null, FinishReason: null, ToolCalls: null, Safety: null));

        var tool = CreateTool(openApiParser: null, llmProvider: mockLlm.Object, fileWriter: mockFileWriter.Object);

        var toolCall = new ToolCall(
            "call-7",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement(new
            {
                clientOutputPath = clientPath,
                baseUrl = "https://api.example.com",
                namespaceName = "Test.ApiClient",
                sampleAppName = "SampleApp"
            }));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.True(result.Success, result.Error ?? "Expected success");
        Assert.NotNull(result.Result);

        // Client .csproj should be created (directory had no .csproj)
        var clientCsprojWrite = writtenFiles.FirstOrDefault(w => w.File.Path.EndsWith(".csproj") && w.OutputPath == clientPath);
        Assert.NotNull(clientCsprojWrite.File.Path);
        Assert.Equal($"{clientDirName}.csproj", clientCsprojWrite.File.Path);
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", clientCsprojWrite.File.Content);

        // Sample app .csproj and Program.cs
        var sampleAppPath = Path.Combine(Path.GetDirectoryName(clientPath)!, "SampleApp");
        var sampleCsprojWrite = writtenFiles.FirstOrDefault(w => w.OutputPath == sampleAppPath && w.File.Path == "SampleApp.csproj");
        Assert.NotNull(sampleCsprojWrite.File.Path);
        Assert.Contains("ProjectReference", sampleCsprojWrite.File.Content);
        Assert.Contains("OutputType>Exe", sampleCsprojWrite.File.Content);

        var programWrite = writtenFiles.FirstOrDefault(w => w.OutputPath == sampleAppPath && w.File.Path == "Program.cs");
        Assert.NotNull(programWrite.File.Path);
        Assert.Equal(expectedProgramContent, programWrite.File.Content);

        // Result object
        var resultJson = JsonSerializer.Serialize(result.Result);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(resultJson);
        Assert.True(resultObj.TryGetProperty("sampleAppPath", out var samplePathProp));
        Assert.True(resultObj.TryGetProperty("instructions", out var instructionsProp));
        Assert.Contains("dotnet run", instructionsProp.GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenClientCsprojAlreadyExists_DoesNotOverwriteClientCsproj()
    {
        var clientDirName = "ExistingClient";
        var clientPath = CreateClientOutputDirectory(clientDirName);
        var existingCsproj = Path.Combine(clientPath, $"{clientDirName}.csproj");
        await File.WriteAllTextAsync(existingCsproj, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var writtenPaths = new List<string>();
        var mockFileWriter = new Mock<IFileWriter>();
        mockFileWriter
            .Setup(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<GeneratedFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string outputPath, GeneratedFile file, CancellationToken _) =>
            {
                var full = Path.Combine(outputPath, file.Path);
                writtenPaths.Add(full);
                return full;
            });

        var mockLlm = new Mock<ILLMProvider>();
        mockLlm
            .Setup(m => m.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(Content: "var x = 1;", Usage: null, FinishReason: null, ToolCalls: null, Safety: null));

        var tool = CreateTool(openApiParser: null, llmProvider: mockLlm.Object, fileWriter: mockFileWriter.Object);

        var toolCall = new ToolCall(
            "call-8",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement(new
            {
                clientOutputPath = clientPath,
                baseUrl = "https://api.example.com",
                namespaceName = "Test.Client",
                sampleAppName = "SampleApp"
            }));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.True(result.Success);
        // Should only write sample .csproj and Program.cs (2 writes), not client .csproj
        Assert.Equal(2, writtenPaths.Count);
        Assert.DoesNotContain(writtenPaths, p => p.EndsWith($"{clientDirName}.csproj") && p.StartsWith(clientPath));
    }

    [Fact]
    public async Task ExecuteAsync_WithSwaggerUrl_CallsOpenApiParserAndIncludesSummaryInContext()
    {
        var clientPath = CreateClientOutputDirectory("SwaggerClient");
        var swaggerUrl = "https://example.com/openapi.json";

        var mockParser = new Mock<IOpenApiParser>();
        var doc = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Description = "A test API" },
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<Microsoft.OpenApi.Models.OperationType, OpenApiOperation>
                    {
                        [Microsoft.OpenApi.Models.OperationType.Get] = new OpenApiOperation { Summary = "List pets", OperationId = "getPets" }
                    }
                }
            }
        };
        mockParser
            .Setup(p => p.ParseAsync(swaggerUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        ChatRequest? capturedRequest = null;
        var mockLlm = new Mock<ILLMProvider>();
        mockLlm
            .Setup(m => m.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ChatRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new ChatResponse(Content: "// Program", Usage: null, FinishReason: null, ToolCalls: null, Safety: null));

        var mockFileWriter = new Mock<IFileWriter>();
        mockFileWriter
            .Setup(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<GeneratedFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string outputPath, GeneratedFile file, CancellationToken _) => Path.Combine(outputPath, file.Path));

        var tool = CreateTool(openApiParser: mockParser.Object, llmProvider: mockLlm.Object, fileWriter: mockFileWriter.Object);

        var toolCall = new ToolCall(
            "call-9",
            "api_client_sample_app_generator",
            JsonSerializer.SerializeToElement(new
            {
                clientOutputPath = clientPath,
                baseUrl = "https://api.example.com",
                namespaceName = "Test.Client",
                swaggerUrl
            }));

        var result = await tool.ExecuteAsync(toolCall);

        Assert.True(result.Success);
        mockParser.Verify(p => p.ParseAsync(swaggerUrl, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(capturedRequest);
        var userMessage = capturedRequest.Messages.FirstOrDefault(m => m.Role == MessageRole.User);
        Assert.NotNull(userMessage);
        Assert.Contains("Test API", userMessage.Content);
        Assert.Contains("List pets", userMessage.Content);
    }

    /// <summary>
    /// Creates a temp client output directory with Clients/*.cs and ServiceCollectionExtensions.cs so BuildApiContextAsync has content.
    /// </summary>
    private string CreateClientOutputDirectory(string folderName)
    {
        var clientPath = Path.Combine(_tempRoot, folderName);
        Directory.CreateDirectory(clientPath);
        var clientsDir = Path.Combine(clientPath, "Clients");
        Directory.CreateDirectory(clientsDir);
        var clientCs = @"namespace Test.ApiClient.Clients;
public interface IPetClient { Task<string> GetPetsAsync(CancellationToken ct = default); }
public class PetClient : IPetClient { public Task<string> GetPetsAsync(CancellationToken ct = default) => Task.FromResult(""[]""); }";
        File.WriteAllText(Path.Combine(clientsDir, "PetClient.cs"), clientCs);
        var extCs = @"namespace Test.ApiClient;
public static class ServiceCollectionExtensions { public static IServiceCollection AddApiClients(this IServiceCollection services, string baseUrl) => services; }";
        File.WriteAllText(Path.Combine(clientPath, "ServiceCollectionExtensions.cs"), extCs);
        return clientPath;
    }

    private static ApiClientSampleAppGeneratorTool CreateTool(
        IOpenApiParser? openApiParser = null,
        ILLMProvider? llmProvider = null,
        IFileWriter? fileWriter = null)
    {
        var mockParser = openApiParser ?? new Mock<IOpenApiParser>().Object;
        var mockLlm = llmProvider ?? new Mock<ILLMProvider>().Object;
        var mockFileWriter = fileWriter ?? new Mock<IFileWriter>().Object;
        var mockLogger = new Mock<ILogger<ApiClientSampleAppGeneratorTool>>().Object;

        return new ApiClientSampleAppGeneratorTool(mockParser, mockLlm, mockFileWriter, mockLogger);
    }
}
