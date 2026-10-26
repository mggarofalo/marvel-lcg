using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Production Images and Vapors on nonpublic boards; no pack admission.</summary>
public sealed class InvocationRowsIndependentReviewTests
{
    private static readonly string[] StatusKinds = ["confused", "stunned", "tough"];

    [Theory]
    [InlineData(false, "tough", "stunned")]
    [InlineData(false, "tough", "confused")]
    [InlineData(false, "stunned", "tough")]
    [InlineData(false, "stunned", "confused")]
    [InlineData(false, "confused", "tough")]
    [InlineData(false, "confused", "stunned")]
    [InlineData(true, "tough", "stunned")]
    [InlineData(true, "tough", "confused")]
    [InlineData(true, "stunned", "tough")]
    [InlineData(true, "stunned", "confused")]
    [InlineData(true, "confused", "tough")]
    [InlineData(true, "confused", "stunned")]
    [Rule("rr:status-cards.1")]
    public void ActualVaporsOffersAndResolvesEveryDifferentStatusOnTheChosenPhysicalHost(
        bool master, string oldKind, string newKind)
    {
        // "A character cannot have more than one status card of each type at a time."
        // Printed 09035 replaces the chosen status with a different status card.
        var (world, game, deck, _) = BoardActual("09035", false, master);
        var host = world.Seats[0].IdentityCard;
        var selected = Statuses.Give(world, host, oldKind);
        var untouched = Statuses.Give(world, world.TheCardIn(DeckType.VillainArea)!, oldKind);
        var original = deck.PublicTop!;
        var next = deck.Deck.Cards[^2];
        long words = world.Random.Generator.WordsConsumed;
        var start = game.Resolve(Answer(world, game, original, master));
        Assert.Equal(1, game.Pending!.Player);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances,
            offer => offer.AnchorId == selected.ObjectId).Id));
        Assert.Equal(1, game.Pending!.Player);
        var options = game.Pending.Affordances;
        Assert.Equal(StatusKinds.Where(kind => kind != oldKind)
            .Select(kind => "Replace with " + kind), options.Select(offer => offer.Description).Order());
        var finish = game.Resolve(Decision.Take(Assert.Single(options,
            offer => offer.Description == "Replace with " + newKind).Id));

        Assert.Equal(DeckType.RemovedArea, selected.Area.Type);
        Assert.Equal(DeckType.StatusArea, untouched.Area.Type);
        Assert.Equal(0, Statuses.Count(world, host, oldKind));
        Assert.Equal(1, Statuses.Count(world, host, newKind));
        var created = Assert.Single(Assert.Single(finish.Events.OfType<CardsCreated>()).Cards);
        Assert.Equal(newKind, created.Card);
        var attached = Assert.Single(finish.Events.OfType<CardAttached>());
        Assert.Equal(created.Id, attached.Card);
        Assert.Equal(host.ObjectId, attached.Host);
        Assert.Equal("Replace_Status", attached.Verb);
        Assert.DoesNotContain(finish.Events, item => item.Verb == "Give_Status");
        Assert.Equal(master ? original : next, deck.PublicTop);
        Assert.Equal(master, world.Seats[1].IdentityCard.Ready);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Single(start.Events.Concat(finish.Events).OfType<CardsMoved>(), move =>
            move.Verb == "Discard" && move.Cards.Any(card => card.Card == original.ObjectId));
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    public void ActualPaidImagesRejectsEmptyPaymentAndThenResolvesTheSameOffer(bool master)
    {
        // "Pay the cost(s)." 09033 costs one; Master adds its own printed one.
        var (world, game, deck, _) = BoardActual("09033", false, master);
        var original = deck.PublicTop!;
        var next = deck.Deck.Cards[^2];
        var hero = world.Seats[1].IdentityCard;
        var answer = Answer(world, game, original, master);
        var offer = Assert.Single(game.Pending!.Affordances, item => item.Id == answer.Affordance);
        Assert.Equal(master ? "2" : "1", Assert.Single(offer.CostOptions).Cost);
        var prompt = game.Pending;
        string before = Snapshot(world);
        var error = Assert.Throws<ArgumentException>(() => game.Resolve(
            Decision.Take(offer.Id, [original.ObjectId], [])));
        Assert.Contains("InvalidPayment", error.Message, StringComparison.Ordinal);
        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, Snapshot(world));
        Assert.True(hero.Ready);

        game.Resolve(answer);

        Assert.Equal(1, Statuses.Count(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.Equal(master ? original : next, deck.PublicTop);
        Assert.Equal(master ? deck.Deck : deck.Discard, original.Area);
        Assert.Equal(master, hero.Ready);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Decision Answer(World world, Game game, Card original, bool master)
    {
        var offer = master ? Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId)
            : Mastery(game, world.Seats[1].IdentityCard);
        int[] sources = master || original.FaceId == "09033"
            ? [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId] : [];
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!;
        return new Decision(offer.Id, [original.ObjectId], sources, Allocations: allocations);
    }
}
