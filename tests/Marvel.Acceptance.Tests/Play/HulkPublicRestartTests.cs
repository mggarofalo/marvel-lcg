using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class HulkPublicRestartTests(ITestOutputHelper output)
{
    [Fact]
    public void SoloUnstoppableForceFromNaturalStarterHandSurvivesFreshHost()
    {
        var journey = new HulkRestartJourney(new GameSpecification("rhino", ["hulk"], null, 1));
        var policy = new HulkGamePolicy(journey.Factory.Game.State.Facts);
        for (int ordinal = 1; ordinal < 6; ordinal++) journey.Answer(journey.Next(policy));
        var action = journey.Next(policy);
        var source = journey.Factory.Game.State.Cards[
            Assert.Single(journey.Current.Prompt!.Affordances, offer => offer.Id == action.Affordance).AnchorId];
        Assert.Equal("10006", source.FaceId);
        Assert.NotEmpty(action.Spent);
        Assert.False(journey.Factory.Game.State.Seats[0].IdentityCard.Ready);
        int deck = journey.Factory.Game.State.Seats[0].Deck.Cards.Count;
        var fresh = journey.Restart();
        fresh.Reject(action, stale: true);
        fresh.Reject(Decision.Take(action.Affordance, action.Targets, []));
        journey.Continue(fresh, action);
        Assert.True(fresh.Factory.Game.State.Seats[0].IdentityCard.Ready);
        // Printed 10006: "Ready Hulk. If you paid ... using only [physical] resources, draw 1 card."
        Assert.Equal(deck - 1, fresh.Factory.Game.State.Seats[0].Deck.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, fresh.Factory.Game.State.Cards[source.ObjectId].Area.Type);
        ContinueFour(journey, fresh, policy);
        output.WriteLine("rhino/1 decision 6: Unstoppable Force; restart, stale/unpaid rejection, ready/draw and four further decisions matched.");
    }

    [Fact]
    [Rule("rr:choose-game-element.1")]
    public void NonFirstPlayerPaidThunderclapRecipientChoiceSurvivesSecondFreshHost()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var heroes = factory.DiscoverDecks().Heroes;
        string[] hulk = [.. heroes.Single(hero => hero.Key == "hulk").Starter.SkipLast(3), "10032", "10032", "10032"];
        Assert.True(factory.AssessDeck("hulk", hulk).IsValid);
        PlayerDeckRecipe[] decks = [new(heroes.Single(hero => hero.Key == "captain_marvel").Starter), new(hulk)];
        var journey = new HulkRestartJourney(new GameSpecification("rhino", ["captain_marvel", "hulk"], null, 8, decks));
        var policy = new HulkGamePolicy(journey.Factory.Game.State.Facts);
        for (int ordinal = 1; ordinal < 14; ordinal++) journey.Answer(journey.Next(policy));
        var action = journey.Next(policy);
        var source = journey.Factory.Game.State.Cards[
            Assert.Single(journey.Current.Prompt!.Affordances, offer => offer.Id == action.Affordance).AnchorId];
        Assert.Equal("10005", source.FaceId);
        Assert.Equal(1, journey.Current.Prompt.Player);
        Assert.NotEmpty(action.Spent);
        var fresh = journey.Restart();
        fresh.Reject(Decision.Take(action.Affordance, action.Targets, []));
        journey.Continue(fresh, action);
        Assert.False(fresh.Factory.Game.IsRootPrompt);
        // "The player resolving the ability ... is the player who makes the choice."
        Assert.Equal(1, fresh.Current.Prompt!.Player);
        Assert.Contains("choose damage recipients", fresh.Current.Prompt.Label);
        Assert.All(action.Spent, card => Assert.Equal(DeckType.DiscardPile, fresh.Factory.Game.State.Cards[card].Area.Type));
        int[] paidHand = fresh.Factory.Game.State.Seats[1].Hand.Cards.Select(card => card.ObjectId).ToArray();
        var suspended = fresh.Restart();
        var choice = Assert.Single(suspended.Current.Prompt!.Affordances);
        int villain = suspended.Factory.Game.State.TheCardIn(DeckType.VillainArea)!.ObjectId;
        Assert.Contains(villain, choice.Targets!.Legal);
        long damage = suspended.Factory.Game.State.Cards[villain].Damage;
        var answer = Decision.Take(choice.Id, [villain], []);
        suspended.Reject(answer, stale: true);
        suspended.Reject(Decision.Take(choice.Id, [source.ObjectId], []));
        journey.Continue(suspended, answer);
        // Printed 10005: "Choose up to 3 different enemies. Deal 3 damage to each of them."
        Assert.Equal(damage + 3, suspended.Factory.Game.State.Cards[villain].Damage);
        Assert.True(suspended.Factory.Game.IsRootPrompt);
        Assert.Equal(paidHand, suspended.Factory.Game.State.Seats[1].Hand.Cards.Select(card => card.ObjectId));
        Assert.Equal(DeckType.DiscardPile, suspended.Factory.Game.State.Cards[source.ObjectId].Area.Type);
        ContinueFour(journey, suspended, policy);
        output.WriteLine("rhino custom multiplayer/8 decisions 14-15: Hulk seat 1 paid Thunderclap, restart before/after payment, invalid/stale choice rejection and exact damage/continuation matched.");
    }

    private static void ContinueFour(HulkRestartJourney original, HulkRestartJourney fresh, HulkGamePolicy policy)
    {
        for (int count = 0; count < 4; count++) original.Continue(fresh, original.Next(policy));
    }
}
