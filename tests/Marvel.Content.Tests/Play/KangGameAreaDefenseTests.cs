using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Kang II faces and the full authored book on a prepared two-area split.
public sealed class KangGameAreaDefenseTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [Rule("rr:defend-defense.5")]
    public void OnlyPlayersInTheAttackersGameAreaAreOfferedTheDefence(bool joined, int otherOffers)
    {
        // pack:mc11:game-areas: "Players cannot attack or defend enemies in other
        // game areas." rr:defend-defense.5 lets a player defend another player
        // once they share the attacker's game area.
        var (world, kang, _, _) = KangStageTwoVillainTests.Board("11003", "11010b", otherVillain: !joined);
        if (joined) world.Join(PlayArea.Of(1), world.GameAreaOf(PlayArea.Of(0))!, "prepared join", []);
        var other = world.Seats[1].IdentityCard;
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        KangTemporalFixture.Attack(world, kang, 0);
        var askedSeats = new List<int>();
        int offersOfOther = 0;
        int offersOfAlly = 0;

        KangTemporalFixture.Drive(world, (Marvel.Cards.Run.AbilityRunner)world.Abilities, prompt =>
        {
            if (prompt.Asking != Question.Defender) return null;
            askedSeats.Add(prompt.Player);
            offersOfOther += prompt.Affordances.Count(option => option.Id == other.ObjectId);
            offersOfAlly += prompt.Affordances.Count(option => option.Id == ally.ObjectId);
            return Decision.Decline;
        });

        Assert.Equal(otherOffers, offersOfOther);
        Assert.Equal(otherOffers, offersOfAlly);
        Assert.Equal(joined, askedSeats.Contains(1));
        Assert.Contains(0, askedSeats);
        Assert.True(other.Ready);
        Assert.True(ally.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:defend-defense.4")]
    public void ADefenseAbilityIsOfferedOnlyInTheAttackersGameArea(bool joined)
    {
        // rr:defend-defense.4: "When a player initiates a triggered ability labeled
        // as a defense [...] during an enemy attack, that player's identity becomes
        // the defender". pack:mc11:game-areas: "Players cannot attack or defend
        // enemies in other game areas." Synthetic: The Golden City in player 1's
        // play area gains "Interrupt (defense): When an enemy initiates an attack,
        // draw 1 card."
        var (world, kang, _, _) = KangStageTwoVillainTests.Board("11003", "11010b", otherVillain: !joined);
        world.Abilities = KangTemporalFixture.Replacing("01045", """
            {"card":"01045","abilities":[{"labels":["Defense"],"trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"enemy"},
              "effect":{"draw":{"player":"you","count":1}}}]}
            """);
        if (joined) world.Join(PlayArea.Of(1), world.GameAreaOf(PlayArea.Of(0))!, "prepared join", []);
        var city = world.CreateCard("01045", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        KangTemporalFixture.Attack(world, kang, 0);
        int offers = 0;
        bool? usable = null;

        KangTemporalFixture.Drive(world, (Marvel.Cards.Run.AbilityRunner)world.Abilities, prompt =>
        {
            if (prompt.Asking == Question.Defender) usable ??= Attack.CanUseDefenseAbility(world, 1, city);
            offers += prompt.Affordances.Count(option => option.AnchorId == city.ObjectId);
            return null;
        });

        Assert.Equal(joined, usable);
        Assert.Equal(joined ? 1 : 0, offers);
    }

    [Theory]
    [InlineData(false, "WhenAttackInitiated", "Interrupt")]
    [InlineData(true, "WhenAttackInitiated", "Interrupt")]
    [InlineData(false, "WhenAttackEnds", "Response")]
    [InlineData(true, "WhenAttackEnds", "Response")]
    [Rule("rr:interrupt")]
    [Rule("rr:response")]
    public void ATriggerOnAnAttackInAnotherGameAreaRaisesRatherThanGuessing(bool joined, string trigger, string timing)
    {
        // pack:mc11:game-areas: "Cards and components in one game area cannot affect
        // another game area". Nothing says whether a card may trigger off an enemy
        // attack in another game area, so the engine raises. Synthetic: The Golden
        // City in player 1's play area gains "Interrupt: When an enemy initiates an
        // attack, draw 1 card." or "Response: After an enemy attacks, draw 1 card."
        // Once player 1 shares the area it is offered.
        var (world, kang, _, _) = KangStageTwoVillainTests.Board("11003", "11010b", otherVillain: !joined);
        world.Abilities = KangTemporalFixture.Replacing("01045", """
            {"card":"01045","abilities":[{"trigger":{"event":"TRIGGER","timing":"TIMING","actor":"enemy"},
              "effect":{"draw":{"player":"you","count":1}}}]}
            """.Replace("TRIGGER", trigger, StringComparison.Ordinal).Replace("TIMING", timing, StringComparison.Ordinal));
        if (joined) world.Join(PlayArea.Of(1), world.GameAreaOf(PlayArea.Of(0))!, "prepared join", []);
        var city = world.CreateCard("01045", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        KangTemporalFixture.Attack(world, kang, 0);
        int offers = 0;

        void Drive() => KangTemporalFixture.Drive(world, (Marvel.Cards.Run.AbilityRunner)world.Abilities, prompt =>
        {
            offers += prompt.Affordances.Count(option => option.AnchorId == city.ObjectId);
            return null;
        });

        if (joined)
        {
            Drive();
            Assert.Equal(1, offers);
            return;
        }
        var thrown = Assert.Throws<RulesNotImplementedException>(Drive);
        Assert.Contains("01045", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, offers);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attack-player-ability-type.4")]
    [Rule("rr:crisis-icon.1")]
    public void BasicTargetsAndCrisisStayInTheirGameArea(bool crisis)
    {
        // pack:mc11:game-areas: players "cannot attack [...] enemies in other game
        // areas, and they cannot target any game elements in the other game areas",
        // and a crisis icon in one game area cannot affect another's main scheme.
        var (world, kang, scheme, other) = KangStageTwoVillainTests.Board("11003", "11010b");
        var otherKang = KangStageFlowFixture.LocalKang(world, 1);
        scheme.PlaceTokens("k_threat", 3);
        other.PlaceTokens("k_threat", 3);
        if (crisis) world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(1)));

        var attackable = BasicPowers.Attackable(world, world.Facts, 0);
        var mine = BasicPowers.Thwartable(world, world.Facts, 0);
        var theirs = BasicPowers.Thwartable(world, world.Facts, 1);

        Assert.Contains(kang, attackable);
        Assert.DoesNotContain(otherKang, attackable);
        Assert.Contains(scheme, mine);
        Assert.DoesNotContain(other, mine);
        Assert.Equal(!crisis, theirs.Contains(other));
        Assert.DoesNotContain(scheme, theirs);
    }
}
