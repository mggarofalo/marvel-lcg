using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical Lightning Blast in arranged worlds; public scenario admission stays closed.</summary>
public sealed class WreckingLightningBlastTests
{
    [Theory]
    [InlineData("undefended", 3)]
    [InlineData("tough", 3)]
    [InlineData("hero", 0)]
    [InlineData("ally", 0)]
    [InlineData("allyBeforeDamage", 3)]
    [InlineData("stunned", 0)]
    [InlineData("webbed", 0)]
    [Rule("rr:attack-enemy-activation.3.2")]
    [Rule("rr:attack-enemy-activation.4")]
    public void ThreatUsesCompletedDefendednessRatherThanDamageOrLiveAttack(string mode, int threat)
    {
        // "If the defending ally leaves play prior to damage ... no character defending".
        // "If no character was declared the defender ... considered undefended."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07030", 1);
        world.EncounterTopology!.SelectActive(groups[0].Villain, "setup", []);
        var book = Book();
        if (mode == "allyBeforeDamage")
        {
            var boost = Row(book, "07037");
            boost["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"dealDamage":{"cards":"defendingCharacter","amount":3}}}]
            """);
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        world.Abilities = runner;
        var ally = PrepareDefense(world, groups[1].Villain, mode);
        runner.ExecuteReveal(world, source, 0);
        Finish(world, runner, ally?.ObjectId ?? (mode == "hero" ? world.Seats[0].IdentityCard.ObjectId : -1));
        Assert.Equal(threat, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(groups.Where(group => group != groups[1]), group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        if (ally is not null) Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        // Thunderball's separate after-attack ability deals 1 damage after Tough prevents the attack.
        if (mode == "tough") Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Same(groups[0], world.EncounterTopology.Active);
    }

    private static Card? PrepareDefense(World world, Card villain, string mode)
    {
        Card? ally = mode is "ally" or "allyBeforeDamage"
            ? world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)) : null;
        if (mode == "ally") ally!.TakeDamage(2);
        if (mode == "tough") Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        if (mode == "stunned") Statuses.Give(world, villain, Statuses.Stunned);
        if (mode == "webbed") world.CreateCard("01009", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), cardOwner: 0, host: villain.ObjectId));
        return ally;
    }

    [Fact]
    public void AlterEgoTiesAreChosenByFirstPlayerIncludingNonSignatureSchemes()
    {
        // 07030: "Place 3 threat on the side scheme with the most threat."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07030", 1, "01001b", players: 2);
        world.FirstPlayer = 1;
        var extra = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        extra.PlaceTokens("k_threat", 4);
        groups[2].Scheme.PlaceTokens("k_threat", 4);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(extra.ObjectId), []);
        Finish(world, runner);
        Assert.Equal(7, extra.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(4, groups[2].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(groups, group => Assert.Equal(2, group.Deck.Cards.Count));
    }

    [Fact]
    public void ANewCanceledActivationCannotReuseThePreviousUndefendedReceipt()
    {
        // Synthetic second activation in the canonical hero sequence tests receipt lifetime.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07030", 1);
        Statuses.Give(world, groups[0].Villain, Statuses.Stunned);
        var book = Book();
        Row(book, "07030")["abilities"]![0]!["effect"]!["seq"]!.AsArray().Insert(1, JsonNode.Parse("""
        {"enemyAttacks":{"enemies":{"titled":"Wrecker"},"against":"you"}}
        """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        Finish(world, runner);
        Assert.Equal(0, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.True(world.Seats[0].IdentityCard.Damage > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThreatWaitsForOptionalAttackEndResponses(bool accept)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07030", 1);
        var book = Book();
        // Synthetic response on Thunderball exercises the canonical treachery's suspension.
        Row(book, "07017")["abilities"]!.AsArray().Add(JsonNode.Parse("""
        {"trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"this","player":"trigger.player"},
         "effect":{"giveStatus":{"card":"you","status":"tough"}}}
        """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        bool offered = false;
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(step < 25, prompt.Label);
            var response = prompt.Affordances.SingleOrDefault(offer => offer.AnchorId == groups[1].Villain.ObjectId);
            if (response is not null)
            {
                offered = true;
                Assert.Equal(0, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
                Assert.Null(world.Attack);
            }
            var answer = response is not null && accept ? Decision.Take(response.Id) : Decision.Decline;
            if (answer == Decision.Decline) Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
        Assert.True(offered);
        Assert.Equal(3, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void AnotherPlayersDefenseStillPreventsThreat()
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07030", 1, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 1);
        Finish(world, runner, world.Seats[0].IdentityCard.ObjectId);
        Assert.Equal(0, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
    }

    private static JsonNode Book() => JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
    private static JsonNode Row(JsonNode book, string face) => book["cards"]!.AsArray().Single(card => card!["card"]!.GetValue<string>() == face)!;

    private static void Finish(World world, AbilityRunner runner, int defender = -1)
    {
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return;
            Assert.True(step < 25, prompt.Label);
            var answer = prompt.Asking == Question.Defender && defender >= 0 ? Decision.Take(defender) : Decision.Decline;
            if (answer == Decision.Decline) Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
    }
}
