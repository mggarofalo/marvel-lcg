using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns Kang I's printed delayed advancement and ordered stage-3 reveals.</summary>
/// <remarks>Nonpublic one-to-four-seat capability; product admission remains closed.</remarks>
public sealed class KangFirstSplit
{
    private static readonly ImmutableArray<string> StageThree = ["11009a", "11010a", "11011a", "11012a"];
    private OccurrenceCard? defeated;
    private PhaseStep? owner;
    private Occurrence? origin;
    private bool expert;

    /// <summary>The exact defeated incarnation awaiting its next phase ending.</summary>
    public OccurrenceCard? Pending => defeated;
    /// <summary>How many Kang (II) the split dealt, one per participating player; 0 before the split.</summary>
    public int LocalKangsDealt { get; private set; }
    /// <summary>Whether this physical face uses the scenario's stage-I departure policy.</summary>
    public static bool IsFirstStage(Card card) => card.FaceId is "11001" or "11034";

    /// <summary>Whether completing this stage loses the game although later stages remain.</summary>
    /// <remarks>
    /// Printed 11007b: "If this stage is completed, the players lose the game."
    /// Stages 2 and 4 wait beneath it in the main scheme deck (MARVEL-894 Q13), so
    /// the generic advance of rr:main-scheme-main-scheme-deck.2 would otherwise
    /// reveal 2A. rr:winning-the-game.1 makes this an alternate loss condition that
    /// "cannot be blanked or canceled", so it is engine-owned rather than ability text.
    /// </remarks>
    public static bool LosesWhenCompleted(Card scheme) => scheme.FaceId == "11007b";

    /// <summary>Called by the authored When Defeated ability in its normal interrupt tier.</summary>
    public void Capture(World world, Card card)
    {
        if (!IsFirstStage(card) || !world.Cards.Contains(card) || defeated is not null)
            throw new RulesNotImplementedException("Kang I split requires one actual defeated first stage");
        defeated = OccurrenceCard.Capture(card, world.Facts);
        expert = card.FaceId == "11034";
    }

    internal void FinalizeDefeat(World world, Card card, string trigger, List<GameEvent> events)
    {
        if (defeated is null || defeated.Card != card.ObjectId || defeated.Incarnation != card.Incarnation)
            throw new RulesNotImplementedException("Kang I defeat requires its authored delayed split ability");
        // pack:mc11:rules-clarifications: there is no next villain in the deck;
        // attachments and statuses are not transferred from Kang I to Kang II.
        Remove(world, card, trigger, events);
    }

    internal bool IsBoundary(PhaseStep step) => defeated is not null && step.Operation is
        PhaseTransitionOperation { Kind: PhaseTransitionKind.EndPlayerPhase or PhaseTransitionKind.EndVillainPhase };

    internal void Preflight(World world, PhaseStep step)
    {
        if (!IsBoundary(step)) return;
        // An eliminated player is out of the game (rr:player-elimination.6) and
        // reveals no stage 3A: SelectStages deals only to world.PlayerOrder.
        if (world.Players is < 1 or > 4 || world.Seats.Count != world.Players
            || !world.PlayerOrder.Any()
            || world.Agenda.KangTransitions.Pending.Count != 0)
            throw new RulesNotImplementedException("the first Kang split requires one to four seats with a participating player");
        RequireStageTwo(world);
        for (int index = 0; index < 4; index++)
        {
            Require(world, $"{11009 + index}a");
            Require(world, (expert ? 11035 + index : 11002 + index).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    internal void Begin(World world, PhaseStep step, List<GameEvent> events)
    {
        if (!IsBoundary(step)) return;
        owner = step;
        origin = world.Agenda.Occurrence ?? throw new InvalidOperationException("the split has no phase owner");
        var prior = world.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new RulesNotImplementedException("Kang split has no stage 1 scheme");
        long acceleration = prior.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        Remove(world, prior, "Kang split", events);
        var central = RequireStageTwo(world);
        Move(world, central, world.AreaOf(DeckType.MainSchemesArea), events);
        central.PlaceTokens(EncounterDeck.AccelerationToken, acceleration);
        Schedule(world, central, world.FirstPlayer, false);
        defeated = null;
    }

    /// <summary>Executes the actual 2A instruction and schedules each complete 3A reveal in player order.</summary>
    public void SelectStages(World world, Card central, List<GameEvent> events)
    {
        RequireOwner(world);
        if (central.FaceId != "11008a") throw new RulesNotImplementedException("Kang split requires stage 2A");
        // 11008a: "Place 1 acceleration token here for each side scheme in play,
        // then discard each side scheme. Each player reveals a random stage 3A in turn order."
        var sides = world.Areas.Where(area => area.Type == DeckType.SideSchemesArea)
            .SelectMany(area => area.Cards).OrderBy(card => card.ObjectId).ToArray();
        long before = central.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        central.PlaceTokens(EncounterDeck.AccelerationToken, sides.Length);
        if (sides.Length > 0) events.Add(new FieldSet(central.ObjectId, EncounterDeck.AccelerationToken, before, before + sides.Length)
            { Trigger = "11008a", Verb = "Place" });
        foreach (var side in sides) Discard.Card(world, side, "11008a", events);
        var choices = Enumerable.Range(11009, 4).Select(code => Require(world, $"{code}a")).ToList();
        // Engine choice: one seeded Choice from the ordered remaining printed codes
        // for each seat in player order. Every assignment consumes a bounded draw.
        LocalKangsDealt = world.PlayerOrder.Count();
        foreach (int player in world.PlayerOrder)
        {
            var chosen = world.Random.Choice(choices);
            choices.Remove(chosen);
            Schedule(world, chosen, player, false);
            Schedule(world, chosen, player, true);
        }
        Schedule(world, central, world.FirstPlayer, true, Array.AsReadOnly(choices.Select(card => card.ObjectId).ToArray()));
        world.Detach(PlayArea.Villains, "11008a", events);
    }

    /// <summary>Executes a selected 3A's topology, matching Kang entry and encounter deal.</summary>
    public void CreateLocalArea(World world, Card scheme, int player, List<GameEvent> events)
    {
        RequireOwner(world);
        int index = StageThree.IndexOf(scheme.FaceId);
        if (index < 0) throw new RulesNotImplementedException("Kang local area requires an actual stage 3A");
        // 11009a-11012a: "Create your own game area and place this scheme in it.
        // Add Kang (...) to the game area and deal yourself an encounter card."
        world.Join(PlayArea.Of(player), world.CreateGameArea(), scheme.FaceId, events);
        Move(world, scheme, world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(player)), events);
        var villain = Require(world, (expert ? 11035 + index : 11002 + index).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Move(world, villain, world.AreaOf(DeckType.VillainArea, PlayArea.Of(player)), events);
        Reveal.EnterPlay(world, world.Facts, villain, events);
        Deal.EncounterCard(world, player, scheme.FaceId, events);
    }

    private void Schedule(World world, Card card, int player, bool flip, IReadOnlyList<int>? unused = null)
    {
        RequireOwner(world);
        world.Agenda.ContinueBeforeOwner(origin!, owner!.Value, new PhaseStep(new KangSplitOperation(flip, unused),
            owner.Value.Round, owner.Value.Number, Subject: card.ObjectId, Seat: player, Plan: true));
    }

    private void RequireOwner(World world)
    {
        if (owner is null || origin is null || !world.Agenda.IsOutstanding(owner.Value, origin))
            throw new RulesNotImplementedException("Kang split instruction requires its retained phase owner");
    }

    private static Card RequireStageTwo(World world)
    {
        // MARVEL-894 Q13: stage 2 waits on top of the main scheme deck, above stage 4.
        var deck = world.AreaOf(DeckType.MainSchemesDeck);
        return deck.Cards.Count > 0 && deck.Cards[^1].FaceId == "11008a" ? deck.Cards[^1]
            : throw new RulesNotImplementedException("Kang split requires stage 2A on top of the main scheme deck");
    }

    private static Card Require(World world, string face) => world.Cards.SingleOrDefault(card => card.Faces.Contains(face)
        && card.Area.Type != DeckType.RemovedArea)
        ?? throw new RulesNotImplementedException($"Kang split requires physical card {face}");

    internal static void Remove(World world, Card card, string trigger, List<GameEvent> events)
    {
        var constants = world.Effects.PreflightConstantsEnding(card);
        using var departure = constants.Begin();
        Discard.Attachments(world, card, trigger, events);
        Move(world, card, world.AreaOf(DeckType.RemovedArea), events);
        constants.Complete(trigger, events);
    }

    private static void Move(World world, Card card, Area into, List<GameEvent> events)
    {
        var from = card.Area;
        World.MoveToTop(card, into);
        card.TurnFaceUp();
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)]) { Trigger = "Kang split", Verb = "Advance" });
    }
}
