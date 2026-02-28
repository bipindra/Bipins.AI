using Microsoft.OpenApi.Models;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Interface for generating authentication handlers from OpenAPI security schemes.
/// </summary>
public interface IAuthGenerator
{
    /// <summary>
    /// Generates authentication handler classes from an OpenAPI document.
    /// </summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="namespaceName">Root namespace for generated auth handlers.</param>
    /// <param name="options">Generator options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of generated auth handler files.</returns>
    Task<List<GeneratedFile>> GenerateAsync(
        OpenApiDocument document,
        string namespaceName,
        GeneratorOptions options,
        CancellationToken cancellationToken = default);
}
