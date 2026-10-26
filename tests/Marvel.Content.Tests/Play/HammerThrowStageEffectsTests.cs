using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.HammerThrowEncounterDealStageTests;

namespace Marvel.Content.Tests.Play;

public sealed class HammerThrowStageEffectsTests
{
    [Theory]
    [InlineData("01113", "01114", "01127")]
    [InlineData("01135", "01136", "01150")]
    [InlineData("04058", "04059", "04064")]
    [InlineData("04059", "04060", "04074")]
    [InlineData("04076", "04077", "04092")]
    [InlineData("04109", "04110", "04123")]
    [InlineData("04110", "04111", "01103")]
    [Rule("rr:villain-defeat")]
    [Rule("rr:reveal.step.3")]
    [Rule("rr:and")]
    public void ActualStageEffectsFinishBeforeTheTerminalReturn(string first, string next, string revealed)
    {
        // rr:villain-defeat: "The next sequential stage of the villain deck is revealed."
        // rr:reveal.step.3: "Resolve each When Revealed ability on that card."
        // rr:and: both effects resolve before the attack's response window.
        var runner = AuthoredCards.Runner();
        var probed = Prepare(runner, first, next);
        var control = Prepare(runner, first, next);
        foreach (var game in new[] { probed, control })
        {
            var area = game.World.AreaOf(next == "04060"
                ? DeckType.ExperimentalWeaponsDeck : DeckType.EncounterDeck);
            var card = game.World.CreateCard(revealed, area);
            if (next is "04110" or "04111") game.World.CreateCard(revealed, area);
            var before = Snapshot(game.World);
            var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
            if (ReferenceEquals(game, probed))
                for (int i = 0; i < 3; i++)
                {
                    runner.Actions(game.World, 0);
                    runner.Describe(game.World, action);
                    Assert.Equal(before, Snapshot(game.World));
                }
            runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
            var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
            Assert.False(game.Hammer.Ready);
            Assert.Equal(DeckType.UpgradesArea, game.Hammer.Area.Type);
            Assert.All(game.Resources, id => Assert.Equal(DeckType.DiscardPile, game.World.Cards[id].Area.Type));
            Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Villain.ObjectId), []);
            var choice = Sequence.Work(game.World, game.World.Facts, runner, []);
            if (next is "04110" or "04111")
            {
                Assert.NotNull(choice);
                Assert.Contains(choice.Affordances, option => option.Id == card.ObjectId);
                Assert.Equal(DeckType.UpgradesArea, game.Hammer.Area.Type);
                var pending = Snapshot(game.World);
                runner.Describe(game.World, action);
                Assert.Equal(pending, Snapshot(game.World));
                Sequence.Answer(game.World, game.World.Facts, runner, choice, Decision.Take(card.ObjectId), []);
                while (Sequence.Work(game.World, game.World.Facts, runner, []) is { } response)
                {
                    Assert.True(response.Cancellable);
                    Sequence.Answer(game.World, game.World.Facts, runner, response, Decision.Decline, []);
                }
            }
            else Assert.Null(choice);
            Sequence.Finish(game.World, game.World.Facts, runner, []);
            AssertStageResult(game, next, card);
        }
        Assert.Equal(Snapshot(control.World), Snapshot(probed.World));
    }

    private static void AssertStageResult(Prepared game, string next, Card card)
    {
        var stage = game.World.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(next, stage.FaceId);
        Assert.Equal(0, stage.Damage);
        Assert.Same(game.World.Seats[0].Hand, game.Hammer.Area);
        Assert.Equal(DeckType.DiscardPile, game.Event.Area.Type);
        if (next is "04059" or "04060") Assert.Equal(stage.ObjectId, card.Area.Host);
        else if (next == "04111") Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type);
        else Assert.Equal(DeckType.SideSchemesArea, card.Area.Type);
        if (next == "01114") Assert.Equal(28, DamagePlacement.Health(game.World, game.World.Facts, stage));
        if (next == "01136")
        {
            var drones = game.World.Cards.Where(c => c.Area.Type == DeckType.EngagedEnemiesArea).ToArray();
            Assert.Equal(2, drones.Length);
            Assert.All(drones, drone => Assert.Equal(2, DamagePlacement.Health(game.World, game.World.Facts, drone)));
        }
        if (next is "04110" or "04111") Assert.Equal(next == "04111" ? 2 : 1, game.World.Seats[0].IdentityCard.Damage);
        Assert.Empty(game.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TerminalReturnUsesLiveSourceAndHealthAfterStageWork(bool discardHammer)
    {
        // Synthetic reveal changes the exact dependency that a terminal live
        // instruction must re-evaluate; it must not use a projected old source.
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var effect = discardHammer ? """{"discard":{"titled":"Mjolnir"}}"""
            : """{"dealDamage":{"cards":"you","amount":1}}""";
        book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "01095")!
            ["abilities"]![0]!["effect"] = JsonNode.Parse(effect);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var game = Prepare(runner, "01094", "01095");
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Villain.ObjectId), []);
        Sequence.Finish(game.World, game.World.Facts, runner, []);
        Assert.Equal(discardHammer ? DeckType.DiscardPile : DeckType.HandsArea, game.Hammer.Area.Type);
        Assert.Equal(discardHammer ? 0 : 1, game.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, game.Event.Area.Type);
        Assert.Empty(game.World.Agenda.Outstanding);
    }
}



