# Agent Tools

Bipins.AI agents can use built-in tools for code generation and API client workflows.

## Available tools

| Tool | Description |
|------|-------------|
| **swagger_client_generator** | Generates a C# client library from an OpenAPI/Swagger spec (models, clients, auth, DI). |
| **api_client_sample_app_generator** | Creates a runnable console app for a generated client: adds .csproj, sample project, and LLM-generated `Program.cs`. |

Detailed docs live next to the source:

- [Swagger Client Generator](../src/Bipins.AI/Agents/Tools/BuiltIn/SwaggerClientGenerator_README.md)
- [API Client Sample App Generator](../src/Bipins.AI/Agents/Tools/BuiltIn/ApiClientSampleAppGenerator_README.md)

## Quick start: Swagger client generator

1. **Configure OpenAI** (user secrets or env):
   ```bash
   cd samples/Bipins.AI.AgentSamples
   dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key"
   ```

2. **Run the sample**:
   ```bash
   dotnet run
   ```

3. **Choose scenario 7** (Swagger Client Generator). The agent will use `swagger_client_generator` to fetch a spec (e.g. Petstore), generate the client, and write files to the configured output path.

4. **Optional – runnable sample app**: After generating a client, the agent can call `api_client_sample_app_generator` with the same output path, base URL, and namespace to create a console app and LLM-generated `Program.cs` that uses the client.

## Registration

```csharp
services
    .AddBipinsAI()
    .AddOpenAI(o => { ... })
    .AddSwaggerClientGeneratorTool()
    .AddApiClientSampleAppGeneratorTool()
    .AddBipinsAIAgents()
    .AddAgent("CodeGenAgent", options => { ... });
```

See the tool READMEs above for parameters and examples.
