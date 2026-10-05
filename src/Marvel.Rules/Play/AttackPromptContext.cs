using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Adds the current enemy attack's engine facts to a window prompt.</summary>
internal static class AttackPromptContext
{
    internal static Prompt WithAttackContext(
        World world, ICardFacts facts, PhaseStep step, Prompt prompt)
    {
        EnemyAttack? attack = AttackForPrompt(world, step);
        if (attack is null || attack.Enemy < 0 || attack.Target < 0)
        {
            return prompt;
        }

        Card enemy = world.Cards[attack.Enemy];
        Card target = world.Cards[attack.Target];
        string player = AttackPlayerName(world, attack.Player);
        string stage = SequenceDescriptions.AttackStage(step.What);
        string window = AttackWindowDescription(world.Agenda.Stage);
        string situation = AttackSituation(
            world, facts, attack, enemy, target, player, step);
        string[] attachments = AttackAttachments(world, facts, enemy);
        situation = AppendAttackContext(situation, attachments, prompt.Description);
        IReadOnlyList<int> causes = ActivationCausalCards.For(world, step);

        return prompt with
        {
            Description = $"Enemy attack · {stage} · {window}\n{situation}",
            CauseCardIds = causes,
        };
    }

    private static EnemyAttack? AttackForPrompt(World world, PhaseStep step) =>
        world.Attack ?? (step.What == Steps.EndAttack ? world.FinishedAttack : null);

    private static string AttackPlayerName(World world, int player) =>
        player >= 0 && player < world.Seats.Count
            ? world.Seats[player].Name
            : $"Player {player + 1}";

    private static string AppendAttackContext(
        string situation, string[] attachments, string? description)
    {
        if (attachments.Length > 0)
        {
            situation += $" Attacker attachments: {string.Join(", ", attachments)}.";
        }
        if (!string.IsNullOrWhiteSpace(description)
            && !situation.Contains(description, StringComparison.Ordinal))
        {
            situation += $" {description}";
        }
        return situation;
    }

    private static string AttackWindowDescription(Stage stage) => stage switch
    {
        Stage.Interrupts => "Interrupt window",
        Stage.Responses => "Response window",
        _ => "Resolve step",
    };

    private static string[] AttackAttachments(World world, ICardFacts facts, Card enemy) =>
        world.Areas
            .Where(area => area.Host == enemy.ObjectId && DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Select(card => facts.Title(card.FaceId))
            .ToArray();

    private static string AttackSituation(
        World world, ICardFacts facts, EnemyAttack attack, Card enemy, Card target,
        string player, PhaseStep step)
    {
        if (world.Attack is null && step.What == Steps.EndAttack)
        {
            return FinishedAttackSituation(world, facts, attack, enemy, target, player);
        }
        if (attack.CalculatedDamage is { } damage)
        {
            return $"{AttackCompletion.SubjectTitle(world, facts, enemy.ObjectId)} is attacking {EffectiveCards.Title(target, facts)} "
                + $"for {damage} damage against {player}. "
                + Damage.PreviewAttack(world, facts, enemy, enemy, target, damage);
        }
        long attackValue = StateFields.Modified(world, enemy, "attack", facts, world.Players);
        return $"{AttackCompletion.SubjectTitle(world, facts, enemy.ObjectId)} is initiating an attack against {player}. "
            + $"Target: {EffectiveCards.Title(target, facts)}. "
            + $"ATK {attackValue} {AttackBoostDescription.Before(world, facts, enemy, step.What)}.";
    }

    private static string FinishedAttackSituation(
        World world, ICardFacts facts, EnemyAttack attack, Card enemy, Card target,
        string player)
    {
        string targetState;
        if (!DeckTypes.IsInPlay(target.Area.Type))
        {
            targetState = $"{EffectiveCards.Title(target, facts)} was defeated.";
        }
        else
        {
            long maximum = DamagePlacement.Health(world, facts, target);
            long current = Math.Max(0, maximum - target.Damage);
            targetState = $"{EffectiveCards.Title(target, facts)} is now at {current}/{maximum} HP.";
        }
        string damage = attack.CalculatedDamage is { } calculated
            ? $" Calculated attack damage: {calculated}."
            : string.Empty;
        return $"{AttackCompletion.SubjectTitle(world, facts, enemy.ObjectId)} finished attacking "
            + $"{EffectiveCards.Title(target, facts)} against {player}."
            + damage
            + (attack.Damaged ? " The attack dealt damage. " : " No damage was dealt. ")
            + targetState;
    }

}
