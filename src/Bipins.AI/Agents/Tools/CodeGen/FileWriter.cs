using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Writes generated code files to the file system.
/// </summary>
public class FileWriter : IFileWriter
{
    private readonly ILogger<FileWriter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileWriter"/> class.
    /// </summary>
    public FileWriter(ILogger<FileWriter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<string>> WriteAllAsync(
        string outputPath,
        IEnumerable<GeneratedFile> files,
        CancellationToken cancellationToken = default)
    {
        var writtenPaths = new List<string>();

        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            _logger.LogInformation("Created output directory: {Path}", outputPath);
        }

        foreach (var file in files)
        {
            try
            {
                var filePath = await WriteAsync(outputPath, file, cancellationToken);
                writtenPaths.Add(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file {Path}", file.Path);
            }
        }

        _logger.LogInformation("Successfully wrote {Count} files to {Path}", writtenPaths.Count, outputPath);
        return writtenPaths;
    }

    /// <inheritdoc />
    public async Task<string> WriteAsync(
        string outputPath,
        GeneratedFile file,
        CancellationToken cancellationToken = default)
    {
        // Combine paths
        var fullPath = Path.Combine(outputPath, file.Path);
        var directory = Path.GetDirectoryName(fullPath);

        // Create subdirectories if needed
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Created directory: {Directory}", directory);
        }

        // Check if file exists
        if (File.Exists(fullPath))
        {
            _logger.LogWarning("Overwriting existing file: {Path}", fullPath);
        }

        // Write content
#if NETSTANDARD2_1
        await Task.Run(() => File.WriteAllText(fullPath, file.Content, System.Text.Encoding.UTF8), cancellationToken);
#else
        await File.WriteAllTextAsync(fullPath, file.Content, System.Text.Encoding.UTF8, cancellationToken);
#endif

        _logger.LogDebug("Wrote file: {Path} ({Length} characters)", fullPath, file.Content.Length);
        return fullPath;
    }
}
