using System.Collections.Immutable;
using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static IEnumerable<AbilityEffect> AllEffectChildren(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects,
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects,
        AbilityEffect.Conditional conditional => ConditionalBranches(conditional),
        AbilityEffect.Dependent dependent => [dependent.Effect, dependent.Continuation],
        AbilityEffect.EachPlayer each => [each.Effect],
        AbilityEffect.ForEach repeated => [repeated.Effect],
        AbilityEffect.EachTime each => [each.Effect, each.Then],
        AbilityEffect.Choose choose => choose.Options,
        AbilityEffect.ChooseCard choose => [choose.Effect],
        AbilityEffect.AfterActivation after => [after.Effect],
        AbilityEffect.PayOrEffect payment => [payment.Otherwise],
        AbilityEffect.Power power => [power.Effect],
        AbilityEffect.ThwartGroup group => [group.Thwart],
        _ => [],
    };

    private static AbilityNumber? EffectAmount(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Heal heal => heal.Amount,
        AbilityEffect.Damage damage => damage.Amount,
        AbilityEffect.AttackDamage damage => damage.Amount,
        AbilityEffect.IndirectDamage damage => damage.Amount,
        AbilityEffect.MoveDamage damage => damage.Amount,
        AbilityEffect.PlaceThreat threat => threat.Amount,
        AbilityEffect.RemoveThreat threat => threat.Amount,
        AbilityEffect.PreventDamage damage => damage.Amount,
        AbilityEffect.GrantField grant => grant.Amount,
        AbilityEffect.GrantControlledCharacters grant => grant.Amount,
        AbilityEffect.ReduceNextCardCost reduction => reduction.Amount,
        _ => null,
    };

    private static IEnumerable<AbilityNumber> EffectNumbers(AbilityEffect effect)
    {
        if (EffectAmount(effect) is { } amount) yield return amount;
        var count = effect switch
        {
            AbilityEffect.ForEach repeated => repeated.Count,
            AbilityEffect.DiscardAtRandom random => random.Count,
            AbilityEffect.PlaceAtRandom random => random.Count,
            AbilityEffect.DiscardTop discard => discard.Count,
            AbilityEffect.PlaceCounters counters => counters.Count,
            AbilityEffect.PreventThreat prevention => prevention.Amount,
            _ => null,
        };
        if (count is not null) yield return count;
    }

    private static IEnumerable<AbilityCondition> EffectConditions(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Conditional conditional => [conditional.Test],
        AbilityEffect.EachTime each => [each.When],
        AbilityEffect.PreventDamageWhile prevention => [prevention.Condition],
        _ => [],
    };

    private static IEnumerable<char> PaidResourceQueries(AbilityEffect effect) =>
        EffectConditions(effect).SelectMany(PaidResourceQueries)
            .Concat(EffectNumbers(effect).SelectMany(PaidResourceQueries))
            .Concat(AllEffectChildren(effect).SelectMany(PaidResourceQueries));

    private static IEnumerable<char> PaidResourceQueries(AbilityCondition condition) => condition switch
    {
        AbilityCondition.PaidWithResource resource => [resource.Resource],
        AbilityCondition.All all => all.Operands.SelectMany(PaidResourceQueries),
        AbilityCondition.Any any => any.Operands.SelectMany(PaidResourceQueries),
        AbilityCondition.Negated negated => PaidResourceQueries(negated.Operand),
        AbilityCondition.AtLeast comparison => PaidResourceQueries(comparison.Value).Concat(PaidResourceQueries(comparison.Count)),
        _ => [],
    };

    private static IEnumerable<char> PaidResourceQueries(AbilityNumber number) => number switch
    {
        AbilityNumber.Conditional conditional => PaidResourceQueries(conditional.Test)
            .Concat(PaidResourceQueries(conditional.Then)).Concat(PaidResourceQueries(conditional.Else)),
        AbilityNumber.Sum sum => sum.Operands.SelectMany(PaidResourceQueries),
        AbilityNumber.Product product => product.Operands.SelectMany(PaidResourceQueries),
        AbilityNumber.Minimum minimum => minimum.Operands.SelectMany(PaidResourceQueries),
        _ => [],
    };

    private static bool DiscardTopHasCards(AbilityEffect.DiscardTop discard, Cast cast) =>
        discard.Players is { } players
            ? Seats(players, cast).Any(player => cast.World.Seats[player].Deck.Cards.Count > 0)
            : Area(discard.From, cast).Cards.Count > 0;

    private static ImmutableArray<AbilityEffect> OrderedEffects(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects,
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects,
        _ => throw new InvalidOperationException("Expected an ordered effect collection"),
    };

    private static AbilityEffect EffectBody(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Dependent dependent => dependent.Effect,
        AbilityEffect.EachPlayer each => each.Effect,
        AbilityEffect.ForEach repeated => repeated.Effect,
        AbilityEffect.EachTime each => each.Effect,
        AbilityEffect.ChooseCard choose => choose.Effect,
        AbilityEffect.AfterActivation after => after.Effect,
        AbilityEffect.Power power => power.Effect,
        AbilityEffect.DelayedDiscard delayed => new AbilityEffect.CardAction(AbilityCardInstruction.Discard, delayed.Card),
        _ => throw new InvalidOperationException("Expected an effect with a body"),
    };

    private static AbilityEffect EffectFollowing(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Dependent dependent => dependent.Continuation,
        AbilityEffect.EachTime each => each.Then,
        AbilityEffect.PayOrEffect payment => payment.Otherwise,
        _ => throw new InvalidOperationException("Expected an effect with a continuation"),
    };

    private static AbilityEffect? ConditionalBranch(AbilityEffect effect, string branch) =>
        (effect, branch) switch
        {
            (AbilityEffect.Conditional conditional, "then") => conditional.Then,
            (AbilityEffect.Conditional conditional, "else") => conditional.Else,
            _ => throw new InvalidOperationException("Expected a conditional branch"),
        };

    // The persisted path uses engine-chosen field spellings. Decode those
    // structural frames against typed instructions, not the supplied JSON.
    private static AbilityEffect ContinuationChild(AbilityEffect effect, string field) => (effect, field) switch
    {
        (AbilityEffect.Conditional conditional, "then") when conditional.Then is { } then => then,
        (AbilityEffect.Conditional conditional, "else") when conditional.Else is { } otherwise => otherwise,
        (AbilityEffect.Dependent dependent, "effect") => dependent.Effect,
        (AbilityEffect.Dependent { OnFull: true } dependent, "then") => dependent.Continuation,
        (AbilityEffect.Dependent { OnFull: false } dependent, "otherwise") => dependent.Continuation,
        _ => throw new InvalidOperationException("The continuation does not identify an effect child"),
    };
}
