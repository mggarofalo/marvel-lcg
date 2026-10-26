using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns payment and entry as one invocation with explicit child dependencies.</summary>
internal static class CardPlayPayment
{
    internal static void Play(World world, CardPlayInitiation play, List<GameEvent> events)
    {
        var payment = Prepare(world, play);
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, [CardPlay.Verb], Subject: play.Card.ObjectId, Player: play.Seat.Index),
            execution =>
            {
                PlayedCardExposure.Expose(world, play.Card, play.Seat.Index, events);
                var receipt = payment.Commit(events);
                var entry = new CardPaymentEntry
                {
                    World = world, Execution = execution, Facts = play.Facts, Abilities = play.Abilities,
                    Seat = play.Seat, Card = play.Card, Incarnation = play.Card.Incarnation,
                    Cost = play.Cost, Targets = play.Targets, Payment = receipt,
                };
                if (receipt.IsComplete) return entry.Apply(world, events);
                world.Agenda.ThenContinuation(
                    new PhaseStep(new CardPaymentEntryOperation(entry), world.Agenda.Current?.Round ?? 0, 0),
                    execution.Occurrence);
                return execution.Yield(events);
            });
    }

    internal static CardResourcePayment Prepare(World world, CardPlayInitiation play)
    {
        if (EffectiveCards.Kind(play.Card, play.Facts) == CardKind.Upgrade
            && play.Abilities.AttachmentTargets(world, play.Card) is { } eligible
            && (play.Targets.Count != 1 || !eligible.Contains(play.Targets[0])))
            throw new RulesNotImplementedException(
                $"card {play.Card.ObjectId} must attach to exactly one eligible target");
        var payment = new CardResourcePayment(world, play.Facts, world.ResourceAbilities, new CardResourcePaymentRequest
        {
            Hands = [.. CardPayment.Paying(world, play.Facts, play.Seat, play.Card).Select(player => player.Hand)],
            Paying = play.Paying, Cost = play.Cost.Amount,
            Required = Resources.Required(world, play.Card, play.Facts),
            Itself = play.Card.ObjectId, Payer = play.Seat.Index, PayingFor = play.Card,
            Allocations = play.Allocations,
            Price = play.Allocations.Count == 0 ? null : new Marvel.Rules.Prompts.CostOption(
                0, play.Cost.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [Resources.Required(world, play.Card, play.Facts)], Sources: [..
                    CardPayment.Paying(world, play.Facts, play.Seat, play.Card)
                        .SelectMany(seat => CardPayment.Generators(
                            world, play.Facts, seat, world.ResourceAbilities, play.Card))]),
        });
        payment.Prepare();
        return payment;
    }
}
