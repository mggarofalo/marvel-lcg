using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static bool TryRunImmediateEffect(AbilityEffect effect, Cast cast)
    {
        switch (effect)
        {
            case AbilityEffect.ChangeForm change:
                ChangeForm(change, cast);
                return true;

            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.AdvanceMainScheme }:
                AdvanceMainScheme(cast);
                return true;

            case AbilityEffect.Generate:
                // `rr:resource-ability` -- a resource ability is *read* while a
                // cost is being paid rather than run like an effect, so nothing
                // happens here. `ResourceAbilities` takes its letters and
                // `UseResource` counts the use; running it would be a second
                // way to generate the same resource.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' generates a resource, which is read while a "
                    + "cost is paid rather than resolved as an effect");

            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelWhenRevealed }:
                CancelWhenRevealed(cast);
                cast.ResolveEffect();
                return true;

            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelOccurrence }:
                // rr:replacement-effect.1: the interrupted occurrence does
                // not happen. Printed card data decides when this generic
                // agenda operation is part of an ability's effect.
                if (cast.World.Agenda.IsOutstanding(cast.Occurrence))
                {
                    cast.World.Agenda.Cancel(cast.Occurrence);
                }
                cast.ResolveEffect();
                return true;

            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.GiveAdditionalBoost } boost:
                Attack.GiveAdditionalBoostCard(
                    cast.World,
                    ResolveCard(boost.Selection, cast)
                        ?? throw new AbilityException(
                            $"'{cast.Source.FaceId}' cannot find the enemy receiving an additional boost card"),
                    cast.Trigger,
                    cast.Events);
                return true;

            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.AlsoAttackEachOtherHero }:
                Attack.AlsoResolveAgainstEachOtherHero(cast.World);
                cast.ResolveEffect();
                return true;

            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.DeclareDefender } declare:
                var declared = ResolveCard(declare.Selection, cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the character it declares as defender");
                Attack.DeclareByAbility(
                    cast.World, cast.World.Facts, declared,
                    ReplaceableDefenseDefender(cast));
                cast.ResolveEffect();
                return true;

            case AbilityEffect.GrantControlledCharacters grant:
                foreach (string field in grant.Fields)
                {
                    cast.World.Effects.GrantToCharactersControlledBy(
                        cast.Source, Seat(grant.Player, cast), field,
                        ResolveAmount(grant.Amount, cast),
                        grant.Until);
                }
                cast.ResolveEffect();
                return true;

            case AbilityEffect.ReduceNextCardCost reduction:
                CardPlay.ReduceNextCardCost(
                    cast.World, cast.Source, Seat(reduction.Player, cast),
                    ResolveAmount(reduction.Amount, cast));
                cast.ResolveEffect();
                return true;

            case AbilityEffect.GainSurge surge:
                // `rr:surge`: "the player resolving the card deals themself a
                // facedown encounter card from the top of the encounter deck",
                // and `.1` writes it as "**When Revealed**: deal yourself 1
                // facedown encounter card". A card that *gains* surge does the
                // same thing the keyword would have.
                //
                // `rr:keywords.1` makes every additional non-numeric instance
                // inert. Printed and continuously granted Surge already ran in
                // `Reveal.Keywords`; multiple nodes and a value greater than one
                // are multiple gained instances inside this reveal. All four
                // shapes therefore produce at most one deal between them.
                if (surge.Instances > 0
                    && StateFields.Modified(
                        cast.World, cast.Source, "surge", cast.World.Facts,
                        cast.World.Players) <= 0
                    && cast.GainedKeywords.Add("surge"))
                {
                    RememberGainedSurge(cast.World, cast.Source.ObjectId);
                    Deal.EncounterCard(
                        cast.World, cast.Player, cast.Trigger, cast.Events);
                }

                // `.2` finishes the original card first, which the villain
                // phase's reveal queue does without anything else here.
                return true;

            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.MakeAttackIndirect }:
                Attack.MakeIndirect(cast.World);
                return true;

            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.PlaceAccelerationToken }:
                EncounterDeck.PlaceAccelerationToken(cast.World, cast.Trigger, cast.Events);
                return true;

            case AbilityEffect.Draw draw:
                foreach (int player in Seats(draw.Players, cast))
                {
                    if (CanDraw(cast.World, player))
                    {
                        Draw.Cards(
                            cast.World, player,
                            draw.Count,
                            cast.Trigger, cast.Events);
                    }
                }
                return true;

            case AbilityEffect.DrawToHandSize handSize:
                DrawToHandSize(handSize, cast);
                return true;

            case AbilityEffect.GrantField { Until: { } until } fieldGrant:
                GrantUntil(fieldGrant.Cards, fieldGrant.Field, fieldGrant.Amount, until, cast);
                cast.ResolveEffect();
                return true;

            case AbilityEffect.GrantTrait { Until: { } until } traitGrant:
                GrantUntil(traitGrant.Cards, Traits.Granted + traitGrant.Trait,
                    new AbilityNumber.Constant(1), until, cast);
                cast.ResolveEffect();
                return true;

            case AbilityEffect.DelayedStun delayed:
                DelayUntil(delayed, cast);
                cast.ResolveEffect();
                return true;

            case AbilityEffect.DelayedDiscard delayed:
                DelayUntil(delayed, cast);
                cast.ResolveEffect();
                return true;

            default:
                return false;
        }
    }

    /// <summary>Which seats a compiled player selection names.</summary>
    /// <remarks>
    /// <c>rr:each-player.1</c> resolves "each player" in player order when the
    /// effect does not say otherwise, and <c>rr:player-elimination.6</c> is why
    /// that is <c>PlayerOrder</c>: "effects that refer to the players in the
    /// game ignore eliminated players".
    /// </remarks>
    private static IEnumerable<int> Seats(AbilityPlayerSelection players, Cast cast) => players switch
    {
        AbilityPlayerSelection.AllPlayers => cast.World.PlayerOrder,
        AbilityPlayerSelection.OnePlayer one => [Seat(one.Player, cast)],
        _ => throw new InvalidOperationException("Unknown compiled player selection"),
    };
}
