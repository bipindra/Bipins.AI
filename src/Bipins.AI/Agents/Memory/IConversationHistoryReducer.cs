using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents.Memory;

public interface IConversationHistoryReducer
{
    IReadOnlyList<Message> Reduce(IReadOnlyList<Message> history, AgentMemoryOptions? options);
}
