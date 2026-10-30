using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Kang's Dominion (11023) and Pinned Down (11024) with the full authored book on a
// nonpublic Rhino board, where Rhino is the villain in the only game area. Dominion
// enters play here directly; its stage-3B storage and 4A reveal have their own tests.
// Kang's public admission stays closed.
public sealed class KangDominionTests : CoreCardTestBase
{
    private const string Dominion = "11023";
    private const string PinnedDown = "11024";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:damage.step.1")]
    [Rule("rr:tough.2")]
    public void KangCannotTakeDamageAndKeepsTough(bool tough)
    {
        // 11023: "Kang cannot take damage." Ruling 76a9465df95e739c: "if 'Kang's
        // Dominion' is in play, and Kang has Tough, he will keep Tough even if card
        // effects would otherwise try to deal damage to him."
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var dominion = InPlay(world);
        if (tough) Statuses.Give(world, f.Villain, Statuses.Tough);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 1, f.Villain, []);
        f.Finish();

        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(tough, Statuses.Has(world, f.Villain, Statuses.Tough));
        Assert.Equal(DeckType.SideSchemesArea, dominion.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void KangTakesDamageAgainOnceDominionLeaves()
    {
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var dominion = InPlay(world);
        Discard.Card(world, dominion, "test", []);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, f.Villain, []);
        f.Finish();

        Assert.Equal(2, f.Villain.Damage);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void DominionDoesNotProtectAMinion()
    {
        var (world, f) = KangTemporalFixture.Board(players: 1);
        InPlay(world);
        var shocker = KangTemporalFixture.Engaged(world, "01103", 0);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, shocker, []);
        f.Finish();

        Assert.Equal(2, shocker.Damage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatDealsTheDefeatingPlayerAnEncounterCard(int player)
    {
        // 11023: "When Defeated: Deal the player who defeated this scheme an encounter
        // card." Either hero's basic thwart defeats the 1-threat scheme.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var dominion = InPlay(world, threat: 1);
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;

        BasicThwartPowers.BasicThwart(world, world.Facts, player, dominion, []);
        f.Finish();

        Assert.Equal(DeckType.EncounterDiscardPile, dominion.Area.Type);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards);
        Assert.Equal(deck - 1, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1 - player)).Cards);
    }

    [Theory]
    [InlineData(0, 0, 2)]
    [InlineData(1, 0, 4)]
    [InlineData(2, 1, 8)]
    [InlineData(0, 3, 8)]
    [Rule("rr:obligation.5")]
    public void PinnedDownPlacesTwoThreatForEachObligationInPlay(int first, int second, int threat)
    {
        // 11024: "When Revealed: Place 2 threat here for each obligation in play." Its
        // starting threat is 2. Every player's obligations count, not only the
        // revealing player's (player 1).
        var (world, f) = KangTemporalFixture.Board(players: 2);
        KangTemporalFixture.Obligations(world, 0, first);
        KangTemporalFixture.Obligations(world, 1, second);
        var pinned = f.Reveal(PinnedDown, player: 1);

        f.Finish();

        Assert.Equal(DeckType.SideSchemesArea, pinned.Area.Type);
        Assert.Equal(threat, pinned.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card InPlay(World world, int threat = 3)
    {
        var dominion = world.CreateCard(Dominion, world.AreaOf(DeckType.SideSchemesArea));
        dominion.PlaceTokens("k_threat", threat);
        return dominion;
    }
}
