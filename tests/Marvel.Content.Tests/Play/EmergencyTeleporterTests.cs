using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

public sealed class EmergencyTeleporterTests
{
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [Rule("rr:in-play-and-out-of-play.4")]
    public void RewardIsRemovedBeforeSearchAndToughFollowsAllyEntry(bool fromDeck, int entryMove)
    {
        var runner = Runner(entryMove);
        Card? reward = null;
        Card? ally = null;
        var (game, world) = Playing(board =>
        {
            reward = board.CreateCard("04157", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            ally = board.CreateCard("01067", fromDeck ? board.Seats[0].Deck : board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
            if (fromDeck)
                for (int i = 0; i < 8; i++) board.CreateCard("01088", board.Seats[0].Deck);
            var setup = new CampaignGameSetup("synthetic", false, CampaignStage.Crossbones, 1,
                [new CampaignSeatSetup(0, [], null, false, [], false)], [], 0);
            new CampaignAttemptState(setup,
                [new CampaignCardBinding(reward.ObjectId, 0, new CampaignCard("teleporter", "04157", CampaignCardRole.Tech))]).Bind(board);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == reward!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.True(Assert.Single(world.Campaign!.Cards).Removed);
        Assert.Equal(DeckType.DiscardPile, reward!.Area.Type);
        var choice = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == ally!.ObjectId);
        int originIncarnation = ally!.Incarnation;
        game.Resolve(Decision.Take(choice.Id));
        if (entryMove > 0)
        {
            Assert.Equal(entryMove == 1 ? DeckType.HandsArea : DeckType.AlliesArea, ally!.Area.Type);
            if (entryMove == 2) Assert.True(ally.Incarnation > originIncarnation + 1);
            Assert.False(Statuses.Has(world, ally, Statuses.Tough));
            return;
        }
        Assert.Equal(DeckType.AlliesArea, ally!.Area.Type);
        Assert.Equal(originIncarnation + 1, ally.Incarnation);
        Assert.False(Statuses.Has(world, ally, Statuses.Tough));
        var response = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == ally.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.True(Statuses.Has(world, ally, Statuses.Tough));
    }
    private static AbilityRunner Runner(int entryMove)
    {
        if (entryMove == 0) return AuthoredCards.Runner();
        // Synthetic entry response probes the real Teleporter's continuation binding.
        string effect = entryMove == 1 ? """{"returnToHand":"this"}""" : """
            {"seq":[{"returnToHand":"this"},{"putIntoPlay":{"card":"this","where":"yourPlayArea"}}]}
            """;
        var entry = AbilityCatalog.Parse($$"""
        {"cards":[{"card":"01067","abilities":[{"trigger":{"timing":"ForcedResponse","event":"WhenCardEntersPlay","subject":"this"},"maxPerRound":1,"effect":{{effect}}}]}]}
        """);
        return new(AuthoredCards.Book with
        {
            Abilities = [.. AuthoredCards.Book.Abilities.Where(row => row.Card != "01067"), .. entry.Abilities],
        });
    }
}


