namespace Marvel.Content.Tests.Play;

internal static class ManOutOfTimeDraft
{
    internal const string Json = """
{
  "cards": [
    {
      "card": "03026",
      "name": "Man Out of Time",
      "abilities": [
        {
          "trigger": {
            "event": "WhenCardRevealed",
            "timing": "WhenRevealed",
            "subject": "this"
          },
          "effect": {
            "seq": [
              {
                "choose": {
                  "descriptions": [
                    "Change to alter-ego form",
                    "Remain in your current form"
                  ],
                  "options": [
                    {
                      "changeForm": {
                        "player": "you",
                        "to": "alter-ego"
                      }
                    },
                    {
                      "seq": []
                    }
                  ]
                }
              },
              {
                "choose": {
                  "descriptions": [
                    "Exhaust Steve Rogers and remove Man Out of Time from the game",
                    "Discard half your hand (rounded down), then discard Man Out of Time"
                  ],
                  "options": [
                    {
                      "then": {
                        "effect": {
                          "exhaust": "yourAlterEgo"
                        },
                        "then": {
                          "removeFromGame": "this"
                        }
                      }
                    },
                    {
                      "seq": [
                        {
                          "discardHandCards": {
                            "floorDiv": {
                              "value": {
                                "count": {"cardsIn":{"area":"yourHand"}}
                              },
                              "divisor": 2
                            }
                          }
                        },
                        {
                          "discard": "this"
                        }
                      ]
                    }
                  ]
                }
              }
            ]
          }
        }
      ]
    }
  ]
}
""";
}
