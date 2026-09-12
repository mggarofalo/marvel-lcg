using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityTypedRepeatedTraceFixtures
{
    internal static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableRepeatedTraceRunner(string sequence, int index)
    {
        var parsed = AbilityCatalog.Parse($$$$$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},
              "effect":{"eachPlayer":{"effect":{"if":{
                "test":{"inForm":{"player":"firstPlayer","form":"hero"}},
                "then":{"seq":{{{{{{sequence}}}}}}},
                "else":{"attack":{"target":{"query":"villain"},
                  "effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}}}
              }}}}
            }]}]}
            """);
        var conditional = AbilityNode.Of(parsed.Abilities[0].Effect.Require("effect"));
        var conditionFields = ((AbilityValue.Map)conditional.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value);
        var steps = ((AbilityValue.List)AbilityNode.Of(conditionFields["then"]).Argument).Values.ToList();
        var selected = AbilityNode.Of(steps[index]);
        var fields = ((AbilityValue.Map)selected.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value);
        steps[index] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { [selected.Kind] = new AbilityValue.Map(fields), });
        conditionFields["then"] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["seq"] = new AbilityValue.List(steps), });
        var effect = new AbilityNode("eachPlayer", new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["effect"] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["if"] = new AbilityValue.Map(conditionFields), }), }));
        return (new AbilityRunner(new AbilityBook([parsed.Abilities[0] with { Effect = effect }], parsed.Authored)), fields);
    }
}
