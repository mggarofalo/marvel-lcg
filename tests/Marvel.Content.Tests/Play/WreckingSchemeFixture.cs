using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual printed scheme interpretations under test before canonical book handoff.</summary>
internal static class WreckingSchemeFixture
{
    internal static readonly string[] Faces = ["07004", "07019", "07034", "07048"];
    private static readonly string[] Villains = ["Wrecker", "Thunderball", "Piledriver", "Bulldozer"];
    private static readonly string[] Effects =
    [
        """{"dealDamage":{"cards":{"query":"heroesAndAllies"},"amount":2}}""",
        """{"giveStatus":{"card":{"query":"heroesAndAllies"},"status":"stunned"}}""",
        """{"eachPlayer":{"effect":{"chooseCard":{"from":{"maxBy":{"of":{"query":"upgradesAndSupportsYouControl"},"by":"cost"}},"effect":{"discard":"chosen"}}}}}""",
        """{"eachPlayer":{"effect":{"discardTop":{"from":"yourDeck","count":10}}}}""",
    ];

    internal static AbilityRunner Runner()
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var cards = book["cards"]!.AsArray();
        foreach (var row in Rows()) cards.Add(JsonNode.Parse(row));
        return new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
    }

    internal static IEnumerable<string> Rows()
    {
        yield return """{"card":"07001a","note":"Authored encounter recipe owns paired setup and initial active counter.","abilities":[]}""";
        yield return """
        {"card":"07001b","abilities":[{"trigger":{"event":"WhenVillainPhaseStepOneCompleted","timing":"ForcedResponse","subject":"game"},
          "effect":{"seq":[{"placeThreat":{"scheme":{"query":"sideSchemes"},"amount":1}},
            {"chooseCard":{"chooser":"firstPlayer","from":{"query":"villainsWithMostSchemeThreat"},"effect":{"moveActiveCounter":"chosen"}}}]}}]}
        """;
        for (int index = 0; index < Faces.Length; index++)
            yield return """
            {"card":"FACE","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"if":{"test":{"exists":{"titled":"VILLAIN"}},"then":{"preventSideSchemeDeparture":"this"}}}},
              {"trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"this"},
                "when":{"atLeast":{"value":{"tokensOn":"this"},"count":10}},
                "effect":{"seq":[EFFECT,{"removeThreat":{"scheme":"this","amount":{"add":[{"tokensOn":"this"},-3]}}}]}}
            ]}
            """.Replace("FACE", Faces[index], StringComparison.Ordinal).Replace("VILLAIN", Villains[index], StringComparison.Ordinal).Replace("EFFECT", Effects[index], StringComparison.Ordinal);
    }

    internal static void Finish(World world, AbilityRunner runner, List<GameEvent>? events = null)
    {
        events ??= [];
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) return;
            Assert.True(step < 30);
            var decision = Decision.Decline;
            if (!prompt.Cancellable)
            {
                var offer = Assert.Single(prompt.Affordances, item => item.IsLegal);
                decision = new(offer.Id, offer.Targets?.Legal.Take(offer.Targets.Min).ToArray() ?? []);
            }
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
        }
    }
}
