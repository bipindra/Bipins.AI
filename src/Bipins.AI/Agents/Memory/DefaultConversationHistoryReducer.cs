using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents.Memory;

internal sealed class DefaultConversationHistoryReducer : IConversationHistoryReducer
{
    public IReadOnlyList<Message> Reduce(IReadOnlyList<Message> history, AgentMemoryOptions? options)
    {
        if (history.Count == 0)
        {
            return history;
        }

        var threshold = Math.Max(1, options?.HistoryReductionThreshold ?? 40);
        if (history.Count <= threshold)
        {
            return history;
        }

        var target = Math.Max(1, options?.ReducedHistoryTarget ?? 20);
        return history.TakeLast(target).ToList();
    }
}
