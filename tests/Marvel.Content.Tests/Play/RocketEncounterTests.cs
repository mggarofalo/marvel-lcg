using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Crisis on Halfworld and Rocket Raccoon's nemesis set on synthetic boards.
public sealed class RocketEncounterTests : CoreCardTestBase
{
    private const string HardToKeepDown = "01104";

    [Fact]
    [Rule("rr:obligation.1")]
    public void CrisisOnHalfworldMayFlipThenExhaustTheAlterEgoToLeaveTheGame()
    {
        // 16053: "You may flip to alter-ego form. Choose: • Exhaust your alter-ego →
        // remove Crisis on Halfworld from the game."
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var cannon = RocketBoards.Upgrade(world, "16036", charge: 2);
        var crisis = RocketBoards.Reveal(world, "16053");

        Answer(world, prompt => prompt.Affordances[0]);
        Answer(world, prompt => prompt.Affordances[0]);
        Assert.Equal("16029b", rocket.FaceId);
        Sequence.Finish(world, world.Facts, GrootBoards.Runner(world), []);

        Assert.False(rocket.Ready);
        Assert.Equal(DeckType.RemovedArea, crisis.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, cannon.Area.Type);
    }

    [Fact]
    [Rule("rr:obligation.1")]
    public void CrisisOnHalfworldOtherwiseDiscardsTheHighestCostUpgrade()
    {
        // "• Discard the highest cost upgrade you control. ... Discard this obligation." In
        // hero form the alter-ego option has no target. Particle Cannon costs 3, the
        // pistol 1.
        var world = RocketBoards.Board();
        var cannon = RocketBoards.Upgrade(world, "16036", charge: 2);
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 3);
        var crisis = RocketBoards.Reveal(world, "16053");

        Answer(world, prompt => prompt.Affordances[^1]);
        Answer(world, prompt => Assert.Single(prompt.Affordances));
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(DeckType.DiscardPile, cannon.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, pistol.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, crisis.Area.Type);
        Assert.Equal(12, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Fact]
    [Rule("rr:first-player.1")]
    public void CrisisOnHalfworldLetsTheFirstPlayerBreakATieForHighestCost()
    {
        // Particle Cannon and Rocket Launcher both cost 3; the first player chooses
        // which is discarded, as for Oversized Hands (07042).
        var world = RocketBoards.Board();
        var cannon = RocketBoards.Upgrade(world, "16036", charge: 2);
        var launcher = RocketBoards.Upgrade(world, "16037", charge: 2);
        RocketBoards.Reveal(world, "16053");

        Answer(world, prompt => prompt.Affordances[^1]);
        Answer(world, prompt => Assert.Single(prompt.Affordances));
        GrootBoards.Drive(world, GrootBoards.Take(launcher));

        Assert.Equal(DeckType.DiscardPile, launcher.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, cannon.Area.Type);
    }

    [Fact]
    [Rule("rr:surge")]
    public void CrisisOnHalfworldGainsSurgeWhenNoUpgradeWasDiscarded()
    {
        // "If no upgrade was discarded this way, this card gains surge."
        var world = RocketBoards.Board();
        var crisis = RocketBoards.Reveal(world, "16053");

        Answer(world, prompt => prompt.Affordances[^1]);
        Answer(world, prompt => Assert.Single(prompt.Affordances));
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(11, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, crisis.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:attach-to")]
    public void BlackjacksBazookaAttachesToBlackjackIfAbleOtherwiseTheVillain(bool blackjack)
    {
        // 16056: "Attach to Blackjack O'Hare, if able. If you cannot, attach to the
        // villain." It prints ATK +2: Blackjack has ATK 1, Rhino 2.
        var world = RocketBoards.Board("16029b");
        var host = blackjack ? GrootBoards.Engaged(world, "16055") : GrootBoards.Villain(world);
        var bazooka = RocketBoards.Reveal(world, "16056");
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(host.ObjectId, bazooka.Area.Host);
        Assert.Equal(blackjack ? 3 : 4, Modified(world, host, "attack"));
    }

    [Fact]
    [Rule("rr:attachment")]
    public void BlackjacksBazookaIsDiscardedForThreeMentalResources()
    {
        // "Hero Action: Spend [mental][mental][mental] resources → Discard this card."
        var world = RocketBoards.Board();
        var villain = GrootBoards.Villain(world);
        var bazooka = RocketBoards.Reveal(world, "16056");
        GrootBoards.Drive(world, _ => null);
        var paying = new[] { GrootBoards.Hand(world, RocketBoards.Genius), GrootBoards.Hand(world, "16030") };

        GrootBoards.Play(world, bazooka, paying);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(DeckType.EncounterDiscardPile, bazooka.Area.Type);
        Assert.Equal(2, Modified(world, villain, "attack"));
    }

    [Fact]
    [Rule("rr:tough")]
    public void PlanetaryInvasionRevealsTheFirstMinionDiscardedAndGivesItTough()
    {
        // 16057: "Discard cards from the top of the encounter deck until you discard a
        // minion. Reveal that minion, then give it a tough status card." Hydra Mercenary
        // has no When Revealed ability; revealed, it engages Rocket Raccoon.
        var world = RocketBoards.Board("16029b");
        var encounter = world.AreaOf(DeckType.EncounterDeck);
        var minion = world.CreateCard("01101", encounter);
        world.CreateCard(HardToKeepDown, encounter);
        var invasion = RocketBoards.Reveal(world, "16057");
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(DeckType.EncounterDiscardPile, invasion.Area.Type);
        Assert.Equal(12, encounter.Cards.Count);
    }

    [Fact]
    [Rule("rr:quickstrike")]
    [Rule("rr:tough")]
    public void PlanetaryInvasionGivesToughBeforeAQuickstrikeAttackResolves()
    {
        // Ruling ef63f3668700ef73: the reveal resolves Vulture's quickstrike, initiating
        // its attack; "the minion is given a Tough status card immediately after this",
        // before the attack's steps resolve. Vulture's 3 against undefended Rocket.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var encounter = world.AreaOf(DeckType.EncounterDeck);
        var vulture = world.CreateCard("01167", encounter);
        bool toughDuringAttack = false;
        RocketBoards.Reveal(world, "16057");
        GrootBoards.Drive(world, prompt =>
        {
            toughDuringAttack |= world.Attack is not null && Statuses.Has(world, vulture, Statuses.Tough);
            return null;
        });

        Assert.True(toughDuringAttack);
        Assert.Equal(DeckType.EngagedEnemiesArea, vulture.Area.Type);
        Assert.True(Statuses.Has(world, vulture, Statuses.Tough));
        Assert.Equal(3, rocket.Damage);
    }

    [Fact]
    [Rule("rr:encounter-deck.2")]
    public void PlanetaryInvasionIsFulfilledWhenTheEncounterDeckEmptiesWithoutAMinion()
    {
        // rr:encounter-deck.2: "If the encounter deck is emptied this way, that card
        // ability is considered to be fulfilled. Do not continue the discard effect with
        // the newly shuffled encounter deck."
        var world = RocketBoards.Board("16029b");
        RocketBoards.Reveal(world, "16057");
        GrootBoards.Drive(world, _ => null);

        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.DoesNotContain(world.Cards, card => Statuses.Has(world, card, Statuses.Tough));
    }

    private static void Answer(World world, Func<Prompt, Affordance> choose)
    {
        var runner = GrootBoards.Runner(world);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(choose(prompt).Id), []);
    }
}
