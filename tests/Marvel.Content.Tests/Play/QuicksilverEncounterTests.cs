using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for the obligation and nemesis set on synthetic boards.
public sealed class QuicksilverEncounterTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:obligation.1")]
    public void NeedForSpeedMayFlipThenExhaustPietroToLeaveTheGame()
    {
        // rr:obligation.1: "you" is the player whose play area holds the obligation.
        // 14024: "You may flip to alter-ego form. Choose: • Exhaust Pietro Maximoff →
        // remove Need for Speed from the game."
        var world = QuicksilverBoards.Board();
        var identity = world.Seats[0].IdentityCard;
        var need = Reveal(world, "14024");
        var runner = QuicksilverBoards.Runner(world);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[0].Id), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal("14001b", identity.FaceId);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(choice.Affordances[0].Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(identity.Ready);
        Assert.Equal(DeckType.RemovedArea, need.Area.Type);
    }

    [Fact]
    [Rule("rr:obligation.1")]
    [Rule("rr:cannot")]
    public void NeedForSpeedOtherwiseKeepsTheIdentityExhaustedUntilItsNextTurnEnds()
    {
        // rr:cannot: "cannot" is absolute. "Exhaust your identity. You cannot ready
        // your identity until your next turn ends. Discard this obligation." A hero
        // cannot exhaust Pietro Maximoff, so only the second option remains.
        var world = QuicksilverBoards.Board();
        var identity = world.Seats[0].IdentityCard;
        var need = Reveal(world, "14024");
        var runner = QuicksilverBoards.Runner(world);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[^1].Id), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var remaining = Assert.Single(choice.Affordances);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(remaining.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);

        Assert.Equal("14001a", identity.FaceId);
        Assert.False(identity.Ready);
        Assert.False(ReadinessPermissions.CanReady(world, identity));
        Assert.Equal(DeckType.EncounterDiscardPile, need.Area.Type);
        var running = QuicksilverBoards.Hand(world, "14003");
        QuicksilverBoards.Hand(world, "01088");
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == running.ObjectId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:choose-option.1")]
    [Rule("rr:indirect-damage")]
    public void AvalancheOffersIndirectDamageOrExhaustion(bool exhaust)
    {
        // rr:choose-option.1: an encounter option needs valid targets. rr:indirect-damage:
        // the player assigns indirect damage among the characters they control.
        var world = QuicksilverBoards.Board();
        var identity = world.Seats[0].IdentityCard;
        Reveal(world, "14026");
        var runner = QuicksilverBoards.Runner(world);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(2, choice.Affordances.Count);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(choice.Affordances[exhaust ? 1 : 0].Id), []);
        QuicksilverBoards.Drive(world, prompt => prompt.Affordances.FirstOrDefault(a => a.Targets is { AllowRepeated: true }) is { } assign
            ? Decision.Take(assign.Id) with { Targets = [identity.ObjectId, identity.ObjectId] } : null);

        Assert.Equal(!exhaust, identity.Ready);
        Assert.Equal(exhaust ? 0 : 2, identity.Damage);
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void AnExhaustedIdentityMustTakeAvalanchesIndirectDamage()
    {
        // rr:exhausted.1: "An exhausted card cannot be exhausted again until it is
        // ready", so the exhaustion option has no valid target (rr:choose-option.1).
        var world = QuicksilverBoards.Board();
        var identity = world.Seats[0].IdentityCard;
        identity.Exhaust();
        Reveal(world, "14026");
        QuicksilverBoards.Drive(world, prompt =>
        {
            Assert.DoesNotContain(prompt.Affordances, a => a.Description == "Exhaust your identity");
            return prompt.Affordances.FirstOrDefault(a => a.Targets is { AllowRepeated: true }) is { } assign
                ? Decision.Take(assign.Id) with { Targets = [identity.ObjectId, identity.ObjectId] }
                : prompt.Affordances.Count > 0 ? Decision.Take(prompt.Affordances[0].Id) : null;
        });
        Assert.Equal(2, identity.Damage);
    }

    [Fact]
    [Rule("rr:incite-x")]
    public void EarthquakeDiscardsTwoAndExhaustsTheIdentity()
    {
        // 14028: "When Revealed: Discard 2 cards from your hand and exhaust your identity."
        var world = QuicksilverBoards.Board();
        var identity = world.Seats[0].IdentityCard;
        var hand = new[] { QuicksilverBoards.Hand(world, "01088"), QuicksilverBoards.Hand(world, "01089"), QuicksilverBoards.Hand(world, "01090") };
        Reveal(world, "14028");
        QuicksilverBoards.Drive(world, prompt => prompt.Affordances.FirstOrDefault(a => a.Targets is not null) is { } discard
            ? Decision.Take(discard.Id) with { Targets = [hand[0].ObjectId, hand[1].ObjectId] } : null);

        Assert.Equal(DeckType.DiscardPile, hand[0].Area.Type);
        Assert.Equal(DeckType.DiscardPile, hand[1].Area.Type);
        Assert.Same(world.Seats[0].Hand, hand[2].Area);
        Assert.False(identity.Ready);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:boost-boost-icon")]
    public void EarthquakesBoostSpendsTwoPhysicalOrExhaustsTheIdentity(bool pay)
    {
        // rr:boost-boost-icon: "resolve the \"Boost\" ability when the card is turned face up."
        // Rhino attacks Quicksilver, who declines to defend; the boost asks him.
        var world = QuicksilverBoards.Board();
        var identity = world.Seats[0].IdentityCard;
        var strength = QuicksilverBoards.Hand(world, "01090");
        world.CreateCard("14028", world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Attack(world, QuicksilverBoards.Villain(world), 0);

        QuicksilverBoards.Drive(world, prompt =>
            pay && prompt.Affordances.FirstOrDefault(a => a.Label == "spend") is { } spend
                ? Decision.Take(spend.Id, [], [strength.ObjectId], new Dictionary<string, long>(),
                    ResourcePayment.Allocate(Assert.Single(spend.CostOptions), [strength.ObjectId])!)
            : prompt.Affordances.FirstOrDefault(a => a.Label == "effect") is { } exhaust
                ? Decision.Take(exhaust.Id) : null);

        Assert.Equal(pay, identity.Ready);
        Assert.Equal(pay ? DeckType.DiscardPile : DeckType.HandsArea, strength.Area.Type);
    }

    [Fact]
    [Rule("rr:attach-to")]
    public void VibrationResistanceAttachesToAvalancheOrElseTheVillain()
    {
        // rr:attach-to: the card attaches to the first legal named host.
        var world = QuicksilverBoards.Board();
        var villain = QuicksilverBoards.Villain(world);
        var first = RevealDealt(world, "14027");
        QuicksilverBoards.Drive(world, _ => null);
        Assert.Equal(villain.ObjectId, first.Area.Host);

        var avalanche = QuicksilverBoards.Engaged(world, "14026");
        var second = RevealDealt(world, "14027");
        QuicksilverBoards.Drive(world, _ => null);
        Assert.Equal(avalanche.ObjectId, second.Area.Host);
    }


    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void VibrationResistanceReducesEachAttackOnceAndLeavesForAnExhaustedHero()
    {
        // rr:attack-player-ability-type.1: a basic attack "deals damage equal to the
        // character's ATK value to the enemy." 14027: "Reduce the damage attached enemy takes from each attack by 1.
        // Hero Action: Exhaust your hero -> discard this card." Quicksilver's ATK 1
        // basic attack deals Rhino nothing while it is attached; Super Speed readies him.
        var world = QuicksilverBoards.Board();
        var villain = QuicksilverBoards.Villain(world);
        var quicksilver = world.Seats[0].IdentityCard;
        var vibration = RevealDealt(world, "14027");
        QuicksilverBoards.Drive(world, _ => null);

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(quicksilver));
        Assert.Equal(0, villain.Damage);
        Assert.True(quicksilver.Ready);

        QuicksilverBoards.Play(world, vibration, []);
        QuicksilverBoards.Drive(world, _ => null);
        Assert.False(quicksilver.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, vibration.Area.Type);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void VibrationResistanceReducesOnlyTheFirstDamageOfOneAttack()
    {
        // 14027: "Reduce the damage attached enemy takes from each attack by 1." A
        // synthetic attack event deals Rhino 2 damage twice within one attack: the
        // reduction applies to that attack once, so Rhino takes 1 + 2.
        var world = QuicksilverBoards.Board();
        world.Abilities = new AbilityRunner(DoubleStrikeBook());
        var villain = QuicksilverBoards.Villain(world);
        RevealDealt(world, "14027");
        var strike = QuicksilverBoards.Hand(world, "01060");
        var energy = QuicksilverBoards.Hand(world, "01088");

        QuicksilverBoards.Play(world, strike, [energy]);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(villain));

        Assert.Equal(3, villain.Damage);
    }

    // The production Vibration Resistance row beside a synthetic two-hit attack on For Justice!.
    private static AbilityBook DoubleStrikeBook()
    {
        var production = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var vibration = production["cards"]!.AsArray().Single(row => (string?)row!["card"] == "14027")!.ToJsonString();
        return AbilityCatalog.Parse($$$$$$$"""
            {"cards":[{{{{{{{vibration}}}}}}},{"card":"01060","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "labels":["attack"],
              "effect":{"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{"attack":{"target":"chosen","effect":{"seq":[
                {"dealAttackDamage":{"cards":"chosen","amount":2}},
                {"dealAttackDamage":{"cards":"chosen","amount":2}}]}}}}}}]}]}
            """);
    }

    // Deals the card and runs the encounter reveal stage, which places attachments.
    private static Card RevealDealt(World world, string face)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));
        Deal.EncounterCard(world, card, 0, "Quicksilver test deal", []);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));
        QuicksilverBoards.Drive(world, _ => null);
        return card;
    }

    private static Card Reveal(World world, string face)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        QuicksilverBoards.Runner(world).ExecuteReveal(world, card, 0);
        return card;
    }
}
