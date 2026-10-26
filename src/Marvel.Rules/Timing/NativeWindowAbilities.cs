using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Offers authored abilities and rules-defined keywords in one ordered window.</summary>
internal sealed class NativeWindowAbilities(IWindowAbilities authored) : IWindowAbilities
{
    public bool ObservesHeroDefense(World world) => authored.ObservesHeroDefense(world);

    public bool ObservesPlayerTurnBeginning(World world) => authored.ObservesPlayerTurnBeginning(world);
    public bool ObservesVillainStepOneCompletion(World world) => authored.ObservesVillainStepOneCompletion(world);

    public IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window)
    {
        var printed = authored.Waiting(world, occurrence, window);
        var native = Retaliation(world, occurrence, window).ToList();
        if (GreenGoblinActivationReplacement.Waiting(world, occurrence, window) is { } replacement)
            native.Add(replacement);
        return native.Count == 0 ? printed
            : AbilityOfferHandles.Assign(printed.Concat(native), world.Cards.Count);
    }

    private static IEnumerable<PendingAbility> Retaliation(World world, Occurrence occurrence, WindowKind window)
    {
        if (window != WindowKind.Response || !occurrence.Is(Steps.AttackEnds) || occurrence.Retaliation is not { Ranged: false } context
            || RetaliationEligibility.Attacker(world, context) is null) yield break;
        foreach (var recipient in context.Recipients)
        {
            var character = RetaliationEligibility.Character(world, recipient);
            if (character is null || RetaliationEligibility.Amount(world, character) <= 0) continue;
            yield return new PendingAbility(recipient.Attack.Card, AbilityType.ForcedResponse,
                recipient.Attack.Facts.Controller)
            { Native = NativeAbilityKind.Retaliate, TriggeringAttack = recipient.Attack };
        }
    }

    public Affordance Describe(World world, PendingAbility ability)
    {
        if (ability.Native == NativeAbilityKind.Authored) return authored.Describe(world, ability);
        if (ability.Native == NativeAbilityKind.GreenGoblinActivationReplacement)
            return GreenGoblinActivationReplacement.Describe(world, ability);
        if (ability.Native != NativeAbilityKind.Retaliate) throw new InvalidOperationException("Unknown native ability");
        var context = world.Windows.Current?.Occurrence.Retaliation
            ?? throw new InvalidOperationException("Retaliate description has no current attack window");
        var recipient = context.Recipients.Single(item => item.Attack == ability.TriggeringAttack);
        var character = RetaliationEligibility.Character(world, recipient)
            ?? throw new InvalidOperationException("Retaliate recipient is no longer eligible");
        long amount = RetaliationEligibility.Amount(world, character);
        string target = EffectiveCards.Title(world.Cards[context.Attacker], world.Facts);
        string label = $"Retaliate {amount}: deal {amount} damage to {target}";
        return new(ability.OfferId ?? ability.Card, "Retaliate", ability.Card, ability.Player,
            label, Description: $"{EffectiveCards.Title(character, world.Facts)} deals {amount} damage to {target}.")
        { Timing = AbilityType.ForcedResponse, DisplayLabel = label, CommitLabel = $"Resolve retaliate against {target}" };
    }

    public Func<AbilityExecution, AbilityExecutionResult> PrepareWindow(
        World world, Occurrence occurrence, PendingAbility ability, Decision answer) =>
        ability.Native == NativeAbilityKind.Authored
            ? authored.PrepareWindow(world, occurrence, ability, answer)
            : invocation => Resolve(world, invocation, answer);

    public AbilityExecutionResult Resolve(World world, AbilityExecution invocation, Decision answer)
    {
        if (invocation.Ability.Native == NativeAbilityKind.Authored)
            return authored.Resolve(world, invocation, answer);
        if (invocation.Ability.Native == NativeAbilityKind.GreenGoblinActivationReplacement)
            return GreenGoblinActivationReplacement.Resolve(world, invocation);
        if (invocation.Ability.Native != NativeAbilityKind.Retaliate)
            throw new InvalidOperationException("Unknown native ability");
        return NativeRetaliationResolution.Begin(world, invocation);
    }
}
