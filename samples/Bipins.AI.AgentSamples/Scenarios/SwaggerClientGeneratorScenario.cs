using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Core;
using Bipins.AI.AgentSamples.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.AgentSamples.Scenarios;

/// <summary>
/// Scenario: Demonstrates the agent autonomously using the Swagger Client Generator tool
/// to create a complete C# API client library from an OpenAPI specification.
/// Shows agentic behavior where the AI decides to use the tool based on the user's goal.
/// </summary>
public class SwaggerClientGeneratorScenario : ScenarioBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerClientGeneratorScenario"/> class.
    /// </summary>
    public SwaggerClientGeneratorScenario(
        IAgent agent,
        IOutputFormatter outputFormatter,
        ILogger<SwaggerClientGeneratorScenario> logger)
        : base(agent, outputFormatter, logger)
    {
    }

    /// <inheritdoc />
    public override int Number => 7;

    /// <inheritdoc />
    public override string Name => "Swagger Client Generator";

    /// <inheritdoc />
    public override string Description => "Agent generates API client from Swagger/OpenAPI spec";

    /// <inheritdoc />
    public override bool RequiresVectorStore => false;

    /// <inheritdoc />
    protected override string GetGoal() =>
        "Generate a C# client library for the Petstore API from https://petstore.swagger.io/v2/swagger.json";

    /// <inheritdoc />
    protected override async Task ExecuteScenarioAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        // Create output directory in temp folder
        var outputPath = Path.Combine(Path.GetTempPath(), "BipinsAI", "GeneratedClients", "PetstoreClient");
        
        Console.WriteLine($"?? Output directory: {outputPath}");
        OutputFormatter.WriteSeparator();

        // Let the agent autonomously decide to use the swagger_client_generator tool
        var request = new AgentRequest(
            Goal: $"Generate a complete C# client library for the Petstore API. " +
                  $"Use the swagger specification from https://petstore.swagger.io/v2/swagger.json. " +
                  $"Use namespace 'PetstoreClient' and save it to '{outputPath}'. " +
                  $"Include models, API clients with interfaces, authentication handlers, and DI setup.",
            Context: "The user needs a C# client to integrate with the Petstore API in their .NET application. " +
                     "They want clean code following SOLID principles and .NET 8 best practices.");

        Console.WriteLine("?? Agent is analyzing the request and will use tools as needed...");
        OutputFormatter.WriteSeparator();

        // Execute the agent request (agent will autonomously use the swagger_client_generator tool)
        var response = await Agent.ExecuteAsync(request, cancellationToken);
        stopwatch.Stop();

        // Display results
        OutputFormatter.WriteResponse(response.Content);
        OutputFormatter.WriteExecutionDetails(response, stopwatch.ElapsedMilliseconds);
        OutputFormatter.WriteToolCalls(response.ToolCalls);

        // Show additional details if tool was used
        if (response.ToolCalls?.Any() == true)
        {
            var swaggerToolCall = response.ToolCalls
                .FirstOrDefault(tc => tc.Name == "swagger_client_generator");

            if (swaggerToolCall != null)
            {
                OutputFormatter.WriteSeparator();
                OutputFormatter.WriteSuccess("? Swagger Client Generator Tool was used successfully!");
                
                // Try to list generated files if they exist
                if (Directory.Exists(outputPath))
                {
                    Console.WriteLine("?? Generated files:");
                    var files = Directory.GetFiles(outputPath, "*.cs", SearchOption.AllDirectories);
                    
                    foreach (var file in files.OrderBy(f => f))
                    {
                        var relativePath = Path.GetRelativePath(outputPath, file);
                        Console.WriteLine($"   • {relativePath}");
                    }

                    Console.WriteLine($"\n?? Total files generated: {files.Length}");
                    Console.WriteLine($"?? Location: {outputPath}");
                }
            }
        }
        else
        {
            OutputFormatter.WriteWarning("?? Agent did not use the swagger_client_generator tool");
            Console.WriteLine("   The tool may not be registered or the agent chose a different approach");
        }

        OutputFormatter.WriteSeparator();
    }
}
