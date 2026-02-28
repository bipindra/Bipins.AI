using Microsoft.OpenApi.Models;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Interface for parsing OpenAPI/Swagger specifications.
/// </summary>
public interface IOpenApiParser
{
    /// <summary>
    /// Parses an OpenAPI document from a URL.
    /// </summary>
    /// <param name="url">URL to the OpenAPI specification (JSON or YAML).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed OpenAPI document.</returns>
    Task<OpenApiDocument> ParseAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses an OpenAPI document from raw content.
    /// </summary>
    /// <param name="content">OpenAPI specification content (JSON or YAML).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed OpenAPI document.</returns>
    Task<OpenApiDocument> ParseFromContentAsync(string content, CancellationToken cancellationToken = default);
}
