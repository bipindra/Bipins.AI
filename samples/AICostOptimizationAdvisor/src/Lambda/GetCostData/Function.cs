using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.CostExplorer;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Text.Json;
using AICostOptimizationAdvisor.Shared.Models;
using AICostOptimizationAdvisor.Shared.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace GetCostData;

public class Function
{
    private readonly ICostExplorerService _costExplorerService;
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;

    public Function()
    {
        var costExplorerClient = new AmazonCostExplorerClient();
        _costExplorerService = new CostExplorerService(costExplorerClient);
        _dynamoDbClient = new AmazonDynamoDBClient();
        _tableName = Environment.GetEnvironmentVariable("COST_DATA_CACHE_TABLE") ?? "CostDataCache";
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation("GetCostData function invoked");

            // Parse request
            var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();
            var startDate = queryParams.TryGetValue("startDate", out var startDateValue) ? startDateValue : DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            var endDate = queryParams.TryGetValue("endDate", out var endDateValue) ? endDateValue : DateTime.UtcNow.ToString("yyyy-MM-dd");
            var granularity = queryParams.TryGetValue("granularity", out var granularityValue) ? granularityValue : "DAILY";

            // Check cache first
            var cacheKey = $"{startDate}_{endDate}_{granularity}";
            var cachedData = await GetFromCacheAsync(cacheKey);

            if (cachedData != null)
            {
                context.Logger.LogInformation("Returning cached cost data");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } },
                    Body = JsonSerializer.Serialize(cachedData)
                };
            }

            // Fetch from Cost Explorer
            var costRequest = new GetCostDataRequest
            {
                StartDate = startDate,
                EndDate = endDate,
                Granularity = granularity
            };

            var costData = await _costExplorerService.GetCostAndUsageAsync(costRequest);

            // Cache the result
            await SaveToCacheAsync(cacheKey, costData);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } },
                Body = JsonSerializer.Serialize(costData)
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error in GetCostData: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } },
                Body = JsonSerializer.Serialize(new { error = ex.Message })
            };
        }
    }

    private async Task<GetCostDataResponse?> GetFromCacheAsync(string cacheKey)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "date", new AttributeValue { S = cacheKey } },
                    { "service", new AttributeValue { S = "ALL" } }
                }
            });

            if (response.Item == null || !response.Item.ContainsKey("costData"))
            {
                return null;
            }

            var costDataJson = response.Item["costData"].S;
            return JsonSerializer.Deserialize<GetCostDataResponse>(costDataJson);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveToCacheAsync(string cacheKey, GetCostDataResponse costData)
    {
        try
        {
            var ttl = (long)(DateTime.UtcNow.AddHours(24) - new DateTime(1970, 1, 1)).TotalSeconds;

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "date", new AttributeValue { S = cacheKey } },
                    { "service", new AttributeValue { S = "ALL" } },
                    { "costData", new AttributeValue { S = JsonSerializer.Serialize(costData) } },
                    { "ttl", new AttributeValue { N = ttl.ToString() } }
                }
            });
        }
        catch
        {
            // Fail silently - caching is not critical
        }
    }
}
