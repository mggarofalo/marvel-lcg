using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>A revision-bound bulk edit checked by the host before it can be applied.</summary>
public sealed class DeckEditProposal
{
    private readonly DeckDraft draft;
    private readonly long revision;
    private DeckEditProposal(DeckDraft draft, long revision) { this.draft = draft; this.revision = revision; }

    /// <summary>Checks exact quantities, or adds one copy per selected face when quantity is null.</summary>
    public static async Task<DeckEditProposal?> CheckAsync(DeckWorkspace workspace, LocalGameClient client,
        IReadOnlyList<string> faces, int? quantity, CancellationToken cancellationToken = default)
    {
        if (faces.Count == 0) return null;
        if (quantity is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(quantity));
        long revision = workspace.Revision;
        var current = workspace.Draft!;
        var hero = workspace.Catalog.Heroes.SingleOrDefault(item => item.IdentityFaces.Contains(current.HeroCode));
        if (hero is null) return null;
        var selected = faces.Distinct(StringComparer.Ordinal).ToArray();
        var cards = current.Cards.ToList();
        foreach (string face in selected)
        {
            int count = cards.Count(item => item == face);
            int target = quantity ?? count + 1;
            while (count > target) { cards.RemoveAt(cards.LastIndexOf(face)); count--; }
            cards.AddRange(Enumerable.Repeat(face, target - count));
        }
        if (cards.Count > MarvelCdbDeckJson.MaximumCards) return null;
        var result = await client.ReadDecksAsync(new(hero.Key, cards), cancellationToken);
        if (!result.Succeeded) throw new InvalidOperationException(result.Error!.Message);
        return revision == workspace.Revision && result.Decks!.Assessment!.CanExtend
            ? new(new(current.Name, current.HeroCode, cards), revision) : null;
    }

    /// <summary>Applies once; intervening edits, undo, imports and identity changes invalidate it.</summary>
    public bool Apply(DeckWorkspace workspace) => workspace.Apply(draft, revision);
}
