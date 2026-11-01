using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityImmediateExecution
{
    internal static AbilityImmediateResult TryRun(AbilityEffect effect, AbilityImmediateContext context)
    {
        return effect switch
        {
            AbilityPricingEffect.IncreaseNextEventCost increase => IncreaseEventCost(increase, context),
            AbilityEffect.BoostBasicUse bonus => AbilityBasicUseBoost.Run(bonus, context),
            AbilityEffect.ModifyPlayedEvent modifier => AbilityPlayedEventModifier.Run(modifier, context),
            AbilityEffect.Fixed fixedEffect => RunFixed(fixedEffect, context),
            AbilityEffect.CardAction action => RunCardAction(action, context),
            AbilityEffect.ChangeForm or AbilityEffect.Generate =>
                RunIdentityEffect(effect, context),
            AbilityEffect.Draw or AbilityEffect.DrawToHandSize or AbilityEffect.GainSurge =>
                RunPlayerEffect(effect, context),
            AbilityEffect.GrantTraitCharacters grant => AbilityTraitGroupExecution.Run(grant, context),
            AbilityEffect.GrantControlledCharacters or AbilityPricingEffect.ReduceNextCardCost
                or AbilityEffect.GrantField or AbilityEffect.GrantTrait =>
                RunGrantEffect(effect, context),
            AbilityEffect.DelayedStun or AbilityEffect.DelayedDiscard =>
                RunDelayedEffect(effect, context),
            _ => new(false, false),
        };
    }

    private static AbilityImmediateResult IncreaseEventCost(AbilityPricingEffect.IncreaseNextEventCost increase, AbilityImmediateContext context)
    {
        NextEventCost.Register(context.Admission.World, context.Admission.Source,
            context.Admission.Query.Player, increase.Amount);
        return new(true, true);
    }

    private static AbilityImmediateResult RunFixed(
        AbilityEffect.Fixed effect, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        if (AbilityBoostCancellation.TryRun(effect, context) is { } boost) return boost;
        if (AbilityKangImmediateExecution.TryRun(effect, context) is { } kang) return kang;
        if (AbilityReadyReplacement.TryRun(effect, context) is { } ready) return ready;
        switch (effect.Instruction)
        {
            case AbilityFixedInstruction.PlayersWin:
                world.Finish(Outcome.PlayersWin);
                return new(true, true);
            case AbilityFixedInstruction.DiscardOtherEnvironments:
                return new(true, EnvironmentReplacement.DiscardOthers(context));
            case AbilityFixedInstruction.AdvanceMainScheme:
            AdvanceMainScheme(context);
            return new(true, false);
            case AbilityFixedInstruction.CancelWhenRevealed:
                CancelWhenRevealed(context);
                return new(true, true);
            case AbilityFixedInstruction.CancelOccurrence:
                return new(true, AbilityOccurrenceCancellation.Cancel(
                    world, context.Admission.Query.Occurrence));
            case AbilityFixedInstruction.PlaceAccelerationToken:
                EncounterDeck.PlaceAccelerationToken(world, context.Trigger, context.Events);
                return new(true, false);
            default:
                return AbilityCombatImmediateExecution.Run(effect, context);
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
                bool scheduled = Attack.DeclareByAbility(
                    world, world.Facts,
                    context.Admission.Selectors().Find(action.Selection)
                        ?? throw new RulesNotImplementedException(
                            $"'{source.FaceId}' cannot find the character it declares as defender"),
                    checked((int)context.Admission.Expressions.Results
                        .GetValueOrDefault("defenseAbilityDefender", -1)));
                return new(true, true) { ProcedureScheduled = scheduled };
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
        var completion = ChangeForm(change, context, context.Admission.Evaluator().Seat);
        return new(true, false) { ProcedureScheduled = completion is not null, Completion = completion };
    }

    private static AbilityImmediateResult RunPlayerEffect(
        AbilityEffect effect, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        var seat = context.Admission.Evaluator().Seat;
        if (effect is AbilityEffect.Draw draw)
        {
            foreach (int player in Seats(draw.Players, seat, world, context.Admission.Source))
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
            return new(true, false) { ProcedureScheduled = GainSurge((AbilityEffect.GainSurge)effect, context) };
        }
        return new(true, false);
    }

    private static bool GainSurge(
        AbilityEffect.GainSurge surge, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        var source = context.Admission.Source;
        if (surge.Instances <= 0
            || StateFields.Modified(world, source, "surge", world.Facts, world.Players) > 0
            || !context.GainedKeywords.Add("surge")) return false;
        RememberGainedSurge(world, source.ObjectId, context.Admission.Query.Occurrence);
        RevealKeywords.ScheduleSurge(world, source, context.Admission.Query.Player, context.Events,
            dealTrigger: context.Trigger);
        return world.Agenda.Current is not null;
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
            case AbilityPricingEffect.ReduceNextCardCost reduction:
                if (reduction.AnyPlayer)
                    CardPayment.ReduceNextMatchingCardCost(world, source,
                        evaluator.Amount(reduction.Amount), reduction.Filter!);
                else CardPayment.ReduceNextCardCost(
                    world, source, evaluator.Seat(reduction.Player),
                    evaluator.Amount(reduction.Amount), reduction.Filter, reduction.Until);
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
            AbilityDelayedEffects.Register(stun, context);
        else
            AbilityDelayedEffects.Register(
                (AbilityEffect.DelayedDiscard)effect, context,
                context.Admission.Selectors().Find);
        return new(true, true);
    }

    private static EffectCompletion? ChangeForm(AbilityEffect.ChangeForm change, AbilityImmediateContext context, Func<AbilityPlayer, int> seatOf)
    {
        World world = context.Admission.World;
        var seat = world.Seats[seatOf(change.Player)];
        string? destination = AbilityAdmissionFacts.FormDestination(world, seat.Index, change.Destination);
        if (destination is null || !FormChangePermissions.CanChange(world, seat.IdentityCard)) return null;
        var completion = new EffectCompletion(1, 1);
        world.Agenda.Then(new PhaseStep(
            new IdentityFormChangeOperation(seat.IdentityCard.ObjectId, seat.IdentityCard.Incarnation,
                destination, context.Trigger, completion),
            world.Agenda.Current?.Round ?? 0, 0,
            Index: seat.Index, Subject: seat.IdentityCard.ObjectId, Seat: seat.Index));
        return completion;
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
        if (kind == PrintedText.Blanked)
        {
            var ending = world.Effects.PreflightConstantsEnding(target, includeHostedCards: false);
            using var departure = ending.Begin();
            world.Effects.Register(PrintedText.UntilEndOfPhase(world, context.Admission.Source, target));
            ending.Complete(context.Trigger, context.Events);
            return;
        }
        var liveAmount = AbilityLastingAmount.Capture(amount, context.Admission);
        if (liveAmount is not null && kind is not ("attack" or "scheme" or "boost_const"))
            throw new RulesNotImplementedException($"live lasting field '{kind}' is not implemented");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: kind,
            Amount: liveAmount is null ? resolveAmount(amount) : 0, Card: context.Admission.Source.ObjectId,
            Affects: target.ObjectId, Lasts: AbilityLastingDuration.For(world, target, until)) { LiveAmount = liveAmount });
        if (string.Equals(kind, "stalwart", StringComparison.Ordinal))
            Statuses.RemoveAfflictionsIfStalwart(world, world.Facts, target, context.Trigger, context.Events);
    }

    private static void AdvanceMainScheme(AbilityImmediateContext context)
    {
        World world = context.Admission.World;
        var scheme = CardReferences.TheCardIn(world, DeckType.MainSchemesArea, context.Admission.Source) ?? throw new RulesNotImplementedException(
            $"'{context.Admission.Source.FaceId}' advances a main scheme that is not in play");
        MainScheme.Advance(world, world.Facts, context.EncounterAbilities, scheme, context.Trigger, context.Events);
    }

    private static void CancelWhenRevealed(AbilityImmediateContext context) =>
        context.Admission.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Kind: "cancelWhenRevealed",
            Card: context.Admission.Source.ObjectId,
            Affects: context.Admission.Query.Occurrence.Subject,
            Lasts: new Duration(Uses: 1)));

    private static IEnumerable<int> Seats(AbilityPlayerSelection players, Func<AbilityPlayer, int> seat, World world, Card source) => players switch
    {
        AbilityPlayerSelection.AllPlayers => EachPlayerEffects.Players(world, source),
        AbilityPlayerSelection.OnePlayer one => [seat(one.Player)],
        _ => throw new InvalidOperationException("Unknown compiled player selection"),
    };

    private static void RememberGainedSurge(World world, int source, Occurrence reveal)
    {
        world.Agenda.MarkSurgeGained(source, reveal);
        if (world.CharacterAttack is { Source: var attackSource } attack && attackSource == source
            && ReferenceEquals(attack.Origin, reveal))
            world.CharacterAttack = attack with { SurgeGained = true };
        if (world.CharacterThwart is { Source: var thwartSource } thwart && thwartSource == source
            && ReferenceEquals(thwart.Origin, reveal))
            world.CharacterThwart = thwart with { SurgeGained = true };
    }
}
