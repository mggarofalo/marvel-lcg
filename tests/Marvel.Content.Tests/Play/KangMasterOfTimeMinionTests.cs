using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Master of Time minion faces and the full authored book on a nonpublic
// Rhino board; Kang's public admission stays closed.
public sealed class KangMasterOfTimeMinionTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:obligation.5")]
    public void MasterOfTimeCountsOnlyTheEngagedPlayersObligations(int obligations)
    {
        // 11047 prints SCH 1 and ATK 1: "Kang (Master of Time) gets +1 SCH and +1
        // ATK for each obligation in your play area." rr:obligation.5: a revealing
        // player "places that obligation into their play area". Outside an
        // activation his play area is that of player 1, the engaged player; player
        // 0's three obligations do not count.
        var (world, _) = KangTemporalFixture.Board(players: 2);
        var kang = KangTemporalFixture.Engaged(world, "11047", 1);
        KangTemporalFixture.Obligations(world, 0, 3);
        KangTemporalFixture.Obligations(world, 1, obligations);

        Assert.Equal(1 + obligations, Modified(world, kang, "attack"));
        Assert.Equal(1 + obligations, Modified(world, kang, "scheme"));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 2)]
    [Rule("rr:activation.2")]
    [Rule("rr:villainous")]
    public void MasterOfTimeActivatesWithItsObligationStatsAndABoostCard(bool scheme, int obligations)
    {
        // rr:activation.2: an engaged minion attacks a hero and schemes against an
        // alter-ego. rr:villainous: "When this minion activates, give it a boost
        // card." The boost card, Hard to Keep Down, prints no boost icon, so the
        // damage or threat is his modified ATK or SCH. Player 1 is not first.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 3);
        var kang = KangTemporalFixture.Engaged(world, "11047", 1);
        KangTemporalFixture.Obligations(world, 1, obligations);
        KangTemporalFixture.Activate(world, kang, scheme, 1);

        var events = f.Finish();

        Assert.Equal(scheme ? 0 : 1 + obligations, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(scheme ? 1 + obligations : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(scheme ? 0 : 1, events.OfType<AttackCompleted>().Count(e => e.Enemy == kang.ObjectId));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:toughness.1")]
    public void RevealedMasterOfTimeEntersEngagedWithATough()
    {
        // rr:toughness.1: "Forced Response: After this character enters play, give
        // it a tough status card." Player 1 reveals him.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var kang = f.Reveal("11047", player: 1);

        f.Finish();

        Assert.Equal(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)), kang.Area);
        Assert.Single(Statuses.On(world, kang, Statuses.Tough));
        Assert.Equal(1, Modified(world, kang, "villainous"));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:incite-x.1")]
    [Rule("rr:surge.1")]
    public void RevealedTimeDisplacedSoldierIncitesAndSurges()
    {
        // 11048: "Incite 1. Surge." rr:incite-x.1 places 1 threat on the main
        // scheme. rr:surge.1: "Deal yourself 1 facedown encounter card": the
        // revealing player, player 1, is dealt the Hydra Mercenary on top.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var mercenary = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var soldier = f.Reveal("11048", player: 1);

        f.Finish(First);

        Assert.Equal([soldier], world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Equal([mercenary], world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon")]
    [Rule("rr:you-your")]
    public void TimeDisplacedSoldierBoostDealsTheActivatedPlayerAFacedownCard(bool scheme)
    {
        // 11048: "[star] Boost: Deal yourself 1 facedown encounter card." Its boost
        // field is 0, so Rhino's damage or threat is his printed ATK 2 or SCH 1.
        // rr:you-your: "you" is the player the activation is against, player 1.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var dealt = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var soldier = world.CreateCard("11048", world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Activate(world, f.Villain, scheme, 1);

        f.Finish();

        Assert.Equal([dealt], world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(scheme ? 0 : 2, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(scheme ? 1 : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal([soldier], world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    private static Decision First(Prompt prompt) => Decision.Take(prompt.Affordances[0].Id);
}
