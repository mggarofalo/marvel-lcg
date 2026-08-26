using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// An effect written before the card it acts on exists.
/// </summary>
/// <remarks>
/// <para>
/// "Rhino attacks you. <b>If a character is damaged by this attack, that
/// character is stunned.</b>" The second sentence cannot name anybody when it
/// is written: the attack has not happened, and who defends is a question that
/// has not been asked. So the effect is made with no target and the occurrence
/// names one when it comes due.
/// </para>
/// <para>
/// A <b>delayed effect</b> and not a response. <c>rr:delayed-effect.1</c>
/// resolves it "immediately after [its] future condition occurs or becomes
/// true, and <b>before responses to that point or condition may be used</b>",
/// and <c>.2</c> says it "is not treated as a new triggered ability". So it does
/// not go in a window and nobody is asked about it.
/// </para>
/// <para>
/// <b>Damaged, not attacked.</b> <c>rr:tough.3</c> — a character whose tough
/// status card ate the damage "is not considered to have taken damage" — so the
/// "if" has a false branch, and it is reachable.
/// </para>
/// </remarks>
public sealed class StampedeTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:delayed-effect.1")]
    [Fact]
    public void TheDamagedCharacterIsStunned()
    {
        var world = Hero();
        var identity = world.Seats[0].IdentityCard;

        Play(world, AuthoredCards.Stampede);

        Assert.True(identity.Damage > 0, "the attack landed");
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:tough.3")]
    [Fact]
    public void AToughCharacterTakesNoDamageAndIsNotStunned()
    {
        // The false branch, and the reason the card says "damaged" rather than
        // "attacked". The tough card eats the whole attack, so nobody was
        // damaged and nobody is stunned -- and the tough card is spent, which
        // is what makes this different from the attack never happening.
        var world = Hero();
        var identity = world.Seats[0].IdentityCard;
        Statuses.Give(world, identity, Statuses.Tough);

        Play(world, AuthoredCards.Stampede);

        Assert.Equal(0, identity.Damage);
        Assert.False(Statuses.Has(world, identity, Statuses.Tough));
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:form-change-form")]
    [Fact]
    public void AnAlterEgoSurgesInsteadOfBeingAttacked()
    {
        var world = Deal();
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        var card = world.CreateCard(AuthoredCards.Stampede, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(
            queued + 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    [Fact]
    public void TheDelayedStunIsSpentAndDoesNotWaitForTheNextAttack()
    {
        // `rr:delayed-effect` resolves once, at the next time its condition is
        // true. A second attack in the same game must not stun again -- the
        // effect is gone, and `Duration.NextTime` is what makes it so.
        var world = Hero();
        var identity = world.Seats[0].IdentityCard;
        Play(world, AuthoredCards.Stampede);

        foreach (var status in Statuses.On(world, identity, Statuses.Stunned).ToList())
        {
            Discard.Card(world, status, "test", []);
        }

        // A second attack from the same villain, nothing else revealed.
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 2, 2, Subject: villain.ObjectId, Seat: 0));
        Run(world);

        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:delayed-effect")]
    [Fact]
    public void AnAttackThatDamagedNobodyLeavesNothingWaiting()
    {
        // The case a bare "next time damage is dealt" gets wrong. The tough
        // card ate this attack, so nobody was damaged -- and the effect must
        // not sit there waiting for *somebody else's* attack to stun the wrong
        // character two rounds later.
        //
        // "If a character is damaged by **this attack**" is false once the
        // attack is over, which is why the effect is bounded by the end of it
        // as well as by the condition.
        var world = Hero();
        var identity = world.Seats[0].IdentityCard;
        Statuses.Give(world, identity, Statuses.Tough);

        Play(world, AuthoredCards.Stampede);
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));

        // A second attack, with nothing to prevent it. It damages, and it must
        // not stun.
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 2, 2, Subject: villain.ObjectId, Seat: 0));
        Run(world);

        Assert.True(identity.Damage > 0, "the second attack landed");
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
    }

    /// <summary>Reveals the card and lets everything it scheduled happen.</summary>
    private static void Play(World world, string faceId)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));
        Run(world);
    }

    private static void Run(World world)
    {
        var abilities = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 12, $"'{asked.Label}' is still being asked");
            Sequence.Answer(world, Cards, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, Cards, abilities, events);
        }
    }

    /// <summary>The Rhino board with Spider-Man standing up.</summary>
    private static World Hero()
    {
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        return world;
    }

    private static World Deal() => WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
        ["Spider-Man"],
        12345);
}
