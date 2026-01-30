using Bipins.AI;
using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Core;
using Bipins.AI.AgentSamples.Scenarios;
using Bipins.AI.AgentSamples.Services;
using Bipins.AI.AgentSamples.Tools;
using Bipins.AI.Core.Configuration;
using Bipins.AI.Providers.OpenAI;
using Bipins.AI.Vectors.Qdrant;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Configure services
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            config.AddUserSecrets<Program>();
        }
    })
    .ConfigureServices((context, services) =>
    {
        // Register core services
        services.AddSingleton<IOutputFormatter, ConsoleOutputFormatter>();
        services.AddSingleton<IMenuService, MenuService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Add Bipins.AI services
        services.AddDistributedMemoryCache();
        services.AddBipinsAIRuntime(context.Configuration);
        
        services
            .AddBipinsAI()
            .AddOpenAI(o =>
            {
                o.ApiKey = context.Configuration.GetRequiredValueOrEnvironmentVariable("OpenAI:ApiKey", "OPENAI_API_KEY");
                o.BaseUrl = context.Configuration.GetValueOrEnvironmentVariable("OpenAI:BaseUrl", "OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
                o.DefaultChatModelId = context.Configuration.GetValueOrEnvironmentVariable("OpenAI:DefaultChatModelId", "OPENAI_DEFAULT_CHAT_MODEL_ID") ?? "gpt-4o-mini";
                o.DefaultEmbeddingModelId = context.Configuration.GetValueOrEnvironmentVariable("OpenAI:DefaultEmbeddingModelId", "OPENAI_DEFAULT_EMBEDDING_MODEL_ID") ?? "text-embedding-3-small";
            })
            .AddBipinsAIAgents()
            .AddCalculatorTool()
            .AddTool(new WeatherTool())
            .AddAgent("assistant", options =>
            {
                options.Name = "AI Assistant";
                options.SystemPrompt = "You are a helpful AI assistant that can use tools to help users. When you need to perform calculations, use the calculator tool. When you need weather information, use the weather tool.";
                options.EnablePlanning = true;
                options.EnableMemory = true;
                options.MaxIterations = 10;
                options.Temperature = 0.7f;
            });

        // Optionally add Qdrant for vector search tool
        // Only endpoint is required from environment variable, everything else uses defaults
        var qdrantEndpoint = Environment.GetEnvironmentVariable("QDRANT_ENDPOINT") 
                           ?? context.Configuration.GetValue<string>("Qdrant:Endpoint");
        if (!string.IsNullOrEmpty(qdrantEndpoint))
        {
            services
                .AddBipinsAI()
                .AddQdrant(o =>
                {
                    o.Endpoint = qdrantEndpoint;
                    // No API key needed for this endpoint
                    o.DefaultCollectionName = "documents";
                    o.VectorSize = 1536;
                    o.CreateCollectionIfMissing = true;
                })
                .AddVectorSearchTool("documents");
        }

        // Register scenarios (follows Dependency Inversion Principle - depends on abstractions)
        services.AddTransient<IScenario, BasicAgentScenario>();
        services.AddTransient<IScenario, MultipleToolsScenario>();
        services.AddTransient<IScenario, MemoryScenario>();
        services.AddTransient<IScenario, PlanningScenario>();
        services.AddTransient<IScenario, StreamingScenario>();
        services.AddTransient<IScenario, VectorSearchScenario>();

        // Note: ScenarioRunner will be created after scenarios are filtered
        // We'll create it manually in Program.cs after filtering
    })
    .Build();

// Get required services
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var agentRegistry = host.Services.GetRequiredService<IAgentRegistry>();
var outputFormatter = host.Services.GetRequiredService<IOutputFormatter>();
var configurationService = host.Services.GetRequiredService<IConfigurationService>();
var menuService = host.Services.GetRequiredService<IMenuService>();

// Validate configuration
if (!configurationService.ValidateConfiguration(out var errorMessage))
{
    logger.LogError("❌ {Error}", errorMessage ?? "Configuration validation failed");
    outputFormatter.WriteError(errorMessage ?? "Configuration validation failed");
    Environment.Exit(1);
}

// Get agent
var agent = agentRegistry.GetAgent("assistant");
if (agent == null)
{
    logger.LogError("❌ Agent 'assistant' not found. Check service registration.");
    outputFormatter.WriteError("Agent 'assistant' not found. Check service registration.");
    Environment.Exit(1);
}

// Display welcome
outputFormatter.WriteWelcomeBanner();
outputFormatter.WriteAgentInfo(agent);

try
{
    // Get all scenarios and filter based on configuration
    var allScenarios = host.Services.GetServices<IScenario>().ToList();
    var scenarios = allScenarios
        .Where(s => !s.RequiresVectorStore || configurationService.IsVectorStoreConfigured())
        .ToList();

    // Create scenario runner with filtered scenarios
    var scenarioRunner = new ScenarioRunner(scenarios, outputFormatter);

    // Run scenarios
    if (args.Length > 0 && args[0] == "--all")
    {
        await scenarioRunner.RunAllAsync();
    }
    else
    {
        await menuService.ShowMenuAsync(scenarios);
    }

    outputFormatter.WriteSuccess("All scenarios completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Error running scenarios");
    outputFormatter.WriteError($"Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        outputFormatter.WriteError($"   Inner: {ex.InnerException.Message}");
    }
    Environment.Exit(1);
}
