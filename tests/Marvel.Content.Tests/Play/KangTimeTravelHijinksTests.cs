using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangObligationFixture;

namespace Marvel.Content.Tests.Play;

// Time-Travel Hijinks (11021) and the full authored book on a nonpublic Rhino
// board; Kang's public admission stays closed.
public sealed class KangTimeTravelHijinksTests : CoreCardTestBase
{
    private const string Hijinks = "11021";

    [Fact]
    [Rule("rr:ownership-and-control.8")]
    [Rule("rr:obligation.1")]
    public void RevealDiscardsTheRevealersHighestCostCardAndStoresItFacedown()
    {
        // 11021: "When Revealed: Discard the highest-cost card you control, then
        // place it facedown under this card." rr:ownership-and-control.8: "you
        // control" refers "only to cards in play currently under that player's
        // control". Player 1 reveals: Black Cat (cost 2) outranks Aunt May (cost 1),
        // and player 0's Avengers Mansion (cost 4) is not player 1's.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var cat = Ally(world, "01002", 1);
        var may = Support(world, "01006", 1);
        var mansion = Support(world, "01091", 0);

        var hijinks = f.Reveal(Hijinks, player: 1);
        f.Finish(prompt => TakeOnly(prompt, cat, holder: 1));

        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)), hijinks.Area);
        Assert.Equal([cat], Beneath(world, hijinks));
        Assert.False(cat.FaceUp);
        Assert.Empty(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1).Cards);
        Assert.Equal(DeckType.SupportsArea, may.Area.Type);
        Assert.Equal(DeckType.SupportsArea, mansion.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:obligation.5")]
    public void TheHolderChoosesAmongTiedHighestCostCards()
    {
        // rr:obligation.5: the revealing player places the obligation in their play
        // area, and "That player must decide how to resolve the obligation." Player
        // 1's two cost-3 supports tie, and player 1, not the first player, chooses.
        // The cost-1 Aunt May is not offered.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var helicarrier = Support(world, "01092", 1);
        var medTeam = Support(world, "01080", 1);
        Support(world, "01006", 1);
        var askers = new List<int>();

        var hijinks = f.Reveal(Hijinks, player: 1);
        f.Finish(prompt =>
        {
            askers.Add(prompt.Player);
            Assert.Equal(new[] { helicarrier.ObjectId, medTeam.ObjectId }.Order(),
                prompt.Affordances.Select(option => option.AnchorId).Order());
            return Decision.Take(prompt.Affordances.Single(option => option.AnchorId == medTeam.ObjectId).Id);
        });

        Assert.Equal([1], askers);
        Assert.Equal([medTeam], Beneath(world, hijinks));
        Assert.Equal(DeckType.SupportsArea, helicarrier.Area.Type);
    }

    [Fact]
    [Rule("rr:permanent.4.1")]
    public void APermanentCardIsNotRankedForTheDiscard()
    {
        // rr:permanent.4.1: a permanent card cannot be discarded, so it is left out
        // before ranking. Norn Stone (21187, Permanent) and Energy Channel (01018)
        // both cost 0; only Energy Channel is offered and discarded.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var stone = Upgrade(world, "21187a,21187b", 1);
        var channel = Upgrade(world, "01018", 1);

        var hijinks = f.Reveal(Hijinks, player: 1);
        f.Finish(prompt => TakeOnly(prompt, channel, holder: 1));

        Assert.Equal([channel], Beneath(world, hijinks));
        Assert.Equal(DeckType.UpgradesArea, stone.Area.Type);
    }

    [Fact]
    [Rule("rr:ability.3")]
    public void ARevealerControllingNoCardWithACostDiscardsNothing()
    {
        // Player 1 controls only an identity, which prints no cost and is not
        // discarded; the obligation stays in play with nothing beneath it.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        Ally(world, "01002", 0);

        var hijinks = f.Reveal(Hijinks, player: 1);
        f.Finish();

        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)), hijinks.Area);
        Assert.Empty(Beneath(world, hijinks));
        Assert.Equal(DeckType.HeroArea, world.Seats[1].IdentityCard.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:obligation.6")]
    [Rule("rr:leaves-play")]
    public void TheAlterEgoActionDiscardsAPrintedEnergyResourceTheObligationAndTheStoredCard(int player)
    {
        // 11021: "Alter-Ego Action: Discard a [energy] resource from your hand ->
        // discard this obligation. (Discard each facedown card under this
        // obligation.)" The stored Black Cat returns to its owner's discard pile.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var cat = Ally(world, "01002", player);
        var hijinks = f.Reveal(Hijinks, player);
        f.Finish(prompt => TakeOnly(prompt, cat, player));
        AlterEgo(world, player);
        var genius = InHand(world, "01089", player);
        var energy = InHand(world, "01088", player);

        Act(f, hijinks, player, energy);

        Assert.Equal(DeckType.EncounterDiscardPile, hijinks.Area.Type);
        Assert.Equal(new[] { cat.ObjectId, energy.ObjectId }.Order(), world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([genius], world.Seats[player].Hand.Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("01089")]
    [InlineData("01062")]
    [Rule("rr:wild-resource")]
    public void TheActionNeedsAPrintedEnergyResource(string inHand)
    {
        // Ruling 437680ffd6d92fb8: a discarded resource is not spent, so neither a
        // mental card nor the wild The Power of Justice pays.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var hijinks = Obligation(world, Hijinks, 1);
        AlterEgo(world, 1);
        InHand(world, inHand, 1);

        Assert.False(Offers(f, hijinks, 1));
    }

    private static Card Ally(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    private static Card Upgrade(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(player), cardOwner: player));

    private static Card Support(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(player), cardOwner: player));

    // The holder answers the choice of highest-cost card even when one card has it.
    private static Decision TakeOnly(Prompt prompt, Card card, int holder)
    {
        Assert.Equal(holder, prompt.Player);
        var only = Assert.Single(prompt.Affordances);
        Assert.Equal(card.ObjectId, only.AnchorId);
        return Decision.Take(only.Id);
    }
}
