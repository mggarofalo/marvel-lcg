using Marvel.Cards.Dsl;
using Marvel.Cards.Run;

namespace Marvel.Content.Tests.Play;

// Actual printed 04021/04022 rows, staged until the complete pack is admitted.
internal static class AvengersSupportCards
{
    internal static AbilityRunner Runner() => new(AbilityCatalog.Parse(Json));
    internal const string Json = """
        {
          "cards": [
            {
              "card": "04021",
              "name": "Avengers Tower",
              "abilities": [
                {
                  "trigger": {
                    "timing": "Constant",
                    "subject": "this"
                  },
                  "effect": {
                    "if": {
                      "test": {
                        "atLeast": {
                          "value": {
                            "count": {
                              "withTrait": {
                                "cards": {
                                  "query": "alliesYouControl"
                                },
                                "trait": "AVENGER"
                              }
                            }
                          },
                          "count": {
                            "count": {
                              "query": "alliesYouControl"
                            }
                          }
                        }
                      },
                      "then": {
                        "grant": {
                          "card": "you",
                          "keyword": "ally_limit",
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
                    "exhaust": "this"
                  },
                  "effect": {
                    "reduceNextCardCost": {
                      "player": "any",
                      "kind": "ally",
                      "trait": "AVENGER",
                      "amount": 1
                    }
                  }
                }
              ]
            },
            {
              "card": "04022",
              "name": "Earth's Mightiest Heroes",
              "abilities": [
                {
                  "trigger": {
                    "event": "WhenActionTriggered",
                    "timing": "Action",
                  "form": "hero",
                    "subject": "this"
                  },
                  "cost": {
                    "exhaustChosen": {
                      "from": {
                        "query": "charactersYouControl"
                      },
                      "trait": "AVENGER",
                      "count": 1
                    }
                  },
                  "effect": {
                    "chooseCard": {
                      "from": {
                        "withTrait": {
                          "cards": {
                            "query": "charactersYouControlExceptCost"
                          },
                          "trait": "AVENGER"
                        }
                      },
                      "effect": {
                        "ready": "chosen"
                      }
                    }
                  }
                }
              ]
            }
          ]
        }
        """;
}
