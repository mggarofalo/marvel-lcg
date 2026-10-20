using System.Collections.Immutable;
using Marvel.Rules.Setup;
namespace Marvel.Protocol;

/// <summary>An owner-authenticated campaign intention; game rules validate every transition.</summary>
public sealed record CampaignQuery(string Action)
{
    /// <summary>Owner-selected display name for rename.</summary>
    public string? Name { get; init; }
    /// <summary>A bounded portable campaign document for import.</summary>
    public string? Transfer { get; init; }
    /// <summary>The campaign product selected for creation.</summary>
    public string? Product { get; init; }
    /// <summary>Fixed hero identities selected for creation.</summary>
    public ImmutableArray<string> Heroes { get; init; } = [];
    /// <summary>Base recipes, excluding campaign overlays, selected for creation.</summary>
    public ImmutableArray<PlayerDeckRecipe> Decks { get; init; } = [];
    /// <summary>The deterministic root seed selected for creation.</summary>
    public uint Seed { get; init; }
    /// <summary>Campaign difficulty on create; independent scenario difficulty on mode.</summary>
    public bool Expert { get; init; }
    /// <summary>Whether the selected defeated expert player joins the next attempt.</summary>
    public bool Participating { get; init; } = true;
    /// <summary>The seat whose recipe or reward is being chosen.</summary>
    public int Seat { get; init; }
    /// <summary>A replacement base recipe accepted only between scenarios.</summary>
    public PlayerDeckRecipe? Deck { get; init; }
    /// <summary>An offered reward key, or null to decline an optional reward.</summary>
    public string? Choice { get; init; }
}
