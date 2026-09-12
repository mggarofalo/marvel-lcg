using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityGameOwnershipFixtures
{
    internal static AbilityProgram GameOwnershipProgram(string effect) => AbilityLowering.Book(AbilityCatalog.Parse($$$$$$"""
            {"cards":[{"card":"{{{{{{AuthoredCards.AuntMay}}}}}}","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{{{{{{effect}}}}}}
            }]}]}
            """));
}
