using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class WaspStingTests : CoreCardTestBase
{
    private const string Mercenary = "01101";
    private const string Whiplash = "01172";

    [Fact]
    [Rule("rr:guard.1")]
    [Rule("rr:retaliate-x.1")]
    public void GiantStingDividesFourDamageAndEachRecipientIsAttacked()
    {
        // rr:guard.1: "The guard keyword is equivalent to the following constant ability: “The engaged player cannot attack any villain.”"
        // rr:retaliate-x.1: "The retaliate X keyword is equivalent to the following triggered ability: “Forced Response: After this character is attacked, deal X damage to the attacker.”"
        // pack:mc13:wasp-hero-pack#rules-clarifications: "Each enemy is considered to
        // have been attacked." Guard keeps the villain out of the pool.
        var world = WaspBoards.Board("13001c");
        var runner = AuthoredCards.Runner();
        var guard = WaspBoards.Engaged(world, Mercenary);
        var retaliator = WaspBoards.Engaged(world, Whiplash);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var wasp = world.Seats[0].IdentityCard;
        var sting = Play(world, runner, giant: true);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.True(offer.Targets!.AllowRepeated);
        Assert.Equal(4, offer.Targets.Min);
        Assert.Equal(4, offer.Targets.Max);
        Assert.Equal([guard.ObjectId, retaliator.ObjectId], offer.Targets.Legal.Order());
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(offer.Id) with
            { Targets = [guard.ObjectId, guard.ObjectId, guard.ObjectId, retaliator.ObjectId] }, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, guard.Area.Type);
        Assert.Equal(1, retaliator.Damage);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(1, wasp.Damage);
        Assert.Equal(DeckType.DiscardPile, sting.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.2.1")]
    public void GiantStingMayGiveTheWholeTotalToOneEnemy()
    {
        // rr:attack-player-ability-type.2.1: "An ability labeled as an attack is considered a single attack, even if that attack deals multiple instances of damage."
        // Printed 13006: "deal a total of 4 damage divided among enemies as you choose."
        // Ruling 116e678d0ad83200: an enemy given no damage is simply not attacked, so
        // the whole total may go to one enemy as one attack.
        var world = WaspBoards.Board("13001c");
        var runner = AuthoredCards.Runner();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Play(world, runner, giant: true);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal([villain.ObjectId], offer.Targets!.Legal);
        Assert.False(offer.Targets.Allows([villain.ObjectId, villain.ObjectId, villain.ObjectId]));
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(offer.Id) with
            { Targets = [villain.ObjectId, villain.ObjectId, villain.ObjectId, villain.ObjectId] }, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(4, villain.Damage);
    }

    [Theory]
    [InlineData("13001a", false, true)]
    [InlineData("13001c", true, false)]
    [InlineData("13001b", false, false)]
    [Rule("rr:ability.8")]
    public void EachStingActionIsOfferedOnlyInItsForm(string face, bool giant, bool tiny)
    {
        // rr:ability.8: "The resolution of the following ability types is optional: “Action”, “Interrupt”, “Response”, “Resource”."
        var world = WaspBoards.Board(face);
        var runner = AuthoredCards.Runner();
        var sting = world.CreateCard("13006", world.Seats[0].Hand);
        world.CreateCard("13021", world.Seats[0].Hand);
        var offers = runner.Actions(world, 0).Where(a => a.Card == sting.ObjectId).ToArray();
        Assert.Equal((giant ? 1 : 0) + (tiny ? 1 : 0), offers.Length);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void TinyStingDealsFiveToOneEnemy()
    {
        // rr:attack-player-ability-type.1: "A hero or ally can use their basic attack power to attack an enemy."
        var world = WaspBoards.Board("13001a");
        var runner = AuthoredCards.Runner();
        var retaliator = WaspBoards.Engaged(world, Whiplash);
        Play(world, runner, giant: false);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(retaliator.ObjectId), []);
        // Small but Mighty (13001a) answers the event defeating a minion.
        var response = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var small = Assert.Single(response.Affordances, a => a.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        Sequence.Answer(world, Cards, runner, response, Decision.Take(small.Id), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, retaliator.Area.Type);
        // rr:retaliate-x.2: the character "must be in play after the attack resolves".
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    private static Card Play(World world, AbilityRunner runner, bool giant)
    {
        var sting = world.CreateCard("13006", world.Seats[0].Hand);
        var energy = world.CreateCard("13021", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == sting.ObjectId);
        Assert.Equal(giant ? 0 : 1, action.Ordinal);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], []));
        return sting;
    }
}
