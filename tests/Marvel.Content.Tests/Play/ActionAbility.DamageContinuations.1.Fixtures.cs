using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityDamageContinuationsFixtures
{
    internal static Marvel.Cards.Run.AbilityRunner DamageContinuationRunner(string damage) => new(AbilityCatalog.Parse($$"""
            { "cards": [
              { "card": "01006", "abilities": [{
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action", "subject": "game"
                },
                "effect": { "seq": [
                  {{damage}},
                  { "draw": { "player": "you", "count": 1 } }
                ] }
              }] },
              { "card": "01092", "abilities": [{
                "trigger": {
                  "event": "WhenCardWouldBeDefeated", "timing": "Interrupt", "subject": "game"
                },
                "effect": { "draw": { "player": "you", "count": 1 } }
              }] }
            ] }
            """));
}
