using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent actual-book evidence on prepared nonpublic boards.</summary>
public sealed class DoctorStrangeNextFourReviewTests
{
    private static readonly string[] StatNames = ["thwart", "attack", "defense"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:forced.5")]
    [Rule("rr:ownership-and-control.7.2")]
    public void TwoEnhancementsKeepSeparateControllersAndReturnBothCopiesToTheirOwner(bool reverse)
    {
        // "the first player determines the order" of simultaneous forced abilities.
        // A card leaving play goes to "its owner's equivalent out-of-play area".
        // Printed 09010: "Play under any player's control"; round-end discard is forced.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var owner = world.Seats[1];
        long[] Stats(Card hero) => StatNames
            .Select(stat => StateFields.Modified(world, hero, stat, world.Facts, world.Players)).ToArray();
        var baseline = world.Seats.Select(seat => Stats(seat.IdentityCard)).ToArray();
        var copies = world.Seats.Select((seat, player) =>
        {
            var copy = world.CreateCard("09010", owner.Hand);
            var resource = world.CreateCard("01093", owner.Hand);
            CardPlay.Play(world, world.Facts, runner, owner, copy, [resource.ObjectId], [],
                [seat.IdentityCard.ObjectId]);
            Assert.Equal(1, copy.Owner);
            Assert.Equal(player, copy.Area.PlayArea.Player);
            return copy;
        }).ToArray();
        for (int player = 0; player < 2; player++)
            Assert.Equal(baseline[player].Select(value => value + 1), Stats(world.Seats[player].IdentityCard));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Order, prompt.Asking);
        Assert.Equal(world.FirstPlayer, prompt.Player);
        Assert.Equal(copies.Select(copy => copy.ObjectId).Order(), prompt.Affordances.Select(a => a.AnchorId).Order());
        Assert.All(copies, copy => Assert.Equal(DeckType.UpgradesArea, copy.Area.Type));
        int first = reverse ? 1 : 0;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(copies[first].ObjectId), events);
        Sequence.Finish(world, world.Facts, runner, events);
        var discarded = events.OfType<CardsMoved>().Where(move => move.Verb == "Discard")
            .SelectMany(move => move.Cards).Where(card => copies.Any(copy => copy.ObjectId == card.Card)).ToArray();
        Assert.Equal(new[] { copies[first].ObjectId, copies[1 - first].ObjectId }, discarded.Select(card => card.Card));
        Assert.All(copies, copy =>
        {
            Assert.Equal(DeckType.DiscardPile, copy.Area.Type);
            Assert.Equal(1, copy.Area.PlayArea.Player);
        });
        for (int player = 0; player < 2; player++)
            Assert.Equal(baseline[player], Stats(world.Seats[player].IdentityCard));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SevenRingsCanProtectAnAllyAndAnEngagedMinionTogether(bool master, bool last)
    {
        // Printed 09034: "Give up to 3 characters each a tough status card."
        var (world, game, deck, _) = SpellMasteryFixture.BoardActual("09034", last, master);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var minion = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        ally.TakeDamage(1);
        minion.TakeDamage(1);
        var original = deck.PublicTop!;
        game.Resolve(SevenRingsRecipientTests.Payment(world, game, master, original));
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.Contains(ally.ObjectId, offer.Targets!.Legal);
        Assert.Contains(minion.ObjectId, offer.Targets.Legal);
        var result = game.Resolve(new Decision(offer.Id, [minion.ObjectId, ally.ObjectId]));
        Assert.Equal(new[] { ally.ObjectId, minion.ObjectId }.Order(), result.Events.OfType<CardAttached>()
            .Where(item => item.Verb == "Give_Status").Select(item => item.Host).Order());
        Assert.All(new[] { ally, minion }, card =>
        {
            Assert.Equal(1, Statuses.Count(world, card, Statuses.Tough));
            Assert.Equal(1, card.Damage);
        });
        Assert.All(world.Seats, seat => Assert.Equal(0, Statuses.Count(world, seat.IdentityCard, Statuses.Tough)));
        Assert.Equal(0, Statuses.Count(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Tough));
        Assert.Single(result.Events.OfType<CardsMoved>(), move => move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Equal(last ? 1 : 0, result.Events.OfType<AreaReordered>().Count());
        if (master) Assert.Same(original, deck.PublicTop);
        else if (!last) Assert.Same(deck.Discard, original.Area);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
