using Amazon.Lambda.APIGatewayEvents;
using AICostOptimizationAdvisor.Shared.Models;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Samples;

/// <summary>
/// Tests for Lambda function request/response handling logic.
/// Note: These tests focus on the data structures and parsing logic
/// rather than the full Lambda function execution, which would require
/// dependency injection refactoring of the Lambda classes.
/// </summary>
public class LambdaFunctionTests
{
    [Fact]
    public void GetCostDataFunction_WithValidRequest_ParsesParameters()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "startDate", "2024-01-01" },
                { "endDate", "2024-01-31" },
                { "granularity", "DAILY" }
            }
        };

        // Act
        var startDate = request.QueryStringParameters.TryGetValue("startDate", out var startDateValue) ? startDateValue : null;
        var endDate = request.QueryStringParameters.TryGetValue("endDate", out var endDateValue) ? endDateValue : null;
        var granularity = request.QueryStringParameters.TryGetValue("granularity", out var granularityValue) ? granularityValue : "DAILY";

        // Assert
        Assert.NotNull(startDate);
        Assert.NotNull(endDate);
        Assert.Equal("2024-01-01", startDate);
        Assert.Equal("2024-01-31", endDate);
        Assert.Equal("DAILY", granularity);
    }

    [Fact]
    public void GetCostDataFunction_WithMissingParameters_UsesDefaults()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            QueryStringParameters = null
        };

        // Act
        var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();
        var startDate = queryParams.TryGetValue("startDate", out var startDateValue) ? startDateValue : DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = queryParams.TryGetValue("endDate", out var endDateValue) ? endDateValue : DateTime.UtcNow.ToString("yyyy-MM-dd");
        var granularity = queryParams.TryGetValue("granularity", out var granularityValue) ? granularityValue : "DAILY";

        // Assert
        Assert.NotNull(startDate);
        Assert.NotNull(endDate);
        Assert.Equal("DAILY", granularity);
    }

    [Fact]
    public void GetAnalysisHistoryFunction_WithValidRequest_ParsesLimit()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "limit", "5" }
            }
        };

        // Act
        var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();
        var limitStr = queryParams.TryGetValue("limit", out var limitStrValue) ? limitStrValue : "10";
        var limit = int.TryParse(limitStr, out var limitValue) ? limitValue : 10;

        // Assert
        Assert.Equal(5, limit);
    }

    [Fact]
    public void GetAnalysisHistoryFunction_WithInvalidLimit_UsesDefault()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "limit", "invalid" }
            }
        };

        // Act
        var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();
        var limitStr = queryParams.TryGetValue("limit", out var limitStrValue) ? limitStrValue : "10";
        var limit = int.TryParse(limitStr, out var limitValue) ? limitValue : 10;

        // Assert
        Assert.Equal(10, limit); // Should default to 10 when parsing fails
    }

    [Fact]
    public void AnalyzeCostsFunction_WithValidRequest_DeserializesCorrectly()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData> { new CostData { Service = "EC2", Amount = 100.00m } },
            TotalCost = 100.00m
        };

        var analyzeRequest = new AnalyzeCostsRequest
        {
            CostData = costData,
            ModelId = "anthropic.claude-3-sonnet-20240229-v1:0"
        };

        var request = new APIGatewayProxyRequest
        {
            Body = JsonSerializer.Serialize(analyzeRequest)
        };

        // Act
        var deserialized = JsonSerializer.Deserialize<AnalyzeCostsRequest>(request.Body ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.CostData);
        Assert.Equal(100.00m, deserialized.CostData.TotalCost);
        Assert.Equal("anthropic.claude-3-sonnet-20240229-v1:0", deserialized.ModelId);
    }

    [Fact]
    public void AnalyzeCostsFunction_WithMissingCostData_HandlesNull()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            Body = JsonSerializer.Serialize(new { })
        };

        // Act
        var deserialized = JsonSerializer.Deserialize<AnalyzeCostsRequest>(request.Body ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(deserialized);
        // CostData would be null or empty, which should trigger a 400 response in the actual function
        Assert.NotNull(deserialized.CostData); // Default constructor creates empty object
    }

    [Fact]
    public void LambdaFunction_ErrorResponse_FormatsCorrectly()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var errorResponse = new { error = errorMessage };
        var json = JsonSerializer.Serialize(errorResponse);

        // Assert
        Assert.NotNull(json);
        Assert.Contains(errorMessage, json);
    }

    [Fact]
    public void LambdaFunction_SuccessResponse_FormatsCorrectly()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData> { new CostData { Service = "EC2", Amount = 100.00m } },
            TotalCost = 100.00m
        };

        // Act
        var json = JsonSerializer.Serialize(costData);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("EC2", json);
        Assert.Contains("100", json);
    }
}
