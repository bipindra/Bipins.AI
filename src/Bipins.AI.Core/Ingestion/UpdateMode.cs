namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Mode for document updates.
/// </summary>
public enum UpdateMode
{
    /// <summary>
    /// Create new version only, do not delete old versions.
    /// </summary>
    Create,

    /// <summary>
    /// Update existing document by replacing old version.
    /// </summary>
    Update,

    /// <summary>
    /// Upsert - create if not exists, update if exists.
    /// </summary>
    Upsert
}
