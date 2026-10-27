using static Marvel.Cards.Dsl.AbilityLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityInvocationLowering
{
    internal static AbilityEffect.ResolveInvocation Resolve(AbilityValue value, AbilityLocation location)
    {
        FixedWord(value, location, "you");
        return new();
    }

    internal static AbilityEffect.DiscardInvocation Discard(AbilityValue value, AbilityLocation location)
    {
        FixedWord(value, location, "this");
        return new();
    }

    internal static AbilityEffect.ReturnInvocation Return(AbilityValue value, AbilityLocation location)
    {
        FixedWord(value, location, "you");
        return new();
    }
}
