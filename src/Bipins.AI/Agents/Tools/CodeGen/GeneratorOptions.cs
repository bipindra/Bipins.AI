namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Options for code generation from OpenAPI specifications.
/// </summary>
public class GeneratorOptions
{
    /// <summary>
    /// Whether to generate model classes.
    /// </summary>
    public bool GenerateModels { get; set; } = true;

    /// <summary>
    /// Whether to generate API client classes.
    /// </summary>
    public bool GenerateClients { get; set; } = true;

    /// <summary>
    /// Whether to include XML documentation comments.
    /// </summary>
    public bool IncludeXmlDocs { get; set; } = true;

    /// <summary>
    /// Whether to use nullable reference types.
    /// </summary>
    public bool UseNullableReferenceTypes { get; set; } = true;

    /// <summary>
    /// Whether to add "Async" suffix to async methods.
    /// </summary>
    public bool AsyncSuffix { get; set; } = true;

    /// <summary>
    /// Whether to generate interfaces for clients.
    /// </summary>
    public bool GenerateInterfaces { get; set; } = true;

    /// <summary>
    /// Whether to generate dependency injection setup.
    /// </summary>
    public bool GenerateDependencyInjection { get; set; } = true;

    /// <summary>
    /// Whether to generate authentication handlers.
    /// </summary>
    public bool GenerateAuthentication { get; set; } = true;

    /// <summary>
    /// Whether to add retry policies.
    /// </summary>
    public bool IncludeResiliencePolicies { get; set; } = true;
}
