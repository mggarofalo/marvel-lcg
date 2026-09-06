using static Marvel.Cards.Run.AbilityEffectStructure;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>
    /// Runs what is left of the ability after the answered choice.
    /// </summary>
    /// <remarks>
    /// The chosen option has already run; this is the rest of the sequence it
    /// was a step of. If the rest holds another choice, it suspends again and
    /// the step it schedules says where to pick up next.
    /// </remarks>
    private List<GameEvent> Continue(Card source, Cast cast, int from)
    {
        if (cast.AbilityOrdinal >= 0 && cast.AbilityPath.Count > 0)
        {
            var root = AbilityAt(
                source, cast.Tier, cast.AbilityOrdinal, cast.AbilityFace).Effect;
            int eachPlayer = cast.AbilityPath.FindIndex(frame =>
                frame.StartsWith("eachPlayer:", StringComparison.Ordinal));
            ResumeAfter(
                root, cast.AbilityPath, cast,
                stopBefore: cast.EachPlayerFrame && !cast.FinalPlayer ? eachPlayer : -1);
            cast.CompleteResolution();
            DiscardEvent(source, cast);
            return cast.Events;
        }

        var effect = On(source)
            .Select(ability => ability.Effect)
            .FirstOrDefault(tree => Choices(tree).Any());

        // Agenda steps created before structural continuation paths existed
        // still resume from their top-level sequence index.
        if (cast.EachPlayerFrame)
        {
            if (cast.FinalPlayer && effect is AbilityEffect.Sequence && !cast.Suspended)
            {
                Sequence(effect, cast, from);
            }
            DiscardEvent(source, cast);
            cast.CompleteResolution();
            return cast.Events;
        }

        if (effect is AbilityEffect.Sequence && !cast.Suspended)
        {
            Sequence(effect, cast, from);
        }

        DiscardEvent(source, cast);
        cast.CompleteResolution();

        return cast.Events;
    }

    /// <summary>A fresh resolution of one card's ability, by one player.</summary>
    private Cast Resolving(
        World world, Card source, int player, AbilityType? tier, bool finalStep = false,
        Occurrence? continuation = null) =>
        new(world,
            source,
            continuation ?? new Occurrence(
                0, [Steps.CardRevealed], Subject: source.ObjectId, Player: player),
            player,
            [],
            this)
        {
            Tier = tier,
            FinalStep = finalStep,
        };

    /// <summary>A suspended resolution with its persisted card bindings restored.</summary>
    private Cast Resuming(
        World world, Card source, int player, AbilityType? tier, bool finalStep = false,
        Occurrence? continuation = null)
    {
        var cast = Resolving(world, source, player, tier, finalStep, continuation);
        if (world.Agenda.Current?.Discarded is { } discarded)
        {
            cast.Discarded.AddRange(discarded.Select(id => world.Cards[id]));
        }
        return cast;
    }

    /// <summary>The one choice a card offers, found again from the card.</summary>
    /// <remarks>
    /// A step cannot carry an effect tree, so it carries the card, same-timing
    /// ability ordinal, and structural path used to find the node again.
    /// </remarks>
    private AbilityEffect Choice(
        World world, Card source, int player, int stoppedAt, AbilityType? tier)
    {
        // **Which ability, when a card has a choice in more than one.** The
        // step carries the tier that suspended, because the card and the
        // position do not say: Infinite Hunter's "When Revealed" chooses an ally
        // and its "Boost" chooses between two effects, and picking the first
        // ability with a choice in it would resume the wrong one -- silently,
        // and with a legal-looking question about the wrong cards.
        var persisted = ContinuationStep(world, source, stoppedAt, tier);
        var written = AbilitiesOn(source, persisted?.AbilityFace)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ToList();
        var cast = Resuming(
            world, source, player, tier,
            continuation: persisted?.AbilityOccurrence);
        RestorePersisted(cast, persisted);
        if (persisted is
            { AbilityOrdinal: >= 0, AbilityPath: { } path } step)
        {
            var ability = written.ElementAtOrDefault(step.AbilityOrdinal)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' has no '{tier}' ability {step.AbilityOrdinal}");
            cast.RestoreAbility(step.AbilityOrdinal, path, step.AbilityFace);
            RestorePathBindings(cast, path);
            var exact = NodeAtPath(ability.Effect, path);
            return ActiveChoices(exact, cast).SingleOrDefault()
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' has no choice at its persisted ability path");
        }

        if (written.Count > 1
            && written.Count(a => ActiveChoices(a.Effect, cast).Any()) > 1)
        {
            // Two choices at one tier on one card. The tier is as fine as the
            // step gets, so this is the next thing to carry rather than
            // something to guess at.
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has a choice in more than one '{tier}' ability, and a "
                + "suspended ability is found again from its card and its tier");
        }

        var effect = written
            .Select(ability => ability.Effect)
            .FirstOrDefault(tree => ActiveChoices(tree, cast).Any())
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice waiting on an answer");

        if (effect.OperationName() != "seq")
        {
            return ActiveChoices(effect, cast).Single();
        }

        var steps = OrderedEffects(effect).ToList();
        if (stoppedAt >= 1 && stoppedAt <= steps.Count)
        {
            var nested = ActiveChoices(steps[stoppedAt - 1], cast).ToList();
            if (nested.Count == 1)
            {
                return nested[0];
            }
        }

        throw new RulesNotImplementedException(
            $"'{source.FaceId}' has no single choice at step {stoppedAt - 1} of its sequence");
    }

    // MARVEL-408 still owns how a continuation path is stored and how its
    // exact choice node is reconstructed. This bridge ends that wire concern:
    // structural legality receives only typed parent/cursor facts.
    private AbilityContinuationFacts StructuralContinuationFacts(Cast cast)
    {
        if (cast.AbilityOrdinal < 0 || cast.AbilityPath.Count == 0)
            return AbilityContinuationFacts.Empty;

        var root = AbilitiesOn(cast.Source, cast.AbilityFace)
            .Where(ability => cast.Tier is null || ability.Trigger.Timing == cast.Tier)
            .ElementAtOrDefault(cast.AbilityOrdinal)?.Effect;
        if (root is null)
            return AbilityContinuationFacts.Empty;

        var frames = ImmutableArray.CreateBuilder<AbilityContinuationFrame>();
        for (int position = 0; position < cast.AbilityPath.Count; position++)
        {
            string encoded = cast.AbilityPath[position];
            var parts = encoded.Split(':');
            var prefix = cast.AbilityPath.Take(position).ToList();
            var parent = prefix.Count == 0 ? root : NodeAtPath(root, prefix);
            switch (parts[0])
            {
                case "seq":
                    frames.Add(new SequenceContinuationFrame(
                        (AbilityEffect.Sequence)parent, ParseIndex(parts, encoded)));
                    break;
                case "then":
                case "otherwise":
                    if (parts.Length >= 2 && parts[1] == "effect")
                    {
                        AbilityStructuralOutcome? outcome = parts.Length >= 3
                            && Enum.TryParse(parts[2], out ResolutionOutcome parsed)
                                ? (AbilityStructuralOutcome)(int)parsed
                                : null;
                        frames.Add(new DependentContinuationFrame(
                            (AbilityEffect.Dependent)parent, Predecessor: true, outcome));
                    }
                    break;
                case "and":
                    frames.Add(new SimultaneousContinuationFrame(
                        (AbilityEffect.Simultaneous)parent,
                        [.. ValidRemaining(parent, parts, encoded)]));
                    break;
                case "forEach":
                    frames.Add(new ForEachContinuationFrame(
                        (AbilityEffect.ForEach)parent,
                        ParseIndex(parts, encoded), ParseForEachCount(parts, encoded)));
                    break;
                case "eachTime":
                    frames.Add(new EachTimeContinuationFrame(
                        (AbilityEffect.EachTime)parent,
                        ParseIndex(parts, encoded), ParseForEachCount(parts, encoded)));
                    break;
                case "eachPlayer":
                    frames.Add(new EachPlayerContinuationFrame(
                        cast.EachPlayerFrame && !cast.FinalPlayer));
                    break;
            }
        }
        return new AbilityContinuationFacts(true, frames.ToImmutable());
    }

    private static IEnumerable<AbilityEffect> Choices(AbilityEffect node) =>
        AbilityInitiation.Choices(node);

    private static IEnumerable<AbilityEffect> ActiveChoices(AbilityEffect node, Cast cast) =>
        AbilityInitiation.ActiveChoices(node, AdmissionContext(cast));

    private static bool IsChoice(AbilityEffect node) => AbilityInitiation.IsChoice(node);

    private static bool SuspendsInsideAnd(
        AbilityEffect node, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        AbilityInitiation.SuspendsInsideAnd(
            node, AdmissionContext(cast), stateMayChange, bindingMayChange);

}
