using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text that can prohibit a card from readying.</summary>
public interface ICardReadinessAbilities
{
    bool CanReady(World world, Card target, Card source);
}
#pragma warning restore CS1591
