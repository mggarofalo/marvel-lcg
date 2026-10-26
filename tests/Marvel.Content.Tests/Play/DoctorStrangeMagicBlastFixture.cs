using Marvel.Content.Tests.Cards;

namespace Marvel.Content.Tests.Play;

internal static class DoctorStrangeMagicBlastFixture
{
    internal static Marvel.Cards.Run.AbilityRunner Runner(bool sequentialControl = false)
    {
        // Test-local actual printed row; the production book remains unchanged.
        var root = System.Text.Json.Nodes.JsonNode.Parse(AuthoredCards.Text)!;
        root["cards"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse("""
{
  "card": "09004",
  "name": "Magic Blast",
  "note": "Printed Hero Action (attack). The chosen exact enemy takes 5 damage and the player discards their top card. The discard receipt tests presence, not icon count (ruling 59bea1238c870653); wild applies every branch. The energy 2 is a separate damage instance (e4ea1bf855e41aac). Deck reset precedes event discard (d1f9f33e3ae4ba2f).",
  "abilities": [
    {
      "trigger": {
        "event": "WhenActionTriggered",
        "timing": "Action",
        "subject": "this",
        "form": "hero"
      },
      "effect": {
        "chooseCard": {
          "from": {
            "query": "attackableEnemies"
          },
          "effect": {
            "attack": {
              "target": "chosen",
              "effect": {
                "seq": [
                  {
                    "and": [
                      {
                        "dealAttackDamage": {
                          "cards": "chosen",
                          "amount": 5
                        }
                      },
                      {
                        "discardTop": {
                          "from": "yourDeck",
                          "count": 1
                        }
                      }
                    ]
                  },
                  {
                    "if": {
                      "test": {
                        "or": [
                          {
                            "discardedWithResource": "R"
                          },
                          {
                            "discardedWithResource": "G"
                          }
                        ]
                      },
                      "then": {
                        "giveStatus": {
                          "card": "chosen",
                          "status": "stunned"
                        }
                      }
                    }
                  },
                  {
                    "if": {
                      "test": {
                        "or": [
                          {
                            "discardedWithResource": "Y"
                          },
                          {
                            "discardedWithResource": "G"
                          }
                        ]
                      },
                      "then": {
                        "dealAttackDamage": {
                          "cards": "chosen",
                          "amount": 2
                        }
                      }
                    }
                  },
                  {
                    "if": {
                      "test": {
                        "or": [
                          {
                            "discardedWithResource": "B"
                          },
                          {
                            "discardedWithResource": "G"
                          }
                        ]
                      },
                      "then": {
                        "giveStatus": {
                          "card": "chosen",
                          "status": "confused"
                        }
                      }
                    }
                  }
                ]
              }
            }
          }
        }
      }
    }
  ]
}
"""));
        if (sequentialControl)
        {
            var body = root["cards"]!.AsArray()[^1]!["abilities"]![0]!["effect"]!["chooseCard"]!["effect"]!["attack"]!["effect"]!["seq"]![0]!.AsObject();
            var children = body["and"]!.DeepClone();
            body.Remove("and");
            body["seq"] = children;
        }
        return new(Marvel.Cards.Dsl.AbilityCatalog.Parse(root.ToJsonString()));
    }
}
