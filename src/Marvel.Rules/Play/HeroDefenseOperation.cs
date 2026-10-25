using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The captured hero-defense commitment within one enemy activation.</summary>
public sealed record HeroDefenseOperation(
    int Hero, int HeroIncarnation, int Enemy, int EnemyIncarnation, int Activation,
    OccurrenceCard HeroFacts, OccurrenceCard EnemyFacts) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => HeroDefenseTiming.Defends;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Lifecycle;

    internal Occurrence Occurrence(int id, World world)
    {
        bool current = world.Attack is { } attack && attack.Enemy == Enemy && attack.Defender == Hero
            && world.Cards[Hero].Incarnation == HeroIncarnation
            && world.Cards[Enemy].Incarnation == EnemyIncarnation
            && (world.Activation?.Id ?? -1) == Activation;
        return new(id, current ? [HeroDefenseTiming.Defends] : [], Hero, HeroFacts.Controller,
            Hero, Enemy, HeroFacts, EnemyFacts);
    }
}
