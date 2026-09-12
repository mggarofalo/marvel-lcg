using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card-defined continuous effects.</summary>
public interface ICardConstantAbilities
{
    IReadOnlyList<ContinuousEffect> Constant(World world, Card card);
}
#pragma warning restore CS1591
