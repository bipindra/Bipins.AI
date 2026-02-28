using Microsoft.OpenApi.Models;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Interface for generating API client classes from OpenAPI operations.
/// </summary>
public interface IClientGenerator
{
    /// <summary>
    /// Generates API client classes from an OpenAPI document.
    /// </summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="namespaceName">Root namespace for generated clients.</param>
    /// <param name="options">Generator options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of generated client files.</returns>
    Task<List<GeneratedFile>> GenerateAsync(
        OpenApiDocument document,
        string namespaceName,
        GeneratorOptions options,
        CancellationToken cancellationToken = default);
}
