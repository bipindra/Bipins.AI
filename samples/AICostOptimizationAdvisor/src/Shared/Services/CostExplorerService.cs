using Amazon.CostExplorer;
using Amazon.CostExplorer.Model;
using AICostOptimizationAdvisor.Shared.Models;

namespace AICostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service for interacting with AWS Cost Explorer API.
/// Note: Cost Explorer is not an AI/vector service, so it uses AWS SDK directly.
/// Bipins.AI only provides abstractions for AI/LLM and vector database services.
/// </summary>
public class CostExplorerService : ICostExplorerService
{
    private readonly IAmazonCostExplorer _costExplorerClient;

    public CostExplorerService(IAmazonCostExplorer costExplorerClient)
    {
        _costExplorerClient = costExplorerClient;
    }

    public async Task<GetCostDataResponse> GetCostAndUsageAsync(GetCostDataRequest request, CancellationToken cancellationToken = default)
    {
        var getCostAndUsageRequest = new GetCostAndUsageRequest
        {
            TimePeriod = new DateInterval
            {
                Start = request.StartDate,
                End = request.EndDate
            },
            Granularity = request.Granularity,
            Metrics = new List<string> { "BlendedCost", "UnblendedCost", "UsageQuantity" },
            GroupBy = new List<GroupDefinition>
            {
                new GroupDefinition
                {
                    Type = GroupDefinitionType.DIMENSION,
                    Key = "SERVICE"
                },
                new GroupDefinition
                {
                    Type = GroupDefinitionType.DIMENSION,
                    Key = "REGION"
                }
            }
        };

        // Add filters if provided
        if (request.Services != null && request.Services.Count > 0)
        {
            getCostAndUsageRequest.Filter = new Expression
            {
                Dimensions = new DimensionValues
                {
                    Key = Dimension.SERVICE,
                    Values = request.Services
                }
            };
        }

        var response = await _costExplorerClient.GetCostAndUsageAsync(getCostAndUsageRequest, cancellationToken);

        var costDataList = new List<CostData>();
        decimal totalCost = 0;

        foreach (var result in response.ResultsByTime)
        {
            foreach (var group in result.Groups)
            {
                var amount = decimal.Parse(group.Metrics["BlendedCost"].Amount);
                totalCost += amount;

                var costData = new CostData
                {
                    Date = result.TimePeriod.Start,
                    Service = group.Keys[0] ?? "Unknown",
                    Region = group.Keys.Count > 1 ? group.Keys[1] ?? "Unknown" : "Unknown",
                    Amount = amount,
                    Currency = group.Metrics["BlendedCost"].Unit ?? "USD",
                    UsageType = group.Keys.Count > 2 ? group.Keys[2] : string.Empty,
                    UsageQuantity = group.Metrics.ContainsKey("UsageQuantity") ? group.Metrics["UsageQuantity"].Amount : "0"
                };

                costDataList.Add(costData);
            }
        }

        return new GetCostDataResponse
        {
            Costs = costDataList,
            TotalCost = totalCost,
            Currency = response.ResultsByTime.FirstOrDefault()?.Groups.FirstOrDefault()?.Metrics["BlendedCost"].Unit ?? "USD",
            DateRange = $"{request.StartDate} to {request.EndDate}"
        };
    }
}
