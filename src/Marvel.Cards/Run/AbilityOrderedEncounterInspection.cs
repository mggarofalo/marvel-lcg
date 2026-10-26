using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Owns a prompt-scoped physical look/discard/ordered-return transaction.</summary>
internal static class AbilityOrderedEncounterInspection
{
    private const string Prefix = "orderedLook:";
    private const string Count = Prefix + "count";
    private const string Area = Prefix + "area";
    private const string Discarded = Prefix + "discarded";

    internal static Prompt Describe(AbilityStructuralContext context,
        AbilityEffect.InspectDiscardOrderEncounterTop effect)
    {
        var world = context.Expressions.World;
        var memory = context.Expressions.Results;
        var source = context.Expressions.Source;
        if (!memory.ContainsKey(Count))
            return new(context.Player, Question.Element, TimingPriority.Untimed, context.Trigger,
                "Look at encounter cards", false,
                [new Affordance(0, "Look", source.ObjectId, context.Player, "Look at encounter cards")
                { DisplayLabel = "Look at encounter cards", CommitLabel = "Look" }])
            {
                DisplayQuestion = "Look at the encounter deck",
                Description = $"Privately look at up to {effect.Count} top cards, discard one, then choose the order of the others.",
                CauseCardIds = [source.ObjectId],
            };

        var cards = Retained(world, memory);
        bool ordering = memory.ContainsKey(Discarded);
        string description = ordering
            ? "Choose all remaining cards in top-to-bottom order. The first selected card will be the next encounter card."
            : "Discard one of the looked-at cards. The others stay in the deck; you will choose their order next.";
        var offers = ordering
            ? new[] { new Affordance(source.ObjectId, "Order", source.ObjectId, context.Player,
                "Return remaining cards", new TargetRequest([.. cards.Select(card => card.ObjectId)], cards.Count, cards.Count))
                { DisplayLabel = "Order remaining cards", CommitLabel = "Return in selected order", Description = description } }
            : cards.Select(card => new Affordance(card.ObjectId, "Discard", card.ObjectId, context.Player,
                world.Facts.Title(card.FaceId))
                { DisplayLabel = world.Facts.Title(card.FaceId), CommitLabel = "Discard this card", Description = description }).ToArray();
        return new(context.Player, ordering ? Question.Order : Question.Element, TimingPriority.Untimed,
            context.Trigger, ordering ? "Order remaining encounter cards" : "Discard a looked-at encounter card", false, offers)
        {
            PublicKind = PublicDecisionKind.CardLook,
            DisplayQuestion = ordering ? "Choose the next encounter card, then the remaining order" : "Discard one looked-at card",
            Description = description,
            ContextCardIds = [.. cards.Select(card => card.ObjectId)],
            CauseCardIds = [source.ObjectId],
            ExposesConcealedCandidates = true,
        };
    }

    internal static bool Answer(AbilityResolutionExecution execution,
        AbilityEffect.InspectDiscardOrderEncounterTop effect, AbilityResolutionState cast, Decision answer)
    {
        if (!cast.Results.ContainsKey(Count))
        {
            var deck = EncounterRouting.Deck(cast.World);
            var cards = deck.Cards.Reverse().Take(effect.Count).ToArray();
            cast.Results[Count] = cards.Length;
            cast.Results[Area] = deck.Id;
            for (int index = 0; index < cards.Length; index++)
            {
                cast.Results[Prefix + index] = cards[index].ObjectId;
                cast.Results[Prefix + index + ":incarnation"] = cards[index].Incarnation;
            }
            if (cards.Length == 0) return Finish(cast);
            execution.SuspendForChoice(effect, cast);
            return false;
        }

        var retained = Retained(cast.World, cast.Results);
        if (!cast.Results.ContainsKey(Discarded))
        {
            var discarded = retained.Single(card => card.ObjectId == answer.Affordance);
            // rr:discard.5: a card discarded from a looked-at group is discarded
            // "from the top of that deck", even when it was the middle card.
            Discard.Card(cast.World, discarded, cast.Trigger, cast.Events);
            cast.Discarded.Add(discarded);
            cast.Results[Discarded] = discarded.ObjectId;
            cast.Results["discarded"] = cast.Discarded.Count;
            if (discarded.Area.Type == DeckType.EncounterDiscardPile)
                EncounterDeck.Reset(cast.World, cast.Trigger, cast.Events);
            if (retained.Count == 1) return Finish(cast);
            execution.SuspendForChoice(effect, cast);
            return false;
        }

        var area = retained[0].Area;
        // rr:encounter-deck.1 permits order changes when instructed by an ability.
        // The engine chooses top-first answer order; deck storage is bottom-first.
        foreach (int id in answer.Targets.Reverse()) World.MoveToTop(cast.World.Cards[id], area);
        cast.Events.Add(new AreaReordered(Places.Reference(area), [.. area.Cards.Select(card => card.ObjectId)])
        { Trigger = cast.Trigger, Verb = "Order" });
        return Finish(cast);
    }

    private static List<Card> Retained(World world, IReadOnlyDictionary<string, long> memory)
    {
        int count = checked((int)memory[Count]);
        var cards = new List<Card>();
        for (int index = 0; index < count; index++)
        {
            int id = checked((int)memory[Prefix + index]);
            if (memory.TryGetValue(Discarded, out long discarded) && id == discarded) continue;
            var card = world.Cards[id];
            if (card.Area.Id != memory[Area] || card.Incarnation != memory[Prefix + index + ":incarnation"])
                throw new ArgumentException("The inspected physical group is stale.");
            cards.Add(card);
        }
        if (cards.Count > 0 && !cards[0].Area.Cards.Reverse().Take(cards.Count).SequenceEqual(cards))
            throw new ArgumentException("The inspected deck group changed.");
        return cards;
    }

    private static bool Finish(AbilityResolutionState cast)
    {
        foreach (string key in cast.Results.Keys.Where(key => key.StartsWith(Prefix, StringComparison.Ordinal)).ToArray())
            cast.Results.Remove(key);
        cast.ResolveEffect();
        return true;
    }
}
