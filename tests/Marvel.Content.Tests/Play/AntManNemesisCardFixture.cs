using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

internal static class AntManNemesisCardFixture
{
    internal static readonly CardCatalog Facts = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    internal const string Yellowjacket = "12027";
    internal const string SizeIncrease = "12028";

    internal static AbilityRunner Runner(bool nested = false, bool nestedScheme = false)
    {
        if (!nested) return AuthoredCards.Runner();
        // This synthetic Rhino response tests nested activation ordering with actual nemesis rows.
        var authored = JsonNode.Parse(AuthoredCards.Text)!;
        var rhino = authored["cards"]!.AsArray().Single(row => (string?)row!["card"] == "01094")!;
        string ability = """{"trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","actor":"this","player":"trigger.player"},"effect":{"enemyAttacks":{"enemies":{"query":"minionsEngagedWithYou"},"against":"you"}}}""";
        if (nestedScheme) ability = ability.Replace("enemyAttacks", "enemySchemes", StringComparison.Ordinal);
        rhino["abilities"]!.AsArray().Add(JsonNode.Parse(ability));
        return new AbilityRunner(AbilityCatalog.Parse(authored.ToJsonString()));
    }

    internal static List<GameEvent> Finish(World world, AbilityRunner runner)
    {
        var events = new List<GameEvent>();
        for (int index = 0; index < 20; index++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) return events;
            var decision = prompt.Asking == Question.Order && prompt.Affordances[0].Targets is { } targets
                ? Decision.Take(prompt.Affordances[0].Id, targets.Legal, [])
                : prompt.Cancellable && prompt.Asking != Question.Element
                    ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id);
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
        }
        Assert.Fail("Activation did not finish within twenty prompts.");
        return events;
    }
}
