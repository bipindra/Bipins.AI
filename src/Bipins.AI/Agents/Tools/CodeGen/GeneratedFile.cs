namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Represents a generated code file.
/// </summary>
/// <param name="Path">Relative path for the file (e.g., "Models/User.cs").</param>
/// <param name="Content">The generated code content.</param>
/// <param name="Language">Programming language (default: "csharp").</param>
/// <param name="Description">Optional description of what this file contains.</param>
public record GeneratedFile(
    string Path,
    string Content,
    string Language = "csharp",
    string? Description = null);
