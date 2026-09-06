using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// The read-only input to admission. It deliberately omits events, runner
// callbacks, continuation frames, and every mutable collection on Cast.
internal sealed record AbilityAdmissionContext(
    AbilityProgram Program,
    AbilityExpressionContext Expressions,
    AbilityReachabilityContext Reachability,
    string? Power,
    bool HasContinuation = false)
{
    internal AbilityQueryContext Query => Expressions.Bindings;
    internal World World => Query.World;
    internal Card Source => Query.Source;

    internal AbilityAdmissionContext WithQuery(AbilityQueryContext query) =>
        this with { Expressions = Expressions with { Bindings = query } };

    internal AbilityAdmissionContext WithPlayer(int player) =>
        WithQuery(Query with { Player = player });

    internal AbilityAdmissionContext WithSelection(Card? card)
    {
        AbilityCardReference? binding = card is null
            ? null
            : new AbilityCardReference(card, card.Area.Id, card.Incarnation);
        return WithQuery(Query with
        {
            ChosenBinding = binding,
            PlayerSelectionBinding = binding,
        });
    }

    internal AbilityAdmissionContext WithChosen(Card? card)
    {
        AbilityCardReference? binding = card is null
            ? null
            : new AbilityCardReference(card, card.Area.Id, card.Incarnation);
        return WithQuery(Query with { ChosenBinding = binding });
    }

    internal AbilityAdmissionContext WithAltered(Card? card) =>
        WithQuery(Query with { Altered = card });

    internal AbilityAdmissionContext WithPowerTargets(IEnumerable<Card> targets) =>
        WithQuery(Query with { PowerTargets = [.. targets] });

    internal AbilityAdmissionContext WithPower(string? power) => this with { Power = power };

    internal AbilityAdmissionContext WithContinuation(bool hasContinuation) =>
        this with { HasContinuation = hasContinuation };

    internal AbilityAdmissionContext WithReachability(AbilityReachabilityContext reachability) =>
        this with { Reachability = reachability };

    internal AbilitySelectorEvaluation Selectors(
        AbilitySingularAreaAdmission? singularAreaAdmission = null) =>
        new(Query, singularAreaAdmission, Program);

    internal AbilityExpressionEvaluation Evaluator(
        AbilitySingularAreaAdmission? singularAreaAdmission = null)
    {
        var selectors = Selectors(singularAreaAdmission);
        return new AbilityExpressionEvaluation(Expressions, selectors);
    }
}

// The only mutable product of admission is scoped evidence that a continuation
// serializes by address. It is returned explicitly and never aliases Cast.
internal sealed record AbilityAdmissionResult(
    bool IsAdmissible,
    ImmutableHashSet<AbilityEffect> CrisisIgnoringThwarts)
{
    internal static AbilityAdmissionResult Rejected { get; } = new(false, []);
}
