using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Retains authorized forced receipts until an explicit presentation or engine commitment acknowledges them.</summary>
internal sealed class ForcedAbilityQueue
{
    private readonly Queue<ForcedAbilityNotification> pending = new();
    private readonly HashSet<(long Revision, int Index)> seen = [];
    private string? game;
    private long requiredRevision = -1;
    internal ForcedAbilityNotification? Current => pending.TryPeek(out var value) ? value : null;
    internal int Count => pending.Count;

    internal void Record(string gameId, long revision, IReadOnlyList<EventPresentation> events, bool accepted, bool reset)
    {
        if (game != gameId || reset)
        {
            Clear();
            game = gameId;
        }
        if (!accepted) return;
        if (requiredRevision >= 0 && revision > requiredRevision) Acknowledge();
        for (int index = 0; index < events.Count; index++)
            if (events[index].ForcedAbilityNotification is { } notification && seen.Add((revision, index)))
                pending.Enqueue(notification);
    }

    internal ForcedAbilityNotification? RequiredDecision(Prompt? prompt, long revision)
    {
        if (Current is not { Completed: false } current || prompt is not { Cancellable: false }
            || !prompt.ContextCardIds.Contains(current.SourceId))
        {
            requiredRevision = -1;
            return null;
        }
        requiredRevision = revision;
        return current;
    }

    internal void Acknowledge()
    {
        if (pending.Count > 0) pending.Dequeue();
        requiredRevision = -1;
    }

    internal void Clear()
    {
        pending.Clear();
        seen.Clear();
        game = null;
        requiredRevision = -1;
    }
}
