using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Read-only native evidence from the same authorized response the table displays.</summary>
public sealed partial class NativeGameWitness : RefCounted
{
    private Main? table;

    /// <summary>Binds the visible table being observed.</summary>
    public void Bind(Main main) => table = main;

    /// <summary>Reports the engine's public decision purpose, without inferring it from button text.</summary>
    public string DecisionKind() => table?.CurrentGame?.Prompt?.PublicKind.ToString() ?? string.Empty;

    /// <summary>Reports the accepted response revision for an exact commitment witness.</summary>
    public long Revision() => table?.CurrentGame?.Revision ?? -1;

    /// <summary>Locates one authorized physical face in the displayed state.</summary>
    public int Anchor(string title) => table?.CurrentGame?.World?.Areas.SelectMany(area => area.Cards)
        .FirstOrDefault(card => card.Face?.Title == title)?.Id ?? -1;

    /// <summary>Reports the authorized current face of one physical card.</summary>
    public string Face(int anchor) => table?.CurrentGame?.World?.Areas.SelectMany(area => area.Cards)
        .SingleOrDefault(card => card.Id == anchor)?.Face?.Id ?? string.Empty;

    /// <summary>Locates a readable face in the requested displayed area when pile copies share its title.</summary>
    public int AreaAnchor(string title, string zone) => table?.CurrentGame?.World?.Areas
        .Where(area => area.Zone == zone).SelectMany(area => area.Cards)
        .SingleOrDefault(card => card.Face?.Title == title)?.Id ?? -1;

    /// <summary>Reports the authoritative terminal outcome.</summary>
    public string Outcome() => table?.CurrentGame?.World?.Outcome.ToString() ?? string.Empty;

    /// <summary>Reports whether the existing lifecycle currently admits a mutation.</summary>
    public bool CanResolve() => table?.lifecycle.CanResolve ?? false;

    /// <summary>Reports whether the displayed decision editor suppresses mutation controls.</summary>
    public bool SubmissionLocked() => table?.decisions.submitting ?? false;

    /// <summary>Reports the engine's repeated-target contract on the displayed prompt.</summary>
    public bool HasRepeatedTargets() => table?.CurrentGame?.Prompt?.Affordances.Any(
        offer => offer.Targets?.AllowRepeated == true) ?? false;

    /// <summary>Reports operational progress without exposing a capability.</summary>
    public string ProgressKind() => table?.lifecycle.Progress?.Kind.ToString() ?? string.Empty;

    /// <summary>Reports authorized candidates from the displayed engine prompt.</summary>
    public int[] CandidateIds() => table?.CurrentGame?.Prompt?.Affordances.SingleOrDefault()?.Targets?.Legal.ToArray() ?? [];

    /// <summary>Reports exact currently offered anchor identities, preserving engine order.</summary>
    public int[] OfferedAnchors() => table?.CurrentGame?.Prompt?.Affordances.Select(offer => offer.AnchorId).ToArray() ?? [];

    /// <summary>Reports the current draft's selected physical targets.</summary>
    public int[] SelectedTargets() => table?.decisions.composer?.Targets.ToArray() ?? [];

    /// <summary>Captures selected identities, ordered targets, costs and allocations for continuity assertions.</summary>
    public string Draft()
    {
        var draft = table?.decisions?.composer;
        if (draft is null) return "none";
        return $"{draft.Selected?.Id}|{draft.SelectedCost}|{string.Join(',', draft.Targets)}|"
            + $"{string.Join(',', draft.Resources)}|{string.Join(',', draft.Assignments)}|{string.Join(',', draft.Values)}";
    }

    /// <summary>Checks typed ownership, physical public cards and anonymous concealed entries.</summary>
    public bool PileMatches(string top, string discard, int concealed)
    {
        var world = table?.CurrentGame?.World;
        if (world is null || world.InvocationDecks.Count != 1) return false;
        var pile = world.InvocationDecks[0];
        if (pile.Owner != 0) return false;
        var deck = world.Areas.Single(area => area.Id == pile.Deck);
        var removed = world.Areas.Single(area => area.Id == pile.Discard);
        return deck.Owner == 0 && removed.Owner == 0 && PublicTopMatches(deck, top, concealed)
            && DiscardMatches(removed, discard);
    }

    private static bool PublicTopMatches(AreaDescriptor deck, string top, int concealed)
    {
        var hidden = deck.Cards.Where(card => card.Face is null).ToArray();
        return hidden.Length == concealed && hidden.All(card => card.Id is null && card.Location is null)
            && deck.Cards.Count(card => card.Face?.Title == top && card.Id is not null && card.FaceUp) == 1
            && deck.Cards.Count == concealed + 1;
    }

    private static bool DiscardMatches(AreaDescriptor removed, string discard) => discard.Length == 0
        ? removed.Cards.Count == 0
        : removed.Cards.Count == 1 && removed.Cards.Count(card => card.Face?.Title == discard && card.Id is not null) == 1;

    /// <summary>Counts the player's authorized hand.</summary>
    public int HandCount() => table?.CurrentGame?.World?.Areas.Single(area => area.Owner == 0 && area.Zone == "HandsArea").Cards.Count ?? -1;

    /// <summary>Reads the visible identity's exhaustion.</summary>
    public bool IdentityReady() => table?.CurrentGame?.World?.Areas.SelectMany(area => area.Cards)
        .Single(card => card.Face?.Title == "Doctor Strange" || card.Face?.Title == "Stephen Strange").Ready ?? false;
}
