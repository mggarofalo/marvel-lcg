using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionPreventionAndDefeat
{
    internal static void Use(this AbilityResolutionExecution execution,
        World world, Card card, CompiledCardAbility ability, Occurrence? occurrence = null)
        => AbilityUseRecording.Record(world, execution.program, card, ability, occurrence);

    /// <inheritdoc/>
    internal static long WouldBeDealt(this AbilityResolutionExecution execution,
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0)
        {
            return amount;
        }

        var occurrence = new Occurrence(
            0, [Steps.DamageWouldBeDealt], Subject: target.ObjectId, Player: target.Owner);

        long left = amount;
        foreach (var (card, ability) in execution.Waiting(world, occurrence))
        {
            // **Forced only.** `rr:ability.11` makes everything optional unless
            // prefaced by "Forced", and an optional interrupt is a question --
            // which needs a window, which dealing damage has not got. A card
            // that would ask here is refused by name rather than resolved
            // without asking.
            if (ability.Trigger.Timing != AbilityType.ForcedInterrupt)
            {
                // Optional interrupts are offered by the agenda before attack
                // damage is applied. A direct damage call has no window, so it
                // cannot trigger one and must not resolve it on the player's
                // behalf.
                continue;
            }

            var cast = new AbilityResolutionState(world, card, occurrence, target.Owner, events)
            {
                Incoming = left,
                Tier = ability.Trigger.Timing,
            };

            execution.TrackResolution(cast, ability);
            execution.Run(ability, cast);
            cast.CompleteResolution();

            // An ability that touched the damage says so; one that did nothing
            // to it leaves it alone. `rr:damage.step.1` holds abilities that
            // *may* replace the damage, not ones that must.
            left = cast.Remaining < 0 ? left : cast.Remaining;
            if (left <= 0)
            {
                // `rr:replacement-effect.1` -- "when an effect is replaced, it
                // is no longer considered imminent and no further interrupts or
                // responses to that effect can be triggered."
                return 0;
            }
        }

        return left;
    }

    /// <inheritdoc/>
    internal static long WouldTake(this AbilityResolutionExecution execution,
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(events);

        var prevention = world.Effects.Active().FirstOrDefault(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId);
        if (prevention is null || !world.Effects.Use(prevention))
        {
            return amount;
        }

        long prevented = prevention.Amount <= 0 ? amount : prevention.Amount;
        return Math.Max(0, amount - prevented);
    }

    /// <inheritdoc/>
    internal static void DamagePreventedByTough(this AbilityResolutionExecution execution,
        World world, Card target, Card source, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(events);

        var prevention = world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId).ToList();
        foreach (var effect in prevention)
        {
            world.Effects.Use(effect);
        }
    }

    /// <inheritdoc/>
    internal static void WouldBeDefeated(this AbilityResolutionExecution execution, World world, Card target, List<GameEvent> events)
    {
        _ = execution.WouldBeDefeated(
            world, target, target, Steps.CardWouldBeDefeated,
            Steps.CardWouldBeDefeated, -1, events);
    }

    /// <inheritdoc/>
    internal static bool WouldBeDefeated(this AbilityResolutionExecution execution,
        World world, Card target, Card source, string trigger, string verb, int by,
        List<GameEvent> events, Occurrence? recordDefeatOn = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        var occurrence = new Occurrence(
            0, [Steps.CardWouldBeDefeated], Subject: target.ObjectId, Player: target.Owner);
        var spent = world.Agenda.Occurrence;

        while (AbilityWindow.Tiers(
            execution.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(pending => spent?.MayTrigger(WindowKind.Interrupt, pending.Card) ?? true),
            WindowKind.Interrupt,
            occurrence) is { Count: > 0 } tiers)
        {
            var (mandatory, optional) = AbilityWindow.Split(tiers[0]);
            if (mandatory.Count == 0)
            {
                execution.SuspendWouldBeDefeated(
                    world, target, source, trigger, verb, by, occurrence, optional,
                    recordDefeatOn);
                return false;
            }

            if (mandatory.Count > 1)
            {
                execution.SuspendWouldBeDefeated(
                    world, target, source, trigger, verb, by, occurrence, mandatory,
                    recordDefeatOn);
                return false;
            }

            occurrence.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            spent?.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            events.AddRange(execution.Resolve(world, occurrence, mandatory[0], [], []));

            // `rr:would.1`: once the interrupt changes the imminent defeat,
            // no later interrupt to that original condition may be used.
            if (DamagePlacement.Health(world, world.Facts, target) - target.Damage > 0)
            {
                return true;
            }
        }

        return true;
    }

    internal static void SuspendWouldBeDefeated(this AbilityResolutionExecution execution,
        World world, Card target, Card source, string trigger, string verb, int by,
        Occurrence occurrence, IReadOnlyList<PendingAbility> pending,
        Occurrence? recordDefeatOn)
    {
        var step = new PhaseStep(
            Steps.ChooseWouldBeDefeated,
            world.Agenda.Current?.Round ?? 0,
            6,
            Subject: target.ObjectId,
            Seat: target.Owner >= 0 ? target.Owner : world.FirstPlayer,
            Plan: true,
            ProcedureAbilities: [.. pending],
            ProcedureOccurrence: occurrence,
            ProcedureOwnerOccurrence: recordDefeatOn,
            ProcedureSource: source.ObjectId,
            ProcedureTrigger: trigger,
            ProcedureVerb: verb,
            ProcedureBy: by);

        if (world.Agenda.Occurrence is { } parent)
        {
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
    }

    /// <summary>Every authored ability answering one occurrence, with its card.</summary>
    /// <remarks>
    /// <b>Gathered before any of it runs.</b> An ability can make an area —
    /// giving a status card creates one to hold it — and walking
    /// <c>World.Areas</c> lazily while resolving would be modifying the
    /// collection being read.
    /// </remarks>
    internal static List<(Card Card, CompiledCardAbility Ability)> Waiting(this AbilityResolutionExecution execution, World world, Occurrence what) =>
    [
        .. world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .SelectMany(card => execution.On(card)
                .Where(ability => execution.Answers(world, ability, card, what))
                .Select(ability => (Card: card, Ability: ability)))
            .ToList(),
    ];

    /// <summary>Whether one ability answers this occurrence at all.</summary>
    internal static bool Answers(this AbilityResolutionExecution execution,
        World world, CompiledCardAbility ability, Card card, Occurrence what)
    {
        int? restricted = execution.RestrictedPlayer(world, ability, card);
        return ability.Trigger.Event is { } condition
            && what.Conditions.Contains(condition, StringComparer.Ordinal)
            && execution.Subject(world, ability.Trigger.Subject, card, what, restricted)
            && execution.Role(world, ability.Trigger.Actor, card, what.ActorFacts, restricted)
            && execution.Role(world, ability.Trigger.Target, card, what.TargetFacts, restricted)
            && execution.Player(world, ability.Trigger.Player, card, what, restricted);
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Setup(this AbilityResolutionExecution execution, World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (!execution.program.Authored.Contains(card.FaceId))
        {
            // The same distinction `WhenRevealed` makes, and setup is where it
            // matters most: a scenario whose main scheme nobody has read would
            // otherwise deal a board that is quietly missing whatever its first
            // card said, and every later assertion would be about the wrong
            // game.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' is being set up and no ability data is written for it; "
                + $"this engine has {execution.program.Authored.Count} authored card(s)");
        }

        var events = new List<GameEvent>();

        // `rr:setup-triggered-ability.2` times these to a step of setup rather
        // than to anything happening, so `Steps.Setup` is the step's name and
        // not a triggering condition -- no card can name it, because the reader
        // refuses an `event` on a Setup ability. What it is for is the events:
        // a board built during setup is told apart in the stream from one built
        // during a round.
        //
        // There is no player whose turn it is either. The card's owner resolves
        // it, which for an encounter card is the scenario.
        var occurrence = new Occurrence(
            0, [Steps.Setup], Subject: card.ObjectId, Player: card.Owner);

        foreach (var ability in execution.On(card))
        {
            if (ability.Trigger.Timing == AbilityType.Setup)
            {
                var cast = new AbilityResolutionState(world, card, occurrence, card.Owner, events)
                {
                    Tier = ability.Trigger.Timing,
                };
                execution.TrackResolution(cast, ability);
                execution.Run(ability, cast);
                cast.CompleteResolution();
            }
        }

        return events;
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> ResolveEachPlayer(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt,
        AbilityType? tier, bool finalStep, bool finalPlayer)
    {
        var step = world.Agenda.Current;
        var restored = AbilityContinuationCodec.DecodeEachPlayer(
            execution.program, source, step, stoppedAt, tier, player, finalPlayer);

        var cast = execution.Resolving(
            world, source, player, tier, finalStep, step?.AbilityOccurrence) with
        {
            EachPlayerFrame = true,
            FinalPlayer = finalPlayer,
            AbilityPlayer = step?.AbilityPlayer ?? player,
            GainedKeywords = world.Agenda.Current is
            { What: Steps.ResolveEachPlayer, SurgeGained: true }
                    ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
        };
        execution.RestorePersisted(cast, step);
        cast.RestoreAbility(
            restored.Ordinal, restored.Frames,
            step?.AbilityFace);
        cast.TrackResolution(restored.Ordinal);
        execution.RestoreAlteredFromFrames(cast, cast.StructuralPath.ToImmutableArray());
        cast.At(stoppedAt - 1);
        cast.SetContinuation(restored.HasContinuation);
        execution.Run(restored.Body, cast);
        var next = AbilityContinuationCodec.AfterEachPlayer(
            execution.program, source, restored, execution.Capture(cast, restored.Ordinal),
            world.Agenda.Current?.Round ?? 0, tier, finalPlayer, cast.Suspended);
        if (next is not null)
        {
            return execution.ResumeContinuation(cast, source, next);
        }
        cast.CompleteResolution();
        return cast.Events;
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> WhenCardDefeated(this AbilityResolutionExecution execution, World world, Card card, Defeated defeated)
    {
        var events = new List<GameEvent>();
        _ = execution.WhenCardDefeated(world, card, defeated, Steps.CardDefeated, events);
        return events;
    }

    /// <inheritdoc/>
    internal static bool WhenCardDefeated(this AbilityResolutionExecution execution,
        World world, Card card, Defeated defeated, string trigger,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(defeated);

        var written = execution.On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenDefeated)
            .ToList();

        // **The printed check gates the complaint, not the run.** Nothing in
        // the printed attributes records a "When Defeated", so an unwritten one
        // and a card that has none look identical from here -- but that is only
        // a question when there is nothing written. Asking it first would let
        // the text box veto authored data, which is the wrong way round: the
        // data is what the engine runs.
        if (written.Count == 0 && world.Facts.HasWhenDefeated(card.FaceId))
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was defeated and prints a 'When Defeated' "
                + "ability that no ability data is written for");
        }

        // **Two occurrences, and each is asked what only it can answer.**
        //
        // This one is built here because the matching needs the defeated card:
        // "when **attached minion** is defeated" is a claim about which card
        // died, while the occurrence the defeat joined keeps the cause. An
        // attack carries its actor and target separately. This occurrence also
        // carries the provenance because "the player who defeated this scheme"
        // is on the card and not on the board.
        //
        // What it cannot answer is `rr:triggering-condition.1`, "each
        // **Interrupt** ability can only be triggered once per occurrence of
        // its triggering condition". The occurrence there is the one on the
        // agenda: it is what lasts, it is what a still-open interrupt window is
        // polling, and it is where a second defeat in the same moment would
        // find an ability already spent. This one is made fresh on every call
        // and would forget all of that.
        var occurrence = new Occurrence(
            0, [Steps.CardDefeated], Subject: card.ObjectId, Player: card.Owner);
        occurrence.Also(defeated);

        var spent = world.Agenda.Occurrence;
        var elsewhere = execution.Answering(world, card, occurrence, spent);
        if (elsewhere.Count == 0)
        {
            // `rr:when-defeated-abilities.2` says all abilities on the defeated
            // card resolve. Their printed/data order is already authoritative;
            // the cross-card ordering question from `rr:forced.5` does not
            // arise until another card answers the same defeat.
            foreach (var ability in written)
            {
                var cast = new AbilityResolutionState(world, card, occurrence, card.Owner, events)
                {
                    Tier = ability.Trigger.Timing,
                };
                execution.TrackResolution(cast, ability);
                execution.Run(ability, cast);
                cast.CompleteResolution();
            }
            return true;
        }

        var own = written.Select((_, ordinal) => new PendingAbility(
            card.ObjectId, AbilityType.WhenDefeated, card.Owner, ordinal));
        var waiting = own.Concat(elsewhere).ToList();
        while (waiting.Count > 0)
        {
            var mandatory = waiting
                .Where(ability => AbilityTypes.IsMandatory(ability.Type))
                .ToList();
            var offered = mandatory.Count > 0
                ? mandatory
                : waiting.Where(ability => !AbilityTypes.IsMandatory(ability.Type)).ToList();
            if (offered.Count > 1 || mandatory.Count == 0)
            {
                execution.SuspendCardDefeated(
                    world, card, trigger, occurrence, defeated, offered);
                return false;
            }

            var next = offered[0];
            occurrence.Trigger(WindowKind.Interrupt, next.Card);
            spent?.Trigger(WindowKind.Interrupt, next.Card);
            events.AddRange(execution.Resolve(world, occurrence, next, [], []));
            waiting.Remove(next);
        }

        return true;
    }

    internal static void SuspendCardDefeated(this AbilityResolutionExecution execution,
        World world, Card card, string trigger, Occurrence occurrence,
        Defeated defeated, IReadOnlyList<PendingAbility> pending)
    {
        occurrence.Also(defeated);
        var step = new PhaseStep(
            Steps.ChooseCardDefeatedAbility,
            world.Agenda.Current?.Round ?? 0,
            7,
            Subject: card.ObjectId,
            Seat: card.Owner >= 0 ? card.Owner : world.FirstPlayer,
            Plan: true,
            ProcedureAbilities: [.. pending],
            ProcedureOccurrence: occurrence,
            ProcedureTrigger: trigger,
            ProcedureVerb: defeated.How,
            ProcedureBy: defeated.By);

        if (world.Agenda.Occurrence is { } parent)
        {
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
    }
}
