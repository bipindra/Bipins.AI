using Microsoft.OpenApi.Models;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Interface for generating model/DTO classes from OpenAPI schemas.
/// </summary>
public interface IModelGenerator
{
    /// <summary>
    /// Generates model classes from an OpenAPI document.
    /// </summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="namespaceName">Root namespace for generated models.</param>
    /// <param name="options">Generator options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of generated model files.</returns>
    Task<List<GeneratedFile>> GenerateAsync(
        OpenApiDocument document,
        string namespaceName,
        GeneratorOptions options,
        CancellationToken cancellationToken = default);
}
