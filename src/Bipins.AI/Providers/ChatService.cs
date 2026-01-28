using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.LLM;

public class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;
    private readonly ChatServiceOptions _options;
    private readonly ILogger<ChatService> _logger;
    
    public ChatService(ILLMProvider llmProvider, ChatServiceOptions options, ILogger<ChatService> logger)
    {
        _llmProvider = llmProvider;
        _options = options;
        _logger = logger;
    }
    
    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var request = CreateChatRequest(systemPrompt, userMessage);
        var response = await _llmProvider.ChatAsync(request, cancellationToken);
        return response.Content;
    }
    
    public async Task<ChatResponse> ChatWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default)
    {
        var request = CreateChatRequest(systemPrompt, userMessage, tools);
        return await _llmProvider.ChatAsync(request, cancellationToken);
    }
    
    public IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var request = CreateChatRequest(systemPrompt, userMessage);
        return _llmProvider.ChatStreamAsync(request, cancellationToken);
    }
    
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return await _llmProvider.GenerateEmbeddingAsync(text, cancellationToken);
    }

    private ChatRequest CreateChatRequest(string systemPrompt, string userMessage, IReadOnlyList<ToolDefinition>? tools = null)
    {
        var messages = new List<Message>
        {
            new(MessageRole.System, systemPrompt),
            new(MessageRole.User, userMessage)
        };

        var metadata = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(_options.Model))
        {
            metadata["modelId"] = _options.Model;
        }

        return new ChatRequest(
            Messages: messages,
            Tools: tools,
            Temperature: (float?)_options.Temperature,
            MaxTokens: _options.MaxTokens,
            Metadata: metadata.Count > 0 ? metadata : null);
    }
}

public class ChatServiceOptions
{
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
