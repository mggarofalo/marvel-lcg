using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityTypedAreaDependenciesFixtures
{
    internal static string DependencySuffix(string operand, string selection, string condition, string number) => operand switch
    {
        "selector" => $$$$$$"""{"removeFromGame":{{{{{{selection}}}}}}} """,
        "condition" => $$$$$$"""{"if":{"test":{{{{{{condition}}}}}},"then":{"heal":{"card":"you","amount":1}}}}""",
        _ => $$$$$$"""{"heal":{"card":"you","amount":{{{{{{number}}}}}}}}""",
    };
    internal static void MutateDependency(string operand, Dictionary<string, AbilityValue> fields)
    {
        if (operand == "selector")
            fields["cardsIn"] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["area"] = new AbilityValue.Word("encounterDeck"), ["title"] = new AbilityValue.Word("Hydra Mercenary"), });
        else if (operand == "condition")
            fields["test"] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["inForm"] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { ["player"] = new AbilityValue.Word("you"), ["form"] = new AbilityValue.Word("hero"), }), });
        else
            fields["amount"] = new AbilityValue.Number(1);
    }

    internal static void AssertDependencyOutcome(string operand, Card discarded, World world)
    {
        Assert.Equal(operand == "selector" ? DeckType.RemovedArea : DeckType.EncounterDiscardPile, discarded.Area.Type);
        Assert.Equal(operand == "selector" ? 2 : 1, world.Seats[0].IdentityCard.Damage);
    }

    internal static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableAreaSuffixRunner(string suffix, bool allUnsafe)
    {
        string alternative = allUnsafe ? """{"discard":{"titled":"Hydra Mercenary"}}""" : """{"draw":{"player":"you","count":1}}""";
        var parsed = AbilityCatalog.Parse($$$$$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"seq":[
                {"choose":{"options":[{"discard":{"titled":"Hydra Mercenary"}},{{{{{{alternative}}}}}}]}},
                {{{{{{suffix}}}}}}
              ]}
            }]}]}
            """);
        var sequence = (AbilityValue.List)parsed.Abilities[0].Effect.Argument;
        var leaf = AbilityNode.Of(sequence.Values[1]);
        var fields = ((AbilityValue.Map)leaf.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var effect = new AbilityNode("seq", new AbilityValue.List([sequence.Values[0], new AbilityValue.Map(new Dictionary<string, AbilityValue> { [leaf.Kind] = new AbilityValue.Map(fields), }), ]));
        return (new AbilityRunner(new AbilityBook([parsed.Abilities[0] with { Effect = effect }], parsed.Authored)), fields);
    }
}
