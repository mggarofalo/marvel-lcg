using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.CardPayment;
using static Marvel.Rules.Play.CardPlayLegality;
using static Marvel.Rules.Play.CardControlTransfer;
using static Marvel.Rules.Play.CardEntry;

namespace Marvel.Rules.Play;

/// <summary>
/// A card's cost after modifiers, together with the one-use effects that
/// produced it.
/// </summary>
/// <remarks>
/// Kept as data because determining a cost and paying it are separate steps of
/// <c>rr:initiating-abilities</c>. The effects are consumed only after the card
/// has successfully been played; merely describing an affordance does not use
/// them.
/// </remarks>
/// <param name="Amount">The cost after modifiers, never less than zero.</param>
/// <param name="Modifiers">The effects applied while determining it.</param>
public sealed record AdjustedCardCost(
    long Amount, IReadOnlyList<ContinuousEffect> Modifiers);
