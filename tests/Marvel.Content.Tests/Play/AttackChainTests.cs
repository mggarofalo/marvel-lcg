using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackChainTests
{
    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 4)]
    [InlineData(true, true, 4)]
    [Rule("rr:activation.8")]
    public void BoostDistinguishesStandaloneSchemeFromAttackAndItsQueuedScheme(
        bool attack, bool schemeChain, int expectedDamage)
    {
        // Synthetic Boost contracts isolate ruling:8369361114d2929b:
        // "a new attack will initiate ... once you're finished with Villain 2's scheme".
        // rr:activation.8: "resolves after the current activation has finished resolving".
        var (world, groups, schemeBoost) = WreckingEncounterCardFixture.Create(
            "07014", 1, boostFace: "07024");
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {
          "cards": [
            {
              "card": "07014",
              "abilities": [
                {
                  "trigger": {
                    "event": "WhenCardRevealed",
                    "timing": "Boost",
                    "subject": "this"
                  },
                  "effect": {
                    "enemySchemes": {
                      "enemies": {
                        "titled": "Piledriver"
                      },
                      "against": "you"
                    }
                  }
                }
              ]
            },
            {
              "card": "07024",
              "abilities": [
                {
                  "trigger": {
                    "event": "WhenCardRevealed",
                    "timing": "Boost",
                    "subject": "this"
                  },
                  "effect": {
                    "seq": [
                      {
                        "choose": {
                          "options": [
                            {
                              "ready": "you"
                            },
                            {
                              "exhaust": "you"
                            }
                          ]
                        }
                      },
                      {
                        "if": {
                          "test": {
                            "inAttackChain": "attack"
                          },
                          "then": {
                            "afterActivation": {
                              "effect": {
                                "enemyAttacks": {
                                  "enemies": {
                                    "titled": "Wrecker"
                                  },
                                  "against": "you",
                                  "dealBoostCards": "false"
                                }
                              }
                            }
                          }
                        }
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """));
        world.Abilities = runner;
        // Synthetic boosts have explicit Thunderball deck membership; it remains active.
        if (schemeChain) World.MoveToTop(schemeBoost, groups[1].Deck);
        world.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 1, Subject: groups[0].Villain.ObjectId, Seat: 0));
        Assert.True(Finish(world, runner) > 0);
        Assert.Equal(expectedDamage, world.Seats[0].IdentityCard.Damage);
        // This synthetic book supplies boosts only; schemes use the default main destination.
        Assert.Equal(!attack || schemeChain ? 2 : 0,
            world.AreaOf(DeckType.MainSchemesArea).Cards.Single().Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(groups[1], world.EncounterTopology!.Active);
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
        // The completed attack's history cannot turn a later hero-form scheme into an attack chain.
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2,
            Subject: groups[0].Villain.ObjectId, Seat: 0));
        Finish(world, runner);
        Assert.Equal(expectedDamage, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("\"scheme\"")]
    public void AttackChainArgumentIsStrict(string argument)
    {
        var json = """
        {"cards":[{"card":"07024","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
          "effect":{"if":{"test":{"inAttackChain":ARGUMENT},"then":{"draw":1}}}}]}]}
        """.Replace("ARGUMENT", argument, StringComparison.Ordinal);
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(json)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HistoricalOriginSurvivesDeeperSchemesAndDoesNotLeakIntoAddedRoots(bool immediate)
    {
        // Synthetic scheduler contracts: ids name history without requiring a live card.
        var agenda = new Agenda();
        agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: 123, Seat: 0));
        var origin = Assert.IsType<EnemyAttackOrigin>(agenda.Current!.Value.AttackOrigin);
        agenda.Then(new PhaseStep(new EnemySchemeOperation(), 1, 1, Subject: 456, Seat: 0));
        while (agenda.Current!.Value.Subject == 123) agenda.Advance();
        Assert.Equal(origin, agenda.Current!.Value.AttackOrigin);
        var deeper = new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: 789, Seat: 0);
        if (immediate) agenda.Now(deeper);
        else agenda.Then(deeper);
        agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 3, Subject: 456, Seat: 0));
        agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 4, Subject: 456, Seat: 0));
        while (agenda.Current!.Value.Number != 2) agenda.Advance();
        Assert.Equal(origin, agenda.Current!.Value.AttackOrigin);
        while (agenda.Current!.Value.Number != 3) agenda.Advance();
        Assert.Null(agenda.Current!.Value.AttackOrigin);
        while (agenda.Current!.Value.Number != 4) agenda.Advance();
        var next = Assert.IsType<EnemyAttackOrigin>(agenda.Current!.Value.AttackOrigin);
        Assert.NotEqual(origin.ActivationId, next.ActivationId);
        Assert.Equal(456, next.Enemy);
    }

    private static int Finish(World world, AbilityRunner runner)
    {
        int choices = 0;
        for (int count = 0; ; count++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return choices;
            Assert.True(count < 20);
            if (prompt.Asking == Question.Option)
            {
                choices++;
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[0].Id), []);
                continue;
            }
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
    }
}


