using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangObligationFixture;

namespace Marvel.Content.Tests.Play;

// Depowered (11020) and the full authored book on a nonpublic Rhino board;
// Kang's public admission stays closed.
public sealed class KangDepoweredTests : CoreCardTestBase
{
    private const string Depowered = "11020";

    [Theory]
    [InlineData("01002")]
    [InlineData("01006")]
    [InlineData("01008")]
    [Rule("rr:cannot")]
    [Rule("rr:obligation.1")]
    [Rule("rr:identity-specific-card")]
    public void TheHolderCannotPlayAHeroSpecificAllySupportOrUpgrade(string face)
    {
        // 11020: "You cannot play hero-specific cards." rr:obligation.1: "you" is
        // the player whose play area holds it, here player 1, not the first player.
        // Player 0 may still play the same card, player 1 may still play the basic
        // Tenacity, and the prohibition ends when Depowered leaves play.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var depowered = f.Reveal(Depowered, player: 1);
        f.Finish();
        var held = InHand(world, face, 1);
        var basic = InHand(world, "01093", 1);
        var other = InHand(world, face, 0);
        for (int i = 0; i < 2; i++) { InHand(world, "01088", 0); InHand(world, "01088", 1); }

        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)), depowered.Area);
        Assert.Null(Price(world, 1, held));
        Assert.NotNull(Price(world, 1, basic));
        Assert.NotNull(Price(world, 0, other));

        Discard.Card(world, depowered, "test", []);

        Assert.NotNull(Price(world, 1, held));
    }

    [Fact]
    [Rule("rr:cannot")]
    [Rule("rr:player-turn.5")]
    public void TheHolderCannotPlayAHeroSpecificActionEvent()
    {
        // rr:player-turn.5 plays an event by triggering its Action. Swinging Web
        // Kick is hero-specific: offered to player 0, not to Depowered player 1.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        Obligation(world, Depowered, 1);
        var held = InHand(world, "01005", 1);
        var other = InHand(world, "01005", 0);
        for (int i = 0; i < 2; i++) { InHand(world, "01088", 0); InHand(world, "01088", 1); }

        Assert.False(Offers(f, held, 1));
        Assert.True(Offers(f, other, 0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot")]
    [Rule("rr:interrupt")]
    public void TheHolderCannotPlayAHeroSpecificInterruptEvent(bool depowered)
    {
        // Backflip is a hero-specific interrupt event. Rhino attacks player 1;
        // Backflip is offered in the damage window only without Depowered.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        if (depowered) Obligation(world, Depowered, 1);
        var backflip = InHand(world, "01003", 1);
        KangTemporalFixture.Attack(world, f.Villain, 1);

        bool offered = false;
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, f.Runner, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 60, prompt.Description);
            offered |= prompt.Affordances.Any(option => option.AnchorId == backflip.ObjectId);
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, world.Facts, f.Runner, prompt, Decision.Decline, events);
            prompt = Sequence.Work(world, world.Facts, f.Runner, events);
        }

        Assert.Equal(!depowered, offered);
        Assert.Equal(DeckType.HandsArea, backflip.Area.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:obligation.6")]
    public void TheAlterEgoActionDiscardsAHeroSpecificCardAndTheObligation(int player)
    {
        // 11020: "Alter-Ego Action: Discard a hero-specific card from your hand ->
        // discard this obligation." Discarding is not playing, so the prohibition
        // does not bar the cost. The basic Genius stays in hand.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var depowered = Obligation(world, Depowered, player);
        AlterEgo(world, player);
        var genius = InHand(world, "01089", player);
        var kick = InHand(world, "01005", player);

        Act(f, depowered, player, kick);

        Assert.Equal(DeckType.EncounterDiscardPile, depowered.Area.Type);
        Assert.Equal(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player), kick.Area);
        Assert.Equal([genius], world.Seats[player].Hand.Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("01089", true)]
    [InlineData("01005", false)]
    [Rule("rr:obligation.6")]
    public void TheActionNeedsAlterEgoAndAHeroSpecificCard(string inHand, bool alterEgo)
    {
        // A basic card cannot pay the cost, and a hero cannot use an Alter-Ego Action.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var depowered = Obligation(world, Depowered, 1);
        if (alterEgo) AlterEgo(world, 1);
        InHand(world, inHand, 1);

        Assert.False(Offers(f, depowered, 1));
    }

    private static CostOption? Price(World world, int player, Card card) =>
        CardPayment.Price(world, world.Facts, world.Seats[player], card,
            world.CardPlayAbilities.AttachmentTargets(world, card) is { Count: > 0 } hosts ? hosts[0] : null);
}
