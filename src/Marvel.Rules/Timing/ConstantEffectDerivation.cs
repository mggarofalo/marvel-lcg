using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Settles simultaneous printed constant abilities against the live board.</summary>
internal static class ConstantEffectDerivation
{
    /// <summary>What every constant ability in play is doing right now.</summary>
    /// <remarks>
    /// <para>
    /// <b>Constants settle together.</b> <c>rr:modifiers.2</c> treats all
    /// modifiers as simultaneous, and one constant may depend on an attribute
    /// another constant grants. Each pass therefore reads the previous complete
    /// pass until two answers agree. An answer that cycles or keeps changing is
    /// refused rather than taken from an arbitrary intermediate pass.
    /// </para>
    /// </remarks>
    internal static List<ContinuousEffect> Settle(ContinuousEffects effects)
    {
        var seen = new List<IReadOnlyList<ContinuousEffect>>();
        effects.assumedConstants = [];

        try
        {
            // The card vocabulary is finite, but a malformed dependency can change
            // a numeric modifier forever without repeating a prior list. Sixty-four
            // full passes is the engine's chosen guard against that non-game state;
            // ordinary dependency chains settle in one pass per link.
            for (int pass = 0; pass < 64; pass++)
            {
                var found = new List<ContinuousEffect>();
                effects.deriving = true;
                try
                {
                    foreach (var card in effects.world.Cards)
                    {
                        if (DeckTypes.IsInPlay(card.Area.Type)
                            && !effects.departing.Contains(card.ObjectId)
                            && !PrintedText.IsBlanked(effects.world, card))
                        {
                            found.AddRange(effects.world.ConstantAbilities.Constant(effects.world, card));
                        }
                    }
                }
                finally
                {
                    effects.deriving = false;
                }

                if (found.SequenceEqual(effects.assumedConstants))
                {
                    return found;
                }

                if (seen.Any(previous => previous.SequenceEqual(found)))
                {
                    throw new RulesNotImplementedException(
                        "the constant abilities do not settle on one simultaneous effect list");
                }

                seen.Add([.. effects.assumedConstants]);
                effects.assumedConstants = found;
            }

            throw new RulesNotImplementedException(
                "the constant abilities did not settle after 64 simultaneous passes");
        }
        finally
        {
            effects.deriving = false;
            effects.assumedConstants = [];
        }
    }

}
