using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HammerThrowRecoveryPreflightTests
{
    [Theory]
    [InlineData("01186")]
    [InlineData("01101")]
    [Rule("rr:damage.step.6")]
    [Rule("rr:damage.step.9")]
    public void ActualHammerThrowFinishesRecoveryAndReturnsHammerBeforeDefeatResponse(string top)
    {
        // Printed 06005/06028 and ruling 8fe224a8f1e012b2: overkill is applied
        // before determining whether Loki revives. The synthetic board keeps
        // Thor outside public product admission.
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var hall = world.CreateCard("06017", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var discarded = world.CreateCard(top, world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("01090", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        var events = new List<GameEvent>();
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
        var target = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.False(hammer.Ready);
        Assert.Equal(DeckType.UpgradesArea, hammer.Area.Type);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(loki.ObjectId), events);
        var response = Sequence.Work(world, world.Facts, runner, events);
        Assert.Same(world.Seats[0].Hand, hammer.Area);
        Assert.Equal(4, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, discarded.Area.Type);
        Assert.Equal(top == "01186" ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, loki.Area.Type);
        Assert.Equal(0, loki.Damage);
        Assert.Equal(2, StateFields.Modified(world, world.Seats[0].IdentityCard, "attack", world.Facts, world.Players));
        if (top == "01186") Assert.Null(response);
        else
        {
            Assert.NotNull(response);
            var offer = Assert.Single(response.Affordances, a => a.AnchorId == hall.ObjectId);
            Sequence.Answer(world, world.Facts, runner, response, Decision.Take(offer.Id), events);
        }
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(top == "01186" ? 0 : 1, hall.Tokens.GetValueOrDefault("c_glory"));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("health")]
    [InlineData("hosted")]
    [InlineData("lastingHealth")]
    public void RecoveryRefusesHealthDependenciesOrHostedCardsBeforeTargetMutation(string dependency)
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        if (dependency == "hosted")
            world.CreateCard("01185", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), loki.ObjectId));
        else if (dependency == "health")
            world.CreateCard("06010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        else
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", 1,
                Affects: world.Seats[0].IdentityCard.ObjectId));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("01090", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
        var target = Sequence.Work(world, world.Facts, runner, [])!;
        var before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(world, world.Facts, runner, target, Decision.Take(loki.ObjectId), []));
        Assert.Contains(dependency == "hosted" ? "step-6 interrupt" : "unprojected defeat window", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(0, loki.Damage);
        Assert.False(hammer.Ready);
        Assert.Equal(DeckType.UpgradesArea, hammer.Area.Type);
    }
    [Fact]
    public void RecoveryDoesNotAuthorizeAnotherBoardDependentDamageEffect()
    {
        // Synthetic continuation challenges the same recovery boundary with a
        // later damage read. The original Hammer Throw body is tested above.
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner("""{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}""");
        world.Abilities = runner;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("01090", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
        var target = Sequence.Work(world, world.Facts, runner, [])!;
        var before = world.Digest().Canonical();
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(world, world.Facts, runner, target, Decision.Take(loki.ObjectId), []));
        Assert.Contains("unprojected defeat window", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, loki.Damage);
        Assert.Equal(DeckType.UpgradesArea, hammer.Area.Type);
    }

    private static AbilityRunner Runner(string? continuation = null)
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        book["cards"]!.AsArray().Add(JsonNode.Parse(Loki));
        if (continuation is not null)
        {
            var hammer = book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "06005")!;
            hammer["abilities"]![0]!["effect"]!["chooseCard"]!["effect"]!["attack"]!["effect"]!["seq"]![1] =
                JsonNode.Parse(continuation);
        }
        return new(AbilityCatalog.Parse(book.ToJsonString()));
    }

    private const string Loki = """
        {
          "card": "06028",
          "name": "Loki",
          "note": "Printed 06028 and mandatory Forced Interrupt erratum in datasets/marvelcdb-faq/faq.json. The physical top discard is tested after discard and any deck reset; treachery heals all damage on this Loki incarnation instead of defeat. Rulings 8fe224a8f1e012b2 and c55100b5139ab3a5 preserve overkill before healing and deny defeat credit when Loki heals. Receipt syntax is an engine choice.",
          "abilities": [
            {
              "trigger": {
                "event": "WhenCardWouldBeDefeated",
                "timing": "ForcedInterrupt",
                "subject": "this"
              },
              "effect": {
                "forEachDiscarded": {
                  "effect": {
                    "discardTop": {
                      "from": "encounterDeck",
                      "count": 1
                    }
                  },
                  "when": {
                    "isKind": {
                      "card": "that",
                      "kind": "treachery"
                    }
                  },
                  "then": {
                    "heal": {
                      "card": "this",
                      "amount": {
                        "damageOn": "this"
                      }
                    }
                  }
                }
              }
            }
          ]
        }
        """;
}
