using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityTypedNumericPreflightFixtures
{
    internal static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableNumericRunner(string operation, string arguments, bool choice, string? cards = null)
    {
        var parsed = AbilityCatalog.Parse($$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"{{operation}}":{{arguments}}}
            }]}]}
            """);
        var fields = ((AbilityValue.Map)parsed.Abilities[0].Effect.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var effect = new AbilityNode(operation, new AbilityValue.Map(fields));
        if (cards is not null)
        {
            effect = new AbilityNode("chooseCard", new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["from"] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["query"] = new AbilityValue.Word(cards), }), ["effect"] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { [operation] = effect.Argument }), }));
        }
        else if (choice)
        {
            effect = new AbilityNode("choose", new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["options"] = new AbilityValue.List([new AbilityValue.Map(new Dictionary<string, AbilityValue> { [operation] = effect.Argument }), new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["draw"] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["player"] = new AbilityValue.Word("you"), ["count"] = new AbilityValue.Number(1), }), }), ]), }));
        }

        return (new AbilityRunner(new AbilityBook([parsed.Abilities[0] with { Effect = effect }], parsed.Authored)), fields);
    }
}
