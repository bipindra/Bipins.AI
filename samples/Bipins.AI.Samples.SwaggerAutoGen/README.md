# Bipins.AI.Samples.SwaggerAutoGen

A runnable sample that generates a full .NET solution from an OpenAPI/Swagger spec: solution file, **src** (console app + class library with generated API client), and **tests** (unit + integration).

## Running the generator

From the repo root:

```bash
dotnet run --project samples/Bipins.AI.Samples.SwaggerAutoGen/src/Bipins.AI.Samples.SwaggerAutoGen/Bipins.AI.Samples.SwaggerAutoGen.csproj -- <swaggerUrl> <outputPath> [namespace] [baseUrl]
```

**Arguments:**

| Argument     | Required | Description |
|-------------|----------|-------------|
| swaggerUrl  | Yes      | URL to OpenAPI/Swagger JSON or YAML (e.g. `https://petstore.swagger.io/v2/swagger.json`) |
| outputPath  | Yes      | Directory where the solution will be generated (e.g. `./GeneratedSolution` or `/tmp/MyApi`) |
| namespace   | No       | Root namespace for generated client (default: `GeneratedApi.Client`) |
| baseUrl     | No       | API base URL used in the generated Console app and tests (default: `https://api.example.com`) |

**Example (Petstore):**

```bash
dotnet run --project samples/Bipins.AI.Samples.SwaggerAutoGen/src/Bipins.AI.Samples.SwaggerAutoGen/Bipins.AI.Samples.SwaggerAutoGen.csproj -- \
  "https://petstore.swagger.io/v2/swagger.json" \
  "/tmp/PetstoreOut" \
  "Petstore.Client" \
  "https://petstore.swagger.io/v2"
```

## Generated layout

Under `outputPath` you get:

- **`<SolutionName>.sln`** – solution with `src` and `tests` solution folders.
- **`src/<SolutionName>.Client/`** – class library with generated models, API client interfaces/implementations, and optional auth handlers.
- **`src/<SolutionName>.Console/`** – console app that references the client library and registers the generated API clients (e.g. for DI).
- **`tests/<SolutionName>.Tests.Unit/`** – unit tests for the generated client (e.g. constructor).
- **`tests/<SolutionName>.Tests.Integration/`** – integration tests (skipped by default; category `Integration`).

Solution name is derived from the namespace by removing dots (e.g. `Petstore.Client` → `PetstoreClient`).

## Building and running the generated solution

1. Open the generated solution:
   ```bash
   dotnet build /path/to/outputPath/<SolutionName>.sln
   ```

2. Run the console app:
   ```bash
   dotnet run --project /path/to/outputPath/src/<SolutionName>.Console/<SolutionName>.Console.csproj
   ```

3. Run tests:
   ```bash
   dotnet test /path/to/outputPath/<SolutionName>.sln
   ```
   Integration tests are skipped by default; run them explicitly if your environment can reach the API (e.g. filter by category `Integration`).

## Building the generator from the main solution

If the SwaggerAutoGen project is included in `Bipins.AI.sln` (under the **samples** folder), build the whole solution or only the sample:

```bash
dotnet build Bipins.AI.sln
# or
dotnet build samples/Bipins.AI.Samples.SwaggerAutoGen/src/Bipins.AI.Samples.SwaggerAutoGen/Bipins.AI.Samples.SwaggerAutoGen.csproj
```

The generator uses the Bipins.AI CodeGen pipeline (OpenAPI parsing, model/client/auth generation, file writing); the core library remains in `src/Bipins.AI`.
