using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nebula's three stages over the production book: the Forced Interrupt that resolves every
// Technique attachment's Special, and what each stage does with those attachments afterwards.
public sealed class NebulaVillainTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:special")]
    public void NebulaOneResolvesEachTechniqueAgainstThePlayerSheActivatesAgainstThenDiscardsThem(bool scheme)
    {
        // 16088: "Forced Interrupt: When Nebula initiates an activation against you, resolve the
        // 'Special' ability on each Technique attachment in play, then discard each of those
        // attachments." Cutthroat Ambition places 1 threat; Wide Stance discards 1 card at random
        // from the hand of the player she activates against, player 1.
        var world = NebulaBoards.Board(players: 2, face: scheme ? "01001b" : "01001a");
        world.Seats[1].IdentityCard.TurnTo(scheme ? "01001b" : "01001a");
        var cutthroat = NebulaBoards.Technique(world, NebulaBoards.Cutthroat);
        var stance = NebulaBoards.Technique(world, NebulaBoards.WideStance);
        GrootBoards.Hand(world, "01089");
        GrootBoards.Hand(world, "01089", 1);
        GrootBoards.Hand(world, "01090", 1);
        long threat = NebulaBoards.Threat(world);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme, 1);
        BadoonBoards.Drive(world);

        Assert.Equal(DeckType.EncounterDiscardPile, cutthroat.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, stance.Area.Type);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Single(world.Seats[1].Hand.Cards);
        // The Special's threat, and Nebula's 1 SCH when she schemes.
        Assert.Equal(threat + 1 + (scheme ? 1 : 0), NebulaBoards.Threat(world));
    }

    [Fact]
    [Rule("rr:you-your.2")]
    public void WeaponMasterysSpecialDamagesThePlayerNebulaActivatesAgainst()
    {
        // 16097: "Special: Take 1 damage." Nebula schemes against player 1's alter-ego.
        var world = NebulaBoards.Board(players: 2, face: "01001b");
        world.Seats[1].IdentityCard.TurnTo("01001b");
        NebulaBoards.Technique(world, NebulaBoards.WeaponMastery);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: true, 1);
        BadoonBoards.Drive(world);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:special")]
    public void NebulaTwoKeepsAllButTheOneTechniqueTheAttackedPlayerChooses()
    {
        // 16089: "... then choose and discard 1 of those attachments." Both Specials resolve:
        // two threat, and no Technique leaves before both have.
        var world = NebulaBoards.Board(villain: NebulaBoards.NebulaTwo);
        var first = NebulaBoards.Technique(world, NebulaBoards.Cutthroat);
        var second = NebulaBoards.Technique(world, NebulaBoards.Cutthroat);
        long threat = NebulaBoards.Threat(world);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(threat + 2, NebulaBoards.Threat(world));
        Assert.Single(new[] { first, second }, card => card.Area.Type == DeckType.EncounterDiscardPile);
        Assert.Single(new[] { first, second }, card => card.Area.Host == GrootBoards.Villain(world).ObjectId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:special")]
    public void NebulaThreeLetsThePlayerRemoveTheirTopCardToDiscardATechnique(bool remove)
    {
        // 16090: "You may then remove the top card of your deck from the game to choose and
        // discard 1 of those attachments." Player 1 is attacked; only their deck pays.
        var world = NebulaBoards.Board(players: 2, villain: NebulaBoards.NebulaThree);
        var mastery = NebulaBoards.Technique(world, NebulaBoards.WeaponMastery);
        int deck = world.Seats[1].Deck.Cards.Count;

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 1);
        NebulaBoards.Choose(world, remove ? "Remove the top card" : "Do not remove");

        Assert.Equal(remove, mastery.Area.Type == DeckType.EncounterDiscardPile);
        Assert.Equal(remove ? deck - 1 : deck, world.Seats[1].Deck.Cards.Count);
        Assert.Equal(remove ? 1 : 0, world.AreaOf(DeckType.RemovedArea).Cards.Count);
        Assert.Equal(12, world.Seats[0].Deck.Cards.Count);
        // The Special's 1 damage, then 3 ATK.
        Assert.Equal(4, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:activation")]
    public void NebulaThreeAsksNothingWithoutATechniqueInPlay()
    {
        var world = NebulaBoards.Board(villain: NebulaBoards.NebulaThree);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 0);
        var asked = NebulaBoards.Choose(world, "Remove the top card");

        Assert.DoesNotContain(asked, prompt => prompt.Affordances.Any(a => (a.Description ?? a.Label)
            .Contains("Remove the top card", StringComparison.Ordinal)));
        Assert.Equal(12, world.Seats[0].Deck.Cards.Count);
    }

    [Theory]
    [InlineData(false, 0, 1)]
    [InlineData(true, 1, 2)]
    [Rule("rr:you-your.5")]
    public void EvasiveManeuveringStunsYouOrGivesNebulaABoostCard(bool stunned, int player, int boosts)
    {
        // 16095: "Special: You are stunned. If you are already stunned, give Nebula 1 facedown
        // boost card." The card joins this activation, so two boost cards are discarded. The
        // attacked player is player 1 or player 0; the other player is untouched.
        var world = NebulaBoards.Board(players: 2);
        var attacked = world.Seats[player].IdentityCard;
        if (stunned) Statuses.Give(world, attacked, Statuses.Stunned);
        NebulaBoards.Technique(world, NebulaBoards.Evasive);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, player);
        BadoonBoards.Drive(world);

        Assert.Equal(boosts, RonanBoards.Discarded(world) - 1);
        Assert.True(Statuses.Has(world, attacked, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[1 - player].IdentityCard, Statuses.Stunned));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [Rule("rr:status-cards.1")]
    public void UnyieldingPersistenceGivesNebulaToughOrABoostCard(bool tough, int boosts)
    {
        // 16096: "Special: Give Nebula a tough status card. If Nebula already has a tough status
        // card, give Nebula 1 facedown boost card."
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        if (tough) Statuses.Give(world, nebula, Statuses.Tough);
        NebulaBoards.Technique(world, NebulaBoards.Unyielding);

        KangTemporalFixture.Activate(world, nebula, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.True(Statuses.Has(world, nebula, Statuses.Tough));
        // Unyielding Persistence itself is discarded after its Special.
        Assert.Equal(boosts, RonanBoards.Discarded(world) - 1);
    }
}
