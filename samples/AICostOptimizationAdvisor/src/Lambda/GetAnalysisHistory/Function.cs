using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Text.Json;
using AICostOptimizationAdvisor.Shared.Models;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace GetAnalysisHistory;

public class Function
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;

    public Function()
    {
        _dynamoDbClient = new AmazonDynamoDBClient();
        _tableName = Environment.GetEnvironmentVariable("COST_ANALYSES_TABLE") ?? "CostAnalyses";
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation("GetAnalysisHistory function invoked");

            // Parse query parameters
            var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();
            var limitStr = queryParams.TryGetValue("limit", out var limitStrValue) ? limitStrValue : "10";
            var limit = int.TryParse(limitStr, out var limitValue) ? limitValue : 10;

            // Scan DynamoDB table (in production, use GSI for better performance)
            var scanRequest = new ScanRequest
            {
                TableName = _tableName,
                Limit = limit
            };

            var response = await _dynamoDbClient.ScanAsync(scanRequest);

            var analyses = new List<CostAnalysis>();

            foreach (var item in response.Items)
            {
                if (item.ContainsKey("costData"))
                {
                    var costDataJson = item["costData"].S;
                    var analysis = JsonSerializer.Deserialize<CostAnalysis>(costDataJson);
                    if (analysis != null)
                    {
                        analyses.Add(analysis);
                    }
                }
            }

            // Sort by created date (newest first)
            analyses = analyses.OrderByDescending(a => a.CreatedAt).Take(limit).ToList();

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } },
                Body = JsonSerializer.Serialize(new { analyses })
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error in GetAnalysisHistory: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } },
                Body = JsonSerializer.Serialize(new { error = ex.Message })
            };
        }
    }
}
