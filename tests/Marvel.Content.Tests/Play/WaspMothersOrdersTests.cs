using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class WaspMothersOrdersTests : CoreCardTestBase
{
    private const string MothersOrders = "13027";
    private const string Energy = "01088";
    private const string PymParticles = "13007";

    [Fact]
    [Rule("rr:cost.10")]
    public void AHeroSpendsOneResourceWithTheAttacksExhaustion()
    {
        // rr:cost.10: additional costs are paid "simultaneously with the cost that is
        // being added to".
        var (world, runner, villain) = Board();
        var wasp = world.Seats[0].IdentityCard;
        var energy = world.CreateCard(Energy, world.Seats[0].Hand);
        Assert.Equal(1, BasicAttackCosts.Resources(world, wasp));
        BasicPowerInitiation.BasicAttack(world, Cards, 0, villain, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var cost = Assert.Single(prompt.Affordances);
        Assert.Equal(PublicDecisionKind.Choice, prompt.PublicKind);
        Assert.Equal("1", Assert.Single(cost.Costs!).Cost);
        Assert.True(wasp.Ready);
        string digest = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner, prompt,
            new Decision(cost.Id, [], []), []));
        Assert.Equal(digest, world.Digest().Canonical());
        Sequence.Answer(world, Cards, runner, prompt, new Decision(cost.Id, [], [energy.ObjectId]), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.False(wasp.Ready);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(1, villain.Damage);
    }

    [Fact]
    [Rule("rr:cost.10")]
    public void ASpentCardsResponseResolvesBeforeTheAttack()
    {
        // rr:cost.10: "Some card abilities may reference an “additional cost.” A player must pay all additional costs simultaneously with the cost that is being added to."
        // Pym Particles answers the completed cost; the attack follows its window.
        var (world, runner, villain) = Board();
        var particles = world.CreateCard(PymParticles, world.Seats[0].Hand);
        int hand = world.Seats[0].Hand.Cards.Count;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, villain, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances).Id, [], [particles.ObjectId]), []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(response.Affordances, item => item.AnchorId == particles.ObjectId);
        Assert.Equal(0, villain.Damage);
        Sequence.Answer(world, Cards, runner, response, Decision.Take(offer.Id), []);
        Sequence.Finish(world, Cards, runner, []);
        // Tiny draws 1 after the spent card left the hand.
        Assert.Equal(hand, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(1, villain.Damage);
    }

    [Fact]
    [Rule("rr:cost.3")]
    public void WithoutAResourceTheHeroCannotAttackAndAlliesAreUnaffected()
    {
        // rr:cost.3: "To pay a resource cost, a player spends resources that they generate by discarding cards from their hand or by using “Resource” card abilities."
        var (world, _, _) = Board();
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        var ally = WaspBoards.InPlay(world, "01067", DeckType.AlliesArea);
        Assert.False(BasicAttackCosts.CanPay(world, world.Seats[0].IdentityCard));
        Assert.Equal(0, BasicAttackCosts.Resources(world, ally));
        Assert.True(BasicAttackCosts.CanPay(world, ally));
    }

    [Fact]
    public void AnAttackOwingBothDiscardAndResourceCostsRaises()
    {
        // Engine choice: no admitted card combines the two additional costs, so the
        // engine names the combination instead of choosing how shared cards pay.
        var (world, _, _) = Board();
        var wasp = world.Seats[0].IdentityCard;
        world.CreateCard(Energy, world.Seats[0].Hand);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: BasicAttackCosts.DiscardField, Amount: 1, Card: wasp.ObjectId, Affects: wasp.ObjectId));
        Assert.Throws<RulesNotImplementedException>(() => BasicAttackCosts.CanPay(world, wasp));
    }

    private static (World World, Marvel.Cards.Run.AbilityRunner Runner, Card Villain) Board()
    {
        var world = WaspBoards.Board("13001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        WaspBoards.Side(world, MothersOrders, 2);
        return (world, runner, world.TheCardIn(DeckType.VillainArea)!);
    }
}
