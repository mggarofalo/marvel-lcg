using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The production Band of Badoon modular set on synthetic boards.
public sealed class BandOfBadoonTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:quickstrike")]
    public void BadoonAssassinAttacksTheHeroItEngagesWithTwoMoreAttack()
    {
        // 16117: "Forced Response: After Badoon Assassin engages your hero, it attacks you
        // with +2 ATK." Its printed ATK is 1.
        var world = BadoonBoards.Board();
        var hero = world.Seats[0].IdentityCard;
        var assassin = ShipCommandBoards.Reveal(world, BadoonBoards.Assassin);

        var events = BadoonBoards.Drive(world);

        Assert.Equal(DeckType.EngagedEnemiesArea, assassin.Area.Type);
        Assert.Equal(3, hero.Damage);
        Assert.Equal(3, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    [Fact]
    [Rule("rr:quickstrike")]
    public void BadoonAssassinDoesNotAttackAnAlterEgo()
    {
        // "Engages your hero": an alter-ego is not a hero.
        var world = BadoonBoards.Board(face: "01001b");
        ShipCommandBoards.Reveal(world, BadoonBoards.Assassin);

        var events = BadoonBoards.Drive(world);

        Assert.Empty(events.OfType<AttackCompleted>());
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:piercing")]
    public void BadoonAssassinAsABoostCardGivesTheAttackPiercingOverkillAndRanged()
    {
        // 16117: "Boost: If this activation is an attack, this attack gains overkill,
        // piercing, and ranged." Piercing discards the hero's tough status before damage.
        var world = BadoonBoards.Board();
        var hero = world.Seats[0].IdentityCard;
        var villain = GrootBoards.Villain(world);
        Statuses.Give(world, hero, Statuses.Tough);
        BadoonBoards.OnTop(world, BadoonBoards.Assassin);

        KangTemporalFixture.Activate(world, villain, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(2, hero.Damage);
        Assert.Equal(0, StateFields.Modified(world, villain, "piercing", world.Facts, world.Players));
        Assert.Equal(0, StateFields.Modified(world, villain, "overkill", world.Facts, world.Players));
        Assert.Equal(0, StateFields.Modified(world, villain, "ranged", world.Facts, world.Players));
    }

    [Fact]
    [Rule("rr:overkill")]
    public void BadoonAssassinsBoostGivesTheAttackOverkill()
    {
        // Drang with his Spear attacks for 3; Black Cat (2 hit points) defends. Overkill deals
        // the excess 1 damage to her controller's identity.
        var world = BadoonBoards.Board();
        var hero = world.Seats[0].IdentityCard;
        var villain = GrootBoards.Villain(world);
        ShipCommandBoards.Attach(world, villain, BadoonBoards.Spear);
        var cat = GrootBoards.InPlay(world, "01002", DeckType.AlliesArea);
        BadoonBoards.OnTop(world, BadoonBoards.Assassin);

        KangTemporalFixture.Activate(world, villain, scheme: false, 0);
        BadoonBoards.Drive(world, prompt => prompt.Asking == Question.Defender ? Decision.Take(cat.ObjectId) : null);

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(1, hero.Damage);
    }

    [Fact]
    [Rule("rr:overkill")]
    public void WithoutTheBoostTheDefendingAllyAbsorbsTheExcess()
    {
        // The control for the overkill test: an ordinary attack's excess does not carry over
        // (rr:attack-enemy-activation.3).
        var world = BadoonBoards.Board();
        var hero = world.Seats[0].IdentityCard;
        var villain = GrootBoards.Villain(world);
        ShipCommandBoards.Attach(world, villain, BadoonBoards.Spear);
        var cat = GrootBoards.InPlay(world, "01002", DeckType.AlliesArea);

        KangTemporalFixture.Activate(world, villain, scheme: false, 0);
        BadoonBoards.Drive(world, prompt => prompt.Asking == Question.Defender ? Decision.Take(cat.ObjectId) : null);

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(0, hero.Damage);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [Rule("rr:ranged")]
    public void BadoonAssassinsBoostGivesTheAttackRanged(bool boosted, long retaliated)
    {
        // A ranged attack ignores retaliate; without the boost the hero's retaliate 1 deals
        // its damage to Drang.
        var world = BadoonBoards.Board();
        var hero = world.Seats[0].IdentityCard;
        var villain = GrootBoards.Villain(world);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "retaliate", Amount: 1,
            Card: hero.ObjectId, Affects: hero.ObjectId));
        if (boosted) BadoonBoards.OnTop(world, BadoonBoards.Assassin);

        KangTemporalFixture.Activate(world, villain, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(2, hero.Damage);
        Assert.Equal(retaliated, villain.Damage);
    }

    [Fact]
    [Rule("rr:piercing")]
    public void BadoonAssassinAsABoostCardForASchemeGrantsNothing()
    {
        var world = BadoonBoards.Board(face: "01001b");
        BadoonBoards.OnTop(world, BadoonBoards.Assassin);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: true, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(1, GrootBoards.MainScheme(world).Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:engage")]
    public void BadoonGruntDealsAnEncounterCardWhenItIsTheOnlyMinionEngagedWithYou()
    {
        // 16118: "Forced Response: After Badoon Grunt engages you, if there are no other
        // minions engaged with you, deal yourself 1 facedown encounter card."
        var world = BadoonBoards.Board(face: "01001b");
        var grunt = ShipCommandBoards.Reveal(world, BadoonBoards.Grunt);

        BadoonBoards.Drive(world);

        Assert.Equal(DeckType.EngagedEnemiesArea, grunt.Area.Type);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Fact]
    [Rule("rr:engage")]
    public void BadoonAssassinAttacksTheLaterPlayerItEngages()
    {
        // The player it engaged is "you", not the first player.
        var world = BadoonBoards.Board(players: 2, face: "01001b");
        ShipCommandBoards.Reveal(world, BadoonBoards.Assassin, player: 1);

        BadoonBoards.Drive(world);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:engage")]
    public void BadoonGruntDealsTheCardToTheLaterPlayerItEngages()
    {
        var world = BadoonBoards.Board(players: 2);
        ShipCommandBoards.Reveal(world, BadoonBoards.Grunt, player: 1);

        BadoonBoards.Drive(world);

        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Fact]
    [Rule("rr:engage")]
    public void BadoonGruntDealsNothingBesideAnotherMinion()
    {
        var world = BadoonBoards.Board(face: "01001b");
        GrootBoards.Engaged(world, BadoonBoards.Sentry);
        ShipCommandBoards.Reveal(world, BadoonBoards.Grunt);

        BadoonBoards.Drive(world);

        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Fact]
    [Rule("rr:engage")]
    public void BadoonGruntCountsOnlyMinionsEngagedWithYou()
    {
        // Another player's minion is not engaged with you.
        var world = BadoonBoards.Board(players: 2, face: "01001b");
        world.Seats[1].IdentityCard.TurnTo("01001b");
        GrootBoards.Engaged(world, BadoonBoards.Sentry, player: 1);
        ShipCommandBoards.Reveal(world, BadoonBoards.Grunt);

        BadoonBoards.Drive(world);

        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Fact]
    [Rule("rr:boost-boost-icon")]
    public void BadoonGruntAsABoostCardEngagesYouAndThenDealsYouACard()
    {
        // 16118: "Boost: Put Badoon Grunt into play engaged with you." It then engages you,
        // so its own forced response follows.
        var world = BadoonBoards.Board(face: "01001b");
        var grunt = BadoonBoards.OnTop(world, BadoonBoards.Grunt);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: true, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(DeckType.EngagedEnemiesArea, grunt.Area.Type);
        Assert.Equal(PlayArea.Of(0), grunt.Area.PlayArea);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData(true, 4)]
    [InlineData(false, 3)]
    [Rule("rr:scheme-enemy-activation.step.2.c")]
    public void BadoonLieutenantAddsTwoBoostIconsOnlyToASchemeActivation(bool scheme, long total)
    {
        // 16119: "Boost: If this activation is a scheme, this card gets +2 boost icons for this
        // activation." Drang (I) has 1 SCH and 2 ATK; the Lieutenant prints one boost icon.
        var world = BadoonBoards.Board(face: scheme ? "01001b" : "01001a");
        var lieutenant = BadoonBoards.OnTop(world, BadoonBoards.Lieutenant);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(total, scheme ? GrootBoards.MainScheme(world).Tokens.GetValueOrDefault("k_threat")
            : world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, StateFields.Modified(world, lieutenant, "boost_const", world.Facts, world.Players));
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 5)]
    [Rule("rr:status-cards.1")]
    public void BadoonSentryToughensTheVillainOrAddsTwoBoostIconsIfHeIsAlreadyTough(bool tough, long damage)
    {
        // 16120: "Boost: Give the villain a tough status card. If the villain already has a
        // tough status card, this card gets +2 boost icons for this activation."
        var world = BadoonBoards.Board();
        var villain = GrootBoards.Villain(world);
        if (tough) Statuses.Give(world, villain, Statuses.Tough);
        BadoonBoards.OnTop(world, BadoonBoards.Sentry);

        KangTemporalFixture.Activate(world, villain, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.True(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 1)]
    [Rule("rr:attack-enemy-activation.step.3.c")]
    public void BadoonWarlordAddsTwoBoostIconsOnlyToAnAttack(bool scheme, long total)
    {
        // 16121: "Boost: If this activation is an attack, this card gets +2 boost icons for
        // this activation." The Warlord prints no boost icon.
        var world = BadoonBoards.Board(face: scheme ? "01001b" : "01001a");
        BadoonBoards.OnTop(world, BadoonBoards.Warlord);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(total, scheme ? GrootBoards.MainScheme(world).Tokens.GetValueOrDefault("k_threat")
            : world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:overkill")]
    public void BadoonWarlordsAttacksGainOverkill()
    {
        // 16121: "Badoon Warlord's attacks gain overkill." Black Cat (2 hit points) defends its
        // 3-damage attack; the excess 1 damage is dealt to her controller's identity.
        var world = BadoonBoards.Board();
        var hero = world.Seats[0].IdentityCard;
        var cat = GrootBoards.InPlay(world, "01002", DeckType.AlliesArea);
        var warlord = GrootBoards.Engaged(world, BadoonBoards.Warlord);

        KangTemporalFixture.Activate(world, warlord, scheme: false, 0);
        BadoonBoards.Drive(world, prompt => prompt.Asking == Question.Defender ? Decision.Take(cat.ObjectId) : null);

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(1, hero.Damage);
    }
}
