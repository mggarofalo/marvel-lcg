using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Typed live inputs for immediate nodes. The context carries only the current
// expression/admission read model, occurrence identity, event output and the
// reveal-scoped keyword state; it has no interpreter or continuation access.
internal sealed record AbilityImmediateContext(
    AbilityAdmissionContext Admission, string Trigger, List<GameEvent> Events,
    HashSet<string> GainedKeywords, IEncounterCardAbilities EncounterAbilities);

internal readonly record struct AbilityImmediateResult(bool Handled, bool ResolveEffect);

internal static class AbilityImmediateExecution
{
    internal static AbilityImmediateResult TryRun(AbilityEffect effect, AbilityImmediateContext context)
    {
        return effect switch
        {
            AbilityEffect.Fixed fixedEffect => RunFixed(fixedEffect, context),
            AbilityEffect.CardAction action => RunCardAction(action, context),
            AbilityEffect.ChangeForm or AbilityEffect.Generate =>
                RunIdentityEffect(effect, context),
            AbilityEffect.Draw or AbilityEffect.DrawToHandSize or AbilityEffect.GainSurge =>
                RunPlayerEffect(effect, context),
            AbilityEffect.GrantControlledCharacters or AbilityEffect.ReduceNextCardCost
                or AbilityEffect.GrantField or AbilityEffect.GrantTrait =>
                RunGrantEffect(effect, context),
            AbilityEffect.DelayedStun or AbilityEffect.DelayedDiscard =>
                RunDelayedEffect(effect, context),
            _ => new(false, false),
        };
    }

    private static AbilityImmediateResult RunFixed(
        AbilityEffect.Fixed effect, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        switch (effect.Instruction)
        {
            case AbilityFixedInstruction.AdvanceMainScheme:
                AdvanceMainScheme(context);
                return new(true, false);
            case AbilityFixedInstruction.CancelWhenRevealed:
                CancelWhenRevealed(context);
                return new(true, true);
            case AbilityFixedInstruction.CancelOccurrence:
                if (world.Agenda.IsOutstanding(context.Admission.Query.Occurrence))
                    world.Agenda.Cancel(context.Admission.Query.Occurrence);
                return new(true, true);
            case AbilityFixedInstruction.AlsoAttackEachOtherHero:
                Attack.AlsoResolveAgainstEachOtherHero(world);
                return new(true, true);
            case AbilityFixedInstruction.MakeAttackIndirect:
                Attack.MakeIndirect(world);
                return new(true, false);
            case AbilityFixedInstruction.PlaceAccelerationToken:
                EncounterDeck.PlaceAccelerationToken(world, context.Trigger, context.Events);
                return new(true, false);
            default:
                return new(false, false);
        }
    }

    private static AbilityImmediateResult RunCardAction(
        AbilityEffect.CardAction action, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        var source = context.Admission.Source;
        switch (action.Instruction)
        {
            case AbilityCardInstruction.GiveAdditionalBoost:
                Attack.GiveAdditionalBoostCard(
                    world,
                    context.Admission.Selectors().Find(action.Selection)
                        ?? throw new AbilityException(
                            $"'{source.FaceId}' cannot find the enemy receiving an additional boost card"),
                    context.Trigger, context.Events);
                return new(true, false);
            case AbilityCardInstruction.DeclareDefender:
                Attack.DeclareByAbility(
                    world, world.Facts,
                    context.Admission.Selectors().Find(action.Selection)
                        ?? throw new RulesNotImplementedException(
                            $"'{source.FaceId}' cannot find the character it declares as defender"),
                    checked((int)context.Admission.Expressions.Results
                        .GetValueOrDefault("defenseAbilityDefender", -1)));
                return new(true, true);
            default:
                return new(false, false);
        }
    }

    private static AbilityImmediateResult RunIdentityEffect(
        AbilityEffect effect, AbilityImmediateContext context)
    {
        if (effect is AbilityEffect.Generate)
        {
            throw new RulesNotImplementedException(
                $"'{context.Admission.Source.FaceId}' generates a resource, which is read "
                + "while a cost is paid rather than resolved as an effect");
        }
        var change = (AbilityEffect.ChangeForm)effect;
        ChangeForm(change, context, context.Admission.Evaluator().Seat);
        return new(true, false);
    }

    private static AbilityImmediateResult RunPlayerEffect(
        AbilityEffect effect, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        var seat = context.Admission.Evaluator().Seat;
        if (effect is AbilityEffect.Draw draw)
        {
            foreach (int player in Seats(draw.Players, seat, world))
            {
                if (AbilityRepeatedStatusTrace.CanDraw(world, player))
                    Draw.Cards(world, player, draw.Count, context.Trigger, context.Events);
            }
        }
        else if (effect is AbilityEffect.DrawToHandSize handSize)
        {
            DrawToHandSize(handSize, context, seat);
        }
        else
        {
            GainSurge((AbilityEffect.GainSurge)effect, context);
        }
        return new(true, false);
    }

    private static void GainSurge(
        AbilityEffect.GainSurge surge, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        var source = context.Admission.Source;
        if (surge.Instances <= 0
            || StateFields.Modified(world, source, "surge", world.Facts, world.Players) > 0
            || !context.GainedKeywords.Add("surge")) return;
        RememberGainedSurge(world, source.ObjectId);
        Deal.EncounterCard(
            world, context.Admission.Query.Player, context.Trigger, context.Events);
    }

    private static AbilityImmediateResult RunGrantEffect(
        AbilityEffect effect, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        var source = context.Admission.Source;
        var evaluator = context.Admission.Evaluator();
        switch (effect)
        {
            case AbilityEffect.GrantControlledCharacters grant:
                foreach (string field in grant.Fields)
                    world.Effects.GrantToCharactersControlledBy(
                        source, evaluator.Seat(grant.Player), field,
                        evaluator.Amount(grant.Amount), grant.Until);
                return new(true, true);
            case AbilityEffect.ReduceNextCardCost reduction:
                CardPayment.ReduceNextCardCost(
                    world, source, evaluator.Seat(reduction.Player),
                    evaluator.Amount(reduction.Amount));
                return new(true, true);
            case AbilityEffect.GrantField { Until: { } until } field:
                GrantUntil(
                    field.Cards, field.Field, field.Amount, until, context,
                    context.Admission.Selectors().Find, evaluator.Amount);
                return new(true, true);
            case AbilityEffect.GrantTrait { Until: { } until } trait:
                GrantUntil(
                    trait.Cards, Traits.Granted + trait.Trait,
                    new AbilityNumber.Constant(1), until, context,
                    context.Admission.Selectors().Find, evaluator.Amount);
                return new(true, true);
            default:
                return new(false, false);
        }
    }

    private static AbilityImmediateResult RunDelayedEffect(
        AbilityEffect effect, AbilityImmediateContext context)
    {
        if (effect is AbilityEffect.DelayedStun stun)
            DelayUntil(stun, context);
        else
            DelayUntil(
                (AbilityEffect.DelayedDiscard)effect, context,
                context.Admission.Selectors().Find);
        return new(true, true);
    }

    private static void ChangeForm(AbilityEffect.ChangeForm change, AbilityImmediateContext context, Func<AbilityPlayer, int> seatOf)
    {
        World world = context.Admission.World;
        var seat = world.Seats[seatOf(change.Player)];
        if (AbilityAdmissionFacts.AlreadyInForm(world, seatOf(change.Player), change.Form)) return;
        string was = seat.IdentityCard.FaceId;
        Forms.Change(seat, world.Facts);
        context.Events.Add(new CardsFlipped([seat.IdentityCard.ObjectId], true)
        { Trigger = context.Trigger, Verb = "Change_Form" });
        if (!Forms.In(world, seat, world.Facts, change.Form))
            throw new RulesNotImplementedException($"flipping '{was}' did not reach {change.Form}");
    }

    private static void DrawToHandSize(AbilityEffect.DrawToHandSize draw, AbilityImmediateContext context, Func<AbilityPlayer, int> seatOf)
    {
        World world = context.Admission.World;
        int player = seatOf(draw.Player);
        var seat = world.Seats[player];
        long size = draw.Printed
            ? world.Facts.PrintedValue(seat.IdentityCard.FaceId, "HS", world.Players)
            : PhaseEnd.HandSize(world, seat, world.Facts);
        int hand = seat.Hand.Cards.Count - (context.Admission.Source.Area == seat.Hand
            && world.Facts.Kind(context.Admission.Source.FaceId) == CardKind.Event ? 1 : 0);
        Draw.Cards(world, player, (int)Math.Max(0, size - hand), context.Trigger, context.Events);
    }

    private static void GrantUntil(AbilityCardSelection selection, string kind, AbilityNumber amount, string until,
        AbilityImmediateContext context, Func<AbilityCardSelection, Card?> find, Func<AbilityNumber, long> resolveAmount)
    {
        World world = context.Admission.World;
        var target = find(selection) ?? throw new RulesNotImplementedException(
            $"'{context.Admission.Source.FaceId}' would grant to a card that is not there");
        if (!AbilityAdmission.LastingPeriodIsOpen(until, context.Admission))
            throw new RulesNotImplementedException(
                $"'{context.Admission.Source.FaceId}' begins a lasting effect outside its named period");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: kind,
            Amount: resolveAmount(amount), Card: context.Admission.Source.ObjectId,
            Affects: target.ObjectId, Lasts: Duration.UntilEndOf(until)));
        if (string.Equals(kind, "stalwart", StringComparison.Ordinal))
            Statuses.RemoveAfflictionsIfStalwart(world, world.Facts, target, context.Trigger, context.Events);
    }

    private static void DelayUntil(AbilityEffect.DelayedStun delayed, AbilityImmediateContext context) =>
        context.Admission.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, Kind: DelayedEffects.StunTheSubject,
            Card: context.Admission.Source.ObjectId, Affects: null,
            Lasts: new Duration(Until: delayed.Within, OnCondition: Steps.DamageDealt, Uses: 1)));

    private static void DelayUntil(AbilityEffect.DelayedDiscard delayed, AbilityImmediateContext context,
        Func<AbilityCardSelection, Card?> find)
    {
        var target = find(delayed.Card) ?? throw new RulesNotImplementedException(
            $"'{context.Admission.Source.FaceId}' would delay a discard of a card that is not there");
        context.Admission.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, Kind: DelayedEffects.DiscardFromPlay,
            Card: context.Admission.Source.ObjectId, Affects: target.ObjectId,
            Lasts: Duration.NextTime(delayed.Condition)));
    }

    private static void AdvanceMainScheme(AbilityImmediateContext context)
    {
        World world = context.Admission.World;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea) ?? throw new RulesNotImplementedException(
            $"'{context.Admission.Source.FaceId}' advances a main scheme that is not in play");
        MainScheme.Advance(world, world.Facts, context.EncounterAbilities, scheme, context.Trigger, context.Events);
    }

    private static void CancelWhenRevealed(AbilityImmediateContext context) =>
        context.Admission.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Kind: "cancelWhenRevealed",
            Card: context.Admission.Source.ObjectId,
            Affects: context.Admission.Query.Occurrence.Subject,
            Lasts: new Duration(Uses: 1)));

    private static IEnumerable<int> Seats(AbilityPlayerSelection players, Func<AbilityPlayer, int> seat, World world) => players switch
    {
        AbilityPlayerSelection.AllPlayers => world.PlayerOrder,
        AbilityPlayerSelection.OnePlayer one => [seat(one.Player)],
        _ => throw new InvalidOperationException("Unknown compiled player selection"),
    };

    private static void RememberGainedSurge(World world, int source)
    {
        world.Agenda.MarkSurgeGained(source);
        if (world.CharacterAttack is { Source: var attackSource } attack && attackSource == source)
            world.CharacterAttack = attack with { SurgeGained = true };
        if (world.CharacterThwart is { Source: var thwartSource } thwart && thwartSource == source)
            world.CharacterThwart = thwart with { SurgeGained = true };
    }
}
