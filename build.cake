#tool nuget:?package=NuGet.CommandLine&version=6.5.0
#addin nuget:?package=Cake.FileHelpers&version=5.0.0

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var version = Argument("version", "1.0.0");
var skipTests = Argument("skipTests", false);
var publishPackages = Argument("publishPackages", false);
var packageSource = Argument("packageSource", EnvironmentVariable("PACKAGE_SOURCE_URL") ?? "https://api.nuget.org/v3/index.json");
var packageApiKey = Argument("packageApiKey", EnvironmentVariable("PACKAGE_API_KEY") ?? "");

var solutionPath = "./Bipins.AI.sln";
var artifactsDir = "./artifacts";
var testResultsDir = "./test-results";
var coverageDir = "./coverage";

// Directories
var srcDir = "./src";
var testsDir = "./tests";

// Cleanup
Task("Clean")
    .Does(() =>
{
    Information("Cleaning artifacts and build outputs...");
    
    CleanDirectories(new[] {
        artifactsDir,
        testResultsDir,
        coverageDir,
        "**/bin",
        "**/obj"
    });
    
    Information("Clean completed.");
});

// Restore NuGet packages
Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    Information("Restoring NuGet packages...");
    
    DotNetRestore(solutionPath);
    
    Information("Restore completed.");
});

// Build solution
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information($"Building solution in {configuration} configuration...");
    
    var buildSettings = new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
        Verbosity = DotNetVerbosity.Minimal
    };
    
    DotNetBuild(solutionPath, buildSettings);
    
    Information("Build completed.");
});

// Run unit tests (excluding integration tests)
Task("Test")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests)
    .Does(() =>
{
    Information("Running unit tests (excluding integration tests)...");
    
    var testSettings = new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        Verbosity = DotNetVerbosity.Normal,
        Filter = "FullyQualifiedName!~IntegrationTests",
        ResultsDirectory = testResultsDir,
        Loggers = new[] { "trx;LogFileName=test-results.trx", "console;verbosity=normal" },
        Collectors = new[] { "XPlat Code Coverage" },
        Settings = new FilePath("./coverlet.runsettings")
    };
    
    var testProjects = GetFiles($"{testsDir}/**/*.csproj")
        .Where(f => !f.FullPath.Contains("IntegrationTests"));
    
    foreach (var project in testProjects)
    {
        Information($"Running tests in {project.GetFilename()}");
        DotNetTest(project.FullPath, testSettings);
    }
    
    Information("Tests completed.");
});

// Pack NuGet package
Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Packing NuGet package...");
    
    EnsureDirectoryExists(artifactsDir);
    
    var packSettings = new DotNetPackSettings
    {
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        OutputDirectory = artifactsDir,
        Verbosity = DotNetVerbosity.Minimal
    };
    
    // Pack only the main Bipins.AI package
    var packageProject = $"{srcDir}/Bipins.AI/Bipins.AI.csproj";
    
    if (!FileExists(packageProject))
    {
        throw new FileNotFoundException($"Package project not found: {packageProject}");
    }
    
    DotNetPack(packageProject, packSettings);
    
    var packages = GetFiles($"{artifactsDir}/*.nupkg");
    foreach (var package in packages)
    {
        Information($"Created package: {package.GetFilename()} ({GetFileSize(package)} bytes)");
    }
    
    Information("Pack completed.");
});

// Publish packages
Task("Publish")
    .IsDependentOn("Pack")
    .WithCriteria(publishPackages)
    .Does(() =>
{
    if (string.IsNullOrEmpty(packageApiKey))
    {
        throw new Exception("PACKAGE_API_KEY environment variable or --packageApiKey argument is required for publishing.");
    }
    
    Information($"Publishing packages to {packageSource}...");
    
    var packages = GetFiles($"{artifactsDir}/*.nupkg");
    
    foreach (var package in packages)
    {
        Information($"Publishing {package.GetFilename()}...");
        
        var pushSettings = new DotNetNuGetPushSettings
        {
            Source = packageSource,
            ApiKey = packageApiKey,
            SkipDuplicate = true
        };
        
        DotNetNuGetPush(package.FullPath, pushSettings);
        
        Information($"Published {package.GetFilename()}");
    }
    
    Information("Publish completed.");
});

// Default task
Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

// Full CI pipeline
Task("CI")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

// Full release pipeline
Task("Release")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack")
    .IsDependentOn("Publish");

RunTarget(target);
