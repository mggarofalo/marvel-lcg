using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class WaspEncounterCardTests : CoreCardTestBase
{
    private const string Genius = "01089";
    private const string Energy = "01088";
    private const string Strength = "01090";

    [Fact]
    [Rule("rr:obligation.1")]
    public void RedDreamsMayFlipThenExhaustNadiaToLeaveTheGame()
    {
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var identity = world.Seats[0].IdentityCard;
        var dreams = Reveal(world, runner, "13026");
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[0].Id), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal("13001b", identity.FaceId);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(choice.Affordances[0].Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(identity.Ready);
        Assert.Equal(DeckType.RemovedArea, dreams.Area.Type);
    }

    [Fact]
    [Rule("rr:obligation.1")]
    public void RedDreamsOtherwiseDiscardsPrintedMentalCardsAndDealsOne()
    {
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var identity = world.Seats[0].IdentityCard;
        var genius = world.CreateCard(Genius, world.Seats[0].Hand);
        var energy = world.CreateCard(Energy, world.Seats[0].Hand);
        var dreams = Reveal(world, runner, "13026");
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[^1].Id), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal("13001a", identity.FaceId);
        // A hero cannot exhaust Nadia Van Dyne, so only the second option remains.
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(choice.Affordances[^1].Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, genius.Area.Type);
        Assert.Same(world.Seats[0].Hand, energy.Area);
        Assert.Equal(1, identity.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, dreams.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:forced.1")]
    public void BeetleDefeatedAsksForAPhysicalResourceOrReturnsToTheEncounterDeck(bool pay)
    {
        // Ruling 30744014e3139d48: the defeating player chooses.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001c");
        var beetle = WaspBoards.Engaged(world, "13028");
        beetle.TakeDamage(3);
        var strength = world.CreateCard(Strength, world.Seats[0].Hand);
        int encounter = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, beetle, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        if (pay)
        {
            var spend = Assert.Single(prompt.Affordances, item => item.Costs is { Count: > 0 });
            Sequence.Answer(world, Cards, runner, prompt, new Decision(spend.Id, [], [strength.ObjectId],
                Allocations: ResourcePayment.Allocate(Assert.Single(spend.Costs!), [strength.ObjectId])), []);
        }
        else Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, item => item.Costs is not { Count: > 0 }).Id), []);
        while (Sequence.Work(world, Cards, runner, []) is { } response)
            Sequence.Answer(world, Cards, runner, response, Decision.Decline, []);
        Assert.Equal(pay ? DeckType.EncounterDiscardPile : DeckType.EncounterDeck, beetle.Area.Type);
        Assert.Equal(pay ? DeckType.DiscardPile : DeckType.HandsArea, strength.Area.Type);
        Assert.Equal(encounter + (pay ? 0 : 1), world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Fact]
    [Rule("rr:surge.1")]
    public void BeetleManiaSurgesWithoutBeetle()
    {
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        int dealt = Dealt(world);
        Reveal(world, runner, "13030");
        while (Sequence.Work(world, Cards, runner, []) is { } prompt)
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
        // Surge deals the revealing player one more encounter card.
        Assert.Equal(dealt + 1, Dealt(world));
    }

    [Fact]
    [Rule("rr:activation")]
    public void BeetleManiaMakesBeetleAttackWithOneMore()
    {
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var identity = world.Seats[0].IdentityCard;
        WaspBoards.Engaged(world, "13028");
        int dealt = Dealt(world);
        Reveal(world, runner, "13030");
        while (Sequence.Work(world, Cards, runner, []) is { } prompt)
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
        // Undefended: Beetle's printed ATK 1 plus 1.
        Assert.Equal(2, identity.Damage);
        Assert.Equal(dealt, Dealt(world));
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 0)]
    [Rule("rr:defend-defense.2")]
    public void RapidGrowthOnABasicDefenseTurnsGiantAndAddsTwoDefense(bool growth, int damage)
    {
        // Beetle Mania: Beetle attacks with ATK 1 + 1 = 2. Tiny DEF 2 already absorbs
        // it; Rapid Growth turns Wasp Giant before the damage.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var identity = world.Seats[0].IdentityCard;
        WaspBoards.Engaged(world, "13028");
        var card = growth ? world.CreateCard("13005", world.Seats[0].Hand) : null;
        var payment = growth ? world.CreateCard(Energy, world.Seats[0].Hand) : null;
        bool offered = false;
        Reveal(world, runner, "13030");
        while (Sequence.Work(world, Cards, runner, []) is { } prompt)
        {
            if (card is not null && prompt.Affordances.SingleOrDefault(item => item.AnchorId == card.ObjectId) is { } offer)
            {
                offered = true;
                Sequence.Answer(world, Cards, runner, prompt, new Decision(offer.Id, [], [payment!.ObjectId],
                    Allocations: ResourcePayment.Allocate(Assert.Single(offer.Costs!), [payment.ObjectId])), []);
            }
            else if (prompt.Affordances.SingleOrDefault(item => item.AnchorId == identity.ObjectId) is { } defend)
                Sequence.Answer(world, Cards, runner, prompt, Decision.Take(defend.Id), []);
            else Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
        }
        Assert.Equal(growth, offered);
        Assert.Equal(growth ? "13001c" : "13001a", identity.FaceId);
        Assert.Equal(damage, identity.Damage);
        Assert.False(identity.Ready);
        // The bonus lasts only for that attack.
        Assert.Equal(growth ? 3 : 2, StateFields.Modified(world, identity, "defense", world.Facts, world.Players));
    }

    [Fact]
    [Rule("rr:response.1")]
    public void LieInWaitDiscardsToAttackTheMinionThatEngaged()
    {
        Card? lie = null, minion = null;
        var (game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("13001a");
            lie = board.CreateCard("13017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDeck));
            // Rhino's attack flips the top card as a boost; the minion is dealt after it.
            board.CreateCard("01107", board.AreaOf(DeckType.EncounterDeck));
        }, heroes: ["wasp"]);
        // Declining the turn prompt ends the player phase.
        game.Resolve(Decision.Decline);
        bool responded = false;
        var trail = new List<string>();
        for (int step = 0; step < 60 && !responded && game.Pending is { } pending; step++)
        {
            trail.Add(pending.Trigger + ":" + string.Join("/", pending.Affordances.Select(a => a.Verb)));
            if (pending.Affordances.SingleOrDefault(a => a.AnchorId == lie!.ObjectId) is { } offer)
            {
                Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
                game.Resolve(Decision.Take(offer.Id));
                responded = true;
            }
            else game.Resolve(pending.Cancellable || pending.Affordances.Count == 0 ? Decision.Decline
                : Decision.Take(pending.Affordances[0].Id) with
                    { Targets = [.. pending.Affordances[0].Targets?.Legal.Take(pending.Affordances[0].Targets!.Min) ?? []] });
        }
        Assert.True(responded, $"minion in {minion!.Area.Type}; " + string.Join(" > ", trail));
        Assert.Equal(DeckType.DiscardPile, lie!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, minion!.Area.Type);
    }

    [Theory]
    [InlineData(Strength, 3)]
    [InlineData(Genius, 2)]
    [Rule("rr:response.1")]
    public void ThorDealsOneMoreWhenPaidWithAPhysicalResource(string resource, int damage)
    {
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var seat = world.Seats[0];
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var thor = world.CreateCard("13011", seat.Hand);
        int[] payment = [world.CreateCard(resource, seat.Hand).ObjectId, world.CreateCard(Energy, seat.Hand).ObjectId];
        CardPlay.Play(world, world.Facts, runner, seat, thor, payment, []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, response,
            Decision.Take(Assert.Single(response.Affordances, item => item.AnchorId == thor.ObjectId).Id), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(damage, villain.Damage);
        Assert.Equal(DeckType.AlliesArea, thor.Area.Type);
    }

    private static Card Reveal(World world, Marvel.Cards.Run.AbilityRunner runner, string face)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        world.Abilities = runner;
        runner.ExecuteReveal(world, card, 0);
        return card;
    }

    private static int Dealt(World world) => world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;
}
