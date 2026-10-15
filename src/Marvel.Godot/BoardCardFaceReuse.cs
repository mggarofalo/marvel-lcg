using Godot;

namespace Marvel.Godot;

/// <summary>Transfers matching passive faces only between adjacent renders of one table.</summary>
internal sealed class BoardCardFaceReuse : IDisposable
{
    private readonly Dictionary<CardFaceReuseKey, Queue<(Control Body, ICardArtProvider? Art)>> faces = [];

    internal BoardCardFaceReuse(BoardRenderResult? previous)
    {
        if (previous is null) return;
        foreach (CardControl card in previous.VisibleCardControls())
        {
            if (card.FaceReuseKey is not { } key
                || card.GetNodeOrNull<Control>("CardSurface/CardFace") is not { } body
                || !body.IsNodeReady()) continue;
            body.GetParent().RemoveChild(body);
            if (!faces.TryGetValue(key, out var matches)) faces.Add(key, matches = new());
            matches.Enqueue((body, card.FaceArt));
        }
    }

    internal Control? Take(CardFaceReuseKey key, ICardArtProvider? art)
    {
        if (!faces.TryGetValue(key, out var matches)) return null;
        while (matches.TryDequeue(out var candidate))
        {
            // Faces are deliberately outside the tree during this bounded transfer.
            if (ReferenceEquals(candidate.Art, art) && GodotObject.IsInstanceValid(candidate.Body)
                && !candidate.Body.IsQueuedForDeletion())
                return candidate.Body;
            candidate.Body.QueueFree();
        }
        return null;
    }

    public void Dispose()
    {
        foreach (var matches in faces.Values)
            while (matches.TryDequeue(out var candidate)) candidate.Body.QueueFree();
        faces.Clear();
    }
}
