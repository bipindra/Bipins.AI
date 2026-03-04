# API Client Sample App Generator Tool

Creates a **runnable console application** for an existing generated API client: adds a client library `.csproj`, a sample console project with correct project reference, and uses the **LLM** to generate `Program.cs` that correctly uses the generated clients and demonstrates the API.

## When to use

Call this tool **after** `swagger_client_generator` (or after any generator that produced a client folder with `Models/`, `Clients/`, and `ServiceCollectionExtensions.cs`). It:

1. **Adds a client library .csproj** in the client output folder (if missing) so the generated code is buildable.
2. **Creates a sample console project** (e.g. `SampleApp`) as a sibling folder, with a project reference to the client.
3. **Uses the LLM** to understand the API (from optional `swaggerUrl` and from the generated client code) and to generate a `Program.cs` that:
   - Uses `Host.CreateDefaultBuilder` and DI
   - Calls `services.AddApiClients(baseUrl)` from the generated namespace
   - Resolves the generated client(s) and runs a small demo (calls one or more endpoints and prints results)

So the agent can: generate the client with `swagger_client_generator`, then call `api_client_sample_app_generator` to get a runnable sample that utilizes the generated console app.

## Registration

Requires **IOpenApiParser**, **IFileWriter** (e.g. from `AddSwaggerClientGeneratorTool`), and **ILLMProvider** (e.g. from `AddOpenAI`):

```csharp
services
    .AddBipinsAI()
    .AddOpenAI(options => { ... })
    .AddSwaggerClientGeneratorTool()
    .AddApiClientSampleAppGeneratorTool()
    .AddBipinsAIAgents()
    .AddAgent("CodeGenAgent", options =>
    {
        options.Name = "Code Generation Assistant";
        options.SystemPrompt = "You help generate API clients and runnable sample apps.";
        options.EnablePlanning = true;
    });
```

## Parameters

| Parameter           | Required | Description |
|---------------------|----------|-------------|
| `clientOutputPath`  | Yes      | Path where the API client was generated (e.g. by `swagger_client_generator`). |
| `baseUrl`           | Yes      | Base URL of the API (e.g. `https://api.example.com`). |
| `namespaceName`     | Yes      | Root namespace of the generated client (e.g. `MyCompany.ApiClient`). |
| `sampleAppName`     | No       | Name for the sample console app folder/project (default: `SampleApp`). |
| `swaggerUrl`        | No       | URL to OpenAPI/Swagger spec so the LLM can understand API behavior and plan the sample. |

## Example (agent flow)

1. Agent runs **swagger_client_generator** with `swaggerUrl`, `namespace`, `outputPath`.
2. Agent runs **api_client_sample_app_generator** with:
   - `clientOutputPath`: same as `outputPath` from step 1  
   - `baseUrl`: API base URL  
   - `namespaceName`: same as `namespace` from step 1  
   - `swaggerUrl`: same as in step 1 (optional but recommended)

3. User runs the sample: `dotnet run --project <sampleAppPath>/<sampleAppName>.csproj`

## Output

- **Client folder**: `{clientOutputPath}/{ClientProjectName}.csproj` (if it did not exist).
- **Sample folder**: `{parentOfClientOutputPath}/{sampleAppName}/` with:
  - `{sampleAppName}.csproj` (console app, project reference to client).
  - `Program.cs` (LLM-generated, uses generated clients and demonstrates the API).

The tool returns paths and an instruction string for running the sample app.
