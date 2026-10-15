using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CardPowerContinuationTests
{
    [Rule("rr:initiating-abilities.3")]
    [Fact]
    public void SuccessivePowersRestoreTheOuterSelectionAfterTheirSourceLeavesPlay()
    {
        // Synthetic ability on a Core fixture: "the sequence does not stop from
        // completing if that card leaves play during this sequence". Each labelled
        // power has its own target; the chosen player belongs to the outer ability.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" }, "effect": { "seq": [] }
              } },
              { "discard": "this" },
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": { "cards": "chosen", "amount": 1 } }
              } },
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": { "cards": "chosen", "amount": 2 } }
              } },
              { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "removeThreat": { "scheme": "chosen", "amount": 1 } }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long damage = villain.Damage;
        long threat = scheme.Tokens["k_threat"];
        int firstHand = world.Seats[0].Hand.Cards.Count;
        int secondHand = world.Seats[1].Hand.Cards.Count;

        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(damage, villain.Damage);
        Assert.Equal(threat, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);

        game.Resolve(Decision.Take(world.Seats[1].IdentityCard.ObjectId));

        Assert.Equal(damage + 3, villain.Damage);
        Assert.Equal(threat - 1, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(firstHand, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHand + 1, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.NotNull(game.Pending);
    }
}
