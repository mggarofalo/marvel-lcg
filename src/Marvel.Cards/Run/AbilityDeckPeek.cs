using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Chooses a deck before granting prompt-scoped sight of its unchanged top card.</summary>
internal static class AbilityDeckPeek
{
    private const string CardKey = "peekCard";
    private const string AreaKey = "peekArea";
    private const string IncarnationKey = "peekIncarnation";

    internal static IReadOnlyList<Area> Decks(World world) =>
        [.. world.Areas.Where(area => area.Cards.Count > 0 && area.Type is
            DeckType.PlayerDeck or DeckType.EncounterDeck or DeckType.ExperimentalWeaponsDeck
            or DeckType.SideSchemeDeck or DeckType.VillainArea or DeckType.MainSchemesArea)
            .OrderBy(area => area.Id)];

    internal static Prompt Describe(AbilityStructuralContext context)
    {
        var world = context.Expressions.World;
        if (context.Expressions.Results.TryGetValue(CardKey, out long selected))
        {
            var card = world.Cards[checked((int)selected)];
            if (card.Area.Id != context.Expressions.Results[AreaKey]
                || card.Incarnation != context.Expressions.Results[IncarnationKey]
                || card.Area.Cards[^1] != card)
                throw new RulesNotImplementedException("the inspected deck top changed during acknowledgement");
            return new Prompt(context.Player, Question.Element, TimingPriority.Untimed,
                context.Trigger, "Inspect the top card", false,
                [new Affordance(0, "Acknowledge", card.ObjectId, context.Player, "Finish looking")
                { DisplayLabel = "Finish looking", CommitLabel = "Done" }])
            {
                PublicKind = PublicDecisionKind.CardLook,
                DisplayQuestion = "Top card of " + Name(world, card.Area),
                Description = "Look at this card. It stays on top of its deck; nothing is shuffled.",
                ContextCardIds = [card.ObjectId],
                ExposesConcealedCandidates = !card.FaceUp,
            };
        }
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed,
            context.Trigger, "Choose a deck to inspect", false,
            [.. Decks(world).Select(area => new Affordance(area.Id, "Peek", area.Id,
                context.Player, Name(world, area))
            {
                AnchorKind = AffordanceAnchorKind.Area,
                DisplayLabel = Name(world, area), CommitLabel = "Look at top card",
                Description = "Only the top card of this deck will become visible to you.",
            })])
        {
            DisplayQuestion = "Choose a deck", CauseCardIds = [context.Expressions.Source.ObjectId],
            Description = "Look at one deck's top card without moving it or changing the deck order.",
        };
    }

    internal static bool Answer(AbilityResolutionExecution execution, AbilityEffect effect,
        AbilityResolutionState cast, Decision answer)
    {
        if (cast.Results.Remove(CardKey))
        {
            cast.Results.Remove(AreaKey);
            cast.Results.Remove(IncarnationKey);
            cast.ResolveEffect();
            return true;
        }
        var area = Decks(cast.World).Single(deck => deck.Id == answer.Affordance);
        var top = area.Cards[^1];
        cast.Results[CardKey] = top.ObjectId;
        cast.Results[AreaKey] = area.Id;
        cast.Results[IncarnationKey] = top.Incarnation;
        execution.SuspendForChoice(effect, cast);
        return false;
    }

    private static string Name(World world, Area area) => area.Type switch
    {
        DeckType.PlayerDeck => world.Seats[area.PlayArea.Player].Name + "'s deck",
        DeckType.EncounterDeck => "encounter deck",
        DeckType.ExperimentalWeaponsDeck => "Experimental Weapons deck",
        DeckType.SideSchemeDeck => "side-scheme deck",
        // rr:deck: the villain/main scheme are decks whose current top card is
        // already in play. Their unrevealed later stages are not their top card.
        DeckType.VillainArea => "villain deck",
        DeckType.MainSchemesArea => "main-scheme deck",
        _ => throw new InvalidOperationException("Unknown peek deck"),
    };
}
