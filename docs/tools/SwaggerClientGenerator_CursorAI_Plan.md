# Swagger Client Generator - Cursor AI Implementation Plan

This document provides step-by-step prompts for implementing the Swagger Client Generator Tool using Cursor AI.

## Prerequisites

Ensure you have the Bipins.AI project open in Cursor AI with the following context files:
- `src/Bipins.AI/Agents/Tools/BuiltIn/VectorSearchTool.cs`
- `src/Bipins.AI/Agents/Tools/BuiltIn/CalculatorTool.cs`
- `src/Bipins.AI/Agents/Tools/IToolExecutor.cs`

## Phase 1: Add Required NuGet Packages

### Prompt 1.1: Add OpenAPI Packages

```
Add the following NuGet packages to src/Bipins.AI/Bipins.AI.csproj:

1. Microsoft.OpenApi.Readers version 1.6.14
2. NSwag.CodeGeneration.CSharp version 14.0.7
3. Scriban version 5.9.1

Ensure they are compatible with the existing multi-targeting setup (.NET 7, 8, 9, 10, and .NET Standard 2.1).
Check for any version conflicts with existing packages.
```

## Phase 2: Implement OpenAPI Parser

### Prompt 2.1: Create OpenApiParser Implementation

```
Create OpenApiParser.cs in src/Bipins.AI/Agents/Tools/CodeGen/ implementing IOpenApiParser.

Requirements:
1. Use Microsoft.OpenApi.Readers.OpenApiStringReader to parse specifications
2. Inject IHttpClientFactory in constructor for fetching remote specs
3. Inject ILogger<OpenApiParser> for logging
4. In ParseAsync method:
   - Create HttpClient from factory
   - Fetch content from URL with timeout (30 seconds)
   - Parse using OpenApiStringReader
   - Check diagnostic.Errors and throw InvalidOperationException if errors exist
   - Log success/failure at appropriate levels
5. In ParseFromContentAsync method:
   - Parse directly from content string
   - Return OpenApiDocument
6. Handle both JSON and YAML formats automatically
7. Support both OpenAPI 2.0 (Swagger) and 3.0

Follow async/await patterns and include comprehensive error handling.
```

### Prompt 2.2: Add Type Mapper Utility

```
Create TypeMapper.cs in src/Bipins.AI/Agents/Tools/CodeGen/ as a static class.

Implement MapOpenApiTypeToCSharp method that converts OpenAPI schema types to C# types:

OpenAPI Type ? C# Type mapping:
- string ? string
- integer (format: int32) ? int
- integer (format: int64) ? long
- number (format: float) ? float
- number (format: double) ? double
- boolean ? bool
- array ? List<T> (where T is the item type)
- object ? Dictionary<string, object> or custom class
- string (format: date-time) ? DateTime
- string (format: date) ? DateOnly (NET 6+) or DateTime
- string (format: uuid) ? Guid
- string (format: byte) ? byte[]
- string (format: binary) ? Stream

Handle nullable types based on OpenApiSchema.Nullable property.
Include XML documentation with examples.
```

## Phase 3: Implement Model Generator

### Prompt 3.1: Create Scriban Template for Models

```
Create ModelTemplate.scriban in src/Bipins.AI/Agents/Tools/CodeGen/Templates/

Template structure:
- File-scoped namespace
- Using statements (System.Text.Json.Serialization)
- XML documentation comment block (if includeXmlDocs is true)
- Public record type with init-only properties
- JsonPropertyName attributes on each property
- Nullable reference type annotations
- Property XML documentation comments

Variables available in template:
- namespace: Root namespace
- className: Name of the model class
- description: Class description from OpenAPI
- properties: List of { name, type, jsonName, description, isNullable }
- includeXmlDocs: Boolean flag

Follow .NET 8+ conventions with record types and init-only properties.
```

### Prompt 3.2: Implement ModelGenerator

```
Create ModelGenerator.cs in src/Bipins.AI/Agents/Tools/CodeGen/ implementing IModelGenerator.

Requirements:
1. Inject ILogger<ModelGenerator> in constructor
2. In GenerateAsync method:
   - Loop through document.Components.Schemas
   - For each schema, call GenerateModelClass
   - Return List<GeneratedFile> with path "Models/{ClassName}.cs"
3. Implement GenerateModelClass method:
   - Load Scriban template from embedded resource or file
   - Map OpenApiSchema properties to template variables
   - Use TypeMapper to convert OpenAPI types to C# types
   - Handle required properties (not nullable)
   - Handle optional properties (nullable if useNullableReferenceTypes is true)
   - Render template with Scriban
   - Return generated code string
4. Handle nested objects by generating separate classes
5. Handle enums as C# enum types
6. Add proper using statements

Follow async/await patterns and include error handling.
```

## Phase 4: Implement Client Generator

### Prompt 4.1: Create Scriban Templates for Clients

```
Create three Scriban templates in src/Bipins.AI/Agents/Tools/CodeGen/Templates/:

1. InterfaceTemplate.scriban:
   - Interface definition for API client
   - Method signatures (async Task<T> MethodNameAsync(...))
   - XML documentation comments
   - No implementation

2. ClientTemplate.scriban:
   - Class implementing the interface
   - Constructor with HttpClient and ILogger dependencies
   - Method implementations:
     * Build request URI with path parameters
     * Add query string parameters
     * Add headers
     * Create HttpRequestMessage
     * Send request with HttpClient
     * Check status code and throw ApiException on errors
     * Deserialize response with ReadFromJsonAsync
     * Log debug/error messages
   - Async/await throughout
   - CancellationToken support

3. ServiceCollectionTemplate.scriban:
   - Extension method for IServiceCollection
   - Configure HttpClient with base address, timeout, headers
   - Register interfaces and implementations
   - Add retry and circuit breaker policies (if includeResiliencePolicies)
   - Register options class
```

### Prompt 4.2: Implement ClientGenerator

```
Create ClientGenerator.cs in src/Bipins.AI/Agents/Tools/CodeGen/ implementing IClientGenerator.

Requirements:
1. Inject ILogger<ClientGenerator> in constructor
2. In GenerateAsync method:
   - Group operations by tags (one client per tag)
   - For each tag, generate interface and implementation
   - Return List<GeneratedFile> with paths:
     * "Clients/I{Tag}Client.cs" (interface)
     * "Clients/{Tag}Client.cs" (implementation)
     * "ServiceCollectionExtensions.cs" (DI setup)
3. Implement GenerateClientInterface method:
   - Load InterfaceTemplate
   - Extract operations for this tag
   - Generate method signatures
   - Render template
4. Implement GenerateClientImplementation method:
   - Load ClientTemplate
   - Map HTTP methods (GET, POST, PUT, DELETE, PATCH)
   - Handle path parameters, query parameters, headers, request body
   - Generate error handling code
   - Render template
5. Handle pagination patterns (if present)
6. Generate Options class for configuration

Use Scriban for all code generation. Follow REST conventions for method naming.
```

## Phase 5: Implement Authentication Generator

### Prompt 5.1: Create Auth Handler Templates

```
Create two Scriban templates in src/Bipins.AI/Agents/Tools/CodeGen/Templates/:

1. BearerAuthTemplate.scriban:
   - BearerAuthenticationHandler : DelegatingHandler
   - Constructor with ITokenProvider dependency
   - Override SendAsync:
     * Get token from ITokenProvider
     * Set Authorization header with "Bearer {token}"
     * Call base.SendAsync
   - ITokenProvider interface definition
   - XML documentation

2. ApiKeyAuthTemplate.scriban:
   - ApiKeyAuthenticationHandler : DelegatingHandler
   - Constructor with IOptions<ApiKeyOptions>
   - Override SendAsync:
     * Get API key from options
     * Set header or query string parameter
     * Call base.SendAsync
   - ApiKeyOptions class definition
   - Support both header-based and query-based API keys
```

### Prompt 5.2: Implement AuthGenerator

```
Create AuthGenerator.cs in src/Bipins.AI/Agents/Tools/CodeGen/ implementing IAuthGenerator.

Requirements:
1. Inject ILogger<AuthGenerator> in constructor
2. In GenerateAsync method:
   - Check document.Components.SecuritySchemes
   - If none exist, return empty list
   - For each security scheme, generate appropriate handler:
     * "http" + "bearer" ? BearerAuthenticationHandler
     * "apiKey" ? ApiKeyAuthenticationHandler (header or query)
     * "oauth2" ? OAuth2AuthenticationHandler (future)
   - Return List<GeneratedFile> with paths in "Auth/" folder
3. Implement GenerateBearerAuth method using BearerAuthTemplate
4. Implement GenerateApiKeyAuth method using ApiKeyAuthTemplate
5. Handle multiple security schemes (generate all needed handlers)

Include instructions in XML comments on how to implement ITokenProvider.
```

## Phase 6: Implement File Writer

### Prompt 6.1: Implement FileWriter

```
Create FileWriter.cs in src/Bipins.AI/Agents/Tools/CodeGen/ implementing IFileWriter.

Requirements:
1. Inject ILogger<FileWriter> in constructor
2. In WriteAllAsync method:
   - Create output directory if it doesn't exist
   - For each GeneratedFile:
     * Call WriteAsync
     * Collect written paths
   - Return list of written paths
   - Log summary (X files written to path)
3. In WriteAsync method:
   - Combine outputPath with file.Path
   - Create subdirectories as needed (Models/, Clients/, Auth/)
   - Write content to file with UTF-8 encoding
   - Add file header comment with:
     * "Auto-generated by Bipins.AI Swagger Client Generator"
     * Generation timestamp
     * "Do not modify manually"
   - Optionally format code using Roslyn CSharpSyntaxTree (if available)
   - Return full file path
   - Log each file written
4. Handle file conflicts:
   - Overwrite existing files (log warning)
   - Option to skip existing files (future enhancement)

Include error handling for I/O exceptions.
```

## Phase 7: Complete Tool Integration

### Prompt 7.1: Update SwaggerClientGeneratorTool with Full Implementation

```
Update SwaggerClientGeneratorTool.cs in src/Bipins.AI/Agents/Tools/BuiltIn/ to use the implemented generators.

Modify the constructor to inject:
- IOpenApiParser _openApiParser
- IModelGenerator _modelGenerator
- IClientGenerator _clientGenerator
- IAuthGenerator _authGenerator
- IFileWriter _fileWriter

Update ExecuteAsync method:
1. After parameter extraction and validation:
   - Call _openApiParser.ParseAsync(swaggerUrl, cancellationToken)
   - Log "Parsed OpenAPI specification with {Count} paths"
2. If options.GenerateModels:
   - Call _modelGenerator.GenerateAsync
   - Add to generatedFiles list
3. If options.GenerateClients:
   - Call _clientGenerator.GenerateAsync
   - Add to generatedFiles list
4. If options.GenerateAuthentication and security schemes exist:
   - Call _authGenerator.GenerateAsync
   - Add to generatedFiles list
5. Call _fileWriter.WriteAllAsync with all generated files
6. Return ToolExecutionResult with:
   - Success: true
   - Result: { filesGenerated: count, outputPath, files: paths }
   - Metadata: summary statistics

Add comprehensive error handling and logging throughout.
```

### Prompt 7.2: Update DI Registration

```
Update the AddSwaggerClientGeneratorTool method in ServiceCollectionExtensions.cs:

Register all dependencies:
- services.AddSingleton<IOpenApiParser, OpenApiParser>()
- services.AddSingleton<IModelGenerator, ModelGenerator>()
- services.AddSingleton<IClientGenerator, ClientGenerator>()
- services.AddSingleton<IAuthGenerator, AuthGenerator>()
- services.AddSingleton<IFileWriter, FileWriter>()
- services.AddSingleton<IToolExecutor, SwaggerClientGeneratorTool>()

Ensure HttpClientFactory is registered (already done).
```

## Phase 8: Create Unit Tests

### Prompt 8.1: Create Test Infrastructure

```
Create SwaggerClientGeneratorToolTests.cs in tests/Bipins.AI.UnitTests/Tools/

Setup:
1. Create test fixture with mock dependencies:
   - Mock<IHttpClientFactory>
   - Mock<IOpenApiParser>
   - Mock<IModelGenerator>
   - Mock<IClientGenerator>
   - Mock<IAuthGenerator>
   - Mock<IFileWriter>
   - NullLogger<SwaggerClientGeneratorTool>
2. Create helper method CreateMockHttpClientFactory that returns a factory with a mock HttpClient

Write tests:
- ExecuteAsync_ValidParameters_ReturnsSuccess
- ExecuteAsync_MissingSwaggerUrl_ReturnsError
- ExecuteAsync_MissingNamespace_ReturnsError
- ExecuteAsync_MissingOutputPath_ReturnsError
- ExecuteAsync_InvalidJsonArguments_ReturnsError
- ExecuteAsync_ParserThrowsException_ReturnsError
- ExtractOptions_AllOptionsProvided_MapsCorrectly
- ExtractOptions_NoOptionsProvided_UsesDefaults
```

### Prompt 8.2: Create Integration Tests

```
Create SwaggerClientGeneratorIntegrationTests.cs in tests/Bipins.AI.IntegrationTests/Tools/

Requirements:
1. Use real implementations (not mocks)
2. Test against public Swagger specs:
   - https://petstore.swagger.io/v2/swagger.json
   - https://petstore3.swagger.io/api/v3/openapi.json
3. Write to temporary directory (Path.GetTempPath())
4. Verify generated files exist
5. Attempt to compile generated code using Roslyn
6. Clean up temp files after test

Tests:
- GenerateFromPetStoreV2_CreatesValidFiles
- GenerateFromPetStoreV3_CreatesValidFiles
- GeneratedCode_CompilesSuccessfully
- GeneratedModels_AreValidRecordTypes
- GeneratedClients_HaveCorrectMethods
```

## Phase 9: Create Sample Project

### Prompt 9.1: Create Sample Console App

```
Create a new sample project: samples/SwaggerClientGeneratorSample/

Files needed:
1. SwaggerClientGeneratorSample.csproj
   - Target net8.0
   - Reference Bipins.AI project
   - Include OpenAI configuration

2. Program.cs:
   - Setup dependency injection
   - Configure Bipins.AI with OpenAI
   - Add SwaggerClientGeneratorTool
   - Create agent with code generation focus
   - Execute agent with sample request:
     "Generate a C# client for the Pet Store API at https://petstore.swagger.io/v2/swagger.json"
   - Display results
   - Show generated files

3. appsettings.json with OpenAI configuration placeholder

4. README.md with:
   - How to run the sample
   - Expected output
   - How to use generated client
```

## Phase 10: Documentation

### Prompt 10.1: Create Comprehensive Documentation

```
Update the following documentation:

1. src/Bipins.AI/Agents/Tools/BuiltIn/SwaggerClientGenerator_README.md:
   - Add "Getting Started" section with quickstart example
   - Add "API Reference" section with all parameters
   - Add "Generated Code Examples" section
   - Add "Troubleshooting" section
   - Add "FAQ" section

2. Create docs/tools/SwaggerClientGenerator.md:
   - Architectural overview
   - How it works (OpenAPI parsing ? code generation ? file writing)
   - Extension points (custom templates, custom generators)
   - Performance characteristics
   - Limitations and known issues

3. Update main README.md:
   - Add SwaggerClientGeneratorTool to tools list
   - Add quick example

4. Create CHANGELOG.md entry for the new feature
```

## Validation Checklist

After completing all prompts, verify:

- [ ] All files compile without errors
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Code coverage > 80%
- [ ] XML documentation on all public members
- [ ] Nullable reference types enabled and properly used
- [ ] Async/await patterns followed throughout
- [ ] Error handling comprehensive
- [ ] Logging at appropriate levels
- [ ] SOLID principles applied
- [ ] Sample project runs successfully
- [ ] Generated code compiles successfully
- [ ] Documentation complete and accurate

## Tips for Using with Cursor AI

1. **Copy one prompt at a time** - Don't overwhelm the AI with multiple complex tasks
2. **Review generated code** - Always review and test before moving to next prompt
3. **Reference existing code** - Point Cursor AI to similar existing implementations
4. **Iterate** - If the first attempt isn't perfect, refine the prompt with more specifics
5. **Test frequently** - Run tests after each major component is implemented
6. **Use @workspace** - Reference workspace files to maintain consistency

## Next Steps After Implementation

1. **Add more templates**: Support for different coding styles
2. **Add more authentication types**: OAuth2 flows, custom auth
3. **Support bulk generation**: Generate clients for multiple APIs at once
4. **Add caching**: Cache parsed OpenAPI specs
5. **Add validation**: Validate generated code with Roslyn
6. **Add customization**: Allow custom template overrides
7. **Add CLI tool**: Standalone command-line interface
8. **Publish NuGet**: Package as standalone library

---

**Total Estimated Time**: 20-30 hours of development with Cursor AI assistance

**Complexity Level**: Intermediate to Advanced

**Dependencies**: Microsoft.OpenApi.Readers, NSwag, Scriban, Roslyn (optional)
