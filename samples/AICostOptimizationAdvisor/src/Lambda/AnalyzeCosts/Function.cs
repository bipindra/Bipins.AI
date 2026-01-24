using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.BedrockRuntime;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Text.Json;
using AICostOptimizationAdvisor.Shared.Models;
using AICostOptimizationAdvisor.Shared.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace AnalyzeCosts;

public class Function
{
    private readonly IBedrockAnalysisService _bedrockAnalysisService;
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;

    public Function()
    {
        var bedrockClient = new AmazonBedrockRuntimeClient();
        var modelId = Environment.GetEnvironmentVariable("BEDROCK_MODEL_ID") ?? "anthropic.claude-3-sonnet-20240229-v1:0";
        _bedrockAnalysisService = new BedrockAnalysisService(bedrockClient, modelId);
        _dynamoDbClient = new AmazonDynamoDBClient();
        _tableName = Environment.GetEnvironmentVariable("COST_ANALYSES_TABLE") ?? "CostAnalyses";
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation("AnalyzeCosts function invoked");

            // Parse request body
            var analyzeRequest = JsonSerializer.Deserialize<AnalyzeCostsRequest>(request.Body ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (analyzeRequest == null || analyzeRequest.CostData == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } },
                    Body = JsonSerializer.Serialize(new { error = "Invalid request: costData is required" })
                };
            }

            // Analyze using Bedrock
            var analysis = await _bedrockAnalysisService.AnalyzeCostsAsync(analyzeRequest.CostData, analyzeRequest.ModelId);

            // Store in DynamoDB
            await SaveAnalysisAsync(analysis);

            var response = new AnalyzeCostsResponse
            {
                Analysis = analysis,
                Success = true
            };

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } },
                Body = JsonSerializer.Serialize(response)
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error in AnalyzeCosts: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } },
                Body = JsonSerializer.Serialize(new AnalyzeCostsResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                })
            };
        }
    }

    private async Task SaveAnalysisAsync(CostAnalysis analysis)
    {
        try
        {
            var timestamp = (long)(analysis.CreatedAt - new DateTime(1970, 1, 1)).TotalSeconds;

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "analysisId", new AttributeValue { S = analysis.AnalysisId } },
                    { "timestamp", new AttributeValue { N = timestamp.ToString() } },
                    { "costData", new AttributeValue { S = JsonSerializer.Serialize(analysis) } },
                    { "dateRange", new AttributeValue { S = analysis.DateRange } },
                    { "createdAt", new AttributeValue { S = analysis.CreatedAt.ToString("O") } }
                }
            });
        }
        catch (Exception ex)
        {
            // Log but don't fail - analysis can still be returned
            Console.WriteLine($"Error saving analysis to DynamoDB: {ex.Message}");
        }
    }
}
