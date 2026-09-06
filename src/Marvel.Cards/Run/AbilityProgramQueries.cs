using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Program-backed constant queries shared by live resolution and read-only
// admission. Their inputs are immutable definitions plus board facts, never a
// runner callback.
internal static class AbilityProgramQueries
{
    internal static IReadOnlyList<CompiledCardAbility> On(AbilityProgram program, Card card) =>
        FacedownDrones.Is(card) ? [] : program.On(card.FaceId);

    internal static string ResourcesGeneratedBy(
        World world, AbilityProgram program, Card source, Card? payingFor)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        string printed = Resources.GeneratedBy(source.FaceId, world.Facts);
        if (payingFor is null) return printed;

        string classes = world.Facts.Attributes(payingFor.FaceId)
            .GetValueOrDefault("Class", string.Empty);
        bool doubles = On(program, source).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant
            && ability.Effect is AbilityEffect.DoubleResourceFor multiplier
            && classes.Split(';').Contains(
                multiplier.Classification, StringComparer.Ordinal));

        return doubles ? printed + printed : printed;
    }

    internal static DefenderChoice Defenders(
        World world, AbilityProgram program, EnemyAttack attack, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(candidates);

        var enemy = world.Cards[attack.Enemy];
        bool requiresControlledAlly = On(program, enemy).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant
            && ability.Effect is AbilityEffect.Fixed
            { Instruction: AbilityFixedInstruction.RequireAllyDefender });
        if (!requiresControlledAlly) return new DefenderChoice(candidates, Required: false);

        var allies = candidates.Where(card =>
            card.Ready
            && card.Area.PlayArea == PlayArea.Of(attack.Player)
            && world.Facts.Kind(card.FaceId) == CardKind.Ally).ToList();
        return allies.Count > 0
            ? new DefenderChoice(allies, Required: true)
            : new DefenderChoice(candidates, Required: false);
    }

    internal static int? AttachesTo(World world, AbilityProgram program, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var candidates = AttachmentCandidates(world, program, card);
        return candidates is null ? null : candidates.Count switch
        {
            0 => null,
            1 => candidates[0].ObjectId,
            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' can attach to {candidates.Count} equally eligible cards. "
                + "rr:first-player.1 gives that choice to the first player, and attaching "
                + "during a reveal has no target prompt yet"),
        };
    }

    internal static IReadOnlyList<int>? AttachmentTargets(
        World world, AbilityProgram program, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        return AttachmentCandidates(world, program, card) is { } candidates
            ? [.. candidates.Select(candidate => candidate.ObjectId)]
            : null;
    }

    internal static int? SetupController(World world, AbilityProgram program, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        return program.ControlledByFirstPlayer.Contains(card.FaceId) ? world.FirstPlayer : null;
    }

    internal static void ValidateForPlay(World world, AbilityProgram program)
    {
        ArgumentNullException.ThrowIfNull(world);

        var incomplete = world.Cards.FirstOrDefault(card =>
            DeckTypes.IsInPlay(card.Area.Type) && program.PlacementOnly.Contains(card.FaceId));
        if (incomplete is not null)
        {
            throw new RulesNotImplementedException(
                $"card '{incomplete.FaceId}' is in play, but only its setup placement "
                + "and absence of a When Revealed ability are implemented; its remaining "
                + "printed text is not implemented");
        }
    }

    internal static IReadOnlyList<Card> PlayerSetupCards(
        World world, AbilityProgram program, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Players);

        return
        [
            .. world.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type)
                    && area.PlayArea == PlayArea.Of(player))
                .SelectMany(area => area.Cards)
                .Where(card => AbilityCardQueries.ControllerOf(world, card) == player
                    && On(program, card).Any(
                        ability => ability.Trigger.Timing == AbilityType.Setup)),
        ];
    }

    internal static CardCounterPool? CounterPool(World world, AbilityProgram program, Card card)
    {
        if (program.CounterPools.GetValueOrDefault(card.FaceId) is { } authored) return authored;
        var (count, type) = Reveal.Uses(world.Facts.Attributes(card.FaceId));
        return count > 0 ? new CardCounterPool(type, checked((int)count), Uses: true) : null;
    }

    private static IReadOnlyList<Card>? AttachmentCandidates(
        World world, AbilityProgram program, Card card)
    {
        if (program.AttachTo.GetValueOrDefault(card.FaceId) is not { } selector) return null;

        // Attachment placement asks a board question; it neither resolves an
        // ability nor emits events, so its bindings are a query context.
        var context = new AbilityQueryContext(
            world, card, new Occurrence(0, []), card.Owner, card.Incarnation,
            null, null, null, []);
        return new AbilitySelectorEvaluation(context, null, program).Every(selector);
    }

    internal static bool CanReady(World world, AbilityProgram program, Card target)
    {
        foreach (var card in world.Areas.Where(area => DeckTypes.IsInPlay(area.Type))
                     .SelectMany(area => area.Cards))
        {
            if (FacedownDrones.Is(card)) continue;
        foreach (var ability in On(program, card).Where(ability =>
                     ability.Trigger.Timing == AbilityType.Constant))
        {
            var bindings = new AbilityQueryContext(
                world, card, new Occurrence(0, []),
                AbilityCardQueries.ControllerOf(world, card), card.Incarnation,
                null, null, null, []);
            var selectors = new AbilitySelectorEvaluation(bindings, null, program);
            var expressions = new AbilityExpressionEvaluation(
                new AbilityExpressionContext(
                    bindings, ImmutableDictionary<string, long>.Empty, [], string.Empty,
                    -1, false, null), selectors);
            if (ProhibitsReady(ability.Effect, target, selectors, expressions)) return false;
        }
        }
        return true;
    }

    internal static bool CanTakeDamage(
        World world, AbilityProgram program, Card target, Card source)
    {
        if (!DeckTypes.IsInPlay(target.Area.Type) || FacedownDrones.Is(target)) return true;
        foreach (var ability in On(program, target).Where(ability =>
                     ability.Trigger.Timing == AbilityType.Constant))
        {
            var bindings = new AbilityQueryContext(
                world, target, new Occurrence(0, []),
                AbilityCardQueries.ControllerOf(world, target), target.Incarnation,
                null, null, null, []);
            var expressions = new AbilityExpressionEvaluation(
                new AbilityExpressionContext(
                    bindings, ImmutableDictionary<string, long>.Empty, [], string.Empty,
                    -1, false, null),
                new AbilitySelectorEvaluation(bindings));
            if (ProhibitsDamage(ability.Effect, world, source, expressions)) return false;
        }
        return true;
    }

    internal static bool CanRemoveThreat(
        World world, AbilityProgram program, Card scheme, int ignoredSource = -1)
    {
        foreach (var card in world.Cards.Where(card =>
            card.ObjectId != ignoredSource && DeckTypes.IsInPlay(card.Area.Type)))
        {
            foreach (var ability in On(program, card).Where(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
            {
                var bindings = new AbilityQueryContext(
                    world, card, new Occurrence(0, []),
                    AbilityCardQueries.ControllerOf(world, card), card.Incarnation,
                    null, null, null, []);
                var selectors = new AbilitySelectorEvaluation(bindings, null, program);
                var expressions = new AbilityExpressionEvaluation(
                    new AbilityExpressionContext(
                        bindings, ImmutableDictionary<string, long>.Empty, [],
                        string.Empty, -1, false, null), selectors);
                if (ProhibitsThreatRemoval(
                    ability.Effect, scheme, selectors, expressions)) return false;
            }
        }
        return true;
    }

    private static bool ProhibitsThreatRemoval(
        AbilityEffect effect, Card scheme, AbilitySelectorEvaluation selectors,
        AbilityExpressionEvaluation expressions) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
            ProhibitsThreatRemoval(step, scheme, selectors, expressions)),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
            ProhibitsThreatRemoval(step, scheme, selectors, expressions)),
        AbilityEffect.Conditional conditional => (expressions.Test(conditional.Test)
                ? conditional.Then : conditional.Else) is { } branch
            && ProhibitsThreatRemoval(branch, scheme, selectors, expressions),
        AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.PreventThreatRemoval } prohibition =>
            selectors.Find(prohibition.Selection)?.ObjectId == scheme.ObjectId,
        _ => false,
    };

    private static bool ProhibitsDamage(
        AbilityEffect effect, World world, Card source, AbilityExpressionEvaluation expressions) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
            ProhibitsDamage(step, world, source, expressions)),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
            ProhibitsDamage(step, world, source, expressions)),
        AbilityEffect.Conditional conditional => (expressions.Test(conditional.Test)
                ? conditional.Then : conditional.Else) is { } branch
            && ProhibitsDamage(branch, world, source, expressions),
        AbilityEffect.PreventDamageFrom prohibition => world.Facts.Kind(source.FaceId)
            == prohibition.SourceKind && Rules.State.Traits.Has(
                world, source, prohibition.SourceTrait, world.Facts),
        AbilityEffect.PreventDamageWhile prohibition => expressions.Test(prohibition.Condition),
        _ => false,
    };

    private static bool ProhibitsReady(
        AbilityEffect effect, Card target, AbilitySelectorEvaluation selectors,
        AbilityExpressionEvaluation expressions) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
            ProhibitsReady(step, target, selectors, expressions)),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
            ProhibitsReady(step, target, selectors, expressions)),
        AbilityEffect.Conditional conditional => (expressions.Test(conditional.Test)
                ? conditional.Then : conditional.Else) is { } branch
            && ProhibitsReady(branch, target, selectors, expressions),
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PreventReady } prohibition =>
            selectors.Find(prohibition.Selection)?.ObjectId == target.ObjectId,
        _ => false,
    };
}
