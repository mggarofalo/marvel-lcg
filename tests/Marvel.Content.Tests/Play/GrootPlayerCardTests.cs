using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Groot's Protection and Basic cards on synthetic boards.
public sealed class GrootPlayerCardTests : CoreCardTestBase
{
    private const string Strength = "01090";

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [Rule("rr:remaining-hit-points")]
    public void StarhawkReturnsToHandWhenTheDamageExactlyMatchesHisRemainingHitPoints(int damaged, bool returns)
    {
        // 16012: "Interrupt: When Starhawk takes damage exactly equal to his remaining hit
        // points, return him to your hand." Rhino's 2 against Starhawk's 3 hit points
        // matches only after 1 damage. Ruling dc116cb456de74af: cards can take damage
        // beyond their remaining hit points, so the comparison uses all the damage.
        var world = GrootBoards.Board();
        var starhawk = GrootBoards.InPlay(world, "16012", DeckType.AlliesArea);
        starhawk.TakeDamage(damaged);
        var offered = new List<int>();
        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);

        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.FirstOrDefault(a => a.Verb == "Defense" && a.AnchorId == starhawk.ObjectId) is { } defense
                ? Decision.Take(defense.Id)
                : GrootBoards.Take(starhawk, offered)(prompt));

        Assert.Equal(returns ? 1 : 0, offered.Count);
        Assert.Equal(returns ? DeckType.HandsArea : DeckType.AlliesArea, starhawk.Area.Type);
        Assert.Equal(returns ? 0 : 2, starhawk.Damage);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 2)]
    [Rule("rr:hit-points.1")]
    public void FightingFitDealsFiveAtStartingHitPointsAndTwoOtherwise(int damage, long dealt)
    {
        // 16014: "Deal 2 damage to the villain (5 damage instead if your hero's remaining
        // hit points are equal to or greater than your hero's starting hit points)."
        // rr:hit-points.1: "The phrase 'starting hit points' refers to an identity's
        // printed hit point value."
        var world = GrootBoards.Board();
        GrootBoards.Groot(world).TakeDamage(damage);
        var villain = GrootBoards.Villain(world);
        var card = GrootBoards.Hand(world, "16014");
        var paying = new[] { GrootBoards.Hand(world, Strength) };

        GrootBoards.Play(world, card, paying);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(dealt, villain.Damage);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [Rule("rr:retaliate-x")]
    [Rule("rr:hit-points.1")]
    public void DauntlessGivesRetaliateOnlyAtStartingHitPoints(int damage, long retaliate)
    {
        // 16016: "While your hero's remaining hit points are equal to or greater than
        // your hero's starting hit points, your hero gains retaliate 1."
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        groot.TakeDamage(damage);
        GrootBoards.InPlay(world, "16016", DeckType.UpgradesArea);

        Assert.Equal(retaliate, Modified(world, groot, "retaliate"));
    }

    [Theory]
    [InlineData(true, 4)]
    [InlineData(false, 5)]
    [Rule("rr:defend-defense")]
    public void HardToIgnoreRemovesOneThreatAfterAnUndamagedDefense(bool defend, long threat)
    {
        // 16017: "Hero Response: After your hero defends against an attack and takes no
        // damage, exhaust Hard to Ignore → remove 1 threat from the main scheme." Rhino's
        // 2 against DEF 3 deals nothing.
        var world = GrootBoards.Board();
        var hard = GrootBoards.InPlay(world, "16017", DeckType.UpgradesArea);
        var main = GrootBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 5);
        GrootBoards.Groot(world).PlaceTokens("c_growth", 2);
        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);

        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense") is { } defense
                ? (defend ? Decision.Take(defense.Id) : null)
                : GrootBoards.Take(hard)(prompt));

        Assert.Equal(threat, main.Tokens["k_threat"]);
        Assert.Equal(!defend, hard.Ready);
    }

    [Theory]
    [InlineData("16019")]
    [InlineData("16047")]
    [InlineData("16052")]
    [InlineData("17022")]
    [Rule("rr:play-restrictions-and-permissions")]
    public void TheGuardianIconTokenNamesTheGuardianTrait(string face)
    {
        // "Play only if your identity has the [guardian] trait." The bracketed token is
        // the printed trait's icon spelling; Groot and Rocket Raccoon print GUARDIAN.
        Assert.Equal("GUARDIAN", Cards.RequiredIdentityTrait(face));
        Assert.Contains("GUARDIAN", Cards.Traits("16001a"));
    }

    [Fact]
    [Rule("rr:overkill")]
    public void RocketRaccoonAttacksAMinionWithThreeMoreAndOverkill()
    {
        // 16019: "Interrupt: When Rocket Raccoon attacks a minion, he gets +3 ATK for that
        // attack. That attack gains overkill." ATK 1 + 3 against a 2-hit-point Hydra
        // Mercenary leaves 2 excess for Rhino (rr:overkill).
        var world = GrootBoards.Board();
        var rocket = GrootBoards.InPlay(world, "16019", DeckType.AlliesArea);
        var minion = GrootBoards.Engaged(world, "01121");
        var villain = GrootBoards.Villain(world);

        AllyBasicPowers.AllyPower(world, Cards, rocket, minion, BasicPowers.AttackVerb, []);
        GrootBoards.Drive(world, GrootBoards.Take(rocket));

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(2, villain.Damage);
    }

    [Fact]
    [Rule("rr:overkill")]
    public void RocketRaccoonGetsNothingAttackingTheVillain()
    {
        var world = GrootBoards.Board();
        var rocket = GrootBoards.InPlay(world, "16019", DeckType.AlliesArea);
        var villain = GrootBoards.Villain(world);
        var offered = new List<int>();

        AllyBasicPowers.AllyPower(world, Cards, rocket, villain, BasicPowers.AttackVerb, []);
        GrootBoards.Drive(world, GrootBoards.Take(rocket, offered));

        Assert.Empty(offered);
        Assert.Equal(1, villain.Damage);
    }

    [Fact]
    [Rule("rr:modifiers")]
    public void DeftFocusReducesOnlyTheNextSuperpowerCardThisTurn()
    {
        // 16024: "Hero Action: Exhaust Deft Focus → reduce the resource cost of the next
        // [superpower] card you play this turn by 1." Fighting Fit is not a SUPERPOWER
        // card and keeps its cost; "I. AM. GROOT!" costs 1 instead of 2.
        var world = GrootBoards.Board();
        var focus = GrootBoards.InPlay(world, "16024", DeckType.UpgradesArea);
        var fit = GrootBoards.Hand(world, "16014");
        var shout = GrootBoards.Hand(world, "16004");

        GrootBoards.Play(world, focus, []);
        GrootBoards.Drive(world, _ => null);

        var seat = world.Seats[0];
        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, seat, fit).Amount);
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, seat, shout).Amount);
        Assert.False(focus.Ready);
    }

    [Fact]
    [Rule("rr:lasting-effects")]
    public void DeftFocusExpiresWhenTheTurnEnds()
    {
        // Ruling 01084513ea5772f5: "this turn" is the turn in which Deft Focus was
        // exhausted; the unused reduction expires when that turn ends.
        var world = GrootBoards.Board();
        var focus = GrootBoards.InPlay(world, "16024", DeckType.UpgradesArea);
        var shout = GrootBoards.Hand(world, "16004");
        GrootBoards.Play(world, focus, []);
        GrootBoards.Drive(world, _ => null);

        world.Effects.Expire(Marvel.Rules.Timing.TimingPoints.EndOfTurn);

        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[0], shout).Amount);
    }
}
