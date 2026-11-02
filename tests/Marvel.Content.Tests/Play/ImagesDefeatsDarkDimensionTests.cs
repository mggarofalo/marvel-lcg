using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.SpellMasteryFixture;

namespace Marvel.Content.Tests.Play;

/// <summary>Production Images of Ikonn defeating production Open the Dark Dimension mid-Special.</summary>
public sealed class ImagesDefeatsDarkDimensionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-defeated-abilities.1")]
    [Rule("rr:interrupt.3")]
    [Rule("rr:referential-ability.step.1")]
    public void TheMidSpecialShuffleStillLetsImagesPlaceItselfInTheInvocationDiscard(bool master)
    {
        // rr:when-defeated-abilities.1: "When Defeated" is "Forced Interrupt: When this
        // card is defeated...", and rr:interrupt.3 resolves it "when its triggering
        // condition initiates, but before that triggering condition resolves" — inside
        // Images' "remove 4 threat", before "Place this card in the Invocation deck
        // discard pile". 09029's "Shuffle the Invocation card under here into the
        // Invocation deck" shuffles the deck Images is still the top of.
        // rr:referential-ability.step.1: "this card" is the card the ability is printed
        // on, wherever the shuffle left it.
        var (world, game, deck, _) = BoardActual("09033", last: false, master);
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var images = deck.PublicTop!;
        Assert.Equal("09033", images.FaceId);
        var scheme = world.CreateCard("09029", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 3);
        var hosted = deck.Deck.Cards[0];
        BeneathCards.Place(world, hosted, scheme, faceUp: false, "test", []);
        Assert.Same(images, deck.PublicTop);
        var physical = deck.Deck.Cards.Concat(deck.Discard.Cards).Append(hosted)
            .Select(card => card.ObjectId).Order().ToArray();
        var offer = master ? Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId) : Mastery(game, hero);
        int[] sources = [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId];
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!;
        var first = game.Resolve(new Decision(offer.Id, [images.ObjectId], sources, Allocations: allocations));
        Assert.Single(game.Pending!.Affordances, item => item.Id == scheme.ObjectId);

        var second = game.Resolve(Decision.Take(scheme.ObjectId));

        var events = first.Events.Concat(second.Events).ToList();
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Confused));
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Empty(BeneathCards.Of(world, scheme));
        int shuffled = events.FindIndex(happened => happened is AreaReordered reordered && reordered.Verb == "Shuffle");
        int discarded = events.FindIndex(happened => happened is CardsMoved move && move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == images.ObjectId));
        Assert.InRange(shuffled, 0, int.MaxValue);
        Assert.True(discarded > shuffled);
        if (master)
        {
            Assert.Same(images, deck.PublicTop);
            Assert.Same(deck.Deck, hosted.Area);
            Assert.Empty(deck.Discard.Cards);
        }
        else
        {
            Assert.Equal([images], deck.Discard.Cards);
            Assert.True(images.FaceUp);
            Assert.Same(deck.Deck, hosted.Area);
            Assert.Equal(4, deck.Deck.Cards.Count);
        }
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        Assert.Equal([deck.PublicTop], deck.Deck.Cards.Where(card => card.FaceUp));
        Assert.Equal(master, hero.Ready);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
