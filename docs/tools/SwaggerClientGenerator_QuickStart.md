# Quick Start: Swagger Client Generator Demo

## ?? Run the Agentic Code Generation Demo

### 1. Configure OpenAI

Choose one method:

#### Option A: User Secrets (Recommended)
```bash
cd samples/Bipins.AI.AgentSamples
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-api-key-here"
```

#### Option B: Environment Variable
```powershell
# PowerShell
$env:OPENAI_API_KEY = "sk-your-api-key-here"
```

```bash
# Bash
export OPENAI_API_KEY="sk-your-api-key-here"
```

### 2. Run the Sample

```bash
cd samples/Bipins.AI.AgentSamples
dotnet run
```

### 3. Select Scenario 7

You'll see an interactive menu:

```
??????????????????????????????????????????????????????????????????????
?                    BIPINS.AI AGENT SAMPLES                         ?
??????????????????????????????????????????????????????????????????????

?? AGENT: AI Assistant

?? AVAILABLE SCENARIOS:

1. Basic Agent Execution
2. Agent with Multiple Tools
3. Agent with Memory
4. Agent with Planning
5. Streaming Agent Execution
6. Agent with Vector Search
7. Swagger Client Generator                    ? SELECT THIS

A. Run All Scenarios
Q. Quit

Enter your choice:
```

**Type `7` and press Enter**

### 4. Watch the Magic! ?

The agent will:
1. ?? Analyze your goal
2. ?? Recognize it needs to generate API client code
3. ??? Autonomously select the `swagger_client_generator` tool
4. ?? Fetch the Petstore Swagger specification
5. ?? Generate complete C# client library
6. ?? Write files to temp directory
7. ?? Display results and statistics

### 5. Explore Generated Code

Check the output location shown in the results:

```
?? Location: C:\Users\YourName\AppData\Local\Temp\BipinsAI\GeneratedClients\PetstoreClient
```

You'll find:
- `Models/` - Pet, Category, Tag, Order, User, ApiResponse
- `Clients/` - IPetClient, PetClient, IStoreClient, StoreClient, IUserClient, UserClient
- `Auth/` - ApiKeyAuthenticationHandler
- `ServiceCollectionExtensions.cs` - DI setup

## ?? What to Look For

### Agent Intelligence

Watch how the agent:
- **Understands Intent** - "Generate a client" ? uses swagger_client_generator tool
- **Extracts Parameters** - Pulls out URL, namespace, and path from natural language
- **Handles Errors** - Retries if initial attempt fails
- **Reports Progress** - Shows iterations and tool calls

### Generated Code Quality

Review the generated files for:
- **Clean Models** - No generated noise, just clean POCOs
- **Async Methods** - All async with CancellationToken support
- **Interfaces** - SOLID principle: depend on abstractions
- **Documentation** - XML docs for IntelliSense
- **Modern C#** - Nullable reference types, record types where appropriate
- **DI Ready** - Extension methods for `IServiceCollection`

## ?? Next Steps

### Try Other APIs

Modify the scenario to generate clients for:

1. **GitHub API** - Complex, real-world API
2. **Stripe API** - Payment processing
3. **Twilio API** - Communication services
4. **Your Own API** - Point to your company's Swagger spec

### Extend the Demo

Add more agentic behaviors:
- Generate ? Compile ? Test (multi-step)
- Generate ? Review ? Refine (iterative improvement)
- Generate multiple clients ? Compare ? Recommend best practices
- Generate ? Package as NuGet ? Publish (full automation)

### Use in Real Projects

1. Generate a client for a real API you use
2. Add it to your project
3. Register services in DI container
4. Use the generated client in your code

## ?? Pro Tips

### Customize the Output Path

Edit `SwaggerClientGeneratorScenario.cs`:

```csharp
var outputPath = @"C:\Projects\MyApp\ApiClients\PetstoreClient";
```

### Test with Different APIs

Change the goal in the scenario:

```csharp
protected override string GetGoal() =>
    "Generate a C# client for the JSONPlaceholder API from " +
    "https://jsonplaceholder.typicode.com/swagger/v1/swagger.json";
```

### Run Without Menu

Run specific scenario directly:

```bash
dotnet run -- --scenario 7
```

(Note: You'd need to implement this argument parsing)

## ?? Troubleshooting

### "Agent 'assistant' not found"
- Ensure OpenAI API key is configured
- Check that `AddBipinsAIAgents()` is called before `AddAgent()`

### Tool Not Executing
- Verify `AddSwaggerClientGeneratorTool()` is registered
- Check agent system prompt includes tool guidance
- Ensure tool name matches exactly: `swagger_client_generator`

### No Files Generated
- Check write permissions to temp directory
- Look for error messages in output
- Verify Swagger URL is accessible
- Check tool execution result for errors

### Build Errors
- Ensure all NuGet packages are restored: `dotnet restore`
- Rebuild solution: `dotnet build --no-incremental`
- Check that Bipins.AI project includes necessary packages:
  - Microsoft.OpenApi.Readers
  - Scriban

## ?? Performance Expectations

| Metric | Expected Value |
|--------|---------------|
| Total execution time | 4-8 seconds |
| OpenAPI parsing | 500-1000ms |
| Code generation | 100-500ms |
| File writing | 50-200ms |
| Agent iterations | 1-3 |
| Files generated | 10-20 (varies by API) |

## ? Success Indicators

You'll know it worked when you see:
- ? "Swagger Client Generator Tool was used successfully!"
- ? List of generated files displayed
- ? Total file count > 0
- ? Output directory contains .cs files
- ? No error messages in output
- ? Agent status: "Completed"

---

**Ready?** Run the demo and experience AI-powered code generation! ??

For detailed documentation, see:
- [Scenario README](../samples/Bipins.AI.AgentSamples/Scenarios/SwaggerClientGenerator_README.md)
- [Test Results](./SwaggerClientGenerator_TestResults.md)
- [Tool Documentation](../src/Bipins.AI/Agents/Tools/BuiltIn/SwaggerClientGenerator_README.md)
