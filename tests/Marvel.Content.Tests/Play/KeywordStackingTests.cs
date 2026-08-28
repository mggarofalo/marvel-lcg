using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Keyword instances gained while a card is being revealed.</summary>
public sealed class KeywordStackingTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void GainingSurgeDoesNotRepeatPrintedSurge()
    {
        // "If a card gains multiple instances of a keyword, any additional
        // instances have no effect unless that keyword is followed by a
        // number." Weapons Runner already prints Surge; gaining it during the
        // same reveal does not deal a second encounter card.
        var runner = Runner(
            "01121",
            """{ "gainSurge": 1 }""");
        var (world, card) = Board("01121", runner);

        ResolveReveal(world, card, runner);

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void MultipleGainedSurgeInstancesResolveOnce()
    {
        // Surge is not numbered. A value of two and a later second node are
        // three instances, not three additional cards; together they create
        // the keyword's one When Revealed ability.
        var runner = Runner(
            "01110",
            """{ "seq": [ { "gainSurge": 2 }, { "gainSurge": 1 } ] }""");
        var (world, card) = Board("01110", runner);

        ResolveReveal(world, card, runner);

        AssertOneAdditionalCard(world);
    }

    private static AbilityRunner Runner(string faceId, string effect) =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{faceId}}", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": {{effect}}
            } ] } ] }
            """));

    private static (World World, Card Card) Board(string faceId, AbilityRunner runner)
    {
        var world = new World(Cards, players: 1) { Abilities = runner };
        world.CreateSeat("p0");
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        world.CreateCard("01122", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01123", world.AreaOf(DeckType.EncounterDeck));
        return (world, card);
    }

    private static void ResolveReveal(World world, Card card, AbilityRunner runner)
    {
        Reveal.Keywords(world, Cards, runner, card, player: 0, []);
        runner.WhenRevealed(world, card, player: 0);
    }

    private static void AssertOneAdditionalCard(World world)
    {
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
    }
}
