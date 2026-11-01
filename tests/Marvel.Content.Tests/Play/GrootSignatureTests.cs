using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Groot's signature cards on synthetic boards.
public sealed class GrootSignatureTests : CoreCardTestBase
{
    private const string Energy = "01088";
    private const string Genius = "01089";
    private const string Strength = "01090";

    [Theory]
    [InlineData(0, 2)]
    [InlineData(9, 10)]
    [Rule("rr:max-maximum.6")]
    public void FruitionPlacesTwoGrowthCountersToAMaximumOfTen(int growth, long after)
    {
        // 16002: "Action: Place 2 growth counters on Groot (to a maximum of 10)."
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, growth);
        var fruition = GrootBoards.Hand(world, "16002");

        GrootBoards.Play(world, fruition, []);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(after, GrootBoards.Growth(groot));
        Assert.Equal(DeckType.DiscardPile, fruition.Area.Type);
    }

    [Fact]
    [Rule("rr:max-maximum.6")]
    public void FruitionIsNotOfferedAtTenCounters()
    {
        var world = GrootBoards.Board();
        GrootBoards.Grow(GrootBoards.Groot(world), 10);
        var fruition = GrootBoards.Hand(world, "16002");
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == fruition.ObjectId);
    }

    [Fact]
    [Rule("rr:thwart")]
    public void IAmGrootRemovesThreatEqualToGrowthCounters()
    {
        // 16003: "Hero Action (thwart): Remove threat from a scheme equal to the number
        // of growth counters on Groot."
        var world = GrootBoards.Board();
        GrootBoards.Grow(GrootBoards.Groot(world), 4);
        var main = GrootBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 6);
        var card = GrootBoards.Hand(world, "16003");
        var paying = new[] { GrootBoards.Hand(world, Genius), GrootBoards.Hand(world, Genius) };

        GrootBoards.Play(world, card, paying, [main.ObjectId]);
        GrootBoards.Drive(world, GrootBoards.Take(main));

        Assert.Equal(2, main.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData("16003")]
    [InlineData("16004")]
    [Rule("rr:initiating-abilities.step.2")]
    public void GrowthReadingEventsAreNotOfferedWithoutCounters(string face)
    {
        // With no growth counters "I am Groot" removes no threat and "I. AM. GROOT!"
        // deals no damage.
        var world = GrootBoards.Board();
        GrootBoards.MainScheme(world).PlaceTokens("k_threat", 6);
        var card = GrootBoards.Hand(world, face);
        for (int i = 0; i < 3; i++) GrootBoards.Hand(world, Energy);
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == card.ObjectId);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void IAmGrootShoutedDealsDamageEqualToGrowthCounters()
    {
        // 16004: "Hero Action (attack): Deal damage to an enemy equal to the number of
        // growth counters on Groot."
        var world = GrootBoards.Board();
        GrootBoards.Grow(GrootBoards.Groot(world), 5);
        var villain = GrootBoards.Villain(world);
        var card = GrootBoards.Hand(world, "16004");
        var paying = new[] { GrootBoards.Hand(world, Strength) };

        GrootBoards.Play(world, card, paying);
        GrootBoards.Drive(world, GrootBoards.Take(villain));

        Assert.Equal(5, villain.Damage);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    [Rule("rr:defeat")]
    public void RootStompGrowsGrootOnlyWhenItsAttackDefeatsTheEnemy(bool minion, long growth)
    {
        // 16005: "Deal 5 damage to an enemy. If this attack defeats that enemy, place 1
        // growth counter on Groot (to a maximum of 10)." A 2-hit-point Hydra Mercenary
        // is defeated; Rhino is not.
        var world = GrootBoards.Board();
        var target = minion ? GrootBoards.Engaged(world, "01121") : GrootBoards.Villain(world);
        var card = GrootBoards.Hand(world, "16005");
        var paying = new[] { GrootBoards.Hand(world, Strength) };

        GrootBoards.Play(world, card, paying);
        GrootBoards.Drive(world, GrootBoards.Take(target));

        Assert.Equal(growth, GrootBoards.Growth(GrootBoards.Groot(world)));
        Assert.Equal(minion, target.Area.Type == DeckType.EncounterDiscardPile);
    }

    [Fact]
    [Rule("rr:cost.9")]
    [Rule("rr:choose-game-element.3")]
    public void WeAreGrootRemovesTheChosenCountOfCountersAndGivesThatManyTough()
    {
        // 16006: "Hero Action: Remove up to 4 growth counters from Groot → choose that
        // many friendly characters. Give each of those characters a tough status card."
        // rr:cost.9 sets an "up to" cost's minimum at 1. With 3 counters only the first
        // three counts are offered; choosing 2 toughens Groot and Starhawk.
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 3);
        var starhawk = GrootBoards.InPlay(world, "16012", DeckType.AlliesArea);
        var card = GrootBoards.Hand(world, "16006");
        var paying = new[] { GrootBoards.Hand(world, Genius) };
        var offered = new List<int>();

        GrootBoards.Play(world, card, paying);
        GrootBoards.Drive(world, prompt =>
        {
            if (prompt.Affordances.Any(a => a.Description?.StartsWith("Remove 1 growth", StringComparison.Ordinal) == true))
            {
                offered.Add(prompt.Affordances.Count);
                return Decision.Take(prompt.Affordances[1].Id);
            }
            return prompt.Affordances.FirstOrDefault(a => a.Targets is { Max: 2 }) is { } choose
                ? Decision.Take(choose.Id) with { Targets = [groot.ObjectId, starhawk.ObjectId] } : null;
        });

        Assert.Equal([3], offered);
        Assert.Equal(1, GrootBoards.Growth(groot));
        Assert.True(Statuses.Has(world, groot, Statuses.Tough));
        Assert.True(Statuses.Has(world, starhawk, Statuses.Tough));
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void WeAreGrootIsNotOfferedWithoutGrowthCounters()
    {
        var world = GrootBoards.Board();
        var card = GrootBoards.Hand(world, "16006");
        GrootBoards.Hand(world, Genius);
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == card.ObjectId);
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void FertileGroundGrowsGrootAndDrawsInAlterEgoForm()
    {
        // 16007: "Alter-Ego Action: Exhaust Fertile Ground → place 1 growth counter on
        // Groot (to a maximum of 10) and draw 1 card."
        var world = GrootBoards.Board("16001b");
        var ground = GrootBoards.InPlay(world, "16007", DeckType.SupportsArea);

        GrootBoards.Play(world, ground, []);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(1, GrootBoards.Growth(GrootBoards.Groot(world)));
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.False(ground.Ready);
    }

    [Fact]
    [Rule("rr:form-change-form.7")]
    public void FertileGroundIsAnAlterEgoAction()
    {
        var world = GrootBoards.Board();
        var ground = GrootBoards.InPlay(world, "16007", DeckType.SupportsArea);
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == ground.ObjectId);
    }

    [Fact]
    [Rule("rr:basic-power.1.2")]
    public void EntanglingVinesAddsTwoToABasicThwartForOneGrowthCounter()
    {
        // 16008: "Hero Interrupt: When Groot makes a basic thwart, remove 1 growth
        // counter from him and exhaust Entangling Vines → Groot gets +2 THW for that
        // thwart." THW 1 + 2 removes 3.
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 2);
        var vines = GrootBoards.InPlay(world, "16008", DeckType.UpgradesArea);
        var main = GrootBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 5);

        BasicThwartPowers.BasicThwart(world, Cards, 0, main, []);
        GrootBoards.Drive(world, GrootBoards.Take(vines));

        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.Equal(1, GrootBoards.Growth(groot));
        Assert.False(vines.Ready);
    }

    [Fact]
    [Rule("rr:basic-power.1.1")]
    public void VineSpikesAddsTwoToABasicAttackForOneGrowthCounter()
    {
        // 16011: "Hero Interrupt: When Groot makes a basic attack, remove 1 growth counter
        // from him and exhaust Vine Spikes → Groot gets +2 ATK for that attack."
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 1);
        var spikes = GrootBoards.InPlay(world, "16011", DeckType.UpgradesArea);
        var villain = GrootBoards.Villain(world);

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        GrootBoards.Drive(world, GrootBoards.Take(spikes));

        Assert.Equal(4, villain.Damage);
        Assert.Equal(0, GrootBoards.Growth(groot));
        Assert.False(spikes.Ready);
    }

    [Fact]
    [Rule("rr:cost.6")]
    public void VineUpgradesNeedAGrowthCounter()
    {
        // The counter is part of the cost; without one the interrupt is not offered.
        var world = GrootBoards.Board();
        var spikes = GrootBoards.InPlay(world, "16011", DeckType.UpgradesArea);
        var villain = GrootBoards.Villain(world);
        var offered = new List<int>();

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        GrootBoards.Drive(world, GrootBoards.Take(spikes, offered));

        Assert.Empty(offered);
        Assert.Equal(2, villain.Damage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:defend-defense")]
    [Rule("rr:tough.2.2")]
    public void VineShieldAddsThreeDefenseForTheAttack(bool take)
    {
        // 16010: "Hero Interrupt: When Groot defends against an attack, remove 1 growth
        // counter from him and exhaust Vine Shield → Groot gets +3 DEF for that attack."
        // Rhino (III) attacks with 4. rr:tough.2.2: "If the damage is reduced to 0, the
        // hero does not lose their tough status card." DEF 6 keeps Groot's tough status
        // card; DEF 3 leaves 1 damage, which the tough status card prevents.
        var world = GrootBoards.Board(villain: "01096");
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 1);
        Statuses.Give(world, groot, Statuses.Tough);
        var shield = GrootBoards.InPlay(world, "16010", DeckType.UpgradesArea);
        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);

        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense") is { } defense ? Decision.Take(defense.Id)
            : take ? GrootBoards.Take(shield)(prompt) : null);

        Assert.Equal(0, groot.Damage);
        Assert.Equal(take, Statuses.Has(world, groot, Statuses.Tough));
        Assert.Equal(take ? 0 : 1, GrootBoards.Growth(groot));
        Assert.Equal(!take, shield.Ready);
    }

    [Fact]
    [Rule("rr:basic-power")]
    public void LashingVinesReadiesGrootAfterABasicAttackForTwoCounters()
    {
        // 16009: "Hero Response: After Groot uses a basic power, remove 2 growth counters
        // from him and exhaust Lashing Vines → ready Groot."
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 3);
        var lashing = GrootBoards.InPlay(world, "16009", DeckType.UpgradesArea);

        BasicPowers.BasicAttack(world, Cards, 0, GrootBoards.Villain(world), []);
        GrootBoards.Drive(world, GrootBoards.Take(lashing));

        Assert.True(groot.Ready);
        Assert.Equal(1, GrootBoards.Growth(groot));
        Assert.False(lashing.Ready);
    }

    [Fact]
    [Rule("rr:defend-defense.7.2")]
    public void LashingVinesAnswersABasicDefenseAfterTheAttack()
    {
        // rr:defend-defense.7.2: an ability after a basic power "triggers after an attack
        // in which a character made a basic defense resolves."
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 2);
        var lashing = GrootBoards.InPlay(world, "16009", DeckType.UpgradesArea);
        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);

        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense") is { } defense ? Decision.Take(defense.Id)
            : GrootBoards.Take(lashing)(prompt));

        Assert.True(groot.Ready);
        Assert.Equal(0, GrootBoards.Growth(groot));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:team-up")]
    public void FloraAndFaunaGrowsAndReadiesGrootWithRocketRaccoonInPlay(bool rocket)
    {
        // 16020: "Team-Up (Groot and Rocket Raccoon). ... Place 2 growth counters on Groot
        // (to a maximum of 10) and ready him". rr:team-up: the card "cannot be played
        // unless both of the named characters (hero or ally) are in play."
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 3);
        groot.Exhaust();
        if (rocket) GrootBoards.InPlay(world, "16019", DeckType.AlliesArea);
        var card = GrootBoards.Hand(world, "16020");
        var paying = GrootBoards.Hand(world, Energy);

        Assert.Equal(rocket, GrootBoards.Runner(world).Actions(world, 0).Any(a => a.Card == card.ObjectId));
        if (!rocket) return;
        GrootBoards.Play(world, card, [paying]);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(5, GrootBoards.Growth(groot));
        Assert.True(groot.Ready);
    }
}
