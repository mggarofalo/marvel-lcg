using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Discards encounter cards one at a time, puts each discarded minion into play engaged with
/// the player engaged with the fewest minions, then resolves the batch's keyword activations
/// and its deferred response windows.
/// </summary>
/// <remarks>
/// Ruling 82bcc63411002a00 (Drang III) orders what follows the entries: no When Revealed
/// abilities, because the minions were not revealed (<c>rr:when-revealed-abilities.2</c>:
/// "If an encounter card with a When Revealed ability is put into play without being
/// revealed, the When Revealed ability does not trigger"); each minion's Interrupt abilities
/// as it engages; "after they've all engaged, resolve all Quickstrike and Teamwork abilities
/// on those minions, then any Response abilities."
/// </remarks>
public static class MinionEntryBatchProcedure
{
    /// <summary>Schedule the discard of <paramref name="count"/> cards before the containing ability resumes.</summary>
    public static void Schedule(World world, long count, EffectCompletion completion, PhaseStep? continuation = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(completion);
        var current = world.Agenda.Current;
        var discard = new PhaseStep(
            new MinionEntryBatchOperation(new MinionEntryBatch(), MinionEntryBatchStage.Discard, count, completion),
            current?.Round ?? 0, current?.Number ?? 0, Plan: true);
        var steps = continuation is { } resume ? new[] { discard, resume } : [discard];
        if (current is not null) world.Agenda.Now(steps);
        else foreach (var each in steps) world.Agenda.Add(each);
    }

    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var operation = step.Require<MinionEntryBatchOperation>();
        switch (operation.Stage)
        {
            case MinionEntryBatchStage.Discard:
                Discard(world, step, operation, events);
                return null;
            case MinionEntryBatchStage.ChooseEngagedPlayer:
                return MinionEntryBatchPrompts.EngagedPlayer(world, step, operation);
            case MinionEntryBatchStage.Keywords:
                return Keywords(world, step, operation);
            default:
                OpenResponses(world, step, operation);
                return null;
        }
    }

    internal static void Answer(World world, PhaseStep step, Decision input)
    {
        var operation = step.Require<MinionEntryBatchOperation>();
        if (operation.Stage == MinionEntryBatchStage.ChooseEngagedPlayer) AnswerEngagedPlayer(world, step, operation, input);
        else AnswerKeyword(world, step, operation, input);
    }

    private static void AnswerEngagedPlayer(World world, PhaseStep step, MinionEntryBatchOperation operation, Decision input)
    {
        {
            int? chosen = operation.Tied.Cast<int?>().FirstOrDefault(seat =>
                world.Seats[seat!.Value].IdentityCard.ObjectId == input.Affordance);
            if (input.IsDecline || input.Targets.Count != 0 || chosen is null || operation.Minion is not { } minion)
                throw new RulesNotImplementedException("choose one player tied for the fewest engaged minions");
            Enter(world, step, operation, world.Cards[minion], chosen.Value);
        }
    }

    private static void AnswerKeyword(World world, PhaseStep step, MinionEntryBatchOperation operation, Decision input)
    {
        if (operation.Stage != MinionEntryBatchStage.Keywords)
            throw new RulesNotImplementedException($"step '{step.What}' asked nothing and cannot take an answer");
        int index = input.Affordance - 1;
        if (input.IsDecline || input.Targets.Count != 0 || index < 0 || index >= operation.Pending.Length
            || !Applies(world, operation.Pending[index]))
            throw new RulesNotImplementedException($"affordance {input.Affordance} was not offered after the minions engaged");
        Resolve(world, step, operation, index);
    }

    private static void Discard(World world, PhaseStep step, MinionEntryBatchOperation operation, List<GameEvent> events)
    {
        var deck = EncounterRouting.Deck(world);
        if (operation.Remaining <= 0
            || deck.Cards.Count == 0 && !EncounterDeck.Reset(world, step.What, events))
        {
            // The keyword activations and responses are the minions' own triggered abilities,
            // not part of the instruction that put them into play, so the instruction does not
            // own them. An activation initiated during another still waits for it (rr:activation.8).
            world.Agenda.ThenUnowned(step with
            {
                Operation = operation with
                {
                    Stage = MinionEntryBatchStage.Keywords,
                    Pending = [.. operation.Batch.Entries.SelectMany(entry =>
                        new[] { new MinionEntryKeyword(entry, true), new MinionEntryKeyword(entry, false) })],
                },
                OccurrenceId = null,
            });
            return;
        }

        // rr:encounter-deck.2: a discard of a specified number stops when "the encounter deck
        // is empty"; the immediate reset does not extend the instruction into the new deck.
        long remaining = deck.Cards.Count == 1 ? 0 : operation.Remaining - 1;
        // The printed text discards the cards one at a time: "Each time a minion is discarded
        // this way, put it into play", and the ruling's question stops "to check each time a
        // minion is discarded."
        var card = EncounterDeck.DiscardTop(world, 1, step.What, events)[0];
        var next = operation with { Remaining = remaining };
        if (EffectiveCards.Kind(card, world.Facts) == CardKind.Minion)
        {
            var tied = Fewest(world);
            if (tied.Length == 1) Enter(world, step, next, card, tied[0]);
            // rr:first-player.1: "If an encounter card targets a specific player or card, and
            // there are multiple eligible targets, the first player selects among the eligible
            // options."
            else world.Agenda.Then(step with
            {
                Operation = next with { Stage = MinionEntryBatchStage.ChooseEngagedPlayer, Minion = card.ObjectId, Tied = tied },
                OccurrenceId = null,
            });
        }
        world.Agenda.Then(step with { Operation = next with { Stage = MinionEntryBatchStage.Discard }, OccurrenceId = null });
    }

    /// <summary>The players engaged with the fewest minions, in player order.</summary>
    internal static ImmutableArray<int> Fewest(World world)
    {
        var counts = world.PlayerOrder.Select(seat => (Seat: seat, Count: world
            .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat)).Cards
            .Count(card => EffectiveCards.Kind(card, world.Facts) == CardKind.Minion))).ToList();
        int fewest = counts.Min(each => each.Count);
        return [.. counts.Where(each => each.Count == fewest).Select(each => each.Seat)];
    }

    private static void Enter(World world, PhaseStep step, MinionEntryBatchOperation operation, Card card, int player)
    {
        // rr:engage: "When a minion enters play in a player's play area, it engages that
        // player." The entry keeps its own interrupt window; its keyword activations and
        // response window wait for the batch.
        var request = EncounterEntryRequest.For(card, player, new CardEntryPlacement(Verb: "Put_Into_Play"))
            with { Batch = operation.Batch };
        world.Agenda.Then(new PhaseStep(new EncounterEntryOperation(request, new EffectCompletion(1, 1)),
            step.Round, step.Number, Subject: card.ObjectId, Seat: player));
    }

    private static Prompt? Keywords(World world, PhaseStep step, MinionEntryBatchOperation operation)
    {
        var eligible = Enumerable.Range(0, operation.Pending.Length)
            .Where(index => Applies(world, operation.Pending[index])).ToList();
        if (eligible.Count == 0)
        {
            world.Agenda.Then(step with
            {
                Operation = operation with { Stage = MinionEntryBatchStage.Responses, Pending = [] },
                OccurrenceId = null,
            });
            return null;
        }
        // rr:forced.5: "If two or more forced abilities would initiate at the same moment, the
        // first player determines the order in which the abilities initiate."
        if (eligible.Count > 1) return MinionEntryBatchPrompts.Keywords(world, step, operation, eligible);
        Resolve(world, step, operation, eligible[0]);
        return null;
    }

    private static void Resolve(World world, PhaseStep step, MinionEntryBatchOperation operation, int index)
    {
        var keyword = operation.Pending[index];
        var card = world.Cards[keyword.Entry.Card];
        if (keyword.Quickstrike)
            MinionEntryActivations.Quickstrike(world, world.Facts, card, keyword.Entry.Player, step.Round);
        else MinionEntryActivations.Teamwork(world, world.Facts, card, keyword.Entry.Player, step.Round);
        // rr:forced.6: "Each forced ability must resolve as completely as possible before the
        // next forced ability being triggered by the same triggering condition may initiate."
        world.Agenda.Then(step with
        {
            Operation = operation with { Pending = operation.Pending.RemoveAt(index) },
            OccurrenceId = null,
        });
    }

    /// <summary>Whether a pending activation's minion is the same engaged card and its keyword applies.</summary>
    internal static bool Applies(World world, MinionEntryKeyword keyword)
    {
        var card = world.Cards[keyword.Entry.Card];
        int player = keyword.Entry.Player;
        if (card.Incarnation != keyword.Entry.Incarnation || card.Area.Type != DeckType.EngagedEnemiesArea
            || card.Area.PlayArea != PlayArea.Of(player))
            return false;
        return keyword.Quickstrike
            ? MinionEntryActivations.QuickstrikeApplies(world, world.Facts, card, player)
            : MinionEntryActivations.TeamworkApplies(world, world.Facts, card);
    }

    private static void OpenResponses(World world, PhaseStep step, MinionEntryBatchOperation operation)
    {
        operation.Completion.Complete(0, 1);
        // The ruling resolves the Response abilities after every keyword activation. Each
        // minion's entry keeps its own occurrence; the windows open in the order the minions
        // engaged, which is an engine choice.
        foreach (var entry in operation.Batch.Entries)
            world.Agenda.Then(new PhaseStep(new CompletedCardEffectOperation { Occurrence = entry.Occurrence },
                step.Round, step.Number, Subject: entry.Card, Seat: entry.Player));
    }
}
