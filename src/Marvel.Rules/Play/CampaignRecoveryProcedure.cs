using Marvel.Rules.Campaign;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves expert recovery with the game's seeded stream and persistent campaign assignment.</summary>
internal static class CampaignRecoveryProcedure
{
    private const string Trigger = "CampaignRecovery";

    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        if (step.Operation is CampaignEncounterDealOperation)
        {
            Deal.EncounterCard(world, step.Seat, "CampaignSetup", events);
            return null;
        }
        _ = step.Require<CampaignRecoveryOperation>();
        CampaignSeatSetup setup = Seat(world, step.Seat);
        Card[] available = Available(world, setup);
        if (available.Length == 0)
        {
            if (setup.RequiresRecovery) throw new RulesNotImplementedException("required campaign recovery has no available obligation");
            return null;
        }
        var identity = world.Seats[step.Seat].IdentityCard;
        long maximum = DamagePlacement.Health(world, world.Facts, identity);
        return new Prompt(step.Seat, Question.Option, TimingPriority.Untimed, Trigger,
            "Campaign recovery", !setup.RequiresRecovery,
            [new Affordance(0, "CampaignRecovery", identity.ObjectId, step.Seat, "Recover to full health")
            {
                DisplayLabel = "Recover to full health",
                CommitLabel = $"Add a random obligation and recover to {maximum} HP",
                Description = $"Shuffle one random available campaign obligation into your deck; restore your identity to {maximum} HP.",
            }])
        {
            PublicKind = PublicDecisionKind.Choice,
            DisplayQuestion = "Recover before this scenario?",
            DeclineLabel = "Keep recorded hit points",
            Description = setup.RequiresRecovery ? "Recovery is required to rejoin after defeat."
                : "Keep your recorded hit points, or add a random obligation to recover. This happens before opening hands.",
            ContextCardIds = [identity.ObjectId],
        };
    }

    internal static void Answer(World world, PhaseStep step, Decision input, List<GameEvent> events)
    {
        _ = step.Require<CampaignRecoveryOperation>();
        CampaignSeatSetup setup = Seat(world, step.Seat);
        if (input.IsDecline)
        {
            if (setup.RequiresRecovery) throw new RulesNotImplementedException("defeated campaign seat must recover to rejoin");
            return;
        }
        if (input.Affordance != 0 || input.Targets.Count != 0)
            throw new RulesNotImplementedException("campaign recovery accepts only its offered recovery choice");
        Card[] available = Available(world, setup);
        if (available.Length == 0) throw new RulesNotImplementedException("campaign recovery has no available obligation");
        // pack:mc10:persistent-damage: add an obligation to restore full hit points.
        // Stable object-id order fixes the engine's mapping from random index to card.
        Card obligation = world.Random.Choice(available);
        world.Campaign!.AssignObligation(obligation, step.Seat, Trigger, events);
        obligation.TransferScenarioOwnership(step.Seat);
        Area deck = world.Seats[step.Seat].Deck;
        Move(obligation, deck, Trigger, events);
        world.Shuffle(deck);
        events.Add(new AreaReordered(Places.Reference(deck), deck.Cards.Select(card => card.ObjectId).ToArray())
            { Trigger = Trigger, Verb = "Shuffle" });
        SetHitPoints(world, step.Seat, DamagePlacement.Health(world, world.Facts, world.Seats[step.Seat].IdentityCard), events);
    }

    internal static void SetHitPoints(World world, int seat, long hitPoints, List<GameEvent> events)
    {
        Card identity = world.Seats[seat].IdentityCard;
        long maximum = DamagePlacement.Health(world, world.Facts, identity);
        long before = Math.Max(0, maximum - identity.Damage);
        long after = Math.Clamp(hitPoints, 0, maximum);
        identity.TakeDamage(maximum - after - identity.Damage);
        if (before != after) events.Add(new FieldSet(identity.ObjectId, "health", before, after)
            { Trigger = Trigger, Verb = "Campaign" });
    }

    internal static void Move(Card card, Area destination, string trigger, List<GameEvent> events)
    {
        Area from = card.Area;
        World.MoveToTop(card, destination);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(destination),
            [new Landing(card.ObjectId, destination.Cards.Count - 1)]) { Trigger = trigger, Verb = "Move" });
    }

    private static CampaignSeatSetup Seat(World world, int seat) => world.Campaign?.Setup.Seats.Single(value => value.Seat == seat)
        ?? throw new RulesNotImplementedException("campaign recovery requires campaign setup");

    private static Card[] Available(World world, CampaignSeatSetup seat) => [.. world.Campaign!.Cards
        .Where(progress => progress.Binding.Seat == seat.Seat && !progress.Assigned && !progress.Removed
            && seat.AvailableObligations.Contains(progress.Binding.Card))
        .Select(progress => world.Cards[progress.Binding.ObjectId])
        .Where(card => card.Area.Type == DeckType.AsideDeck).OrderBy(card => card.ObjectId)];
}
