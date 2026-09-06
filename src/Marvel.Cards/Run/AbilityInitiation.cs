using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

// Owns admission facts over compiled instructions. Board-dependent work
// receives immutable query, expression, reachability and program snapshots.
internal static partial class AbilityInitiation
{
    /// <summary>Checks the envelope facts that must hold before an ability begins.</summary>
    internal static bool LabelsCanInitiate(
        CompiledCardAbility ability, AbilityAdmissionContext context)
    {
        var labels = ability.Labels;
        if (labels.Length == 0) return true;

        int resolver = AbilityCardQueries.Resolver(context.Query);
        bool cancelled = LabeledAbilities.WouldBeCancelled(
            context.World, context.World.Facts, resolver, context.Source, labels);

        foreach (string power in LabeledAbilities.Known)
        {
            if (!labels.Contains(power, StringComparer.Ordinal)
                && PowerNodes(ability.Effect, power).Any())
            {
                throw new RulesNotImplementedException(
                    $"'{context.Source.FaceId}' contains a {power.ToLowerInvariant()} power "
                    + "that is absent from its ability labels");
            }
        }

        if (!cancelled
            && labels.Contains(Attack.DefenseVerb, StringComparer.Ordinal)
            && !Attack.CanUseDefenseAbility(context.World, resolver))
        {
            return false;
        }

        if (cancelled) return true;
        bool attack = labels.Contains(BasicPowers.AttackVerb, StringComparer.Ordinal);
        bool thwart = labels.Contains(BasicPowers.ThwartVerb, StringComparer.Ordinal);
        if (attack && thwart)
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' has one ability labeled as both attack and "
                + "thwart, whose single combined power occurrence is not implemented");
        if (attack && !GuaranteesOneLabeledPower(ability.Effect, BasicPowers.AttackVerb))
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' has an attack label without exactly one saveable attack power");
        if (thwart && !GuaranteesOneLabeledPower(ability.Effect, BasicPowers.ThwartVerb))
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' has a thwart label without exactly one saveable thwart power");
        return true;
    }

    private static IEnumerable<AbilityEffect> PowerNodes(AbilityEffect node, string power)
    {
        if (string.Equals(node.OperationName(), power.ToLowerInvariant(), StringComparison.Ordinal))
            yield return node;

        foreach (var child in AllEffectChildren(node))
        foreach (var found in PowerNodes(child, power))
            yield return found;
    }

    /// <summary>Whether every executable route enters exactly one labelled power.</summary>
    internal static bool GuaranteesOneLabeledPower(AbilityEffect node, string power)
    {
        if (string.Equals(node.OperationName(), power.ToLowerInvariant(), StringComparison.Ordinal))
            return PowerNodes(node, power).Count() == 1;
        if (node.OperationName() == "chooseCard")
            return GuaranteesOneLabeledPower(EffectBody(node), power);
        if (node.OperationName() == "choose")
        {
            var options = ((AbilityEffect.Choose)node).Options.ToList();
            return options.Count >= 2 && options.All(option => GuaranteesOneLabeledPower(option, power));
        }
        if (node.OperationName() == "if")
            return ConditionalBranch(node, "then") is { } then
                && ConditionalBranch(node, "else") is { } otherwise
                && GuaranteesOneLabeledPower(then, power)
                && GuaranteesOneLabeledPower(otherwise, power);
        if (node.OperationName() != "seq") return false;
        var steps = OrderedEffects(node).ToList();
        return steps.Count > 0 && GuaranteesOneLabeledPower(steps[0], power)
            && steps.Skip(1).All(step => !PowerNodes(step, power).Any());
    }
}
