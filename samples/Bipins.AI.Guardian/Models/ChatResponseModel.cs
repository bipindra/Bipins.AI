namespace Bipins.AI.Guardian.Models;

public class ChatResponseModel
{
    public string Response { get; set; } = string.Empty;
    public bool IsModerated { get; set; }
    public string? SafetyLevel { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public bool HasRetry { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
