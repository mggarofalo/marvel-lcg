using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic characteristic proof; this does not admit the Mutagen Formula pack.</summary>
public sealed class MutagenEscalationTests : CoreCardTestBase
{
    [Rule("rr:villain-phase.step.1")]
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void MutagenCloudCountsLiveGoblinEnemiesIncludingTheVillain(int players)
    {
        // 02018b: "X is equal to the number of Goblin enemies (including Green
        // Goblin) in play." Its acceleration annotation is X, without a player
        // icon; StartingThreat 4* and TargetThreat 11* are separate printed facts.
        // rr:villain-phase.step.1: "Place the amount of threat indicated in the
        // main scheme's acceleration field onto that scheme."
        var (world, scheme) = Mutagen(players);
        var villain = world.CreateCard("02014", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01101", Engaged(world, 0)); // HYDRA, not GOBLIN.
        foreach (var zone in new[] { DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
                     DeckType.BoostingArea, DeckType.RemovedArea })
            world.CreateCard("02023", world.AreaOf(zone));

        AssertPlacement(world, scheme, 1);
        var soldier = world.CreateCard("02023", Engaged(world, 0));
        var knight = world.CreateCard("02022", Engaged(world, players - 1));
        AssertPlacement(world, scheme, 3);

        World.MoveToTop(soldier, world.AreaOf(DeckType.EncounterDiscardPile));
        AssertPlacement(world, scheme, 2);
        World.MoveToTop(villain, world.AreaOf(DeckType.VillainDeck));
        AssertPlacement(world, scheme, 1);
        World.MoveToTop(knight, world.AreaOf(DeckType.EncounterDiscardPile));
        AssertPlacement(world, scheme, 0);
    }

    [Rule("rr:villain-phase.step.1")]
    [Rule("rr:acceleration-icon.1")]
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AccelerationAddsToXOnceRatherThanMultiplyingByPlayers(int players)
    {
        // "If any acceleration icons or tokens are active, additional threat
        // equal to the number of such icons and tokens is also placed at this time."
        var (world, scheme) = Mutagen(players);
        world.CreateCard("02014", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02023", Engaged(world, 0));
        world.CreateCard("02022", Engaged(world, players - 1));
        AddAcceleration(world, scheme);

        AssertPlacement(world, scheme, 8, baseAmount: 3);
    }

    [Rule("rr:villain-phase.step.1")]
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void CorePrintedPerPlayerEscalationStillScales(int players)
    {
        // 01097b prints 1 per player in its acceleration field. Dynamic X and
        // the fixed printed per-player quantity share the same step-one path.
        var world = Hero("01001a", players);
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        AddAcceleration(world, scheme);

        AssertPlacement(world, scheme, players + 5, baseAmount: players);
    }

    [Fact]
    public void GeneratedMutagenFactsDistinguishXFromPerPlayerValues()
    {
        using var data = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var card = data.RootElement.GetProperty("cards").EnumerateArray()
            .Single(card => card.GetProperty("card_id").GetString() == "02018b");
        Assert.Equal("0", card.GetProperty("attributes").GetProperty("EscalationThreat").GetString());
        Assert.Equal("X", card.GetProperty("stat_annotations").GetProperty("EscalationThreat")
            .GetProperty("value").GetString());
        Assert.Equal("4*", card.GetProperty("attributes").GetProperty("StartingThreat").GetString());
        Assert.Equal("11*", card.GetProperty("attributes").GetProperty("TargetThreat").GetString());
        Assert.Contains("including Green Goblin", card.GetProperty("text_plain").GetString());
    }

    private static (World World, Card Scheme) Mutagen(int players)
    {
        // Engine choice: the existing additive constant field represents X on
        // the generated zero placeholder. Authorship is confined to this fixture.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"02018b","abilities":[{
              "trigger":{"timing":"Constant"},
              "effect":{"grant":{"card":"this","keyword":"escalation_threat",
                "amount":{"count":{"enemiesWithTrait":"GOBLIN"}}}}
            }]}]}
            """));
        var world = Hero("01001a", players);
        var scheme = world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        world.Abilities = runner;
        return (world, scheme);
    }

    private static Area Engaged(World world, int seat) =>
        world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat));

    private static void AddAcceleration(World world, Card scheme)
    {
        // Bomb Scare and The "Immortal" Klaw each print one acceleration icon.
        world.CreateCard("01109", world.AreaOf(DeckType.SideSchemesArea));
        world.CreateCard("01127", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens(EncounterDeck.AccelerationToken, 3);
    }

    private static void AssertPlacement(World world, Card scheme, long expected, long? baseAmount = null)
    {
        Assert.Equal(baseAmount ?? expected, Modified(world, scheme, "escalation_threat"));
        long before = scheme.Tokens.GetValueOrDefault("k_threat");
        world.Agenda.Add(new PhaseStep(new ThreatStageOperation(ThreatStageKind.PlaceThreat), 1, 1));
        Agendas.Finish(world, Cards, world.Abilities);
        Assert.Equal(expected, scheme.Tokens.GetValueOrDefault("k_threat") - before);
    }
}
