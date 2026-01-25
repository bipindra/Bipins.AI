namespace AICloudCostOptimizationAdvisor.Shared.Models;

/// <summary>
/// Security risk identified in the Terraform infrastructure.
/// </summary>
public class SecurityRisk
{
    /// <summary>
    /// Severity level (Critical, High, Medium, Low).
    /// </summary>
    public string Severity { get; set; } = "Medium";

    /// <summary>
    /// Category of security risk (Access Control, Encryption, Network, Compliance, etc.).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description of the security risk.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Specific security issue identified.
    /// </summary>
    public string Issue { get; set; } = string.Empty;

    /// <summary>
    /// Recommended remediation steps.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Related resource IDs or services affected.
    /// </summary>
    public List<string> RelatedResources { get; set; } = new();

    /// <summary>
    /// Cloud provider this risk applies to.
    /// </summary>
    public string CloudProvider { get; set; } = string.Empty;

    /// <summary>
    /// Compliance frameworks affected (e.g., "PCI-DSS", "HIPAA", "SOC 2").
    /// </summary>
    public List<string> ComplianceFrameworks { get; set; } = new();
}
