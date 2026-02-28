namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Interface for writing generated files to the file system.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Writes a collection of generated files to the specified output path.
    /// </summary>
    /// <param name="outputPath">Base output directory path.</param>
    /// <param name="files">Collection of files to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of successfully written file paths.</returns>
    Task<List<string>> WriteAllAsync(
        string outputPath,
        IEnumerable<GeneratedFile> files,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a single generated file to the specified output path.
    /// </summary>
    /// <param name="outputPath">Base output directory path.</param>
    /// <param name="file">File to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full path to the written file.</returns>
    Task<string> WriteAsync(
        string outputPath,
        GeneratedFile file,
        CancellationToken cancellationToken = default);
}
