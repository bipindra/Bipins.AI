# Swagger Client Generator - Quick Reference

## ?? Registration (One Line)

```csharp
services.AddBipinsAI().AddBipinsAIAgents().AddSwaggerClientGeneratorTool();
```

## ?? Tool Parameters

```json
{
  "swaggerUrl": "https://api.example.com/swagger.json",
  "namespace": "MyCompany.ApiClient",
  "outputPath": "C:\\Projects\\MyApp\\Client",
  "options": {
    "generateModels": true,
    "generateClients": true,
    "includeXmlDocs": true,
    "useNullableReferenceTypes": true,
    "asyncSuffix": true,
    "generateInterfaces": true,
    "generateAuthentication": true,
    "includeResiliencePolicies": true
  }
}
```

## ?? Generated Structure

```
OutputPath/
??? Models/              # DTOs and data models
??? Clients/             # API client implementations
??? Auth/                # Authentication handlers
??? Exceptions/          # Custom exceptions
??? Options/             # Configuration options
??? ServiceCollectionExtensions.cs
```

## ? Cursor AI - First 3 Steps

### 1. Add Packages
```bash
dotnet add src/Bipins.AI/Bipins.AI.csproj package Microsoft.OpenApi.Readers --version 1.6.14
dotnet add src/Bipins.AI/Bipins.AI.csproj package Scriban --version 5.9.1
```

### 2. Cursor AI Prompt
```
Implement OpenApiParser.cs in src/Bipins.AI/Agents/Tools/CodeGen/ 
implementing IOpenApiParser. Use Microsoft.OpenApi.Readers to parse 
OpenAPI specs from URLs. Include error handling and logging.
```

### 3. Test
```bash
dotnet build src/Bipins.AI/Bipins.AI.csproj
dotnet test
```

## ?? Implementation Sequence

```
1. NuGet Packages
   ?
2. OpenApiParser + TypeMapper
   ?
3. ModelGenerator + Templates
   ?
4. ClientGenerator + Templates
   ?
5. AuthGenerator + Templates
   ?
6. FileWriter
   ?
7. Integration + Tests
```

## ?? Key Files Created

| File | Purpose |
|------|---------|
| `SwaggerClientGeneratorTool.cs` | Main tool (? Complete) |
| `GeneratorOptions.cs` | Configuration (? Complete) |
| `IOpenApiParser.cs` | Interface (? Complete) |
| `OpenApiParser.cs` | ? **Next to implement** |
| `TypeMapper.cs` | ? Type conversions |
| `ModelGenerator.cs` | ? Generate models |
| `ClientGenerator.cs` | ? Generate clients |

## ?? Generated Code Example

### Input (Swagger)
```json
{
  "paths": {
    "/api/users/{id}": {
      "get": {
        "operationId": "GetUser",
        "parameters": [{"name": "id", "in": "path", "type": "integer"}],
        "responses": {"200": {"schema": {"$ref": "#/definitions/User"}}}
      }
    }
  }
}
```

### Output (C#)
```csharp
public async Task<User> GetUserAsync(int id, CancellationToken ct = default)
{
    var response = await _httpClient.GetAsync($"api/users/{id}", ct);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<User>(ct);
}
```

## ?? Testing Checklist

```
? Parse valid OpenAPI 2.0 spec
? Parse valid OpenAPI 3.0 spec
? Generate models with correct types
? Generate clients with async methods
? Generate auth handlers
? Write files to disk
? Generated code compiles
? Unit tests pass (>80% coverage)
? Integration tests pass
```

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| Package conflict | Check multi-targeting compatibility |
| Parse error | Validate OpenAPI spec at swagger.io/validator |
| Type mapping missing | Add to TypeMapper.cs |
| Template error | Check Scriban syntax |
| File write fails | Check permissions and path |

## ?? Full Documentation

- **Plan**: `docs/tools/SwaggerClientGenerator_CursorAI_Plan.md`
- **Summary**: `docs/tools/SwaggerClientGenerator_Summary.md`
- **README**: `src/Bipins.AI/Agents/Tools/BuiltIn/SwaggerClientGenerator_README.md`

## ?? Estimated Time

- **With Cursor AI**: 8-12 hours
- **Manual**: 20-30 hours
- **Testing**: 4-6 hours
- **Documentation**: 2-3 hours

**Total**: 14-21 hours with AI assistance

---

**Start Here**: Open Cursor AI ? Load plan ? Use Prompt 1.1
