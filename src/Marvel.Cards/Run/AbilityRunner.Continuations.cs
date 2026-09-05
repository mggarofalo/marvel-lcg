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
            if (cast.FinalPlayer && effect is { Kind: "seq" } && !cast.Suspended)
            {
                Sequence(effect, cast, from);
            }
            DiscardEvent(source, cast);
            cast.CompleteResolution();
            return cast.Events;
        }

        if (effect is { Kind: "seq" } && !cast.Suspended)
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
    private AbilityNode Choice(
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

        if (effect.Kind != "seq")
        {
            return ActiveChoices(effect, cast).Single();
        }

        var steps = Nodes(effect.Argument).ToList();
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

    /// <summary>Every <c>choose</c> node in one effect tree.</summary>
    private static IEnumerable<AbilityNode> Choices(AbilityNode node)
    {
        if ((node.Kind == "and" && Nodes(node.Argument).Skip(1).Any())
            || IsChoice(node))
        {
            yield return node;
            yield break;
        }

        var children = node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument),
            "if" => Branches
                .Select(node.Field)
                .Where(branch => branch is not null)
                .Select(branch => Tree(branch!)),
            "then" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            "otherwise" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("otherwise")),
            ],
            "eachPlayer" or "forEach" => [Tree(node.Require("effect"))],
            "defense" => [Tree(node.Require("effect"))],
            _ => [],
        };

        foreach (var found in children.SelectMany(Choices))
        {
            yield return found;
        }
    }

    private static bool IsChoice(AbilityNode node) =>
        node.Kind is "choose" or "chooseCard" or "indirectDamage"
            or "resolveSpecials" or "payOrExhaust" or "chooseTopForHand"
            or "chooseDiscardToShuffle" or "thwartDifferentSchemes" or "makeTheCall"
            or "legalPractice" or "payOrEffect" or "enemyAttacks" or "enemySchemes";

    /// <summary>Choice nodes on the control-flow path that can execute now.</summary>
    private static IEnumerable<AbilityNode> ActiveChoices(AbilityNode node, Cast cast)
    {
        if (CurrentlyZeroForEach(node, cast))
        {
            yield break;
        }

        if (node.Kind == "and" && Nodes(node.Argument).Skip(1).Any())
        {
            yield return node;
            yield break;
        }

        if (node.Kind is "enemyAttacks" or "enemySchemes")
        {
            if (ActivationCandidates(ActivationOf(node, cast), cast).Count > 1)
            {
                yield return node;
            }
            yield break;
        }

        if (IsChoice(node))
        {
            if (node.Kind != "indirectDamage"
                || Assignable(((AbilityEffect.IndirectDamage)
                    ((AbilityRunner)cast.Abilities).CompiledEffect(node)).Among, cast).Count > 1)
            {
                yield return node;
            }
            yield break;
        }

        if (node.Kind is "then" or "otherwise")
        {
            var preceding = Tree(node.Require("effect"));
            var precedingChoices = ActiveChoices(preceding, cast).ToList();
            foreach (var found in precedingChoices)
            {
                yield return found;
            }
            if (precedingChoices.Count > 0)
            {
                yield break;
            }

            var required = node.Kind == "then"
                ? ResolutionOutcome.Full
                : ResolutionOutcome.None;
            if (ResolutionOf(preceding, cast) == required)
            {
                foreach (var found in ActiveChoices(
                    Tree(node.Require(node.Kind)), cast))
                {
                    yield return found;
                }
            }
            yield break;
        }

        var children = node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument),
            "if" => node.Field(Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch ? [Tree(branch)] : [],
            "eachPlayer" or "forEach" => [Tree(node.Require("effect"))],
            "defense" => [Tree(node.Require("effect"))],
            _ => [],
        };

        foreach (var found in children.SelectMany(child => ActiveChoices(child, cast)))
        {
            yield return found;
        }
    }

    private static bool SuspendsInsideAnd(
        AbilityNode node, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        node.Kind == "placeThreat"
        || GuardChildren(node, cast, stateMayChange, bindingMayChange, null).Any(child =>
            SuspendsInsideAnd(
                child.Node, cast, child.StateMayChange, child.BindingMayChange));

}
