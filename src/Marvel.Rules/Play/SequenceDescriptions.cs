using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal static class SequenceDescriptions
{
    internal static string AttackStage(string step) => step switch
    {
        Steps.Attack => "Initiation",
        Steps.GiveBoostCard => "Step 1 of 6 · Give boost card",
        Steps.DeclareDefender => "Step 2 of 6 · Declare defender",
        Steps.FlipBoostCards => "Step 3 of 6 · Reveal boost cards",
        Steps.CalculateAttackDamage => "Step 4 of 6 · Calculate damage",
        Steps.DealAttackDamage => "Step 5 of 6 · Deal damage",
        Steps.NextAttackTarget => "Choose the next target",
        Steps.EndAttack => "Step 6 of 6 · End attack",
        _ => step,
    };
}
