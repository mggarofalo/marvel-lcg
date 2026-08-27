using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Prelate Sidearm — the first card in the pool that reads a defeat off the
/// attack that caused it.
/// </summary>
/// <remarks>
/// <para>
/// "Attach to Unus. [star] <b>Forced Response</b>: After Unus attacks and
/// defeats an ally, place 1 threat on Gene Pool. <b>Hero Response</b>: After you
/// make a basic attack against Unus, spend [energy] [physical] resources →
/// discard this card."
/// </para>
/// <para>
/// <b>"Attacks and defeats" is one occurrence, not two joined up.</b>
/// <c>rr:triggering-condition.2</c> is explicit that "a single attack causing a
/// character to both take damage and be defeated" gets "a single interrupt
/// window and a single response window", so the defeat is a condition of the
/// damage step rather than an occurrence of its own — which is what MARVEL-248
/// built. The card answers the defeat and reads the attacker off the same
/// occurrence's subject; nothing has to remember what happened a moment ago.
/// </para>
/// <para>
/// The second half is Prelate Armor's, two letters different, and reaches the
/// board the same way.
/// </para>
/// </remarks>
public sealed class PrelateSidearmTests
{
    private const string Campaign = "unus";
    private const uint Seed = 12345;
    private const string BlackCat = "01002";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void UnusKillingADefendingAllyFeedsGenePoolFromBothCards()
    {
        // One occurrence, two forced responses to it: Gene Pool's three threat
        // for an ally dying by anything but consequential damage, and the
        // Sidearm's one for Unus having been what killed it. Four is the sum,
        // and it is the number that says the defeat reached both cards off a
        // single window rather than one of them missing it.
        var board = Dealt();
        long before = board.GenePool.Tokens.GetValueOrDefault("k_threat");

        var cat = Ally(board);
        Attacks(board, defender: cat.ObjectId);

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(before + 4, board.GenePool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:forced.5")]
    [Fact]
    public void TheFirstPlayerIsAskedWhichOfTheTwoForcedResponsesGoesFirst()
    {
        // "If two or more forced abilities would initiate at the same moment,
        // the first player determines the order in which the abilities
        // initiate, regardless of who controls the cards bearing those
        // abilities." Both cards belong to the scenario and the question is
        // still the player's — and it is not a question they may decline.
        var board = Dealt();
        var cat = Ally(board);
        var asked = Attacks(board, defender: cat.ObjectId, stopAtOrder: true);

        Assert.NotNull(asked);
        Assert.Equal(Question.Order, asked.Asking);
        Assert.False(asked.Cancellable);
        Assert.Equal(
            ["Gene Pool", "Prelate Sidearm"],
            asked.Affordances.Select(offer => offer.Label).Order(StringComparer.Ordinal));
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void UnusKillingTheHeroHimselfFeedsNeitherCard()
    {
        // "An **ally**" on Gene Pool and "an **ally**" on the Sidearm. The word
        // has to be doing something, and an identity defeated by the same
        // attack in the same window is what shows that it is — the occurrence
        // is identical in every other respect.
        var board = Dealt();
        long before = board.GenePool.Tokens.GetValueOrDefault("k_threat");
        var identity = board.World.Seats[0].IdentityCard;
        identity.TakeDamage(Damage.Health(board.World, Cards, identity) - 1);

        Attacks(board, defender: identity.ObjectId);

        // Defeated, and by the same attack in the same window: `rr:defeat.2`
        // takes an identity out of the game, which is what makes this the same
        // occurrence with one word of the card reading differently.
        Assert.Equal(DeckType.RemovedArea, identity.Area.Type);
        Assert.Equal(before, board.GenePool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:retaliate-x.1")]
    [Fact]
    public void AnAllyKilledByUnussRetaliateFeedsGenePoolAndNotTheSidearm()
    {
        // "After Unus **attacks** and defeats an ally." Retaliate is the
        // triggered ability "after **this character is attacked**, deal X damage
        // to the attacker" -- so the attacker is the ally, and Unus being
        // attacked is not Unus attacking. The role snapshot tells them apart:
        // Unus is the target here, while Sidearm requires him to be the actor.
        //
        // Gene Pool still eats: an ally died and not by consequential damage.
        var board = Dealt();
        long before = board.GenePool.Tokens.GetValueOrDefault("k_threat");
        var cat = Ally(board);
        cat.TakeDamage(1);

        var events = new List<GameEvent>();
        BasicPowers.AllyPower(
            board.World, Cards, cat, board.Unus, BasicPowers.AttackVerb, events);
        Agendas.Finish(board.World, Cards, board.Abilities);

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(before + 3, board.GenePool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:star-icon.2")]
    [Fact]
    public void AMinionKillingTheSameAllyTheSameWayFeedsGenePoolAndNotTheSidearm()
    {
        // "[star] **Forced Response**: After **Unus** attacks and defeats an
        // ally." The star is `rr:star-icon.2` -- the ability is about the
        // attached enemy -- and a minion's attack is the case that says so.
        // Every triggering condition of the occurrence is the same as Unus's
        // own attack; only the actor differs.
        var board = Dealt();
        long before = board.GenePool.Tokens.GetValueOrDefault("k_threat");
        var soldier = board.World.CreateCard(
            AuthoredCards.InfiniteSoldier,
            board.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        var cat = Ally(board);
        Attacks(board, defender: cat.ObjectId, attacker: soldier);

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(before + 3, board.GenePool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.4")]
    [Fact]
    public void TheSidearmAsksForEnergyAndPhysicalWhereTheArmourAsksForMental()
    {
        // Two attachments on one villain printing the same sentence with
        // different icons. `rr:resource.4` wants "the specified types in the
        // specified quantities", so the two are not interchangeable and the
        // affordance carries which is which.
        var board = Dealt();
        board.World.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);

        var events = new List<GameEvent>();
        BasicPowers.BasicAttack(board.World, Cards, 0, board.Unus, events);
        var asked = Sequence.Work(board.World, Cards, board.Abilities, events);

        var offer = Assert.Single(asked!.Affordances);
        Assert.Equal("Prelate Sidearm", offer.Label);
        Assert.Equal(["YR"], Assert.Single(offer.Costs!).Rule);
    }

    /// <summary>The dealt Unus board with the Sidearm attached to him.</summary>
    private static Board Dealt()
    {
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed,
            abilities);

        var sidearm = world.Cards.First(card => card.FaceId == AuthoredCards.PrelateSidearm);
        Reveal.Resolve(world, Cards, sidearm, 0, []);

        return new Board(
            world,
            abilities,
            world.TheCardIn(DeckType.VillainArea)!,
            world.Cards.First(card => card.FaceId == AuthoredCards.GenePool));
    }

    /// <summary>A ready ally in the player's area, whom Unus's attack kills.</summary>
    /// <remarks>
    /// Black Cat has two hit points; Unus prints ATK 2 and the Sidearm adds its
    /// printed <c>ATK+ 1</c>, so the attack is lethal without the board being
    /// arranged to make it so.
    /// </remarks>
    private static Card Ally(Board board) => board.World.CreateCard(
        BlackCat,
        board.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

    /// <summary>
    /// Unus attacking seat zero, answered to the end.
    /// </summary>
    /// <param name="board">The board.</param>
    /// <param name="defender">Who defends, by object id.</param>
    /// <param name="stopAtOrder">
    /// Whether to hand back the first ordering question instead of answering it.
    /// </param>
    /// <param name="attacker">Which enemy attacks, or Unus.</param>
    private static Prompt? Attacks(
        Board board, int defender, bool stopAtOrder = false, Card? attacker = null)
    {
        var events = new List<GameEvent>();
        board.World.Agenda.Add(new PhaseStep(
            Steps.Attack,
            1,
            2,
            Index: 0,
            Subject: (attacker ?? board.Unus).ObjectId,
            Seat: 0));

        var asked = Sequence.Work(board.World, Cards, board.Abilities, events);
        while (asked is not null)
        {
            if (stopAtOrder && asked.Asking == Question.Order)
            {
                return asked;
            }

            // The defence is the only question this board answers with anything
            // but the first thing offered: `rr:forced.5`'s ordering cannot be
            // declined, and every other window is optional.
            var input = asked.Asking switch
            {
                Question.Defender => Decision.Take(defender),
                Question.Order => Decision.Take(asked.Affordances[0].Id),
                _ => Decision.Decline,
            };

            Sequence.Answer(board.World, Cards, board.Abilities, asked, input, events);
            asked = Sequence.Work(board.World, Cards, board.Abilities, events);
        }

        return null;
    }

    private sealed record Board(
        World World, AbilityRunner Abilities, Card Unus, Card GenePool);
}
