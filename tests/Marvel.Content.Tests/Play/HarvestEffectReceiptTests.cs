using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual full-book Harvest encounters and isolated receipt variants.</summary>
public sealed class HarvestEffectReceiptTests : CoreCardTestBase
{
    private const string Persona = """
        {"withTrait":{"cards":{"query":"supports"},"trait":"PERSONA"}}
        """;
    private const string Body = """
        {"seq":[{"exhaust":{"withTrait":{"cards":{"query":"supports"},"trait":"PERSONA"}}},
          {"heal":{"card":{"query":"villain"},"amount":{"result":"exhausted"}}},
          {"if":{"test":{"not":{"atLeast":{"value":{"result":"exhausted"},"count":1}}},
            "then":{"gainSurge":1}}}]}
        """;

    [Theory]
    [InlineData(0, 5, 3)]
    [InlineData(1, 5, 3)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 0, 0)]
    [Rule("rr:exhausted.1")]
    [Rule("rr:heal.1")]
    public void PrintedHarvestExhaustsOnlyReadyPersonasGloballyAndHealsWithinMaximum(
        int resolver, int damage, int expected)
    {
        // "An exhausted card cannot be exhausted again until it is ready."
        // A heal "can only bring a character to its maximum hit points".
        // 05029: "For each support exhaust this way, the villain heals 1 damage."
        var f = Create(Body);
        var a = Support(f.World, "05006", 0);
        var b = Support(f.World, "05008", 1);
        var tired = Support(f.World, "05007", 1); tired.Exhaust();
        var ordinary = Support(f.World, "01091", 0);
        var ally = f.World.CreateCard("01002", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        var hand = f.World.CreateCard("05008", f.World.Seats[0].Hand);
        f.Villain.TakeDamage(damage);
        var harvest = f.Reveal("05029", resolver);
        var events = f.Finish();
        Assert.False(a.Ready); Assert.False(b.Ready); Assert.False(tired.Ready);
        Assert.True(ordinary.Ready); Assert.True(ally.Ready); Assert.True(hand.Ready);
        Assert.Equal(expected, f.Villain.Damage);
        Assert.Equal(2, events.OfType<FieldSet>().Count(e => e.Verb == "Exhaust"));
        Assert.Equal(DeckType.EncounterDiscardPile, harvest.Area.Type);
        Assert.Equal(4, f.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:surge.1")]
    public void ZeroActualExhaustionsSurgesAndCompletesBothEncounterCleanups(bool exhaustedPersona)
    {
        // "When Revealed: Deal yourself 1 facedown encounter card."
        var f = Create(Body);
        if (exhaustedPersona) Support(f.World, "05006", 1).Exhaust();
        f.Villain.TakeDamage(5);
        var harvest = f.World.CreateCard("05029", f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4));
        var events = f.Finish();
        Assert.Equal(5, f.Villain.Damage);
        Assert.DoesNotContain(events.OfType<FieldSet>(), e => e.Verb == "Exhaust");
        Assert.Equal(DeckType.EncounterDiscardPile, harvest.Area.Type);
        Assert.Single(f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Equal(3, f.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.RevealingArea).Cards);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Fact]
    public void CommittedReceiptSurvivesNestedChoicesAndSourceCardsReadying()
    {
        var f = Create($$$$$$"""
            {"seq":[{"exhaust":{{{{{{Persona}}}}}}},
              {"choose":{"options":[{"choose":{"options":[{"ready":{{{{{{Persona}}}}}}},{"exhaust":"you"}]}},{"exhaust":"you"}]}},
              {"placeCounters":{"card":"you","counter":"observed","count":{"result":"exhausted"}}},
              {"placeCounters":{"card":"you","counter":"cost","count":{"cardsExhaustedForCost":"game"}}}]}
            """);
        var a = Support(f.World, "05006", 0);
        var b = Support(f.World, "05008", 1);
        f.Reveal("05029", 1);
        f.Finish(prompt => Decision.Take(prompt.Affordances[0].Id));
        Assert.True(a.Ready); Assert.True(b.Ready);
        Assert.Equal(2, f.World.Seats[1].IdentityCard.Tokens["c_observed"]);
        Assert.False(f.World.Seats[1].IdentityCard.Tokens.ContainsKey("c_cost"));
    }

    [Fact]
    public void LaterNoOpExhaustionOverwritesEarlierPositiveReceipt()
    {
        var f = Create($$$$$$"""
            {"seq":[{"exhaust":{{{{{{Persona}}}}}}},{"exhaust":{{{{{{Persona}}}}}}},
              {"heal":{"card":{"query":"villain"},"amount":{"result":"exhausted"}}}]}
            """);
        Support(f.World, "05006", 0);
        f.Villain.TakeDamage(5);
        f.Reveal("05029"); f.Finish();
        Assert.Equal(5, f.Villain.Damage);
    }

    private static Card Support(World world, string face, int player) => world.CreateCard(face,
        world.AreaOf(DeckType.SupportsArea, PlayArea.Of(player), cardOwner: player));

    private static RedSkullFixture Create(string body)
    {
        var runner = body == Body ? AuthoredCards.Runner() : new AbilityRunner(AbilityCatalog.Parse($$$$$$"""
            {"cards":[{"card":"05029","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{{{{{{body}}}}}}}]},{"card":"01101","abilities":[]}]}
            """));
        var world = Hero("01001a", 2); world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 4; i++) world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        return new(world, runner);
    }
}

