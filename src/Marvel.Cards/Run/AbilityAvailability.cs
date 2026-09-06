using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Stable identities and read-only availability for printed limits and maxima.
// Recording a use remains an execution responsibility.
internal static class AbilityAvailability
{
    internal static bool Available(
        World world, Card card, CompiledCardAbility ability, int abilityIndex,
        Occurrence? occurrence = null)
    {
        if (ability.Limit is { } limit
            && world.Effects.Active().Count(effect => effect.Card == card.ObjectId
                && string.Equals(effect.Kind, Spent(card, ability, abilityIndex), StringComparison.Ordinal)) >= limit)
        {
            return false;
        }

        if (ability.Maximum is not { } maximum) return true;
        if (maximum.Period == MaximumPeriod.Instance && occurrence is null)
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' has a per-instance maximum outside an occurrence window");
        }

        string key = MaximumSpent(world, card, maximum.Period, occurrence);
        return world.Effects.Active().Count(effect =>
            string.Equals(effect.Kind, key, StringComparison.Ordinal)) < maximum.Uses;
    }

    internal static int IndexOf(AbilityProgram program, Card card, CompiledCardAbility ability)
    {
        var written = AbilityProgramQueries.On(program, card);
        int index = written.ToList().FindIndex(candidate => ReferenceEquals(candidate, ability));
        if (index >= 0) return index;
        throw new RulesNotImplementedException(
            $"card '{card.FaceId}' used an ability that is not on its current face");
    }

    internal static string MaximumSpent(
        World world, Card card, MaximumPeriod period, Occurrence? occurrence)
    {
        string instance = period == MaximumPeriod.Instance
            ? ":" + (occurrence?.Id
                ?? throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has a per-instance maximum without an occurrence"))
                .ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        return $"maximum:{period}:{world.Facts.Title(card.FaceId)}{instance}";
    }

    internal static string Spent(Card card, CompiledCardAbility ability, int abilityIndex) =>
        "spent:"
        + card.Incarnation.ToString(CultureInfo.InvariantCulture)
        + ":" + ability.Card + ":"
        + abilityIndex.ToString(CultureInfo.InvariantCulture);
}
