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
    private long excessDealt;

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
    /// <summary>Primary damage dealt beyond the target's remaining hit points, whether or not it was taken.</summary>
    public long ExcessDealt { get { RequireComplete(); return excessDealt; } }

    internal void Publish(Damage.PlacedDamage placement, long spillTaken, long excessDamage, long remaining)
    {
        // rr:excess-damage: "any amount of damage that is dealt to a character beyond
        // that character's remaining hit points." Ruling dd353e3498f6b923: excess can be
        // dealt "even if that target doesn't take that damage".
        excessDealt = Math.Max(0, placement.Dealt - remaining);
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
