using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic checkpoint perturbations around the canonical Convict boost.</summary>
public sealed class WreckingEscapedConvictReferenceTests
{
    [Theory]
    [InlineData("counter")]
    [InlineData("promotion")]
    [InlineData("departure")]
    [Rule("rr:villain-defeat.3.1")]
    public void DelayedAttackKeepsTheSelectedCharacter(string change)
    {
        // rr:villain-defeat.3.1: "considered the same character as the defeated stage".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07009", 0, extreme: true);
        var document = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var original = document["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "07017")!;
        // Synthetic response belongs to the original attacking villain, before its activation completes.
        original["abilities"]!.AsArray().Add(JsonNode.Parse("""
        {"trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"this","player":"trigger.player"},
         "effect":{"giveStatus":{"card":"you","status":"tough"}}}
        """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(document.ToJsonString()));
        world.Abilities = runner;
        for (int index = 0; index < 4; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == 2 ? 0 : 5);
        World.MoveToTop(source, groups[1].Deck);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: groups[1].Villain.ObjectId, Seat: 0));
        List<GameEvent> events = [];
        Prompt checkpoint;
        for (int count = 0; ; count++)
        {
            checkpoint = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
            Assert.True(count < 15);
            if (world.Attack is null
                && checkpoint.Affordances.Any(offer => offer.AnchorId == groups[1].Villain.ObjectId)) break;
            Assert.True(checkpoint.Cancellable, checkpoint.Description);
            Sequence.Answer(world, world.Facts, runner, checkpoint, Decision.Decline, events);
        }
        Assert.Single(events.OfType<AttackBoundary>(), item => item.Started);
        Assert.Null(world.Attack);
        Assert.Same(groups[2], world.EncounterTopology!.Active);
        // Perturb only the selected character after capture; another active villain cannot inherit it.
        world.EncounterTopology.SelectActive(groups[3].Villain, "synthetic counter change", []);
        ChangeSelectedCharacter(world, groups[2], change);
        Sequence.Answer(world, world.Facts, runner, checkpoint, Decision.Decline, events);
        for (int count = 0; ; count++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Assert.True(count < 30);
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        }
        int[] expected = change == "departure" ? [groups[1].Villain.ObjectId]
            : [groups[1].Villain.ObjectId, change == "promotion" ? groups[2].Stages[1].ObjectId : groups[2].Villain.ObjectId];
        Assert.Equal(expected, events.OfType<AttackBoundary>().Where(item => item.Started).Select(item => item.Source));
        Assert.Same(groups[3], world.EncounterTopology.Active);
        Assert.Equal(2, groups[3].Deck.Cards.Count);
    }
    private static void ChangeSelectedCharacter(World world, EncounterDeckGroup selected, string change)
    {
        if (change == "counter") return;
        World.MoveToTop(selected.Villain, world.AreaOf(DeckType.RemovedArea));
        if (change == "promotion")
            World.MoveToTop(selected.Stages[1], world.AreaOf(DeckType.VillainArea));
    }
}
