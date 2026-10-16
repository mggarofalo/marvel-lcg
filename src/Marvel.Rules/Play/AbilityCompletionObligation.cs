namespace Marvel.Rules.Play;

/// <summary>Retains unfinished staged work without preventing the owner from resuming to release it.</summary>
internal sealed class AbilityCompletionObligation
{
    private readonly AbilityExecution owner;

    internal AbilityCompletionObligation(AbilityExecution owner)
    {
        owner.Agenda.Executions.RequireActive(owner);
        this.owner = owner;
        owner.CompletionObligations.Add(this);
    }

    internal void Release()
    {
        owner.Agenda.Executions.RequireActive(owner);
        if (!owner.CompletionObligations.Remove(this))
            throw new InvalidOperationException("the completion obligation was already released");
    }
}
