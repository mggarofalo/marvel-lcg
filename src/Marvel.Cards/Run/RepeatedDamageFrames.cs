using System.Collections.Immutable;
using static Marvel.Cards.Run.AbilityPlayerSelectorRelations;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityRepeatedDamageTrace;
using static Marvel.Cards.Run.AbilityRepeatedDamageAnalysis;
using static Marvel.Cards.Run.AbilityRepeatedSelectorTrace;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;
namespace Marvel.Cards.Run;

/// <summary>Enumerates player-frame order when measuring repeated damage pressure.</summary>
internal static class RepeatedDamageFrames
{
    internal static long Peak(
        Card target, AbilityEffect repeatedEffect, AbilityAdmissionScope cast,
        RepeatedChange assumed, bool binding, int frames)
    {
        int original = cast.Player;
        try
        {
            IReadOnlyList<DamageTraceState> states = [DamageTraceStateFactory.Initial(target, cast)];
            for (int frame = 0; frame < frames; frame++)
            {
                var next = new List<DamageTraceState>();
                foreach (var state in states)
                {
                    foreach (int player in EachPlayerEffects.Players(cast.World, cast.Source).Where(player =>
                        !state.Players.Contains(player)))
                    {
                        cast.RestorePlayer(player);
                        var traces = DamageTraces(
                            repeatedEffect, cast, assumed, binding);
                        foreach (var trace in traces)
                        {
                            var advanced = ApplyDamageTrace(
                                state, trace, target, cast);
                            next.Add(advanced with
                            {
                                Players = [.. state.Players, player],
                            });
                        }
                    }
                }
                states = next;
            }
            return states.Select(state => state.PeakTargetPressure)
                .DefaultIfEmpty(target.Damage)
                .Max();
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

}
