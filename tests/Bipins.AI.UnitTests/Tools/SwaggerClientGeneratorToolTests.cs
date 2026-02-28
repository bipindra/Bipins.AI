using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Bipins.AI.Agents.Tools.BuiltIn;
using Bipins.AI.Agents.Tools.CodeGen;
using Bipins.AI.Core.Models;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Bipins.AI.UnitTests.Tools;

public class SwaggerClientGeneratorToolTests
{
    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Arrange
        var tool = CreateTool();

        // Act
        var name = tool.Name;

        // Assert
        Assert.Equal("swagger_client_generator", name);
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        // Arrange
        var tool = CreateTool();

        // Act
        var description = tool.Description;

        // Assert
        Assert.NotEmpty(description);
        Assert.Contains("OpenAPI", description);
    }

    [Fact]
    public void ParametersSchema_ContainsRequiredProperties()
    {
        // Arrange
        var tool = CreateTool();

        // Act
        var schema = tool.ParametersSchema;

        // Assert
        Assert.True(schema.TryGetProperty("required", out var required));
        var requiredArray = JsonSerializer.Deserialize<string[]>(required.GetRawText());
        
        Assert.NotNull(requiredArray);
        Assert.Contains("swaggerUrl", requiredArray);
        Assert.Contains("namespace", requiredArray);
        Assert.Contains("outputPath", requiredArray);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSwaggerUrl_ReturnsError()
    {
        // Arrange
        var tool = CreateTool();
        var toolCall = new ToolCall(
            "test-1",
            "swagger_client_generator",
            JsonSerializer.SerializeToElement(new { @namespace = "Test", outputPath = "C:\\temp" }));

        // Act
        var result = await tool.ExecuteAsync(toolCall);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("swaggerUrl", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_MissingNamespace_ReturnsError()
    {
        // Arrange
        var tool = CreateTool();
        var toolCall = new ToolCall(
            "test-2",
            "swagger_client_generator",
            JsonSerializer.SerializeToElement(new { swaggerUrl = "https://example.com/swagger.json", outputPath = "C:\\temp" }));

        // Act
        var result = await tool.ExecuteAsync(toolCall);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("namespace", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_MissingOutputPath_ReturnsError()
    {
        // Arrange
        var tool = CreateTool();
        var toolCall = new ToolCall(
            "test-3",
            "swagger_client_generator",
            JsonSerializer.SerializeToElement(new { swaggerUrl = "https://example.com/swagger.json", @namespace = "Test" }));

        // Act
        var result = await tool.ExecuteAsync(toolCall);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("outputPath", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_PetstoreSwaggerUrl_GeneratesCode()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddHttpClient();
        
        // Setup real implementations
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        
        var parser = new OpenApiParser(
            httpClientFactory,
            loggerFactory.CreateLogger<OpenApiParser>());

        // Mock generators and file writer to avoid actual file I/O
        var mockModelGen = new Mock<IModelGenerator>();
        var mockClientGen = new Mock<IClientGenerator>();
        var mockAuthGen = new Mock<IAuthGenerator>();
        var mockFileWriter = new Mock<IFileWriter>();

        // Setup mocks to return expected results
        mockModelGen
            .Setup(m => m.GenerateAsync(It.IsAny<OpenApiDocument>(), It.IsAny<string>(), It.IsAny<GeneratorOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeneratedFile>
            {
                new GeneratedFile("Models/Pet.cs", "public class Pet { }"),
                new GeneratedFile("Models/Category.cs", "public class Category { }"),
                new GeneratedFile("Models/Tag.cs", "public class Tag { }")
            });

        mockClientGen
            .Setup(c => c.GenerateAsync(It.IsAny<OpenApiDocument>(), It.IsAny<string>(), It.IsAny<GeneratorOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeneratedFile>
            {
                new GeneratedFile("Clients/IPetClient.cs", "public interface IPetClient { }"),
                new GeneratedFile("Clients/PetClient.cs", "public class PetClient : IPetClient { }")
            });

        mockAuthGen
            .Setup(a => a.GenerateAsync(It.IsAny<OpenApiDocument>(), It.IsAny<string>(), It.IsAny<GeneratorOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeneratedFile>
            {
                new GeneratedFile("Auth/ApiKeyAuthenticationHandler.cs", "public class ApiKeyAuthenticationHandler { }")
            });

        mockFileWriter
            .Setup(f => f.WriteAllAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<GeneratedFile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, IReadOnlyList<GeneratedFile> files, CancellationToken ct) =>
            {
                return files.Select(f => Path.Combine(path, f.Path)).ToList();
            });

        var tool = new SwaggerClientGeneratorTool(
            parser,
            mockModelGen.Object,
            mockClientGen.Object,
            mockAuthGen.Object,
            mockFileWriter.Object,
            loggerFactory.CreateLogger<SwaggerClientGeneratorTool>());

        var toolCall = new ToolCall(
            "test-petstore",
            "swagger_client_generator",
            JsonSerializer.SerializeToElement(new
            {
                swaggerUrl = "https://petstore.swagger.io/v2/swagger.json",
                @namespace = "PetstoreClient",
                outputPath = Path.Combine(Path.GetTempPath(), "PetstoreClient")
            }));

        // Act
        var result = await tool.ExecuteAsync(toolCall, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");
        Assert.Null(result.Error);
        Assert.NotNull(result.Result);

        // Verify the result contains expected data
        var resultJson = JsonSerializer.Serialize(result.Result);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(resultJson);

        Assert.True(resultObj.TryGetProperty("filesGenerated", out var filesGen));
        Assert.True(filesGen.GetInt32() > 0, "Should generate at least one file");

        Assert.True(resultObj.TryGetProperty("files", out var files));
        var fileList = JsonSerializer.Deserialize<List<string>>(files.GetRawText());
        Assert.NotNull(fileList);
        Assert.NotEmpty(fileList);

        Assert.True(resultObj.TryGetProperty("statistics", out var stats));
        Assert.True(stats.TryGetProperty("models", out var models));
        Assert.Equal(3, models.GetInt32());
        
        Assert.True(stats.TryGetProperty("clients", out var clients));
        Assert.Equal(2, clients.GetInt32());

        Assert.True(stats.TryGetProperty("authHandlers", out var authHandlers));
        Assert.Equal(1, authHandlers.GetInt32());

        // Verify parser was called correctly
        mockModelGen.Verify(
            m => m.GenerateAsync(
                It.Is<OpenApiDocument>(d => d.Info.Title == "Swagger Petstore"),
                "PetstoreClient",
                It.IsAny<GeneratorOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockClientGen.Verify(
            c => c.GenerateAsync(
                It.Is<OpenApiDocument>(d => d.Info.Title == "Swagger Petstore"),
                "PetstoreClient",
                It.IsAny<GeneratorOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockFileWriter.Verify(
            f => f.WriteAllAsync(
                It.Is<string>(p => p.Contains("PetstoreClient")),
                It.Is<IReadOnlyList<GeneratedFile>>(files => files.Count == 6),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _logger.LogInformation("? Successfully generated Petstore client with {Count} files", fileList.Count);
    }

    private SwaggerClientGeneratorTool CreateTool()
    {
        var mockParser = new Mock<IOpenApiParser>();
        var mockModelGen = new Mock<IModelGenerator>();
        var mockClientGen = new Mock<IClientGenerator>();
        var mockAuthGen = new Mock<IAuthGenerator>();
        var mockFileWriter = new Mock<IFileWriter>();
        var mockLogger = new Mock<ILogger<SwaggerClientGeneratorTool>>();

        return new SwaggerClientGeneratorTool(
            mockParser.Object,
            mockModelGen.Object,
            mockClientGen.Object,
            mockAuthGen.Object,
            mockFileWriter.Object,
            mockLogger.Object);
    }

    private readonly ILogger<SwaggerClientGeneratorToolTests> _logger;

    public SwaggerClientGeneratorToolTests()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddDebug())
            .BuildServiceProvider();
        
        _logger = serviceProvider.GetRequiredService<ILogger<SwaggerClientGeneratorToolTests>>();
    }
}
