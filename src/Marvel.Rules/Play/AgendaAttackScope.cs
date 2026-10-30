using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Identifies a player attack while its nested work and response windows resolve.</summary>
public static class AgendaAttackScope
{
    /// <summary>The nearest begun player attack's scheduler identity, or null outside one.</summary>
    /// <remarks>Frame identity is an engine choice. Pending future attacks do not establish a scope.</remarks>
    public static long? PlayerAttackScope(this Agenda agenda) => agenda.items
        .Where(item => item.Step.Operation is CharacterAttackOperation && item.Occurrence is not null)
        .Select(item => (long?)item.Step.FrameId).FirstOrDefault();

    /// <summary>The begun player attack whose initiation is exactly <paramref name="occurrence"/>, or null.</summary>
    public static CharacterAttack? PlayerAttackOf(this Agenda agenda, Occurrence? occurrence) =>
        PlayerAttackOperation(agenda, occurrence)?.Attack;

    internal static CharacterAttackOperation? PlayerAttackOperation(this Agenda agenda, Occurrence? occurrence) =>
        agenda.items.Where(item => occurrence is not null && ReferenceEquals(item.Occurrence, occurrence))
            .Select(item => item.Step.Operation).OfType<CharacterAttackOperation>().FirstOrDefault();

    /// <summary>
    /// The begun player attack whose attack-initiated occurrence this is: its own
    /// initiation, or the announcement of an enemy it reaches later.
    /// </summary>
    internal static CharacterAttackOperation? InitiatingPlayerAttack(this Agenda agenda, Occurrence? occurrence) =>
        PlayerAttackOperation(agenda, occurrence)
        ?? (agenda.items.Any(item => occurrence is not null && ReferenceEquals(item.Occurrence, occurrence)
                && item.Step.Operation is LateAttackRecipientOperation)
            ? agenda.items.Where(item => item.Occurrence is not null)
                .Select(item => item.Step.Operation).OfType<CharacterAttackOperation>().FirstOrDefault()
            : null);

    /// <summary>The enemy activation owning one exact initiation window.</summary>
    public static int? EnemyActivationScope(this Agenda agenda, Occurrence? occurrence) => agenda.items
        .Where(item => item.Step.Operation is EnemyAttackOperation or EnemySchemeOperation
            && occurrence is not null && ReferenceEquals(item.Occurrence, occurrence) && item.Step.ActivationId >= 0)
        .Select(item => (int?)item.Step.ActivationId).FirstOrDefault();

}
