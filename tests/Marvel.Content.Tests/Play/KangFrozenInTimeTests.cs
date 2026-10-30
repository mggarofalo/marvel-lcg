using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Frozen in Time (11016) and the full authored book on a nonpublic Rhino board;
// Kang's public admission stays closed. 11016: "Attach to your identity. Forced
// Interrupt: When attached character would ready, discard this card instead.
// ★ Boost: Attach to your identity."
public sealed class KangFrozenInTimeTests : CoreCardTestBase
{
    private const string FrozenInTime = "11016";
    private const string AllTiedUp = "02048";
    private const string ArcReactor = "01035";

    [Fact]
    [Rule("rr:attach-to")]
    [Rule("rr:replacement-effect.1")]
    [Rule("rr:end-of-player-phase.step.3")]
    public void RevealedFrozenInTimeKeepsItsIdentityExhaustedForOneReady()
    {
        // Revealed by player 1, not the first player, it attaches to that identity
        // (rr:attach-to). At the end of the player phase "each player
        // simultaneously readies all of their cards"; the replaced ready "is no
        // longer considered imminent" (rr:replacement-effect.1), so player 1's
        // identity stays exhausted while player 0's identity and player 1's ally
        // ready. Frozen in Time is discarded, and the next ready succeeds.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var host = world.Seats[1].IdentityCard;
        var other = world.Seats[0].IdentityCard;
        var ally = Ally(world, 1);
        var frozen = f.Reveal(FrozenInTime, player: 1);
        f.Finish();
        Assert.Equal(host.ObjectId, frozen.Area.Host);
        foreach (var card in new[] { host, other, ally }) card.Exhaust();

        ReadyCards(world, f);

        Assert.False(host.Ready);
        Assert.True(other.Ready);
        Assert.True(ally.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, frozen.Area.Type);

        ReadyCards(world, f);

        Assert.True(host.Ready);
    }

    [Fact]
    [Rule("rr:attach-to.2")]
    public void ReadyingAnotherCharacterLeavesFrozenInTimeAttached()
    {
        // "When attached character would ready": player 1's ally readying, while
        // the attached identity is already ready, is not that condition.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var ally = Ally(world, 1);
        var frozen = f.Reveal(FrozenInTime, player: 1);
        f.Finish();
        ally.Exhaust();

        ReadyCards(world, f);

        Assert.True(ally.Ready);
        Assert.Equal(world.Seats[1].IdentityCard.ObjectId, frozen.Area.Host);
    }

    [Fact]
    [Rule("rr:interrupt.3.1")]
    [Rule("rr:replacement-effect.1")]
    public void ACardEffectReadyingTheIdentityIsReplaced()
    {
        // Arc Reactor (01035): "Action: Exhaust Arc Reactor → ready your hero."
        // rr:interrupt.3.1: the "would" interrupt resolves before the ready
        // initiates. Arc Reactor's cost stays paid; the hero stays exhausted.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var hero = world.Seats[1].IdentityCard;
        var reactor = world.CreateCard(ArcReactor, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var frozen = f.Reveal(FrozenInTime, player: 1);
        f.Finish();
        hero.Exhaust();

        var action = Assert.Single(f.Runner.Actions(world, 1), ability => ability.Card == reactor.ObjectId);
        f.Runner.ExecuteAction(world, new PlayerAction(action, [], []));
        f.Finish();

        Assert.False(hero.Ready);
        Assert.False(reactor.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, frozen.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.2")]
    [Rule("rr:you-your")]
    [Rule("rr:attach-to.3.1")]
    public void TheBoostAttachesToTheAttackedPlayersIdentity()
    {
        // "★ Boost: Attach to your identity." rr:boost-boost-icon.2: only the text
        // beneath the divider is active while it resolves as a boost card. "Your"
        // is the player the activation is against: Rhino attacks player 1, not the
        // first player. rr:attach-to.3.1: this ability names the element, so the
        // card stays attached rather than being discarded with the boost cards.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var frozen = world.CreateCard(FrozenInTime, world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Attack(world, f.Villain, 1);

        var events = f.Finish();

        Assert.Equal(DeckType.UpgradesArea, frozen.Area.Type);
        Assert.Equal(world.Seats[1].IdentityCard.ObjectId, frozen.Area.Host);
        Assert.Single(events.OfType<BoostResolved>());
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:cannot.1")]
    [Rule("rr:interrupt.3.1")]
    public void AnIdentityThatCannotReadyNeverWouldReady()
    {
        // All Tied Up (02048): its host "cannot ready". rr:cannot.1: "the ability
        // with 'cannot' takes precedence". The identity is never about to ready, so
        // Frozen in Time's interrupt has no imminent condition and stays attached.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var host = world.Seats[1].IdentityCard;
        var tied = f.Reveal(AllTiedUp, player: 1);
        f.Finish();
        var frozen = f.Reveal(FrozenInTime, player: 1);
        f.Finish();
        host.Exhaust();

        ReadyCards(world, f);

        Assert.False(host.Ready);
        Assert.Equal(host.ObjectId, tied.Area.Host);
        Assert.Equal(host.ObjectId, frozen.Area.Host);
    }

    private static void ReadyCards(World world, RedSkullFixture f)
    {
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 1, 3));
        f.Finish();
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card Ally(World world, int player) =>
        world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));
}
