namespace Marvel.Client;

/// <summary>Serializes independent row edits while the latest pending quantity wins for the same face.</summary>
public sealed class DeckQuantityEditor(DeckWorkspace workspace, LocalGameClient client, Action changed) : IDisposable
{
    private readonly Dictionary<string, long> latest = new(StringComparer.Ordinal);
    private Task<string?> tail = Task.FromResult<string?>(null);
    private CancellationTokenSource cancellation = new();
    private long nextTicket;
    private long expectedRevision;
    private int epoch;

    /// <summary>Queues one row intention without dropping edits to other rows.</summary>
    public Task<string?> SetAsync(string face, int quantity)
    {
        if (quantity is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (workspace.Revision != expectedRevision) Cancel();
        if (tail.IsCompleted) expectedRevision = workspace.Revision;
        long ticket = ++nextTicket;
        latest[face] = ticket;
        tail = ApplyAfterAsync(tail, face, quantity, ticket, epoch);
        return tail;
    }

    private async Task<string?> ApplyAfterAsync(Task previous, string face, int quantity, long ticket, int generation)
    {
        await previous;
        if (!Current(face, ticket, generation)) return null;
        if (workspace.Revision != expectedRevision) return "The draft changed. Choose the quantity again.";
        if (workspace.Draft!.Cards.Count(item => item == face) == quantity) return null;
        return await ApplyCurrentAsync(face, quantity, ticket, generation);
    }

    private async Task<string?> ApplyCurrentAsync(string face, int quantity, long ticket, int generation)
    {
        try
        {
            if (quantity == 0) workspace.ClearCards([face]);
            else
            {
                var proposal = await DeckEditProposal.CheckAsync(workspace, client, [face], quantity, cancellation.Token);
                if (!Current(face, ticket, generation)) return null;
                if (proposal?.Apply(workspace) != true) return "This quantity is not accepted for the current draft.";
            }
            expectedRevision = workspace.Revision;
            changed();
            return null;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception error) { return Current(face, ticket, generation) ? error.Message : null; }
    }

    private bool Current(string face, long ticket, int generation) => generation == epoch && latest.GetValueOrDefault(face) == ticket;

    /// <summary>Cancels pending row intentions before undo, replacement or bulk actions.</summary>
    public void Cancel()
    {
        epoch++;
        latest.Clear();
        tail = Task.FromResult<string?>(null);
        cancellation.Cancel();
        cancellation.Dispose();
        cancellation = new();
        expectedRevision = workspace.Revision;
    }

    /// <inheritdoc />
    public void Dispose() { Cancel(); cancellation.Dispose(); }
}
