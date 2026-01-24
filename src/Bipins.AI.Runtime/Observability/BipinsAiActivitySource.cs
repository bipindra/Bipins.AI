using System.Diagnostics;

namespace Bipins.AI.Runtime.Observability;

/// <summary>
/// Activity source for Bipins.AI tracing.
/// </summary>
public static class BipinsAiActivitySource
{
    /// <summary>
    /// The activity source name.
    /// </summary>
    public const string Name = "Bipins.AI";

    /// <summary>
    /// The activity source instance.
    /// </summary>
    public static readonly ActivitySource Instance = new(Name);
}
