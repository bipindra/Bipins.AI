using Bipins.AI.Validation;
using Bipins.AI.Validation.JsonSchema;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Validation;

public class NJsonSchemaValidatorTests
{
    private readonly Mock<ILogger<NJsonSchemaValidator<TestResponse>>> _mockLogger;

    public NJsonSchemaValidatorTests()
    {
        _mockLogger = new Mock<ILogger<NJsonSchemaValidator<TestResponse>>>();
    }

    [Fact]
    public async Task ValidateAsync_WhenValid_ReturnsValidResult()
    {
        var validator = new NJsonSchemaValidator<TestResponse>(_mockLogger.Object);
        var response = new TestResponse { Name = "Test", Age = 25 };
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""integer"", ""minimum"": 0 }
            },
            ""required"": [""name"", ""age""]
        }";

        var result = await validator.ValidateAsync(response, schema);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WhenInvalid_ReturnsErrors()
    {
        var validator = new NJsonSchemaValidator<TestResponse>(_mockLogger.Object);
        var response = new TestResponse { Name = "Test", Age = -1 };
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""integer"", ""minimum"": 0 }
            },
            ""required"": [""name"", ""age""]
        }";

        var result = await validator.ValidateAsync(response, schema);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WhenNoSchema_ReturnsValid()
    {
        var validator = new NJsonSchemaValidator<TestResponse>(_mockLogger.Object);
        var response = new TestResponse { Name = "Test", Age = 25 };

        var result = await validator.ValidateAsync(response, null);

        Assert.True(result.IsValid);
    }
}

// Test model
public class TestResponse
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}
