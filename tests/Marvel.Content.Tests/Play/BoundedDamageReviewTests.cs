using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BoundedDamageReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayerSummaryMatchesDirectVillainDamageAndTheChosenPlayersLaterGroup(bool bounded)
    {
        var runner = Runner("01006", "Action", """
            {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[
              {"dealDamage":{"cards":{"query":"villain"},"amount":1}},
              {"dealDamage":{"cards":{"query":"enemiesEngagedWithChosenPlayer"},"amount":3MAXIMUM}}
            ]}}}
            """.Replace("MAXIMUM", bounded ? ",\"maximum\":1" : string.Empty, StringComparison.Ordinal));
        Card? source = null;
        Card[] enemies = [];
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            enemies = [board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
                board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1))),
                board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)))];
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == source!.ObjectId).Id));
        var player = Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == world.Seats[1].IdentityCard.ObjectId);
        Assert.Contains("Deal 1 damage to the villain", player.Description);
        Assert.Equal(bounded, player.Description!.Contains("up to 1", StringComparison.Ordinal));
        Assert.Equal(!bounded, player.Description.Contains("to each enemy engaged", StringComparison.Ordinal));
        game.Resolve(Decision.Take(player.Id));
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        if (bounded)
        {
            Assert.All(enemies, enemy => Assert.Equal(0, enemy.Damage));
            Assert.Equal(0, game.Pending!.Player);
            var group = Assert.Single(game.Pending.Affordances);
            Assert.Equal(0, group.Targets!.Min);
            Assert.Equal(1, group.Targets.Max);
            Assert.Equal(enemies.Skip(1).Select(enemy => enemy.ObjectId), group.Targets.Legal);
            game.Resolve(new Decision(group.Id, [enemies[1].ObjectId]));
        }
        Assert.Equal(0, enemies[0].Damage);
        Assert.Equal(3, enemies[1].Damage);
        Assert.Equal(bounded ? 0 : 3, enemies[2].Damage);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DependentDrawRequiresASelectedDamageRecipient(bool select)
    {
        var runner = Runner("01006", "Action", """
            {"then":{"effect":{"dealDamage":{"cards":{"query":"minionsEngagedWithYou"},"amount":3,"maximum":1}},
              "then":{"draw":{"player":"you","count":1}}}}
            """);
        Card? source = null;
        Card? enemy = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            enemy = board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == source!.ObjectId).Id));
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id,
            select ? [enemy!.ObjectId] : []));
        Assert.Equal(hand + (select ? 1 : 0), world.Seats[0].Hand.Cards.Count);
        Assert.Equal(select ? 3 : 0, enemy!.Damage);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void PlayerChoiceExplainsTheLaterBoundedGroupInsteadOfAllEngagedEnemies()
    {
        // Synthetic composition challenges the generic damage vocabulary; no pack is admitted.
        var runner = Runner("01006", "Action", """
            {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[
              {"dealDamage":{"cards":{"query":"villain"},"amount":1}},
              {"dealDamage":{"cards":{"query":"enemiesEngagedWithChosenPlayer"},"amount":3,"maximum":1}}
            ]}}}
            """);
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            for (int i = 0; i < 2; i++)
                board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var prompt = Assert.IsType<Prompt>(runner.PreviewChoice(world, source!, 0, 0, null, false));
        string explanation = string.Join(" ", prompt.Affordances.Select(offer => offer.Description));
        Assert.DoesNotContain("to each enemy engaged", explanation);
        Assert.Contains("up to 1", explanation);
    }
}
