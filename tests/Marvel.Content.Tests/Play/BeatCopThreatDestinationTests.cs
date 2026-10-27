using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BeatCopThreatDestinationTests
{
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [Rule("rr:move.6")]
    public void BeatCopMovesOneThreatToItsOwnSupportAndExhaustsInEitherForm(bool hero, int player)
    {
        // "If threat is moved off a scheme, the moved threat is considered to be removed".
        var runner = Runner();
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, hero: hero, owner: player, runner: runner);
        var source = world.CreateCard("10029", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(player), cardOwner: 1 - player));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var other = world.CreateCard("10029", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1 - player)));
        var baseline = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());
        Assert.DoesNotContain(runner.Actions(world, 1 - player), a => a.Card == source.ObjectId);
        var action = Assert.Single(runner.Actions(world, player), a => a.Card == source.ObjectId);
        Assert.Equal(baseline.Item1, world.Digest().Canonical());
        Assert.Equal(baseline.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(baseline.Item3, world.Agenda.Outstanding);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        Answer(world, runner, events, scheme.ObjectId);
        Assert.Equal(2, scheme.Tokens["k_threat"]);
        Assert.Equal(1, source.Tokens["k_threat"]);
        Assert.Equal(0, other.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(1 - player, source.Owner);
        Assert.False(source.Ready);
        Assert.DoesNotContain(runner.Actions(world, player), a => a.Card == source.ObjectId);
        Assert.DoesNotContain(events, e => e.Verb == "Thwart");
    }

    // Actual printed 10029 draft, injected only into this nonpublic prerequisite probe.
    private static AbilityRunner Runner()
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        book["cards"]!.AsArray().Add(JsonNode.Parse("""
            {
              "card": "10029",
              "name": "Beat Cop",
              "note": "Printed 10029. Move threat onto this support; discard cost retains the source incarnation and its captured threat for damage. Neither action is a thwart or attack.",
              "abilities": [
                {
                  "trigger": {
                    "event": "WhenActionTriggered",
                    "timing": "Action",
                    "subject": "this"
                  },
                  "cost": {
                    "exhaust": "this"
                  },
                  "effect": {
                    "chooseCard": {
                      "from": {
                        "query": "schemes"
                      },
                      "effect": {
                        "moveThreat": {
                          "from": "chosen",
                          "to": "this",
                          "amount": 1
                        }
                      }
                    }
                  }
                },
                {
                  "trigger": {
                    "event": "WhenActionTriggered",
                    "timing": "Action",
                    "subject": "this"
                  },
                  "cost": {
                    "seq": [
                      {
                        "exhaust": "this"
                      },
                      {
                        "discard": "this"
                      }
                    ]
                  },
                  "effect": {
                    "chooseCard": {
                      "from": {
                        "query": "minions"
                      },
                      "effect": {
                        "dealDamage": {
                          "cards": "chosen",
                          "amount": {
                            "tokensOn": "this"
                          }
                        }
                      }
                    }
                  }
                }
              ]
            }
            """));
        return new(AbilityCatalog.Parse(book.ToJsonString()));
    }

    private static void Answer(World world, Marvel.Cards.Run.AbilityRunner runner,
        List<GameEvent> events, int target)
    {
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        var beforeRejected = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray(), events.ToArray());
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner,
            prompt, Decision.Take(int.MaxValue), events));
        Assert.Equal(beforeRejected.Item1, world.Digest().Canonical());
        Assert.Equal(beforeRejected.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(beforeRejected.Item3, world.Agenda.Outstanding);
        Assert.Equal(beforeRejected.Item4, events);
        var offer = prompt.Affordances.Single(a => a.Targets?.Legal.Contains(target) == true || a.Id == target);
        Sequence.Answer(world, world.Facts, runner, prompt,
            offer.Targets is null ? Decision.Take(offer.Id) : new(offer.Id, [target]), events);
        for (int i = 0; i < 8; i++)
        {
            var next = Sequence.Work(world, world.Facts, runner, events);
            if (next is null) break;
            Sequence.Answer(world, world.Facts, runner, next, Decision.Take(next.Affordances[0].Id), events);
        }
        Assert.Empty(world.Agenda.Outstanding);
    }
}
