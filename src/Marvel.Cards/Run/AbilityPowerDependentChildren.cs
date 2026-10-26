using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

// A dependent continuation is reachable only from its required projected outcome.
internal static class AbilityPowerDependentChildren
{
    internal static IEnumerable<(AbilityEffect Node, bool StateMayChange,
        bool BindingMayChange, RuleProjection<AbilityPowerState> Reachability)> Of(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, RuleProjection<AbilityPowerState> state)
    {
        var body = EffectBody(node);
        yield return (body, stateMayChange, bindingMayChange, state);
        var required = node.OperationName() == "then"
            ? AdmissionResolution.Full : AdmissionResolution.None;
        foreach (var outcome in AbilityPowerTrace.PowerOutcomeStates(
            body, cast, stateMayChange, bindingMayChange, state))
        {
            if (outcome.Outcome != required) continue;
            yield return (EffectFollowing(node), true, bindingMayChange, outcome.State);
        }
    }
}
