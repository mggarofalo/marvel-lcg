using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic synthetic Special: Doctor Strange is eliminated partway through an Invocation.</summary>
public sealed class InvocationEliminationTests
{
    // Synthetic control: damage that defeats the resolving identity, then a board
    // effect, then the printed Invocation self-discard.
    private const string Special = """
        {"seq":[
          {"dealDamage":{"cards":"you","amount":20}},
          {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}},
          {"discardInvocation":"this"}
        ]}
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:removed-from-the-game.2")]
    public void AnInvocationWhoseResolverIsEliminatedFinishesTheSpecialAndStaysRemoved(bool master)
    {
        // rr:player-elimination.step.5: "Remove the eliminated player's play area and
        // each other game element within it ... from the game." rr:player-elimination.5:
        // "If a player is eliminated partway through the resolution of an ability,
        // resolve the entire ability." The eliminated player finishes the board effect
        // (ruling 79c57a49f6e38fe0), and "a card that has been removed from the game
        // cannot reenter the game through any means" (rr:removed-from-the-game.2).
        // Through 09005 the final "place it back on top of the Invocation deck" also
        // resolves and moves nothing.
        var (world, game, deck, _) = BoardWithSpecial(Special, last: false, master: master ? 1 : 0);
        var hero = world.Seats[1].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var invocations = deck.Deck.Cards.ToArray();
        var source = deck.PublicTop!;
        var removed = world.AreaOf(DeckType.RemovedArea);

        var result = game.Resolve(master ? MasterPayment(world, game, source) : Decision.Take(Mastery(game, hero).Id, [source.ObjectId], []));

        Assert.True(world.Seats[1].Eliminated);
        Assert.Same(removed, hero.Area);
        Assert.All(invocations, card => Assert.Same(removed, card.Area));
        Assert.Equal(2, scheme.Tokens["k_threat"]);
        var events = result.Events.ToList();
        int eliminated = events.FindIndex(happened => happened is CardsMoved move && move.Verb == "Eliminate"
            && move.Cards.Any(card => card.Card == source.ObjectId));
        Assert.True(eliminated >= 0);
        Assert.DoesNotContain(events.Skip(eliminated + 1), happened => happened is CardsMoved move
            && move.Cards.Any(card => card.Card == source.ObjectId));
        Assert.Empty(world.Areas.Where(area => area.Type is DeckType.AdditionalDeck or DeckType.AdditionalDiscardPile)
            .SelectMany(area => area.Cards));
        Assert.Equal(0, game.Pending!.Player);
    }

    private static Decision MasterPayment(World world, Game game, Card source)
    {
        var offer = Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId);
        int[] sources = [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId];
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!;
        return new Decision(offer.Id, [source.ObjectId], sources, Allocations: allocations);
    }
}
