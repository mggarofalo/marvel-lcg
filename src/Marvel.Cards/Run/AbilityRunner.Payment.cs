using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static void DiscardEvent(Card card, Cast cast)
    {
        bool playedInWindow = !cast.Suspended
            && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
            && card.Area.Type == DeckType.RevealingArea
            && card.Area.PlayArea == PlayArea.Of(card.Owner)
            && !cast.Occurrence.Is(Steps.TurnAction);
        if (!cast.Suspended
            && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
            && card.Area.Type == DeckType.RevealingArea
            && card.Area.PlayArea == PlayArea.Of(card.Owner))
        {
            Rules.Play.Discard.Card(cast.World, card, CardPlay.Verb, cast.Events);
            foreach (var payment in cast.World.Effects.Active().Where(effect =>
                effect.Card == card.ObjectId
                && effect.Kind.StartsWith("paid:", StringComparison.Ordinal)).ToList())
            {
                cast.World.Effects.Use(payment);
            }

            if (playedInWindow)
            {
                cast.World.Agenda.NowEventPlayed(
                    cast.World.Agenda.Current?.Round ?? 0,
                    card.ObjectId,
                    cast.Player);
            }
        }
    }

    /// <summary>Pays an ability's cost — <c>rr:initiating-abilities.step.5</c>.</summary>
    private static void PayNonResourceCosts(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, Cast cast)
    {
        if (cost is null or AbilityCost.Spend or AbilityCost.SpendEnergy)
        {
            return;
        }
        if (cost is AbilityCost.Sequence sequence)
        {
            foreach (var step in sequence.Costs)
            {
                PayNonResourceCosts(step, paying, chosen, values, cast);
            }
            return;
        }

        Pay(cost, paying, chosen, values, cast);
    }

    /// <summary>Pays an ability's cost — <c>rr:initiating-abilities.step.5</c>.</summary>
    private static void Pay(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, Cast cast)
        => Pay(cost, paying, chosen, values: null, cast);

    private static void Pay(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, Cast cast)
    {
        if (cost is null)
        {
            return;
        }

        if (cost is AbilityCost.Sequence sequence)
        {
            var steps = sequence.Costs;
            // A take-damage cost is not paid unless all of the damage is
            // taken. Resolve that uncertain component before any payment that
            // cannot fail after validation, so a prevention never leaves an
            // otherwise simultaneous card or resource cost paid by itself.
            foreach (var step in steps.Where(step => step is AbilityCost.Damage { MustTakeAll: true }))
            {
                Pay(step, paying, chosen, values, cast);
            }
            var spends = steps.OfType<AbilityCost.Spend>().ToList();
            if (spends.Count > 0)
            {
                bool printed = spends.All(step => step.PrintedOnly);
                if (!printed && spends.Any(step => step.PrintedOnly))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' mixes printed and ordinary simultaneous "
                        + "resource costs, whose allocation is not implemented");
                }
                string required = string.Concat(spends.Select(step => step.Resources));
                SpendAbilityResources(required, paying, cast, printed);
            }

            foreach (var step in steps.Where(
                         step => step is not (AbilityCost.Spend or AbilityCost.Damage { MustTakeAll: true })))
            {
                Pay(step, paying, chosen, values, cast);
            }
            return;
        }

        if (cost is AbilityCost.DiscardFromHand discard)
        {
            DiscardToPay(discard, chosen, cast);
            return;
        }

        if (cost is AbilityCost.ExhaustChosen)
        {
            foreach (int id in chosen)
            {
                var target = cast.World.Cards[id];
                target.Exhaust();
                cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
                {
                    Trigger = cast.Trigger,
                    Verb = "Exhaust",
                });
            }
            return;
        }

        if (cost is AbilityCost.Spend spend)
        {
            SpendAbilityResources(spend.Resources, paying, cast, spend.PrintedOnly);
            return;
        }

        if (cost is AbilityCost.SpendEnergy)
        {
            if (paying.Count == 0 || paying.Distinct().Count() != paying.Count)
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' requires one or more distinct generators for X");
            }

            var selected = paying.ToHashSet();
            string generated = string.Concat(CardPlay.Generators(
                    cast.World, cast.World.Facts, cast.World.Seats[cast.Player])
                .Where(source => selected.Contains(source.Effect))
                .Select(source => source.Generates));
            long x = DefinedVariable(values, "X", cast.Source);
            CardPlay.Spend(
                cast.World, cast.World.Facts, [cast.World.Seats[cast.Player].Hand], paying,
                x, new string(Resources.Energy, checked((int)x)), itself: -1,
                cast.Player, cast.Events);
            cast.Results["energy"] = x;
            return;
        }

        if (cost is AbilityCost.Damage { MustTakeAll: true } damage)
        {
            var target = CostTarget(
                    cast.World, cast.Source, cast.Player, damage.Card)
                ?? throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' cannot find its take-damage cost target");
            long amount = damage.Amount;
            long before = target.Damage;
            var outcome = Damage.DealOutcome(
                cast.World, cast.World.Facts, cast.Source, target, amount,
                cast.Trigger, CardPlay.Verb, cast.Events);
            long taken = target.Damage - before;
            if (taken != amount)
            {
                // `rr:cost.12`: "If any of the damage is prevented, then the
                // cost has not been paid." The prevention itself has happened,
                // but neither another cost nor the post-arrow effect has.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' requires {amount} damage to be taken as a "
                    + $"cost, but only {taken} was taken; rr:cost.12 leaves it unpaid");
            }
            if (outcome == Damage.Outcome.Suspended)
            {
                cast.Results["costProcedurePending"] = 1;
                cast.Suspend();
            }
            return;
        }

        PayPrimitiveCost(cost, cast);
    }

    private static void SpendAbilityResources(
        string required, IReadOnlyList<int> paying, Cast cast, bool printed = false) =>
        CardPlay.Spend(
            cast.World,
            cast.World.Facts,
            [cast.World.Seats[cast.Player].Hand],
            paying,
            required.Length,
            required,
            itself: -1,
            cast.Player,
            cast.Events);

    /// <summary>
    /// "Discard a card from your hand" — a cost whose payment is a card and not
    /// a number of resources.
    /// </summary>
    /// <remarks>
    /// Refused rather than corrected when the answer does not match the
    /// request. <c>rr:initiating-abilities.step.5</c> aborts "without paying
    /// any costs" if the cost cannot be paid, and an engine that picked a card
    /// for the player would be making a decision the player was asked to make.
    /// </remarks>
    private static void DiscardToPay(AbilityCost.DiscardFromHand cost, IReadOnlyList<int> chosen, Cast cast)
    {
        var hand = cast.World.Seats[cast.Player].Hand;
        var (minimum, maximum) = AbilityCostSelection.Range(cost.Range, hand.Cards.Count);

        if (chosen.Count < minimum || chosen.Count > maximum)
        {
            string required = minimum == maximum
                ? $"{minimum}"
                : $"{minimum}..{maximum}";
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' costs {required} card(s) from hand and "
                + $"{chosen.Count} "
                + "were chosen; rr:initiating-abilities.step.5 aborts without paying");
        }

        foreach (int id in chosen)
        {
            var card = cast.World.Cards[id];
            if (card.Area != hand)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is not in {cast.World.Seats[cast.Player].Name}'s hand "
                    + "and cannot be discarded from it");
            }

            Rules.Play.Discard.Card(cast.World, card, CardPlay.Verb, cast.Events);
        }
    }

}
