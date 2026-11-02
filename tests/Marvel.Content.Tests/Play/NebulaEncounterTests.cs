using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Lethal Intent, Barrel Roll, Combat Ready and Ruthless over the production book.
public sealed class NebulaEncounterTests : CoreCardTestBase
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:reveal")]
    public void LethalIntentRevealsTheNextTechniqueAttachment(int players)
    {
        // 16099: "When Revealed: Discard cards from the top of the encounter deck until a
        // Technique attachment is discarded. Reveal that card." The revealed Technique is the
        // first this round, so it gains surge; the side scheme enters with 2 threat per player.
        var world = NebulaBoards.Board(players);
        var nebula = GrootBoards.Villain(world);
        nebula.TakeDamage(20);
        var technique = BadoonBoards.OnTop(world, NebulaBoards.WeaponMastery);
        BadoonBoards.OnTop(world, "01104");
        var scheme = RonanBoards.Reveal(world, NebulaBoards.LethalIntent);

        BadoonBoards.Drive(world);

        Assert.Equal(nebula.ObjectId, technique.Area.Host);
        Assert.Equal(2 * players, scheme.Tokens.GetValueOrDefault("k_threat"));
        // Hard to Keep Down is discarded on the way, and surge deals another encounter card.
        Assert.Equal(1, RonanBoards.Discarded(world));
        Assert.Equal(1, NebulaBoards.Dealt(world));
    }

    [Fact]
    [Rule("rr:surge")]
    public void BarrelRollPlacesAnEvasionCounterAndSurges()
    {
        // 16100: "Incite 1. Surge. When Revealed: Place 1 evasion counter on Nebula's Ship."
        var world = NebulaBoards.Board();
        GrootBoards.Villain(world).TakeDamage(10);
        var ship = NebulaBoards.PutShip(world);
        long threat = NebulaBoards.Threat(world);

        RonanBoards.Reveal(world, NebulaBoards.BarrelRoll);
        BadoonBoards.Drive(world);

        Assert.Equal(1, NebulaBoards.Evasion(ship));
        Assert.Equal(threat + 1, NebulaBoards.Threat(world));
        Assert.Equal(1, NebulaBoards.Dealt(world));
    }

    [Fact]
    [Rule("rr:boost-boost-icon")]
    public void BarrelRollAsABoostCardPlacesAnEvasionCounter()
    {
        // 16100 Boost: "Place 1 evasion counter on Nebula's Ship." Its 1 boost icon adds to ATK.
        var world = NebulaBoards.Board();
        var ship = NebulaBoards.PutShip(world);
        BadoonBoards.OnTop(world, NebulaBoards.BarrelRoll);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(1, NebulaBoards.Evasion(ship));
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:special")]
    public void CombatReadyRevealsATechniqueThenResolvesItsSpecialForTheRevealingPlayer(int player)
    {
        // 16101: "When Revealed: Discard cards from the top of the encounter deck until a
        // Technique attachment is discarded. Reveal that card, then resolve its 'Special'
        // ability." Wide Stance's Special discards from the revealing player's hand.
        var world = NebulaBoards.Board(players: 2);
        var nebula = GrootBoards.Villain(world);
        nebula.TakeDamage(20);
        GrootBoards.Hand(world, "01089", 0);
        GrootBoards.Hand(world, "01089", 1);
        var technique = BadoonBoards.OnTop(world, NebulaBoards.WideStance);

        RonanBoards.Reveal(world, NebulaBoards.CombatReady, player);
        BadoonBoards.Drive(world);

        Assert.Equal(nebula.ObjectId, technique.Area.Host);
        Assert.Empty(world.Seats[player].Hand.Cards);
        Assert.Single(world.Seats[1 - player].Hand.Cards);
    }

    [Fact]
    [Rule("rr:special")]
    public void CombatReadyResolvesTheSpecialOfTheCardItRevealed()
    {
        // Cutthroat Ambition is already attached; Combat Ready reveals Weapon Mastery, whose
        // Special deals 1 damage. Cutthroat Ambition's Special does not resolve.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        nebula.TakeDamage(20);
        NebulaBoards.Technique(world, NebulaBoards.Cutthroat);
        BadoonBoards.OnTop(world, NebulaBoards.WeaponMastery);
        long threat = NebulaBoards.Threat(world);

        RonanBoards.Reveal(world, NebulaBoards.CombatReady);
        BadoonBoards.Drive(world);

        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(threat, NebulaBoards.Threat(world));
    }

    [Theory]
    [InlineData("01001a", false, 1)]
    [InlineData("01001a", true, 0)]
    [InlineData("01001b", false, 1)]
    [Rule("rr:attack-enemy-activation")]
    public void RuthlessPlacesAnEvasionCounterWhenNebulasActivationLands(string face, bool defend, long evasion)
    {
        // 16102: "When Revealed (Alter-Ego): Nebula schemes. If threat is placed by this
        // activation, place 1 evasion counter on Nebula's Ship. When Revealed (Hero): Nebula
        // attacks you. If damage is dealt by this activation, place 1 evasion counter." Player
        // 1 reveals it; Spider-Man's 3 DEF leaves none of Nebula's 2 ATK to deal.
        var world = NebulaBoards.Board(players: 2);
        world.Seats[1].IdentityCard.TurnTo(face);
        var ship = NebulaBoards.PutShip(world);
        var defender = world.Seats[1].IdentityCard;

        RonanBoards.Reveal(world, NebulaBoards.Ruthless, 1);
        BadoonBoards.Drive(world, prompt => defend && prompt.Asking == Question.Defender && prompt.Player == 1
            ? Decision.Take(defender.ObjectId) : null);

        Assert.Equal(evasion, NebulaBoards.Evasion(ship));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(face == "01001a" && !defend ? 2 : 0, defender.Damage);
    }

    [Fact]
    [Rule("rr:confuse-confused.1")]
    public void RuthlessPlacesNoEvasionCounterWhenNebulasSchemePlacesNoThreat()
    {
        // "When this character would scheme or thwart, remove each confused status card from it
        // instead." Nebula places no threat, so Ruthless places no evasion counter.
        var world = NebulaBoards.Board(face: "01001b");
        var ship = NebulaBoards.PutShip(world);
        var nebula = GrootBoards.Villain(world);
        Statuses.Give(world, nebula, Statuses.Confused);
        long threat = NebulaBoards.Threat(world);

        RonanBoards.Reveal(world, NebulaBoards.Ruthless);
        BadoonBoards.Drive(world);

        Assert.False(Statuses.Has(world, nebula, Statuses.Confused));
        Assert.Equal(threat, NebulaBoards.Threat(world));
        Assert.Equal(0, NebulaBoards.Evasion(ship));
    }

    [Fact]
    [Rule("rr:special")]
    public void CombatReadyResolvesNoSpecialWhenNoTechniqueIsRevealed()
    {
        // The encounter deck and discard pile hold no Technique, so nothing is revealed.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        long threat = NebulaBoards.Threat(world);

        RonanBoards.Reveal(world, NebulaBoards.CombatReady);
        BadoonBoards.Drive(world);

        Assert.DoesNotContain(world.Cards, card => card.Area.Host == nebula.ObjectId);
        Assert.Equal(threat, NebulaBoards.Threat(world));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:tough.3")]
    public void RuthlessCountsDamageDealtToAToughHero()
    {
        // Ruling ea69fd70a070ee2c: Ruthless needs damage dealt, not taken.
        var world = NebulaBoards.Board();
        var ship = NebulaBoards.PutShip(world);
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Tough);

        RonanBoards.Reveal(world, NebulaBoards.Ruthless);
        BadoonBoards.Drive(world);

        Assert.Equal(0, hero.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(1, NebulaBoards.Evasion(ship));
    }
}
