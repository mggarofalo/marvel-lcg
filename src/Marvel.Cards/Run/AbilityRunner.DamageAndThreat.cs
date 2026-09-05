using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    // MARVEL-375: syntax identifies the suspended procedure until continuations
    // use program addresses directly. Only the compiled instruction supplies
    // the operation's arguments.
    private static bool TryRunDamageAndThreat(AbilityEffect instruction, AbilityNode syntax, Cast cast)
    {
        switch (instruction)
        {
            case AbilityEffect.Damage damage:
                DealDamage(damage, syntax, cast);
                return true;
            case AbilityEffect.AttackDamage damage:
                DealAttackDamage(damage, syntax, cast);
                return true;
            case AbilityEffect.MoveDamage { Attack: false } movement:
                MoveDamage(movement, syntax, cast);
                return true;
            case AbilityEffect.MoveDamage movement:
                MoveAttackDamage(movement, syntax, cast);
                return true;
            case AbilityEffect.IndirectDamage damage:
                Indirect(damage, syntax, cast);
                return true;
            case AbilityEffect.PlaceThreat threat:
                PlaceThreat(threat, cast);
                return true;
            case AbilityEffect.RemoveThreat removal:
                RemoveThreat(removal, cast);
                return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.SoakDamage } soak:
                Soak(soak.Selection, cast);
                return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ReplaceThreatWithDamage } replacement:
                ReplaceThreatWithDamage(replacement.Selection, syntax, cast);
                return true;
            default:
                return false;
        }
    }
}
