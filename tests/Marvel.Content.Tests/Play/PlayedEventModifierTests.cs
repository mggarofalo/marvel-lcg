using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PlayedEventModifierTests
{
    // Printed upgrade interpretations; event bodies isolate each-instance and
    // lifetime behavior without admitting an incomplete pack into public play.
    private static AbilityRunner Runner(bool thwart = false, string? trait = null) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"UPGRADE","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
          "when":{"hasTrait":{"card":"trigger.subject","trait":"TRAIT"}},
          "cost":{"exhaust":"this"},
          "effect":{"modifyPlayedEvent":{"quantity":"QUANTITY","amount":2}}
        }]},{"card":"EVENT","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"seq":[BODY,BODY]}
        }]}]}
        """.Replace("UPGRADE", thwart ? "05011" : "05010", StringComparison.Ordinal)
        .Replace("EVENT", thwart ? "05004" : "05003", StringComparison.Ordinal)
        .Replace("TRAIT", trait ?? (thwart ? "THWART" : "ATTACK"), StringComparison.Ordinal)
        .Replace("QUANTITY", thwart ? "threatRemoval" : "damage", StringComparison.Ordinal)
        .Replace("BODY", thwart
            ? """{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":3}}"""
            : """{"dealDamage":{"cards":{"query":"villain"},"amount":3}}""", StringComparison.Ordinal)));

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [Rule("rr:event.5")]
    public void EachInstanceReceivesTheBonusAndAReplayedPhysicalEventDoesNot(bool thwart, bool accept)
    {
        // rr:event.5: "each of those instances is modified."
        // FAQ 05010: "increases all instances of damage ... by 2".
        // FAQ 05011: "increases each instance of threat removed by a thwart event".
        var (game, world, played, payment, upgrade) = Begin(thwart);
        var target = world.TheCardIn(thwart ? DeckType.MainSchemesArea : DeckType.VillainArea)!;
        long before = ObservedAmount(target, thwart);
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        Assert.Equal(before, ObservedAmount(target, thwart));
        var interrupt = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(upgrade!.ObjectId, interrupt.AnchorId);
        Assert.Contains("Increase each", interrupt.Description);
        Assert.Contains("until this play finishes", interrupt.Description);
        game.Resolve(accept ? Decision.Take(interrupt.Id) : Decision.Decline);
        long amount = accept ? 10 : 6;
        Assert.Equal(before + SignedAmount(amount, thwart), ObservedAmount(target, thwart));
        Assert.Equal(!accept, upgrade.Ready);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind is "eventDamage" or "eventThreatRemoval");
        if (!accept) upgrade.Exhaust();
        target.TakeDamage(-target.Damage);
        World.MoveToTop(played!, world.Seats[0].Hand);
        var secondPayment = world.CreateCard("01088", world.Seats[0].Hand);
        var runner = Runner(thwart);
        long replayBefore = ObservedAmount(target, thwart);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == played!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [secondPayment.ObjectId], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(replayBefore + SignedAmount(6, thwart), ObservedAmount(target, thwart));
        Assert.True(game.IsRootPrompt);
    }

    private static long ObservedAmount(Card target, bool thwart) =>
        thwart ? target.Tokens["k_threat"] : target.Damage;

    private static long SignedAmount(long amount, bool thwart) => thwart ? -amount : amount;

    private static (Game Game, World World, Card Played, Card Payment, Card Upgrade) Begin(bool thwart)
    {
        Card? played = null;
        Card? payment = null;
        Card? upgrade = null;
        var (game, world) = Playing(board =>
        {
            upgrade = board.CreateCard(thwart ? "05011" : "05010", board.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            played = board.CreateCard(thwart ? "05004" : "05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 20);
        }, hero: true, abilities: Runner(thwart));
        return (game, world, played!, payment!, upgrade!);
    }

    [Theory]
    [InlineData("otherPlayer")]
    [InlineData("wrongTrait")]
    [InlineData("exhausted")]
    public void IneligibleUpgradeCannotModifyThePaidEvent(string control)
    {
        Card? played = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            int owner = control == "otherPlayer" ? 1 : 0;
            var upgrade = board.CreateCard("05010", board.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(owner), cardOwner: owner));
            if (control == "exhausted") upgrade.Exhaust();
            played = board.CreateCard("05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, heroes: ["spider_man", "captain_marvel"],
            abilities: Runner(trait: control == "wrongTrait" ? "THWART" : "ATTACK"));
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        Assert.Equal(6, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Effects.Registered);
    }

    [Fact]
    [Rule("rr:event.5")]
    public void DistinctRecipientsEachReceiveTheFullBonus()
    {
        // "each of those instances is modified"; FAQ 05010 uses Melee's 3+3
        // against different enemies as its explicit example of 5+5.
        var runner = Runner();
        Card? played = null;
        Card? payment = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            board.CreateCard("05010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            played = board.CreateCard("05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        // A synthetic second recipient isolates modifier routing. Printed Melee
        // admission, target exclusion and attack grouping remain separate work.
        var authored = """
        {"cards":[{"card":"05010","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
          "when":{"hasTrait":{"card":"trigger.subject","trait":"ATTACK"}},
          "cost":{"exhaust":"this"},"effect":{"modifyPlayedEvent":{"quantity":"damage","amount":2}}
        }]},{"card":"05003","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"seq":[{"dealDamage":{"cards":{"query":"villain"},"amount":3}},
            {"dealDamage":{"cards":{"titled":"Whirlwind"},"amount":3}}]}
        }]}]}
        """;
        runner = new(AbilityCatalog.Parse(authored));
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == played!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment!.ObjectId], []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(Assert.Single(prompt.Affordances).Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(5, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(5, minion!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }
}
