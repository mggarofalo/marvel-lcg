using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The Unus scenario's encounter cards.
/// </summary>
/// <remarks>
/// <para>
/// Every card in this scenario is about one side scheme. Gene Pool sits on the
/// table from setup — it prints the setup keyword, so
/// <c>rr:appendix-ii-setup.step.11</c> puts it there — the main scheme feeds it
/// a threat every villain phase, half the encounter deck adds more, and both
/// Unus and the Infinite Soldier read the total and get harder.
/// </para>
/// <para>
/// So the scenario is a single escalating dial, and it is the first one this
/// engine can express: it needs constant abilities that re-read a condition
/// (<c>rr:ability.9</c>), which is what MARVEL-243 was for.
/// </para>
/// </remarks>
public sealed class UnusScenarioTests
{
    private const string Campaign = "unus";
    private const uint Seed = 12345;

    /// <summary>Spider-Man's ally, and the only one his deck has.</summary>
    private const string BlackCat = "01002";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:boost-boost-icon.2")]
    [Theory]
    [InlineData("WhenRevealed", 4)]
    [InlineData("Boost", 2)]
    public void CullingTheWeakFeedsGenePoolByADifferentAmountEachWay(string how, long placed)
    {
        // "**When Revealed:** Place 4 threat on Gene Pool. [star] **Boost:**
        // Place 2 threat on Gene Pool." One card, two abilities, and the
        // amounts differ — which is what makes this a test of `rr:boost-boost-icon.2`
        // keeping them apart rather than of either one alone.
        var (world, pool) = Board();
        var card = world.CreateCard("45070", world.AreaOf(DeckType.RevealingArea));
        long before = pool.Tokens.GetValueOrDefault("k_threat");
        var runner = AuthoredCards.Runner();

        if (how == "Boost")
        {
            runner.Boost(world, card, 0);
        }
        else
        {
            runner.WhenRevealed(world, card, 0);
        }
        Finish(world, runner);

        Assert.Equal(before + placed, pool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:when-defeated-abilities.2.1")]
    [Fact]
    public void EndlessRanksFeedsGenePoolAsItIsDefeated()
    {
        // "**When Defeated:** Place 3 threat on Gene Pool." `.2.1` keeps the
        // scheme in play while the ability resolves, which is why the threat
        // lands on Gene Pool rather than on a card that has already gone.
        var (world, pool) = Board();
        var ranks = world.CreateCard("45068", world.AreaOf(DeckType.SideSchemesArea));
        long before = pool.Tokens.GetValueOrDefault("k_threat");

        var runner = AuthoredCards.Runner();
        runner.WhenCardDefeated(
            world, ranks, new Defeated(ranks.ObjectId, -1, BasicPowers.ThwartVerb));
        Finish(world, runner);

        Assert.Equal(before + 3, pool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:replacement-effect")]
    [Rule("rr:you-your.5")]
    [Fact]
    public void WarWearyStunsYouOrHurtsYouAndNeverBoth()
    {
        // "You are stunned. If you were already stunned, take 2 damage
        // **instead**." The word does the work: a player who is already stunned
        // does not also gain a second status card, and one who is not takes no
        // damage.
        var (world, _) = Board();
        var hero = world.Seats[0].IdentityCard;
        var card = world.CreateCard("45073", world.AreaOf(DeckType.RevealingArea));

        AuthoredCards.Runner().WhenRevealed(world, card, 0);

        Assert.True(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(0, hero.Damage);

        AuthoredCards.Runner().WhenRevealed(world, card, 0);

        Assert.Equal(1, Statuses.Count(world, hero, Statuses.Stunned));
        Assert.Equal(2, hero.Damage);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:maximum-hit-points")]
    [Theory]
    [InlineData(0, false, false, 3)]
    [InlineData(3, true, false, 3)]
    [InlineData(6, true, true, 3)]
    [InlineData(9, true, true, 6)]
    public void TheInfiniteSoldierReadsGenePoolTheSameWayUnusDoes(
        long threat, bool quickstrike, bool surge, long health)
    {
        // "A character's maximum hit points is their base hit points plus or
        // minus all 'gets' hit point modifiers" active on it. Gene Pool's
        // threshold grants +3, taking the soldier's printed three to six while
        // the condition holds; the lower thresholds grant only keywords.
        var (world, pool) = Board();
        var soldier = world.CreateCard(
            "45069", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        pool.PlaceTokens("k_threat", threat - pool.Tokens.GetValueOrDefault("k_threat"));

        Assert.Equal(quickstrike ? 1 : 0, Modified(world, soldier, "quickstrike"));
        Assert.Equal(surge ? 1 : 0, Modified(world, soldier, "surge"));
        Assert.Equal(health, Damage.Health(world, Cards, soldier));
    }

    [Rule("rr:you-your.5")]
    [Rule("rr:ownership-and-control.2")]
    [Fact]
    public void TargetedForExterminationConfusesWhoeverThwartedIt()
    {
        // "**When Defeated:** The player who defeated this scheme confuses
        // their identity." Not the card's owner and not the first player: a
        // side scheme is defeated by the character that removed its last
        // threat, and the status lands on that player's identity.
        var (world, _) = Board();
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo(AuthoredCards.SpiderMan);
        var scheme = world.CreateCard("45074", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 1);

        BasicPowers.BasicThwart(world, Cards, 0, scheme, []);
        Agendas.Finish(world, Cards, AuthoredCards.Runner());

        Assert.True(Statuses.Has(world, hero, Statuses.Confused));
    }

    [Rule("rr:ownership-and-control.2")]
    [Fact]
    public void ItIsWhoeverThwartedItAndNotWhoeverGoesFirst()
    {
        // One seat cannot tell "the player who defeated it" apart from "the
        // first player", or from "the card's owner", or from "everybody". Two
        // can: the second player thwarts and the first is untouched.
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(
                Dealer.DealOrder(Setup, Campaign, ["spider_man", "spider_man"]), Cards),
            ["Spider-Man", "Spider-Woman"],
            Seed,
            AuthoredCards.Runner());
        var scheme = world.CreateCard("45074", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 1);

        foreach (var seat in world.Seats)
        {
            seat.IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        BasicPowers.BasicThwart(world, Cards, 1, scheme, []);
        Agendas.Finish(world, Cards, AuthoredCards.Runner());

        Assert.True(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Confused));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
    }

    [Rule("rr:triggering-condition.2")]
    [Rule("rr:consequential-damage")]
    [Fact]
    public void GenePoolIsFedByAnAllyKilledByRetaliateAndNotByOneKilledByItsOwnIcons()
    {
        // "**Forced Response:** After an ally is defeated by anything other
        // than consequential damage, place 3 threat here." One card, two
        // deaths, and only the negative condition tells them apart — so the
        // test is both halves or it is nothing. An assertion about the first
        // alone passes with the whole clause deleted.
        //
        // Both deaths are real paths. Gene Pool deals with 4 threat on it,
        // which is already enough for Unus's constant ability to grant him
        // retaliate 1, so an ally on its last hit point dies attacking him.
        // Black Cat prints one consequential damage icon under THW and none
        // under ATK, so the same ally on its last hit point dies thwarting.
        long killed = Fed(BasicPowers.AttackVerb);
        long ownIcons = Fed(BasicPowers.ThwartVerb);

        Assert.Equal(3, killed);
        Assert.Equal(0, ownIcons);
    }

    [Rule("rr:defeat")]
    [Fact]
    public void GenePoolIsNotFedByAMinionDyingTheSameWay()
    {
        // "After **an ally** is defeated". The word narrowing it has to be
        // doing something, and only a card that is not an ally, dying by
        // something that is not consequential damage, can show that it is: with
        // the clause deleted this card reads "after a card is defeated" and the
        // two ally tests above pass either way.
        var (world, pool) = Board();
        long before = pool.Tokens.GetValueOrDefault("k_threat");
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var soldier = world.CreateCard(
            AuthoredCards.InfiniteSoldier,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        soldier.TakeDamage(Damage.Health(world, Cards, soldier) - 1);

        BasicPowers.BasicAttack(world, Cards, 0, soldier, []);
        Agendas.Finish(world, Cards, AuthoredCards.Runner());

        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Equal(before, pool.Tokens.GetValueOrDefault("k_threat"));
    }

    /// <summary>
    /// How much threat Gene Pool gains from one ally dying one way.
    /// </summary>
    /// <remarks>
    /// Black Cat has two hit points and is put on her last one, so the single
    /// point she takes either way is what defeats her: retaliate from Unus when
    /// she attacks him, her own icon when she thwarts.
    /// </remarks>
    private static long Fed(string verb)
    {
        var (world, pool) = Board();
        long before = pool.Tokens.GetValueOrDefault("k_threat");
        var cat = world.CreateCard(
            BlackCat, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        cat.TakeDamage(1);

        Card target;
        if (verb == BasicPowers.AttackVerb)
        {
            target = world.TheCardIn(DeckType.VillainArea)!;
        }
        else
        {
            target = world.TheCardIn(DeckType.MainSchemesArea)!;

            // `rr:thwart.1.1` -- a character can only thwart a scheme that has
            // at least one threat to remove, and the main scheme is dealt at
            // zero. Not Gene Pool, whose threat is what is being measured.
            target.PlaceTokens("k_threat", 5);
        }

        BasicPowers.AllyPower(world, Cards, cat, target, verb, []);
        Agendas.Finish(world, Cards, AuthoredCards.Runner());

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        return pool.Tokens.GetValueOrDefault("k_threat") - before;
    }

    [Theory]
    [InlineData("unus")]
    [InlineData("unus_expert")]
    public void EverySeedEitherEndsOrStopsOnACardNobodyHasReadYet(string campaign)
    {
        // The list this file earns its keep by shrinking. Forty seeds with a
        // player who acts: some reach an ending, and the rest stop on one of
        // the three encounter cards still to author — never on anything else,
        // so a card that starts failing for a different reason is visible here
        // rather than lost in a count.
        //
        // **Shrinking it is the point, so it is kept short.** `45063` Prelate
        // Sidearm, `45064` Prelate Armor and `45065` Infinite Hunter have been
        // authored and no seed stops on them any more. Left in the list they
        // would still pass — the assertion only asks that the card stopped on
        // is *somewhere* in it — so a name that has stopped being reachable
        // quietly stops testing anything.
        //
        // `RealCardsGameTests` carried the same list for the Rhino board while
        // there were any, and what is left of it there is one assertion. This
        // is that list at the stage before.
        string[] unread = ["45066", "45067", "45072"];
        int ended = 0;

        for (uint seed = 1; seed <= 40; seed++)
        {
            string? stopped = Play(campaign, seed);
            if (stopped is null)
            {
                ended++;
                continue;
            }

            Assert.True(
                unread.Any(card => stopped.Contains($"'{card}'", StringComparison.Ordinal)),
                $"seed {seed} stopped on something else: {stopped}");
        }

        Assert.True(ended > 0, "no seed reached an ending at all");
    }

    /// <summary>Plays one seed out; answers with the message it stopped on.</summary>
    private static string? Play(string campaign, uint seed)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            seed,
            AuthoredCards.Runner(),
            expert: Setup.Campaign(campaign).Expert);
        var game = Game.Begin(world, Cards, AuthoredCards.Runner());
        var policy = new ActingPolicy((int)seed);

        try
        {
            for (int decisions = 0; game.Pending is not null; decisions++)
            {
                Assert.True(decisions < 3000, $"seed {seed} is still playing");
                game.Resolve(policy.Answer(game.Pending));
            }

            return null;
        }
        catch (RulesNotImplementedException stopped)
        {
            return stopped.Message;
        }
    }

    private static long Modified(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, Cards, world.Players);

    private static void Finish(World world, ICardAbilities abilities)
    {
        var asked = Sequence.Work(world, Cards, abilities, []);
        while (asked is not null)
        {
            Sequence.Answer(world, Cards, abilities, asked, Decision.Decline, []);
            asked = Sequence.Work(world, Cards, abilities, []);
        }
    }

    /// <summary>The Unus board, and the side scheme every card on it reads.</summary>
    private static (World World, Card GenePool) Board()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed,
            AuthoredCards.Runner());

        return (world, world.Cards.First(card => card.FaceId == AuthoredCards.GenePool));
    }
}
