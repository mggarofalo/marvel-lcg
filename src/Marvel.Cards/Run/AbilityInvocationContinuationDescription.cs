using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Explains an accepted Invocation action while its exact Special still asks for choices.</summary>
internal static class AbilityInvocationContinuationDescription
{
    internal static Prompt WithContext(AbilityResolutionExecution execution, World world, PhaseStep step, Prompt prompt)
    {
        foreach (var parent in EnclosingAbilityContinuations.For(world.Agenda, step).OfType<AbilityContinuationState>())
        {
            if (!MatchesPaidInvocation(parent, world, step, prompt.Player, out long paid)) continue;
            return Describe(execution, world, step, prompt, parent, paid);
        }
        return prompt;
    }

    private static bool MatchesPaidInvocation(AbilityContinuationState parent, World world,
        PhaseStep step, int player, out long paid)
    {
        paid = 0;
        var memory = parent.Memory.Results;
        if (!memory.TryGetValue(AbilityInvocationPayment.CardKey, out long id) || id != step.Subject
            || !memory.TryGetValue(AbilityInvocationPayment.CopyKey, out long copy)
            || !memory.TryGetValue(AbilityInvocationPayment.OwnerKey, out long owner) || owner != player
            || !memory.TryGetValue(AbilityInvocationPayment.CostKey, out paid)) return false;
        var invocation = world.Cards[step.Subject];
        return invocation.Incarnation == copy && invocation.FaceUp
            && world.Seats[player].InvocationDeck?.PublicTop == invocation;
    }

    private static Prompt Describe(AbilityResolutionExecution execution, World world, PhaseStep step,
        Prompt prompt, AbilityContinuationState parent, long paid)
    {
        var invocation = world.Cards[step.Subject];
        var source = world.Cards[parent.Invocation.Source.ObjectId];
        var address = parent.Invocation.Address;
        var ability = AbilityContinuationFrames.AbilityAt(execution.program, source, address.Tier, address.Ordinal, address.Face);
        bool returns = ability.Effect is AbilityEffect.Dependent
            { Effect: AbilityEffect.ResolveInvocation, Continuation: AbilityEffect.ReturnInvocation, OnFull: true };
        string title = world.Facts.Title(address.Face);
        string name = string.IsNullOrWhiteSpace(ability.Name) ? title : ability.Name;
        string commitment = $"Already paid: {paid} resource{(paid == 1 ? string.Empty : "s")} for {world.Facts.Title(invocation.FaceId)}'s Special.";
        if (world.Facts.Kind(address.Face) == CardKind.Event)
            commitment += $" {title}'s event cost is also paid.";
        foreach (int exhausted in Exhaustions(ability.Cost, source.ObjectId, world.Seats[prompt.Player].IdentityCard.ObjectId)
                     .Concat(parent.Memory.ExhaustedForCost).Distinct().Order())
            commitment += $" {world.Facts.Title(world.Cards[exhausted].FaceId)} was exhausted for this cost.";
        return prompt with
        {
            Description = $"Continuing {(name == title ? title : $"{name} ({title})")}. {commitment}"
                + AbilityInvocationDescription.Consequences(execution, world, invocation, returns)
                + " Closing this choice does not refund these costs. " + prompt.Description,
            // The prompt retains the shared instructions beside the accepted parent context.
            // Distinct option descriptions remain attached to their exact choices.
            Affordances = prompt.Affordances.Count == 1 && !string.IsNullOrWhiteSpace(prompt.Description)
                && prompt.Affordances[0].Description == prompt.Description
                    ? [prompt.Affordances[0] with { Description = null }] : prompt.Affordances,
        };
    }

    private static IEnumerable<int> Exhaustions(AbilityCost? cost, int source, int identity) => cost switch
    {
        AbilityCost.Exhaust exhaust => [exhaust.Card == AbilityCostCard.Source ? source : identity],
        AbilityCost.Sequence sequence => sequence.Costs.SelectMany(step => Exhaustions(step, source, identity)),
        _ => [],
    };
}
