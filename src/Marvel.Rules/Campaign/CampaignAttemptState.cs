using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Campaign;

/// <summary>Replayable campaign facts owned by one game's world.</summary>
public sealed class CampaignAttemptState
{
    private readonly Dictionary<int, CampaignCardProgress> cards;
    private World? owner;
    private readonly HashSet<int> weapons;

    /// <summary>Binds the exact saved setup and deterministic physical-card mapping once.</summary>
    public CampaignAttemptState(CampaignGameSetup setup, IReadOnlyList<CampaignCardBinding> bindings,
        IReadOnlyList<int>? experimentalWeapons = null)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(bindings);
        Setup = setup;
        weapons = (experimentalWeapons ?? []).ToHashSet();
        if (bindings.Select(binding => binding.Card.Key).Distinct(StringComparer.Ordinal).Count() != bindings.Count)
            throw new ArgumentException("campaign card keys must be unique", nameof(bindings));
        cards = bindings.ToDictionary(binding => binding.ObjectId,
            binding => new CampaignCardProgress(binding, false, false, false));
    }

    /// <summary>The immutable attempt input retained by the session trace.</summary>
    public CampaignGameSetup Setup { get; }

    /// <summary>Attaches validated physical identities to their game once.</summary>
    public void Bind(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (owner is not null || world.Campaign is not null)
            throw new InvalidOperationException("campaign attempts bind to exactly one world");
        if (weapons.Any(id => id < 0 || id >= world.Cards.Count))
            throw new ArgumentException("campaign weapon binding is outside this world", nameof(world));
        foreach (var progress in Cards)
        {
            int id = progress.Binding.ObjectId;
            if (id < 0 || id >= world.Cards.Count || !world.Cards[id].Faces.Contains(progress.Binding.Card.Face))
                throw new ArgumentException("campaign binding does not match this world's physical card", nameof(world));
        }
        owner = world;
        world.Campaign = this;
    }

    /// <summary>Cumulative facts in deterministic object-id order.</summary>
    public IReadOnlyList<CampaignCardProgress> Cards => [.. cards.Values.OrderBy(value => value.Binding.ObjectId)];

    /// <summary>Experimental attachments that actually entered play, retained after leaving it.</summary>
    public IReadOnlyList<Card> EnteredExperimentalWeapons => owner is null ? [] :
        [.. weapons.Order().Select(id => owner.Cards[id]).Where(card => card.HasEnteredPlay)];

    /// <summary>Records a printed removal from the campaign, independently of the card's current zone.</summary>
    public void Remove(Card card, string trigger, List<GameEvent> events)
    {
        var progress = Get(card);
        if (progress.Removed) throw new InvalidOperationException("campaign card has already been removed");
        cards[card.ObjectId] = progress with { Removed = true };
        Changed(card, "campaign_removed", 0, 1, trigger, events);
    }

    /// <summary>Records the player rescuing one physical captive ally.</summary>
    public void Rescue(Card card, int player, string trigger, List<GameEvent> events)
    {
        var progress = Get(card);
        ValidateSeat(player);
        if (progress.Binding.Card.Role != CampaignCardRole.RescuedAlly || progress.Rescued)
            throw new InvalidOperationException("card is not an unrescued campaign captive");
        cards[card.ObjectId] = progress with { Binding = progress.Binding with { Seat = player }, Rescued = true };
        Changed(card, "campaign_seat", progress.Binding.Seat, player, trigger, events);
        Changed(card, "campaign_rescued", 0, 1, trigger, events);
    }

    /// <summary>Records a randomly assigned recovery obligation before the attempt can proceed.</summary>
    public void AssignObligation(Card card, int player, string trigger, List<GameEvent> events)
    {
        var progress = Get(card);
        ValidateSeat(player);
        if (progress.Binding.Card.Role != CampaignCardRole.Obligation || progress.Assigned
            || progress.Binding.Seat != player)
            throw new InvalidOperationException("card is not an available recovery obligation for this seat");
        cards[card.ObjectId] = progress with { Assigned = true };
        Changed(card, "campaign_assigned", 0, 1, trigger, events);
    }

    /// <summary>Registered digest values for tracked physical cards, including zero values out of play.</summary>
    public IReadOnlyDictionary<string, long> Fields(Card card)
    {
        if (!cards.TryGetValue(card.ObjectId, out var progress))
            return weapons.Contains(card.ObjectId)
                ? new Dictionary<string, long> { ["campaign_entered_play"] = card.HasEnteredPlay ? 1 : 0 }
                : new Dictionary<string, long>();
        // These names and numeric role assignments are engine-owned wire choices.
        // The immutable setup plus content hash fixes the object-id/key mapping.
        return new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["campaign_role"] = RoleValue(progress.Binding.Card.Role),
            ["campaign_seat"] = progress.Binding.Seat,
            ["campaign_removed"] = progress.Removed ? 1 : 0,
            ["campaign_rescued"] = progress.Rescued ? 1 : 0,
            ["campaign_assigned"] = progress.Assigned ? 1 : 0,
        };
    }

    private static long RoleValue(CampaignCardRole role) => role switch
    {
        CampaignCardRole.Tech => 1,
        CampaignCardRole.Condition => 2,
        CampaignCardRole.RescuedAlly => 3,
        CampaignCardRole.Obligation => 4,
        _ => throw new InvalidOperationException("unknown campaign card role"),
    };

    private CampaignCardProgress Get(Card card) => owner is not null && owner.Cards.Contains(card)
        && cards.TryGetValue(card.ObjectId, out var progress)
        ? progress : throw new InvalidOperationException("card has no campaign binding");

    private void ValidateSeat(int player)
    {
        if (!Setup.Seats.Any(seat => seat.Seat == player))
            throw new ArgumentOutOfRangeException(nameof(player));
    }

    private static void Changed(Card card, string field, long before, long after, string trigger, List<GameEvent> events) =>
        events.Add(new FieldSet(card.ObjectId, field, before, after) { Trigger = trigger, Verb = "Campaign" });
}
