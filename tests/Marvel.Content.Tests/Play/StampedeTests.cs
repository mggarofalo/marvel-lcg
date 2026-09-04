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
        // A delayed effect "is not treated as a new triggered ability": it
        // resolves at its condition without opening another offered window.
        var world = Hero();
        var identity = world.Seats[0].IdentityCard;

        Play(world, AuthoredCards.Stampede);

        Assert.True(identity.Damage > 0, "the attack landed");
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:delayed-effect.1")]
    [Rule("rr:damage.step.5")]
    [Rule("rr:damage.step.8")]
    [Rule("rr:leaves-play.2.3")]
    [Fact]
    public void ADefeatedDefendersDelayedStunReturnsToTheSupply()
    {
        // Damage placement makes Stampede due before defeat. Daredevil gains
        // the status while he is still in play, then both leave in damage step
        // 8 instead of a status appearing under his discarded copy.
        var world = Hero();
        var daredevil = world.CreateCard(
            "01058", world.AreaOf(
                DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        daredevil.TakeDamage(2);

        var events = Play(world, AuthoredCards.Stampede, daredevil);

        Assert.Equal(DeckType.DiscardPile, daredevil.Area.Type);
        var stunned = Assert.Single(
            world.Cards, card => card.FaceId == Statuses.Stunned);
        Assert.Equal(DeckType.RemovedArea, stunned.Area.Type);
        Assert.Empty(world.Areas
            .Where(area => area.Host == daredevil.ObjectId)
            .SelectMany(area => area.Cards));

        int damage = events.FindIndex(gameEvent => gameEvent is FieldSet changed
            && changed.Card == daredevil.ObjectId
            && changed.Field == "health");
        int attached = events.FindIndex(gameEvent => gameEvent is CardAttached status
            && status.Card == stunned.ObjectId
            && status.Host == daredevil.ObjectId);
        int statusRemoved = events.FindIndex(gameEvent => gameEvent is CardsMoved moved
            && moved.Cards.Any(card => card.Card == stunned.ObjectId));
        int allyDiscarded = events.FindIndex(gameEvent => gameEvent is CardsMoved moved
            && moved.Cards.Any(card => card.Card == daredevil.ObjectId));

        Assert.True(damage >= 0, "attack damage was recorded");
        Assert.True(damage < attached, "the delayed status followed damage placement");
        Assert.True(attached < statusRemoved, "the status existed before cleanup");
        Assert.True(statusRemoved < allyDiscarded, "the status left before its host");
    }

    [Rule("rr:tough.3")]
    [Rule("rr:prevent.1.2")]
    [Rule("rr:prevent.1.3")]
    [Fact]
    public void AToughCharacterTakesNoDamageAndIsNotStunned()
    {
        // Preventing all means the character "is not considered to have taken
        // damage"; for an attack, the attacker still dealt damage but did not
        // "attack and damage" that character. The false branch is therefore
        // taken and nobody is stunned -- while Tough being spent proves the
        // attack and its damage instance still happened.
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
    [Rule("rr:alteration-effect.1")]
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
    private static List<GameEvent> Play(
        World world, string faceId, Card? defender = null)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));
        return Run(world, defender);
    }

    private static List<GameEvent> Run(World world, Card? defender = null)
    {
        var abilities = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 12, $"'{asked.Label}' is still being asked");
            Decision answer = asked.Asking == Question.Defender && defender is not null
                ? Decision.Take(Assert.Single(
                    asked.Affordances,
                    option => option.AnchorId == defender.ObjectId).Id)
                : Decision.Decline;
            Sequence.Answer(world, Cards, abilities, asked, answer, events);
            asked = Sequence.Work(world, Cards, abilities, events);
        }
        return events;
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
