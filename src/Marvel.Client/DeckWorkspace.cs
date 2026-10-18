using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>An editable draft and its revision-bound host assessment.</summary>
public sealed class DeckWorkspace(DeckCatalog catalog)
{
    /// <summary>Public admitted facts used for display and exact identity selection.</summary>
    public DeckCatalog Catalog { get; } = catalog;
    /// <summary>The currently edited value, independent of saved files and games.</summary>
    public DeckDraft? Draft { get; private set; }
    /// <summary>A local edit generation, used to reject stale read-only responses.</summary>
    public long Revision { get; private set; }
    /// <summary>The assessment for precisely the current revision, or null.</summary>
    public DeckAssessmentResult? Assessment { get; private set; }
    /// <summary>The latest library snapshot from which this draft was edited.</summary>
    public SavedDeck? Saved { get; private set; }
    /// <summary>Whether edits require a save or explicit discard.</summary>
    public bool Dirty { get; private set; }
    /// <summary>Whether the host has accepted this exact draft for construction.</summary>
    public bool CanUse => Assessment is { Size: not null, Diagnostics.Count: 0 };

    /// <summary>Starts an independent draft; this never overwrites a file.</summary>
    public void Replace(DeckDraft draft, SavedDeck? saved = null)
    {
        Draft = draft;
        Saved = saved;
        Dirty = saved is null;
        Revision++;
        Assessment = null;
    }

    /// <summary>Changes only the name and selected identity; all existing cards remain.</summary>
    public void Describe(string name, string heroCode) => Edit(new(name, heroCode, Draft!.Cards));

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
        var result = await client.ReadDecksAsync(new(hero.Key, draft.Cards), cancellationToken);
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

    /// <summary>Retains the open draft after deletion of its saved source.</summary>
    public void ForgetSaved()
    {
        Saved = null;
        Dirty = true;
    }

    private void Edit(DeckDraft draft)
    {
        Draft = draft;
        Dirty = true;
        Revision++;
        Assessment = null;
    }
}
