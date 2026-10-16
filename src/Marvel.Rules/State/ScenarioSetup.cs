using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>Resumable scenario setup, before opening hands and mulligans.</summary>
internal sealed class ScenarioSetup
{
    private int stage;
    private int nextReveal;
    private List<Card>? pendingCards;
    private readonly List<(int Card, int Incarnation)> reveals = [];
    private readonly HashSet<(int Card, int Incarnation)> seen = [];

    internal Prompt? Pending { get; private set; }

    internal void Advance(World world, List<GameEvent> events, int cursor = 0)
    {
        while (true)
        {
            Pending = Sequence.WorkWithWorldAbilities(
                world, world.Facts, events, WindowAbilityScope.EncounterCardsOnly);
            SetupCardPlacement.TrackSetupEntrants(world, world.Facts, events, ref cursor, reveals, seen);
            if (Pending is not null) return;
            if (stage == 9)
            {
                if (RevealNext(world, events)) continue;
                DrawOpeningHands(world);
                world.ScenarioSetup = null;
                return;
            }
            RunStage(world, events, stage++);
        }
    }

    private void RunStage(World world, List<GameEvent> events, int current)
    {
        // rr:appendix-ii-setup.step.12: resolve main-scheme Setup, flip and
        // resolve its When Revealed, then the villain's Setup and When Revealed.
        // Stage advances before execution so a suspended choice cannot repeat it.
        switch (current)
        {
            case 0:
            case 2:
            case 4:
            case 7:
                pendingCards = SetupCardPlacement.SetupCards(world, world.Facts, events, pendingCards);
                break;
            case 1:
                RunPrintedSetup(world, events, DeckType.MainSchemesArea);
                break;
            case 3:
                RevealMainScheme(world, events);
                break;
            case 5:
                RunPrintedSetup(world, events, DeckType.VillainArea);
                break;
            case 6:
                RevealVillain(world, events);
                break;
            case 8:
                ValidateRemainingSetupCards();
                break;
            default:
                throw new InvalidOperationException($"unknown setup stage {current}");
        }
    }

    private static void RunPrintedSetup(World world, List<GameEvent> events, DeckType area)
    {
        if (world.TheCardIn(area) is Card card)
            ActivityBatches.Append(events, world.SetupAbilities.Setup(world, card));
    }

    private static void RevealMainScheme(World world, List<GameEvent> events)
    {
        if (world.TheCardIn(DeckType.MainSchemesArea) is not Card scheme) return;
        scheme.TurnTo(scheme.Faces[^1]);
        scheme.PlaceTokens("k_threat", world.Facts.PrintedValue(scheme.FaceId, "StartingThreat", world.Players));
        ActivityBatches.Append(events, world.EncounterAbilities.WhenRevealed(world, scheme, world.FirstPlayer));
    }

    private static void RevealVillain(World world, List<GameEvent> events)
    {
        if (world.TheCardIn(DeckType.VillainArea) is Card villain)
            ActivityBatches.Append(events, world.EncounterAbilities.WhenRevealed(world, villain, world.FirstPlayer));
    }

    private void ValidateRemainingSetupCards()
    {
        if (pendingCards is { Count: > 0 })
            throw new RulesNotImplementedException($"card '{pendingCards[0].FaceId}' has the setup keyword but no setup destination");
    }

    private bool RevealNext(World world, List<GameEvent> events)
    {
        // rr:when-revealed-abilities.1: setup entries resolve in captured entry
        // order. New entries join the same queue; stale incarnations do not resolve.
        while (nextReveal < reveals.Count)
        {
            var address = reveals[nextReveal++];
            Card card = world.Cards[address.Card];
            if (card.Incarnation != address.Incarnation || !DeckTypes.IsInPlay(card.Area.Type)) continue;
            RevealKeywords.Keywords(world, world.Facts, world.ThreatAbilities, card, world.FirstPlayer, events);
            ActivityBatches.Append(events, world.EncounterAbilities.WhenRevealed(world, card, world.FirstPlayer));
            return true;
        }
        return false;
    }

    private static void DrawOpeningHands(World world)
    {
        // rr:appendix-ii-setup.step.14: "Each player draws cards equal to
        // their hand size." This follows every scenario setup decision.
        foreach (Seat seat in world.Seats)
        {
            long size = world.Facts.PrintedValue(seat.IdentityCard.FaceId, "HS", world.Players);
            for (long drawn = 0; drawn < size; drawn++)
            {
                Card? card = seat.Deck.TakeTop();
                if (card is null) break;
                seat.Hand.Append(card);
            }
        }
    }
}
