using Bipins.AI.Agents.Tools.CodeGen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Samples.SwaggerAutoGen;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Bipins.AI.Samples.SwaggerAutoGen <swaggerUrl> <outputPath> [namespace] [baseUrl]");
            Console.WriteLine("  swaggerUrl  - URL to OpenAPI/Swagger JSON or YAML (e.g. https://petstore.swagger.io/v2/swagger.json)");
            Console.WriteLine("  outputPath  - Directory where the solution will be generated (e.g. ./GeneratedSolution)");
            Console.WriteLine("  namespace   - Optional. Root namespace for generated client (default: GeneratedApi.Client)");
            Console.WriteLine("  baseUrl     - Optional. API base URL for Program.cs and tests (default: https://api.example.com)");
            return 1;
        }

        var swaggerUrl = args[0].Trim();
        var outputPath = Path.GetFullPath(args[1].Trim());
        var namespaceName = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]) ? args[2].Trim() : "GeneratedApi.Client";
        var baseUrl = args.Length > 3 && !string.IsNullOrWhiteSpace(args[3]) ? args[3].Trim() : "https://api.example.com";

        var solutionName = string.IsNullOrEmpty(namespaceName) ? "GeneratedApi" : namespaceName.Replace(".", "");
        if (string.IsNullOrWhiteSpace(solutionName)) solutionName = "GeneratedApi";

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddHttpClient();
        services.AddSingleton<IOpenApiParser, OpenApiParser>();
        services.AddSingleton<IModelGenerator, ModelGenerator>();
        services.AddSingleton<IClientGenerator, ClientGenerator>();
        services.AddSingleton<IAuthGenerator, AuthGenerator>();
        services.AddSingleton<IFileWriter, FileWriter>();

        await using var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SwaggerAutoGen");

        try
        {
            logger.LogInformation("Parsing OpenAPI from {Url}...", swaggerUrl);
            var parser = sp.GetRequiredService<IOpenApiParser>();
            var document = await parser.ParseAsync(swaggerUrl);

            var clientDir = Path.Combine(outputPath, "src", $"{solutionName}.Client");
            Directory.CreateDirectory(clientDir);

            var options = new GeneratorOptions();
            var generatedFiles = new List<GeneratedFile>();

            var modelGenerator = sp.GetRequiredService<IModelGenerator>();
            var modelFiles = await modelGenerator.GenerateAsync(document, namespaceName, options);
            generatedFiles.AddRange(modelFiles);

            var clientGenerator = sp.GetRequiredService<IClientGenerator>();
            var clientFiles = await clientGenerator.GenerateAsync(document, namespaceName, options);
            generatedFiles.AddRange(clientFiles);

            if (document.Components?.SecuritySchemes?.Count > 0)
            {
                var authGenerator = sp.GetRequiredService<IAuthGenerator>();
                var authFiles = await authGenerator.GenerateAsync(document, namespaceName, options);
                generatedFiles.AddRange(authFiles);
            }

            var fileWriter = sp.GetRequiredService<IFileWriter>();
            await fileWriter.WriteAllAsync(clientDir, generatedFiles);

            logger.LogInformation("Generating solution structure...");
            var structureGenerator = new SolutionStructureGenerator();
            await structureGenerator.GenerateAsync(outputPath, solutionName, namespaceName, baseUrl, document);

            var slnPath = Path.Combine(outputPath, $"{solutionName}.sln");
            logger.LogInformation("Done. Solution: {Path}", slnPath);
            Console.WriteLine($"Generated: {slnPath}");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generation failed");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
