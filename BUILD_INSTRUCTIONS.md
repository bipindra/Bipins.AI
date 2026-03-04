# ?? Build Instructions - NuGet Packages Required

## Current Status: Phase 1 Complete ?

The core infrastructure is in place, but the project won't build until NuGet packages are added.

## ? Current Build Error

```
CS0234: The type or namespace name 'OpenApi' does not exist in the namespace 'Microsoft'
```

**Reason**: The `Microsoft.OpenApi.Models` package is not yet installed.

## ? How to Fix

### Step 1: Add Required NuGet Packages

Run these commands from the repository root:

```bash
dotnet add src/Bipins.AI/Bipins.AI.csproj package Microsoft.OpenApi.Readers --version 1.6.14
dotnet add src/Bipins.AI/Bipins.AI.csproj package NSwag.CodeGeneration.CSharp --version 14.0.7
dotnet add src/Bipins.AI/Bipins.AI.csproj package Scriban --version 5.9.1
```

### Step 2: Verify Build

```bash
dotnet build src/Bipins.AI/Bipins.AI.csproj
```

### Step 3: Continue with Implementation

Once the build succeeds, see **docs/TOOLS.md** for agent tools overview and the Swagger client generator README in this repo for implementation details.

## ?? What's Been Created (Phase 1)

### ? Core Files (Ready)
1. `src/Bipins.AI/Agents/Tools/CodeGen/GeneratorOptions.cs`
2. `src/Bipins.AI/Agents/Tools/CodeGen/GeneratedFile.cs`
3. `src/Bipins.AI/Agents/Tools/CodeGen/IFileWriter.cs`
4. `src/Bipins.AI/Agents/Tools/BuiltIn/SwaggerClientGeneratorTool.cs`
5. `src/Bipins.AI/ServiceCollectionExtensions.cs` (updated)

### ? Interface Files (Waiting for NuGet packages)
1. `src/Bipins.AI/Agents/Tools/CodeGen/IOpenApiParser.cs` - ?? References `Microsoft.OpenApi.Models`
2. `src/Bipins.AI/Agents/Tools/CodeGen/IModelGenerator.cs` - ?? References `Microsoft.OpenApi.Models`
3. `src/Bipins.AI/Agents/Tools/CodeGen/IClientGenerator.cs` - ?? References `Microsoft.OpenApi.Models`
4. `src/Bipins.AI/Agents/Tools/CodeGen/IAuthGenerator.cs` - ?? References `Microsoft.OpenApi.Models`

### ?? Documentation Files
1. `src/Bipins.AI/Agents/Tools/BuiltIn/SwaggerClientGenerator_README.md`
2. `docs/TOOLS.md` (agent tools overview and quick start)

## ?? Quick Start

### Option 1: Add Packages Now (Recommended)

```powershell
# From repository root
cd C:\src\AI\Bipins.AI

# Add packages
dotnet add src/Bipins.AI/Bipins.AI.csproj package Microsoft.OpenApi.Readers --version 1.6.14
dotnet add src/Bipins.AI/Bipins.AI.csproj package NSwag.CodeGeneration.CSharp --version 14.0.7
dotnet add src/Bipins.AI/Bipins.AI.csproj package Scriban --version 5.9.1

# Build to verify
dotnet build src/Bipins.AI/Bipins.AI.csproj

# Success! Now continue with Cursor AI implementation
```

### Option 2: Skip Package Addition for Now

If you want to review the structure first without building:
1. Review all created files
2. Read the documentation
3. Plan your implementation approach
4. Add packages when ready to implement

## ?? Next Steps After Packages Are Added

Follow this sequence using Cursor AI:

1. **Phase 2**: Implement `OpenApiParser.cs` and `TypeMapper.cs`
2. **Phase 3**: Create model templates and `ModelGenerator.cs`
3. **Phase 4**: Create client templates and `ClientGenerator.cs`
4. **Phase 5**: Create auth templates and `AuthGenerator.cs`
5. **Phase 6**: Implement `FileWriter.cs`
6. **Phase 7**: Integrate everything into `SwaggerClientGeneratorTool.cs`
7. **Phase 8**: Write unit and integration tests
8. **Phase 9**: Create sample project
9. **Phase 10**: Complete documentation

## ?? Implementation Progress

```
? Phase 1: Core Infrastructure (Complete)
? Phase 2: Add NuGet Packages (YOU ARE HERE)
? Phase 3-10: Implementation with Cursor AI
```

## ?? Pro Tip

Use Cursor AI Chat with this exact prompt after adding packages:

```
I've added the required NuGet packages. Now implement OpenApiParser.cs 
in src/Bipins.AI/Agents/Tools/CodeGen/ following the IOpenApiParser interface.

Requirements:
- Use Microsoft.OpenApi.Readers.OpenApiStringReader
- Inject IHttpClientFactory and ILogger in constructor
- Implement ParseAsync to fetch and parse from URL
- Implement ParseFromContentAsync to parse from string
- Handle both JSON and YAML formats
- Support OpenAPI 2.0 and 3.0
- Add comprehensive error handling and logging

Reference existing code patterns from:
@src/Bipins.AI/Agents/Tools/BuiltIn/VectorSearchTool.cs
```

## ?? Need Help?

- **Build Issues**: Ensure packages are added to the correct project
- **Cursor AI**: Reference the Cursor AI plan for step-by-step prompts
- **Questions**: See `docs/TOOLS.md` and the tool READMEs in `src/Bipins.AI/Agents/Tools/BuiltIn/`

---

**Ready to continue?** ? Add packages ? Start Phase 2 ? Follow Cursor AI plan! ??
