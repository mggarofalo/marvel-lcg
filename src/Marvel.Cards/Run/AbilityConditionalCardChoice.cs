using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

/// <summary>A candidate cannot be affected by an untaken conditional with no other branch.</summary>
internal static class AbilityConditionalCardChoice
{
    internal static bool HasEffect(AbilityEffect effect, AbilityAdmissionScope scope)
    {
        if (effect is not AbilityEffect.Conditional conditional) return true;
        var branch = Test(conditional.Test, scope) ? conditional.Then : conditional.Else;
        return branch is not null && HasEffect(branch, scope);
    }
}
