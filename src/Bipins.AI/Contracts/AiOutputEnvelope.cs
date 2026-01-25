using System.Text.Json;

namespace Bipins.AI.Core.Contracts;

/// <summary>
/// Universal output envelope for AI operations.
/// </summary>
/// <param name="Status">The operation status (Success, Error, Partial).</param>
/// <param name="ResultType">The type of result (e.g., "chat", "embed", "query").</param>
/// <param name="Data">The result data as JSON.</param>
/// <param name="Citations">List of citations from retrieved documents.</param>
/// <param name="Telemetry">Model usage telemetry.</param>
/// <param name="Errors">List of error messages if any.</param>
public record AiOutputEnvelope(
    OutputStatus Status,
    string ResultType,
    JsonElement? Data = null,
    List<Citation>? Citations = null,
    ModelTelemetry? Telemetry = null,
    List<string>? Errors = null);
