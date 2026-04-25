namespace Bipins.AI.SemanticKernel;

/// <summary>
/// Feature flags for Semantic Kernel integrations.
/// </summary>
public class SemanticKernelOptions
{
    public bool EnableToolCalling { get; set; }
    public bool EnablePlanner { get; set; }
    public bool EnableMemory { get; set; }
    public bool EnableChatOrchestration { get; set; }
}
