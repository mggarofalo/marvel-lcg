using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Rejects unsupported completion semantics before a predecessor changes state.</summary>
internal static class AbilityCompletionSupport
{
    internal static void Validate(AbilityEffect effect, string source)
    {
        if (effect.OperationName() is "seq" or "and" or "if" or "then" or "otherwise"
            or "forEach" or "choose" or "chooseCard")
        {
            foreach (var child in AbilityEffectStructure.AllEffectChildren(effect)) Validate(child, source);
            return;
        }
        if (effect.OperationName() is "draw" or "heal" or "removeThreat" or "dealDamage" or "placeThreat"
            or "exhaust" or "ready" or "discard" or "changeForm" or "declareDefender") return;
        throw new RulesNotImplementedException(
            $"'{source}' has no completed-result semantics for '{effect.OperationName()}'");
    }
}
