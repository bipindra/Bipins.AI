# Swagger Client Generator Tool

An intelligent agentic tool for generating complete C# client libraries from Swagger/OpenAPI specifications.

## Overview

The `SwaggerClientGeneratorTool` is an `IToolExecutor` implementation that can be used by AI agents to automatically generate type-safe, async/await-based C# client libraries from OpenAPI 2.0 and 3.0 specifications. It follows SOLID principles and .NET 8+ best practices.

## Features

- ? **Parse OpenAPI Specifications**: Supports both JSON and YAML formats
- ? **Generate Clean Models**: Record types with proper null handling
- ? **Async API Clients**: Full async/await support with CancellationToken
- ? **Authentication Handlers**: Bearer token, API key, and OAuth2 support
- ? **Resilience Policies**: Built-in retry and circuit breaker patterns
- ? **SOLID Principles**: Interface-based, dependency injection ready
- ? **XML Documentation**: Comprehensive code documentation
- ? **.NET 8 Features**: Record types, nullable reference types, file-scoped namespaces

## Registration

Add the tool to your agent configuration:

```csharp
services
    .AddBipinsAI()
    .AddOpenAI(options => { ... })
    .AddBipinsAIAgents()
    .AddSwaggerClientGeneratorTool()  // Register the tool
    .AddAgent("CodeGenAgent", options =>
    {
        options.Name = "Code Generation Assistant";
        options.SystemPrompt = "You help developers generate client libraries.";
        options.EnablePlanning = true;
    });
```

## Usage

### Agent Invocation

The agent will automatically use this tool when asked to generate client libraries:

```csharp
var request = new AgentRequest(
    Goal: "Generate a C# client for the GitHub API",
    Context: "I need to interact with GitHub's REST API");

var response = await agent.ExecuteAsync(request);
```

### Direct Tool Invocation

You can also invoke the tool directly:

```csharp
var toolCall = new ToolCall(
    Name: "swagger_client_generator",
    Arguments: JsonSerializer.SerializeToElement(new
    {
        swaggerUrl = "https://petstore.swagger.io/v2/swagger.json",
        @namespace = "PetStore.Client",
        outputPath = @"C:\Projects\MyApp\PetStore.Client",
        options = new
        {
            generateModels = true,
            generateClients = true,
            includeXmlDocs = true,
            useNullableReferenceTypes = true,
            asyncSuffix = true,
            generateInterfaces = true,
            generateAuthentication = true,
            includeResiliencePolicies = true
        }
    }));

var result = await tool.ExecuteAsync(toolCall, cancellationToken);
```

## Parameters

### Required Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `swaggerUrl` | string | URL to OpenAPI/Swagger JSON or YAML specification |
| `namespace` | string | Root namespace for generated code (e.g., `MyCompany.ApiClient`) |
| `outputPath` | string | File system path where client library will be generated |

### Optional Parameters (in `options` object)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `generateModels` | boolean | true | Generate model/DTO classes |
| `generateClients` | boolean | true | Generate API client classes |
| `includeXmlDocs` | boolean | true | Include XML documentation comments |
| `useNullableReferenceTypes` | boolean | true | Use nullable reference types |
| `asyncSuffix` | boolean | true | Add "Async" suffix to async methods |
| `generateInterfaces` | boolean | true | Generate interfaces for clients |
| `generateAuthentication` | boolean | true | Generate authentication handlers |
| `includeResiliencePolicies` | boolean | true | Include retry and circuit breaker policies |

## Generated Structure

The tool generates a complete client library with the following structure:

```
OutputPath/
??? Models/
?   ??? User.cs
?   ??? Product.cs
?   ??? ...
??? Clients/
?   ??? IUserClient.cs
?   ??? UserClient.cs
?   ??? IProductClient.cs
?   ??? ProductClient.cs
?   ??? ...
??? Auth/
?   ??? ITokenProvider.cs
?   ??? BearerAuthenticationHandler.cs
?   ??? ApiKeyAuthenticationHandler.cs
??? Exceptions/
?   ??? ApiException.cs
??? Options/
?   ??? {ClientName}Options.cs
??? ServiceCollectionExtensions.cs
```

## Example Generated Code

### Model (Record Type)

```csharp
namespace PetStore.Client.Models;

/// <summary>
/// Represents a user in the system
/// </summary>
public record User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// Username of the user
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; init; } = default!;

    /// <summary>
    /// Email address
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
```

### API Client

```csharp
namespace PetStore.Client.Clients;

public class UserClient : IUserClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserClient> _logger;

    public UserClient(HttpClient httpClient, ILogger<UserClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    public async Task<User> GetUserAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Getting user with ID {UserId}", id);
            
            var response = await _httpClient.GetAsync($"api/users/{id}", ct);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<User>(ct) 
                ?? throw new ApiException("Response was null");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error getting user {UserId}", id);
            throw new ApiException("Network error", ex);
        }
    }
}
```

### Dependency Injection Setup

```csharp
services.AddPetStoreClient(options =>
{
    options.BaseUrl = "https://petstore.swagger.io/v2";
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

## Implementation Status

### Phase 1: Core Infrastructure ?
- [x] Tool interface and registration
- [x] Parameter schema definition
- [x] Options extraction
- [x] Basic tool structure

### Phase 2: OpenAPI Parsing (Next)
- [ ] Implement `IOpenApiParser`
- [ ] Add `Microsoft.OpenApi.Readers` package
- [ ] Parse from URL
- [ ] Parse from content
- [ ] Validate specification

### Phase 3: Code Generation
- [ ] Implement `IModelGenerator`
- [ ] Implement `IClientGenerator`
- [ ] Implement `IAuthGenerator`
- [ ] Implement `IFileWriter`
- [ ] Add code templates (Scriban)

### Phase 4: Advanced Features
- [ ] Type mapping (OpenAPI ? C#)
- [ ] Complex schema handling
- [ ] Enum generation
- [ ] Inheritance support
- [ ] Polymorphism support

### Phase 5: Testing & Documentation
- [ ] Unit tests
- [ ] Integration tests
- [ ] Sample projects
- [ ] Full documentation

## Development Roadmap

Follow the [comprehensive plan](../../../docs/SwaggerClientGenerator_Plan.md) for step-by-step implementation using Cursor AI.

### Quick Start with Cursor AI

1. **Add NuGet Packages**
   ```bash
   dotnet add package Microsoft.OpenApi.Readers --version 1.6.14
   dotnet add package NSwag.CodeGeneration.CSharp --version 14.0.7
   dotnet add package Scriban --version 5.9.1
   ```

2. **Implement OpenApiParser**
   - Use Cursor AI prompt from Slide 4 of the plan
   - Reference `Microsoft.OpenApi.Readers` documentation

3. **Implement Generators**
   - Follow prompts for `ModelGenerator`, `ClientGenerator`, `AuthGenerator`
   - Use Scriban templates for clean code generation

4. **Add Tests**
   - Create unit tests for each component
   - Add integration tests with real Swagger specs

## Contributing

When extending this tool:

1. **Follow SOLID Principles**: Each generator has a single responsibility
2. **Use Async/Await**: All I/O operations should be async
3. **Add Tests**: 80%+ code coverage required
4. **Document**: XML comments and README updates
5. **.NET 8+ Features**: Use records, nullable types, file-scoped namespaces

## See Also

- [IToolExecutor Interface](../IToolExecutor.cs)
- [Agent Tools Documentation](../../README.md)
- [Calculator Tool Example](./CalculatorTool.cs)
- [Vector Search Tool Example](./VectorSearchTool.cs)

## License

MIT License - Part of Bipins.AI framework
