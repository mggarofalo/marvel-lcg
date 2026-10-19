using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>An editable draft and its revision-bound host assessment.</summary>
public sealed class DeckWorkspace(DeckCatalog catalog)
{
    private readonly List<DeckDraft> undo = [];
    private readonly List<DeckDraft> redo = [];
    /// <summary>Whether a local deck edit can be undone.</summary>
    public bool CanUndo => undo.Count > 0;
    /// <summary>Whether an undone local edit can be restored.</summary>
    public bool CanRedo => redo.Count > 0;

    /// <summary>Public admitted facts used for display and exact identity selection.</summary>
    public DeckCatalog Catalog { get; } = catalog;
    /// <summary>The currently edited value, independent of saved files and games.</summary>
    public DeckDraft? Draft { get; private set; }
    /// <summary>A local edit generation, used to reject stale read-only responses.</summary>
    public long Revision { get; private set; }
    /// <summary>Identifies replacement by another draft independently of local edits.</summary>
    public long Generation { get; private set; }
    /// <summary>The assessment for precisely the current revision, or null.</summary>
    public DeckAssessmentResult? Assessment { get; private set; }
    /// <summary>The latest library snapshot from which this draft was edited.</summary>
    public SavedDeck? Saved { get; private set; }
    /// <summary>Whether edits have not yet been persisted.</summary>
    public bool Dirty { get; private set; }
    /// <summary>Whether the host has accepted this exact draft for construction.</summary>
    public bool CanUse => Assessment is { Size: not null, Diagnostics.Count: 0 };

    /// <summary>Starts an independent draft; this never overwrites a file.</summary>
    public void Replace(DeckDraft draft, SavedDeck? saved = null)
    {
        Draft = draft;
        Saved = saved;
        Generation++;
        undo.Clear();
        redo.Clear();
        Dirty = saved is null;
        Revision++;
        Assessment = null;
    }

    /// <summary>Displays an untouched starting deck without inventing an edit.</summary>
    public void Initialize(DeckDraft draft)
    {
        Replace(draft);
        Dirty = false;
    }

    /// <summary>Changes only the name and selected identity; all existing cards remain.</summary>
    public void Describe(string name, string heroCode) => Edit(new(name, heroCode, Draft!.Cards));

    /// <summary>Uses the supplied identity recipe while retaining all non-signature draft cards.</summary>
    public void ChooseIdentity(string key)
    {
        var next = Catalog.Heroes.Single(hero => hero.Key == key);
        if (next.IdentityFaces.Contains(Draft!.HeroCode, StringComparer.Ordinal)) return;
        var previous = Catalog.Heroes.SingleOrDefault(hero => hero.IdentityFaces.Contains(Draft.HeroCode, StringComparer.Ordinal));
        var replaced = (previous?.Signature ?? []).Concat(next.Signature).ToHashSet(StringComparer.Ordinal);
        Edit(new(Draft.Name, next.IdentityFaces[0], next.Signature.Concat(Draft.Cards.Where(face => !replaced.Contains(face))).ToArray()));
    }

    /// <summary>Adds or removes one exact face without reproducing deckbuilding rules.</summary>
    public void ChangeCard(string face, bool add)
    {
        var cards = Draft!.Cards.ToList();
        if (add)
        {
            if (cards.Count >= MarvelCdbDeckJson.MaximumCards) throw new InvalidOperationException("Draft input limit reached.");
            cards.Add(face);
        }
        else cards.Remove(face);
        Edit(new(Draft.Name, Draft.HeroCode, cards));
    }

    /// <summary>Requests assessment and ignores a completion superseded by an edit.</summary>
    public async Task<string?> AssessAsync(LocalGameClient client, CancellationToken cancellationToken = default)
    {
        long revision = Revision;
        var draft = Draft ?? throw new InvalidOperationException("Choose or create a deck first.");
        var hero = Catalog.Heroes.SingleOrDefault(item => item.IdentityFaces.Contains(draft.HeroCode, StringComparer.Ordinal));
        if (hero is null) return "This identity is outside the host's supported Core content. Choose a Core identity; imported cards will be retained.";
        var result = await client.ReadDecksAsync(new(hero.Key, draft.Cards) { IncludeQuantities = true }, cancellationToken);
        if (revision != Revision) return null;
        if (!result.Succeeded) return result.Error!.Message;
        Assessment = result.Decks!.Assessment;
        return null;
    }

    /// <summary>Updates the saved revision only after the file replacement succeeds.</summary>
    public void Save(LocalDeckStore store)
    {
        Saved = store.Save(Draft!, Saved?.Id, Saved?.Revision ?? 0);
        Dirty = false;
    }

    /// <summary>Persists edits before leaving a draft, without inventing a file for an untouched preview.</summary>
    public void Preserve(LocalDeckStore store)
    {
        if (!Dirty) return;
        if (string.IsNullOrWhiteSpace(Draft!.Name)) Describe("Untitled deck", Draft.HeroCode);
        Save(store);
    }

    /// <summary>Retains a deleted source in memory; only a further edit or explicit save writes it again.</summary>
    public void ForgetSaved()
    {
        Saved = null;
        Dirty = false;
    }

    /// <summary>Commits a captured bulk edit only to its originating draft revision.</summary>
    public bool Apply(DeckDraft draft, long revision)
    {
        if (revision != Revision) return false;
        Edit(draft);
        return true;
    }

    /// <summary>Clears selected optional cards, restoring the supplied signature quantities.</summary>
    public void ClearCards(IEnumerable<string> faces)
    {
        var selected = faces.ToHashSet(StringComparer.Ordinal);
        var signature = Catalog.Heroes.FirstOrDefault(hero => hero.IdentityFaces.Contains(Draft!.HeroCode))?.Signature ?? [];
        Edit(new(Draft!.Name, Draft.HeroCode, Draft.Cards.Where(face => !selected.Contains(face))
            .Concat(signature.Where(selected.Contains)).ToArray()));
    }

    /// <summary>Repairs an imported signature recipe while retaining every optional card.</summary>
    public void RestoreRequiredCards()
    {
        var signature = Catalog.Heroes.FirstOrDefault(hero => hero.IdentityFaces.Contains(Draft!.HeroCode))?.Signature;
        if (signature is null) return;
        Edit(new(Draft!.Name, Draft.HeroCode, signature.Concat(Draft.Cards.Where(face => !signature.Contains(face))).ToArray()));
    }

    /// <summary>Resets to the host-supplied required cards, including missing imported signatures.</summary>
    public void ClearDeck()
    {
        var signature = Catalog.Heroes.FirstOrDefault(hero => hero.IdentityFaces.Contains(Draft!.HeroCode))?.Signature ?? [];
        Edit(new(Draft!.Name, Draft.HeroCode, signature));
    }

    /// <summary>Restores the preceding local draft; saved files and captured seats are unchanged.</summary>
    public void Undo() => Restore(undo, redo);
    /// <summary>Restores the next local draft.</summary>
    public void Redo() => Restore(redo, undo);

    private void Restore(List<DeckDraft> source, List<DeckDraft> destination)
    {
        if (source.Count == 0) return;
        destination.Add(Draft!);
        var draft = source[^1];
        source.RemoveAt(source.Count - 1);
        SetDraft(draft);
    }

    private void Edit(DeckDraft draft)
    {
        if (Draft is { } previous)
        {
            if (previous.Name == draft.Name && previous.HeroCode == draft.HeroCode && previous.Cards.SequenceEqual(draft.Cards)) return;
            undo.Add(previous);
            if (undo.Count > 100) undo.RemoveAt(0);
        }
        redo.Clear();
        SetDraft(draft);
    }

    private void SetDraft(DeckDraft draft)
    {
        Draft = draft;
        Dirty = true;
        Revision++;
        Assessment = null;
    }
}
