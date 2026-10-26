using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Describes the current owned Invocation without predicting its later choices.</summary>
internal static class AbilityInvocationDescription
{
    internal static string Cost(World world, int player)
    {
        var top = AbilityInvocationPayment.Top(world, player)
            ?? throw new RulesNotImplementedException("no owned public Invocation top to describe");
        return $"Pay {AbilityInvocationPayment.Amount(world, player)} resources for {world.Facts.Title(top.FaceId)}'s Special";
    }

    internal static string? Summary(AbilityResolutionExecution execution, World world,
        int player, AbilityEffect effect)
    {
        bool returns = effect is AbilityEffect.Dependent
        { Effect: AbilityEffect.ResolveInvocation, Continuation: AbilityEffect.ReturnInvocation, OnFull: true };
        if (effect is not AbilityEffect.ResolveInvocation && !returns) return null;
        if (AbilityInvocationPayment.Top(world, player) is not { } top) return null;
        string summary = $"Resolve {world.Facts.Title(top.FaceId)}'s Special from your public Invocation top. Later choices and interrupts can change the result.";
        // An unconditional final sequence instruction is independent of earlier application.
        // Do not infer self-discard from the Invocation's printed name or card kind.
        var specials = execution.On(top).Where(ability => ability.Trigger.Timing == AbilityType.Special).ToArray();
        if (specials.Length == 1 && specials[0].Effect is AbilityEffect.Sequence sequence
            && sequence.Effects.LastOrDefault() is AbilityEffect.DiscardInvocation)
            summary += " Its instructions independently discard this Invocation and refresh the public top, even if the other effects do not apply.";
        if (returns)
            summary += " After its Special completes, return that exact physical Invocation faceup to the top of your Invocation deck.";
        return summary;
    }
}
