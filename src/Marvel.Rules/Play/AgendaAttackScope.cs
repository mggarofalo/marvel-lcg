namespace Marvel.Rules.Play;

/// <summary>Identifies a player attack while its nested work and response windows resolve.</summary>
public static class AgendaAttackScope
{
    /// <summary>The nearest begun player attack's scheduler identity, or null outside one.</summary>
    /// <remarks>Frame identity is an engine choice. Pending future attacks do not establish a scope.</remarks>
    public static long? PlayerAttackScope(this Agenda agenda) => agenda.items
        .Where(item => item.Step.Operation is CharacterAttackOperation && item.Occurrence is not null)
        .Select(item => (long?)item.Step.FrameId).FirstOrDefault();
}
