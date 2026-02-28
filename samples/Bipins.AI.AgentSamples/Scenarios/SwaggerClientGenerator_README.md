# Scenario 7: Swagger Client Generator (Agentic Code Generation)

## Overview

This scenario demonstrates **agentic code generation** where an AI agent autonomously uses the `swagger_client_generator` tool to create a complete, production-ready C# API client library from an OpenAPI/Swagger specification.

## What Makes This "Agentic"?

Unlike traditional code generation tools that require explicit commands, this scenario shows:

1. **Autonomous Tool Selection** - The agent decides to use the swagger_client_generator tool based on the goal description
2. **Parameter Inference** - The agent extracts and formats the necessary parameters from natural language
3. **Intelligent Decision Making** - The agent understands the user's intent and translates it into concrete tool actions
4. **Self-Guided Execution** - No explicit tool invocation needed - the agent figures it out

## How It Works

```
User Goal (Natural Language)
        ?
   AI Agent Analysis
        ?
   Tool Selection (swagger_client_generator)
        ?
   Parameter Extraction
   - swaggerUrl: https://petstore.swagger.io/v2/swagger.json
   - namespace: PetstoreClient
   - outputPath: C:\Users\...\Temp\BipinsAI\GeneratedClients\PetstoreClient
        ?
   Tool Execution
   - Parse OpenAPI spec
   - Generate Models (Pet, Category, Tag, Order, User)
   - Generate Clients (IPetClient, PetClient, IStoreClient, etc.)
   - Generate Auth Handlers (ApiKeyAuthenticationHandler)
   - Write files to disk
        ?
   Success Response with Statistics
```

## What Gets Generated

The tool generates a complete, SOLID-principled C# client library:

### ?? Project Structure

```
PetstoreClient/
??? Models/
?   ??? Pet.cs
?   ??? Category.cs
?   ??? Tag.cs
?   ??? Order.cs
?   ??? User.cs
?   ??? ApiResponse.cs
??? Clients/
?   ??? IPetClient.cs
?   ??? PetClient.cs
?   ??? IStoreClient.cs
?   ??? StoreClient.cs
?   ??? IUserClient.cs
?   ??? UserClient.cs
??? Auth/
?   ??? ApiKeyAuthenticationHandler.cs
??? ServiceCollectionExtensions.cs
```

### Generated Features

- ? **Clean Models** - POCOs with nullable reference types
- ? **Async Clients** - Async/await throughout
- ? **SOLID Principles** - Interfaces for clients, dependency injection
- ? **.NET 8 Best Practices** - Modern C# patterns
- ? **Authentication** - Auth handlers for API Key, Bearer Token, Basic Auth
- ? **Resilience** - Optional Polly policies for retries and circuit breakers
- ? **XML Documentation** - IntelliSense-ready documentation
- ? **DI Setup** - Extension methods for easy registration

## Running the Scenario

### Prerequisites

- OpenAI API key configured (see [main README](../README.md))
- Internet connection (to fetch Swagger spec)
- Write permissions to temp directory

### Execute

```bash
cd samples/Bipins.AI.AgentSamples
dotnet run
```

Select **Option 7** from the menu.

### Expected Output

```
???????????????????????????????????????????????????????????????????
 SCENARIO 7: Swagger Client Generator
???????????????????????????????????????????????????????????????????

?? GOAL: Generate a C# client library for the Petstore API from https://petstore.swagger.io/v2/swagger.json

?? Output directory: C:\Users\YourName\AppData\Local\Temp\BipinsAI\GeneratedClients\PetstoreClient

????????????????????????????????????????????????????????????????????

?? Agent is analyzing the request and will use tools as needed...

????????????????????????????????????????????????????????????????????

? EXECUTING...

? RESPONSE:
I've successfully generated a complete C# client library for the Petstore API. The library includes:
- 6 model classes (Pet, Category, Tag, Order, User, ApiResponse)
- 6 client classes (3 interfaces + 3 implementations for Pet, Store, and User operations)
- 1 authentication handler for API key authentication
- Dependency injection setup with extension methods

All files have been written to the specified output directory and are ready to use in your .NET application.

?  EXECUTION DETAILS:
   • Status: Completed
   • Iterations: 2
   • Time: 4,523 ms

?? TOOL CALLS (1):
   • swagger_client_generator
     Arguments: {
       "swaggerUrl": "https://petstore.swagger.io/v2/swagger.json",
       "namespace": "PetstoreClient",
       "outputPath": "C:\\Users\\YourName\\AppData\\Local\\Temp\\BipinsAI\\GeneratedClients\\PetstoreClient"
     }

????????????????????????????????????????????????????????????????????

? Swagger Client Generator Tool was used successfully!

?? Generated files:
   • Auth/ApiKeyAuthenticationHandler.cs
   • Clients/IPetClient.cs
   • Clients/IStoreClient.cs
   • Clients/IUserClient.cs
   • Clients/PetClient.cs
   • Clients/StoreClient.cs
   • Clients/UserClient.cs
   • Models/ApiResponse.cs
   • Models/Category.cs
   • Models/Order.cs
   • Models/Pet.cs
   • Models/Tag.cs
   • Models/User.cs
   • ServiceCollectionExtensions.cs

?? Total files generated: 14
?? Location: C:\Users\YourName\AppData\Local\Temp\BipinsAI\GeneratedClients\PetstoreClient

????????????????????????????????????????????????????????????????????
```

## Key Learning Points

### 1. Natural Language to Code

The agent understands requests like:
- "Generate a client for the GitHub API"
- "Create API bindings for Stripe from their OpenAPI spec"
- "I need a C# client for https://api.example.com/swagger.json"

### 2. Automatic Parameter Mapping

The agent automatically:
- Extracts the Swagger URL from the user's goal
- Infers an appropriate namespace from the API name
- Chooses a sensible output location
- Sets default options for code generation

### 3. Tool Integration

This scenario shows how to:
- Register complex tools with multiple dependencies
- Pass structured data to tools via JSON
- Handle tool execution results
- Display tool-generated artifacts

### 4. Production-Ready Output

The generated code includes:
- Proper error handling
- Nullable reference types
- Async/await patterns
- XML documentation
- Dependency injection setup

## Advanced Usage

### Custom Generation Options

You can guide the agent to use specific options:

```csharp
var request = new AgentRequest(
    Goal: "Generate a C# client for the Petstore API from " +
          "https://petstore.swagger.io/v2/swagger.json. " +
          "Use namespace 'MyApp.PetstoreClient' and save to 'C:\\MyProjects\\Clients'. " +
          "Do not generate authentication handlers, but include XML documentation and interfaces.",
    Context: "I already have my own auth implementation");
```

The agent will translate this into:

```json
{
  "swaggerUrl": "https://petstore.swagger.io/v2/swagger.json",
  "namespace": "MyApp.PetstoreClient",
  "outputPath": "C:\\MyProjects\\Clients",
  "options": {
    "generateAuthentication": false,
    "includeXmlDocs": true,
    "generateInterfaces": true
  }
}
```

### Different APIs

Try with other public APIs:

```csharp
// GitHub API
Goal: "Generate a client for the GitHub REST API from https://api.github.com/swagger"

// Stripe API
Goal: "Create API bindings for Stripe payments"

// JSONPlaceholder (testing API)
Goal: "Generate a client for JSONPlaceholder from https://jsonplaceholder.typicode.com/swagger/v1/swagger.json"
```

## Technical Details

### Tool Parameters Schema

```json
{
  "type": "object",
  "properties": {
    "swaggerUrl": {
      "type": "string",
      "description": "URL to OpenAPI/Swagger JSON or YAML specification"
    },
    "namespace": {
      "type": "string",
      "description": "Root namespace for generated code"
    },
    "outputPath": {
      "type": "string",
      "description": "File system path where client library will be generated"
    },
    "options": {
      "type": "object",
      "properties": {
        "generateModels": { "type": "boolean", "default": true },
        "generateClients": { "type": "boolean", "default": true },
        "includeXmlDocs": { "type": "boolean", "default": true },
        "useNullableReferenceTypes": { "type": "boolean", "default": true },
        "asyncSuffix": { "type": "boolean", "default": true },
        "generateInterfaces": { "type": "boolean", "default": true },
        "generateAuthentication": { "type": "boolean", "default": true },
        "includeResiliencePolicies": { "type": "boolean", "default": true }
      }
    }
  },
  "required": ["swaggerUrl", "namespace", "outputPath"]
}
```

### Code Generation Pipeline

1. **OpenApiParser** - Fetches and parses the Swagger/OpenAPI spec
2. **TypeMapper** - Maps OpenAPI types to C# types
3. **ModelGenerator** - Generates model/DTO classes using Scriban templates
4. **ClientGenerator** - Generates API client classes with interfaces
5. **AuthGenerator** - Generates authentication handlers based on security schemes
6. **FileWriter** - Writes all generated files to disk with proper structure

## Comparing Traditional vs. Agentic

### Traditional Approach (CLI Tool)

```bash
# User must know exact command syntax
swagger-codegen generate \
  -i https://petstore.swagger.io/v2/swagger.json \
  -l csharp \
  -o ./PetstoreClient \
  --additional-properties packageName=PetstoreClient
```

### Agentic Approach (Bipins.AI)

```csharp
// Natural language request
var request = new AgentRequest(
    Goal: "Generate a C# client for the Petstore API");

// Agent figures out:
// - Which tool to use (swagger_client_generator)
// - Where to get the spec (searches for swagger URL)
// - What namespace to use (infers from API name)
// - Where to save files (uses temp directory)
// - Which options to enable (uses sensible defaults)

var response = await agent.ExecuteAsync(request);
```

## Benefits of Agentic Code Generation

1. **Lower Barrier to Entry** - No need to learn tool syntax
2. **Intelligent Defaults** - Agent chooses appropriate settings
3. **Context Awareness** - Agent considers the user's development context
4. **Error Recovery** - Agent can retry with different parameters if generation fails
5. **Multi-Tool Orchestration** - Agent can use multiple tools in sequence (e.g., fetch spec ? validate ? generate ? test)

## Future Enhancements

This scenario can be extended to:

- **Automatic Testing** - Agent generates unit tests for the generated client
- **Code Review** - Agent reviews generated code for best practices
- **Integration** - Agent adds the client to an existing project
- **Documentation** - Agent generates API usage documentation
- **Validation** - Agent compiles the generated code to verify correctness

## Related Documentation

- [Main README](../README.md) - AgentSamples overview
- [Bipins.AI README](../../README.md) - Library documentation
- [SwaggerClientGeneratorTool](../../src/Bipins.AI/Agents/Tools/BuiltIn/SwaggerClientGenerator_README.md) - Tool documentation
- [Code Generation Architecture](../../docs/tools/SwaggerClientGenerator_Diagrams.md) - Technical diagrams

---

**Try it now!** Run the sample and experience autonomous code generation powered by AI agents. ??
