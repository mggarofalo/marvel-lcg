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
internal static class ActionAbilityTypedDamageFixtures
{
    internal static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableEffectRunner(string operation, string arguments, bool repeated)
    {
        var parsed = AbilityCatalog.Parse($$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"{{operation}}":{{arguments}}}
            }]}]}
            """);
        var fields = ((AbilityValue.Map)parsed.Abilities[0].Effect.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var effect = new AbilityNode(operation, new AbilityValue.Map(fields));
        if (repeated)
        {
            effect = new AbilityNode("forEach", new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { ["count"] = new AbilityValue.Number(2), ["effect"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [operation] = effect.Argument, }), }));
        }

        return (new AbilityRunner(new AbilityBook([parsed.Abilities[0] with { Effect = effect }], parsed.Authored)), fields);
    }
}
