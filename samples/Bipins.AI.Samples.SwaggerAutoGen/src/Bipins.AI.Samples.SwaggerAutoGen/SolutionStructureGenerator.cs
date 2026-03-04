using System.Text;
using Bipins.AI.Agents.Tools.CodeGen;
using Microsoft.OpenApi.Models;

namespace Bipins.AI.Samples.SwaggerAutoGen;

/// <summary>
/// Generates a full solution structure: .sln, src (Console + Client), tests (Unit + Integration).
/// Client content is already written by the CodeGen pipeline; this adds .csproj files, Console Program.cs, and test projects.
/// </summary>
public class SolutionStructureGenerator
{
    private const string ProjectTypeGuidCsharp = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
    private const string SolutionFolderGuid = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

    /// <summary>
    /// Generates the complete solution structure under outputPath.
    /// </summary>
    /// <param name="outputPath">Root path for the generated solution (e.g. ./GeneratedSolution).</param>
    /// <param name="solutionName">Solution and project name prefix (e.g. "PetstoreClient").</param>
    /// <param name="namespaceName">Root namespace for the client (e.g. "Petstore.Client").</param>
    /// <param name="baseUrl">Base URL for the API (used in Program.cs and integration tests).</param>
    /// <param name="document">Parsed OpenAPI document (used to discover client names and operations).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task GenerateAsync(
        string outputPath,
        string solutionName,
        string namespaceName,
        string baseUrl,
        OpenApiDocument document,
        CancellationToken cancellationToken = default)
    {
        outputPath = Path.GetFullPath(outputPath);
        var clientTags = GetClientTags(document);

        // 1. Solution file
        var slnPath = Path.Combine(outputPath, $"{solutionName}.sln");
        Directory.CreateDirectory(outputPath);
        await WriteSolutionAsync(slnPath, solutionName, outputPath, cancellationToken);

        // 2. Client project already has generated .cs files; add .csproj
        var clientDir = Path.Combine(outputPath, "src", $"{solutionName}.Client");
        await WriteClientCsprojAsync(clientDir, solutionName, cancellationToken);

        // 3. Console project
        var consoleDir = Path.Combine(outputPath, "src", $"{solutionName}.Console");
        Directory.CreateDirectory(consoleDir);
        await WriteConsoleCsprojAsync(consoleDir, solutionName, cancellationToken);
        await WriteConsoleProgramAsync(consoleDir, solutionName, namespaceName, baseUrl, clientTags, cancellationToken);

        // 4. Unit test project
        var unitTestDir = Path.Combine(outputPath, "tests", $"{solutionName}.Tests.Unit");
        Directory.CreateDirectory(unitTestDir);
        await WriteUnitTestCsprojAsync(unitTestDir, solutionName, outputPath, cancellationToken);
        await WriteUnitTestsAsync(unitTestDir, solutionName, namespaceName, clientTags, cancellationToken);

        // 5. Integration test project
        var integrationTestDir = Path.Combine(outputPath, "tests", $"{solutionName}.Tests.Integration");
        Directory.CreateDirectory(integrationTestDir);
        await WriteIntegrationTestCsprojAsync(integrationTestDir, solutionName, outputPath, cancellationToken);
        await WriteIntegrationTestsAsync(integrationTestDir, solutionName, namespaceName, baseUrl, clientTags, cancellationToken);
    }

    private static List<string> GetClientTags(OpenApiDocument document)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (document.Paths == null) return tags.ToList();
        foreach (var path in document.Paths.Values)
        {
            foreach (var op in path.Operations.Values)
            {
                var tag = op.Tags?.FirstOrDefault()?.Name ?? "Default";
                tags.Add(tag);
            }
        }
        return tags.OrderBy(t => t).ToList();
    }

    private async Task WriteSolutionAsync(string slnPath, string solutionName, string outputPath, CancellationToken ct)
    {
        var guidConsole = Guid.NewGuid();
        var guidClient = Guid.NewGuid();
        var guidUnit = Guid.NewGuid();
        var guidIntegration = Guid.NewGuid();
        var guidSrc = Guid.NewGuid();
        var guidTests = Guid.NewGuid();

        var relSrc = Path.GetRelativePath(Path.GetDirectoryName(slnPath)!, Path.Combine(outputPath, "src"));
        var relTests = Path.GetRelativePath(Path.GetDirectoryName(slnPath)!, Path.Combine(outputPath, "tests"));
        relSrc = relSrc.Replace('\\', '\\');
        relTests = relTests.Replace('\\', '\\');

        var sb = new StringBuilder();
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio Version 17");
        sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
        sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
        sb.AppendLine($"Project(\"{{{SolutionFolderGuid}}}\") = \"src\", \"src\", \"{{{guidSrc}}}\"");
        sb.AppendLine("EndProject");
        sb.AppendLine($"Project(\"{{{SolutionFolderGuid}}}\") = \"tests\", \"tests\", \"{{{guidTests}}}\"");
        sb.AppendLine("EndProject");
        sb.AppendLine($"Project(\"{{{ProjectTypeGuidCsharp}}}\") = \"{solutionName}.Console\", \"{relSrc}\\{solutionName}.Console\\{solutionName}.Console.csproj\", \"{{{guidConsole}}}\"");
        sb.AppendLine("EndProject");
        sb.AppendLine($"Project(\"{{{ProjectTypeGuidCsharp}}}\") = \"{solutionName}.Client\", \"{relSrc}\\{solutionName}.Client\\{solutionName}.Client.csproj\", \"{{{guidClient}}}\"");
        sb.AppendLine("EndProject");
        sb.AppendLine($"Project(\"{{{ProjectTypeGuidCsharp}}}\") = \"{solutionName}.Tests.Unit\", \"{relTests}\\{solutionName}.Tests.Unit\\{solutionName}.Tests.Unit.csproj\", \"{{{guidUnit}}}\"");
        sb.AppendLine("EndProject");
        sb.AppendLine($"Project(\"{{{ProjectTypeGuidCsharp}}}\") = \"{solutionName}.Tests.Integration\", \"{relTests}\\{solutionName}.Tests.Integration\\{solutionName}.Tests.Integration.csproj\", \"{{{guidIntegration}}}\"");
        sb.AppendLine("EndProject");
        sb.AppendLine("Global");
        sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        sb.AppendLine($"\t\t{{{guidConsole}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
        sb.AppendLine($"\t\t{{{guidConsole}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
        sb.AppendLine($"\t\t{{{guidConsole}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
        sb.AppendLine($"\t\t{{{guidConsole}}}.Release|Any CPU.Build.0 = Release|Any CPU");
        sb.AppendLine($"\t\t{{{guidClient}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
        sb.AppendLine($"\t\t{{{guidClient}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
        sb.AppendLine($"\t\t{{{guidClient}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
        sb.AppendLine($"\t\t{{{guidClient}}}.Release|Any CPU.Build.0 = Release|Any CPU");
        sb.AppendLine($"\t\t{{{guidUnit}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
        sb.AppendLine($"\t\t{{{guidUnit}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
        sb.AppendLine($"\t\t{{{guidUnit}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
        sb.AppendLine($"\t\t{{{guidUnit}}}.Release|Any CPU.Build.0 = Release|Any CPU");
        sb.AppendLine($"\t\t{{{guidIntegration}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
        sb.AppendLine($"\t\t{{{guidIntegration}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
        sb.AppendLine($"\t\t{{{guidIntegration}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
        sb.AppendLine($"\t\t{{{guidIntegration}}}.Release|Any CPU.Build.0 = Release|Any CPU");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
        sb.AppendLine("\t\tHideSolutionNode = FALSE");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(NestedProjects) = preSolution");
        sb.AppendLine($"\t\t{{{guidConsole}}} = {{{guidSrc}}}");
        sb.AppendLine($"\t\t{{{guidClient}}} = {{{guidSrc}}}");
        sb.AppendLine($"\t\t{{{guidUnit}}} = {{{guidTests}}}");
        sb.AppendLine($"\t\t{{{guidIntegration}}} = {{{guidTests}}}");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("EndGlobal");

        await File.WriteAllTextAsync(slnPath, sb.ToString(), ct);
    }

    private async Task WriteClientCsprojAsync(string clientDir, string solutionName, CancellationToken ct)
    {
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>{solutionName}.Client</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.Extensions.Http"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Abstractions"" Version=""8.0.0"" />
    <PackageReference Include=""System.Net.Http.Json"" Version=""8.0.0"" />
  </ItemGroup>
</Project>
";
        Directory.CreateDirectory(clientDir);
        await File.WriteAllTextAsync(Path.Combine(clientDir, $"{solutionName}.Client.csproj"), content, ct);
    }

    private async Task WriteConsoleCsprojAsync(string consoleDir, string solutionName, CancellationToken ct)
    {
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>{solutionName}.Console</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\{solutionName}.Client\{solutionName}.Client.csproj"" />
    <PackageReference Include=""Microsoft.Extensions.Hosting"" Version=""8.0.0"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(Path.Combine(consoleDir, $"{solutionName}.Console.csproj"), content, ct);
    }

    private async Task WriteConsoleProgramAsync(string consoleDir, string solutionName, string namespaceName, string baseUrl, List<string> clientTags, CancellationToken ct)
    {
        var firstClient = clientTags.Count > 0 ? TypeMapper.ToPascalCase(clientTags[0]) + "Client" : "DefaultClient";
        var ns = namespaceName;
        var content = $@"using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using {ns};
using {ns}.Clients;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {{
        services.AddApiClients(""{baseUrl}"");
    }})
    .Build();

var client = host.Services.GetRequiredService<I{firstClient}>();
Console.WriteLine(""API client resolved. Base URL: {baseUrl}"");
Console.WriteLine(""Client type: I{firstClient}. Add your API calls in Program.cs."");
";
        await File.WriteAllTextAsync(Path.Combine(consoleDir, "Program.cs"), content, ct);
    }

    private async Task WriteUnitTestCsprojAsync(string unitTestDir, string solutionName, string outputPath, CancellationToken ct)
    {
        var relToClient = Path.GetRelativePath(unitTestDir, Path.Combine(outputPath, "src", $"{solutionName}.Client"));
        relToClient = relToClient.Replace('\\', '/');
        if (!relToClient.StartsWith(".")) relToClient = "./" + relToClient;
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>{solutionName}.Tests.Unit</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.8.0"" />
    <PackageReference Include=""xunit"" Version=""2.9.3"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.3"" />
    <PackageReference Include=""Moq"" Version=""4.20.70"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""{relToClient}\{solutionName}.Client.csproj"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(Path.Combine(unitTestDir, $"{solutionName}.Tests.Unit.csproj"), content, ct);
    }

    private async Task WriteUnitTestsAsync(string unitTestDir, string solutionName, string namespaceName, List<string> clientTags, CancellationToken ct)
    {
        var ns = namespaceName;
        var sb = new StringBuilder();
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using Microsoft.Extensions.Logging.Abstractions;");
        sb.AppendLine($"using {ns}.Clients;");
        sb.AppendLine("using Moq;");
        sb.AppendLine("using Xunit;");
        sb.AppendLine();
        sb.AppendLine($"namespace {solutionName}.Tests.Unit;");
        sb.AppendLine();
        sb.AppendLine("public class GeneratedClientTests");
        sb.AppendLine("{");
        if (clientTags.Count > 0)
        {
            var clientName = TypeMapper.ToPascalCase(clientTags[0]) + "Client";
            sb.AppendLine($"    [Fact]");
            sb.AppendLine($"    public void {clientName}_CanBeConstructed()");
            sb.AppendLine("    {");
            sb.AppendLine("        var httpClient = new HttpClient();");
            sb.AppendLine($"        var logger = NullLogger<{clientName}>.Instance;");
            sb.AppendLine($"        var client = new {clientName}(httpClient, logger);");
            sb.AppendLine("        Assert.NotNull(client);");
            sb.AppendLine("    }");
        }
        sb.AppendLine("}");
        await File.WriteAllTextAsync(Path.Combine(unitTestDir, "GeneratedClientTests.cs"), sb.ToString(), ct);
    }

    private async Task WriteIntegrationTestCsprojAsync(string integrationTestDir, string solutionName, string outputPath, CancellationToken ct)
    {
        var relToClient = Path.GetRelativePath(integrationTestDir, Path.Combine(outputPath, "src", $"{solutionName}.Client"));
        relToClient = relToClient.Replace('\\', '/');
        if (!relToClient.StartsWith(".")) relToClient = "./" + relToClient;
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>{solutionName}.Tests.Integration</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.8.0"" />
    <PackageReference Include=""xunit"" Version=""2.9.3"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.3"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""{relToClient}\{solutionName}.Client.csproj"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(Path.Combine(integrationTestDir, $"{solutionName}.Tests.Integration.csproj"), content, ct);
    }

    private async Task WriteIntegrationTestsAsync(string integrationTestDir, string solutionName, string namespaceName, string baseUrl, List<string> clientTags, CancellationToken ct)
    {
        var ns = namespaceName;
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using Microsoft.Extensions.Logging.Abstractions;");
        sb.AppendLine($"using {ns}.Clients;");
        sb.AppendLine("using Xunit;");
        sb.AppendLine();
        sb.AppendLine($"namespace {solutionName}.Tests.Integration;");
        sb.AppendLine();
        sb.AppendLine("[Trait(\"Category\", \"Integration\")]");
        sb.AppendLine("public class GeneratedClientIntegrationTests");
        sb.AppendLine("{");
        sb.AppendLine($"    private const string BaseUrl = \"{baseUrl}\";");
        sb.AppendLine();
        if (clientTags.Count > 0)
        {
            var clientName = TypeMapper.ToPascalCase(clientTags[0]) + "Client";
            sb.AppendLine("    [Fact(Skip = \"Requires running API. Remove Skip and call a method on the client to run.\")]");
            sb.AppendLine($"    public void {clientName}_CanBeConstructed_WithHttpClient()");
            sb.AppendLine("    {");
            sb.AppendLine("        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };");
            sb.AppendLine($"        var logger = NullLogger<{clientName}>.Instance;");
            sb.AppendLine($"        var client = new {clientName}(httpClient, logger);");
            sb.AppendLine("        Assert.NotNull(client);");
            sb.AppendLine("    }");
        }
        sb.AppendLine("}");
        await File.WriteAllTextAsync(Path.Combine(integrationTestDir, "GeneratedClientIntegrationTests.cs"), sb.ToString(), ct);
    }
}
