using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The completed damage facts of one owned attack-damage procedure.</summary>
public sealed class AttackDamageResult
{
    private IReadOnlyList<Card> characters = [];
    private long amount;
    private long excess;
    private long dealt;
    private long taken;

    internal AttackDamageResult(AbilityExecution execution) => Execution = execution;
    /// <summary>The game-owned execution including replacement, defeat and aftermath.</summary>
    public AbilityExecution Execution { get; }
    /// <summary>Whether every owned damage procedure has finished.</summary>
    public bool IsComplete => Execution.IsComplete;
    /// <summary>Characters that actually took the primary attack damage.</summary>
    public IReadOnlyList<Card> Characters { get { RequireComplete(); return characters; } }
    /// <summary>Total damage taken, including overkill.</summary>
    public long Amount { get { RequireComplete(); return amount; } }
    /// <summary>Unprevented damage beyond a defeated target's remaining hit points.</summary>
    public long Excess { get { RequireComplete(); return excess; } }
    /// <summary>Primary damage dealt after replacement.</summary>
    public long Dealt { get { RequireComplete(); return dealt; } }
    /// <summary>Primary damage taken after prevention.</summary>
    public long Taken { get { RequireComplete(); return taken; } }

    internal void Publish(Damage.PlacedDamage placement, long spillTaken, long excessDamage)
    {
        characters = placement.Landed ? Array.AsReadOnly(new[] { placement.Target }) : [];
        dealt = placement.Dealt;
        taken = placement.Taken;
        amount = taken + spillTaken;
        excess = excessDamage;
    }

    private void RequireComplete()
    {
        if (!IsComplete) throw new InvalidOperationException("attack damage is still resolving");
    }
}
