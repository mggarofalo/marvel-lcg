using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class WaspFormCardTests : CoreCardTestBase
{
    private const string Genius = "01089";
    private const string Strength = "01090";
    private const string Energy = "01088";

    [Fact]
    [Rule("rr:form-change-form.1")]
    public void SwarmTacticsChangesToTheOtherHeroFormAndReadies()
    {
        // rr:form-change-form.1: "Once each round, during their turn, each player is permitted to change form by flipping their identity card."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var wasp = world.Seats[0].IdentityCard;
        wasp.Exhaust();
        Play(world, runner, "13020", Genius);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal("13001c", wasp.FaceId);
        Assert.True(wasp.Ready);
    }

    [Fact]
    [Rule("rr:response.1")]
    public void PerseveranceAnswersTheFormChangeWithATough()
    {
        // rr:response.1: "Players can only trigger response abilities on cards they control or on encounter cards."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var wasp = world.Seats[0].IdentityCard;
        var perseverance = world.CreateCard("13033", world.Seats[0].Hand);
        var payment = world.CreateCard(Genius, world.Seats[0].Hand);
        Play(world, runner, "13020", Genius);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(response.Affordances, item => item.AnchorId == perseverance.ObjectId);
        Sequence.Answer(world, Cards, runner, response, new Decision(offer.Id, [], [payment.ObjectId],
            Allocations: ResourcePayment.Allocate(Assert.Single(offer.Costs!), [payment.ObjectId])), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(1, Statuses.Count(world, wasp, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, perseverance.Area.Type);
    }

    [Theory]
    [InlineData(Strength, 4)]
    [InlineData(Energy, 3)]
    [Rule("rr:attack-player-ability-type.1")]
    public void SurpriseAttackDealsOneMoreWhenPaidWithAPhysicalResource(string resource, int damage)
    {
        // rr:attack-player-ability-type.1: "A hero or ally can use their basic attack power to attack an enemy."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var surprise = world.CreateCard("13014", world.Seats[0].Hand);
        var payment = world.CreateCard(resource, world.Seats[0].Hand);
        Play(world, runner, "13020", Genius);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(response.Affordances, item => item.AnchorId == surprise.ObjectId);
        Sequence.Answer(world, Cards, runner, response,
            new Decision(offer.Id, [], [payment.ObjectId],
                Allocations: ResourcePayment.Allocate(Assert.Single(offer.Costs!), [payment.ObjectId])), []);
        while (Sequence.Work(world, Cards, runner, []) is { } prompt)
            Sequence.Answer(world, Cards, runner, prompt,
                prompt.Affordances.Any(item => item.AnchorId == villain.ObjectId)
                    ? Decision.Take(villain.ObjectId) : Decision.Decline, []);
        Assert.Equal(damage, villain.Damage);
    }

    [Fact]
    [Rule("rr:interrupt.1")]
    public void RapidGrowthTurnsTinyGiantAndAddsTwoToThatAttack()
    {
        // rr:interrupt.1: "Players can only trigger interrupt abilities on cards they control or on encounter cards."
        // Ruling 9cfb9119e5c7f0d7: the interrupt boosts the basic power being used.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var wasp = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var growth = world.CreateCard("13005", world.Seats[0].Hand);
        var payment = world.CreateCard(Energy, world.Seats[0].Hand);
        BasicPowerInitiation.BasicAttack(world, Cards, 0, villain, []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(interrupt.Affordances, item => item.AnchorId == growth.ObjectId);
        Sequence.Answer(world, Cards, runner, interrupt,
            new Decision(offer.Id, [], [payment.ObjectId],
                Allocations: ResourcePayment.Allocate(Assert.Single(offer.Costs!), [payment.ObjectId])), []);
        while (Sequence.Work(world, Cards, runner, []) is { } prompt)
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
        Assert.Equal("13001c", wasp.FaceId);
        // Giant ATK 2 plus 2 for this use.
        Assert.Equal(4, villain.Damage);
        Assert.False(wasp.Ready);
    }

    [Theory]
    [InlineData("13001c")]
    [InlineData("13001a")]
    [Rule("rr:thwart.1")]
    public void GiantHelpDividesFourWhileGiantAndOtherwiseRemovesThree(string face)
    {
        // rr:thwart.1: "A hero or ally can use their basic thwart power to thwart a scheme."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board(face);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var side = WaspBoards.Side(world, "01107", 3);
        Play(world, runner, "13003", Genius);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        if (face == "13001c")
        {
            var offer = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(offer.Id) with
                { Targets = [main.ObjectId, main.ObjectId, side.ObjectId, side.ObjectId] }, []);
            Sequence.Finish(world, Cards, runner, []);
            Assert.Equal(3, main.Tokens["k_threat"]);
            Assert.Equal(1, side.Tokens["k_threat"]);
        }
        else
        {
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(main.ObjectId), []);
            Sequence.Finish(world, Cards, runner, []);
            Assert.Equal(2, main.Tokens["k_threat"]);
            Assert.Equal(3, side.Tokens["k_threat"]);
        }
    }

    private static void Play(World world, Marvel.Cards.Run.AbilityRunner runner, string face, string resource)
    {
        // Team-Up (Ant-Man and Wasp) needs both named characters in play (rr:team-up.1).
        if (face == "13020" && !world.Cards.Any(card => card.FaceId == "13002"))
            WaspBoards.InPlay(world, "13002", DeckType.AlliesArea);
        var card = world.CreateCard(face, world.Seats[0].Hand);
        var payment = world.CreateCard(resource, world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
    }
}
