using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static Card? CostReference(AbilityCostCard binding, Cast cast) => binding switch
    {
        AbilityCostCard.Source => cast.SourceReference,
        AbilityCostCard.Identity => cast.World.Seats[Resolver(cast)].IdentityCard,
        _ => throw new InvalidOperationException("Unknown compiled cost binding"),
    };

    private static void PayPrimitiveCost(AbilityCost cost, Cast cast)
    {
        int eventsBefore = cast.Events.Count;
        var healthBefore = cast.World.Effects.CaptureCharacterHealth();
        bool suspended = false;
        switch (cost)
        {
            case AbilityCost.Exhaust exhaust:
                if (CostReference(exhaust.Card, cast) is { } exhausted) Exhaust(exhausted, cast);
                break;
            case AbilityCost.Discard discard:
                if (CostReference(discard.Card, cast) is { } discarded
                    && (DeckTypes.IsInPlay(discarded.Area.Type)
                        || discarded.Area.Type is DeckType.BoostingArea or DeckType.ProcessingArea or DeckType.RevealingArea)
                    && (discard.Card != AbilityCostCard.Source || cast.SourceBindingIsCurrent(discarded))
                    && Rules.Play.Discard.EffectCanRemove(cast.World, cast.World.Facts, cast.Source, discarded))
                {
                    Rules.Play.Discard.CardFromEffect(cast.World, cast.World.Facts, cast.Source, discarded,
                        cast.Trigger, cast.Events);
                }
                break;
            case AbilityCost.RemoveCounters counters:
                var holder = CostReference(counters.Card, cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the card paying its counter cost");
                RemoveCounters(holder, counters.Counter, counters.Count, cast);
                break;
            case AbilityCost.Heal heal:
                cast.Results["healed"] = CostReference(heal.Card, cast) is { } healed
                    ? Damage.Heal(cast.World, cast.World.Facts, healed, heal.Amount, cast.Trigger, "Heal", cast.Events)
                    : 0;
                break;
            case AbilityCost.Damage damage:
                long amount = ModifiedAbilityDamage(damage.Amount, cast);
                if (CostReference(damage.Card, cast) is { } damaged)
                {
                    long before = damaged.Damage;
                    suspended = Damage.DealOutcome(cast.World, cast.World.Facts, cast.Source, damaged,
                        amount, cast.Trigger, "Deal_Damage", cast.Events) == Damage.Outcome.Suspended;
                    if (cast.Power == BasicPowers.AttackVerb && damaged.Damage > before)
                    {
                        cast.Occurrence.Also(Steps.DamageDealt);
                    }
                }
                break;
            default:
                throw new InvalidOperationException("A non-primitive cost reached primitive execution");
        }

        if (cast.Events.Count > eventsBefore) cast.ResolveEffect();
        Statuses.RemoveAfflictionsIfStalwart(cast.World, cast.World.Facts, "stalwart", cast.Events);
        suspended |= cast.World.Effects.SettleLostHealth(healthBefore, cast.Trigger, cast.Events);
        if (suspended)
        {
            // The initiation entry point owns the post-cost continuation.
            // A cost is not a node in the post-arrow effect's structural path.
            cast.Results["costProcedurePending"] = 1;
            cast.Suspend();
        }
        Attack.RefreshDefender(cast.World, cast.World.Facts);
    }
}
