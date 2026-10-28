using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A placed damage recipient participating in shared defeat windows.</summary>
internal interface ISimultaneousDefeatRecipient
{
    DamageRequest Request { get; }
    Defeated? Defeated { get; }
    bool IsLethal(World world, ICardFacts facts);
    void RecordDefeat(World world);
    bool Depart(World world, ICardFacts facts, List<GameEvent> events);
}
