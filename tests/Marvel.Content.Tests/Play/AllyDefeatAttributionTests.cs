using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AllyDefeatAttributionTests
{
    [Fact]
    [Rule("rr:you-your.6")]
    [Rule("rr:you-your.15")]
    public void MockingbirdsMinionDefeatDoesNotOfferInterrogationRoom()
    {
        // Interrogation Room: "After you defeat a minion". rr:you-your.6
        // excludes "when an ally under that player's control attacks and
        // defeats a minion"; .15 says allies' functions are not performed
        // by that player's identity.
        Card room = null!;
        Card ally = null!;
        Card minion = null!;
        var (game, world) = Playing(board =>
        {
            room = InPlay(board, "01063");
            ally = board.CreateCard("01083", board.AreaOf(
                DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            minion = board.Cards.First(card => card.FaceId == "01101");
            World.MoveToTop(minion, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(2);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, hero: true);
        Card scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = scheme.Tokens["k_threat"];
        Affordance attack = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == ally.ObjectId && option.Verb == BasicPowers.AttackVerb);

        game.Resolve(Decision.Take(attack.Id, [minion.ObjectId], []));

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == room.ObjectId);
        Assert.False(ally.Ready);
        Assert.Equal(1, ally.Damage);
        Assert.True(room.Ready);
        Assert.Equal(threat, scheme.Tokens["k_threat"]);
        Assert.True(game.IsRootPrompt);
    }
}
