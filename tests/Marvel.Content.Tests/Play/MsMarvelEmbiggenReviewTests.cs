using System.Text.Json.Nodes;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelEmbiggenReviewTests
{
    private static AbilityRunner WithEvent(string face, string effect)
    {
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = root["cards"]!.AsArray();
        foreach (var existing in cards.Where(card => card!["card"]!.GetValue<string>() == face).ToArray())
            cards.Remove(existing);
        cards.Add(JsonNode.Parse("{\"card\":\"" + face + "\",\"abilities\":[{\"trigger\":"
            + "{\"event\":\"WhenActionTriggered\",\"timing\":\"Action\",\"subject\":\"this\",\"form\":\"hero\"},\"effect\":"
            + effect + "}]}"));
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:event.5")]
    public void ActualEmbiggenModifiesBothMeleeDamageInstancesAndExpires(bool accept)
    {
        // rr:event.5: "each of those instances is modified". FAQ 05010's
        // printed Melee example is 3+3 becoming 5+5 against different enemies.
        // The test-only Melee row fixes the two legal recipients; target-choice
        // admission and whole-pack support are outside this modifier test.
        var runner = WithEvent("05030", """
            {"seq":[{"dealDamage":{"cards":{"query":"villain"},"amount":3}},
              {"dealDamage":{"cards":{"titled":"Whirlwind"},"amount":3}}]}
            """);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var upgrade = world.CreateCard("05010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("05030", world.Seats[0].Hand);
        var minion = world.CreateCard("01130", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var payment1 = world.CreateCard("01088", world.Seats[0].Hand);
        var payment2 = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment1.ObjectId, payment2.ObjectId], []));
        Assert.Equal(DeckType.DiscardPile, payment1.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment2.Area.Type);
        Assert.Equal(0, villain.Damage);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var interrupt = Assert.Single(prompt.Affordances);
        Assert.Equal(upgrade.ObjectId, interrupt.AnchorId);
        Sequence.Answer(world, world.Facts, runner, prompt,
            accept ? Decision.Take(interrupt.Id) : Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(accept ? 5 : 3, villain.Damage);
        Assert.Equal(accept ? 5 : 3, minion.Damage);
        Assert.Equal(!accept, upgrade.Ready);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventDamage");
    }

    [Theory]
    [InlineData("05003", true)]
    [InlineData("01086", false)]
    [Rule("rr:response.2")]
    public void ActualMorphogeneticsChecksTraitRatherThanAnAttackBody(string face, bool qualified)
    {
        // Ruling 556ecad3abe55ce9: "Ms. Marvel’s ability references the traits".
        // Both fixtures execute the same attack-labelled body; only Big Hands
        // has the printed Attack trait. First Aid is a synthetic negative body.
        var runner = WithEvent(face, """
            {"attack":{"target":{"query":"villain"},"effect":
              {"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}}}
            """);
        Card? card = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            var seat = board.Seats[0];
            World.MoveToTop(seat.IdentityCard, board.AreaOf(DeckType.RemovedArea, PlayArea.Of(0)));
            seat.IdentityCard = board.CreateCard("05001a,05001b", seat.Hero);
            card = board.CreateCard(face, seat.Hand);
            payment = board.CreateCard("01088", seat.Hand);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == card!.ObjectId);
        game.Resolve(PaidDecision.Take(action, [payment!.ObjectId]));
        var prompt = game.Pending;
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        if (qualified)
        {
            Assert.NotNull(prompt);
            var recovery = Assert.Single(prompt.Affordances);
            Assert.Equal(world.Seats[0].IdentityCard.ObjectId, recovery.AnchorId);
            game.Resolve(Decision.Take(recovery.Id));
            Assert.Same(world.Seats[0].Hand, card.Area);
            Assert.False(world.Seats[0].IdentityCard.Ready);
        }
        else
        {
            Assert.True(game.IsRootPrompt);
            Assert.True(world.Seats[0].IdentityCard.Ready);
        }
        Assert.False(world.Agenda.IsBusy);
    }
}
