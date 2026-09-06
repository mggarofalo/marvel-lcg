using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// One card with two abilities, at two tiers.
/// </summary>
/// <remarks>
/// <para>
/// Sweeping Swoop is a treachery when it is revealed and a boost card when it
/// is turned faceup during an activation, and it says different things in the
/// two places. That is what makes it the card the boost guard was tightened
/// for: "is this card authored" would have passed on the strength of the half
/// somebody had written, and the other half would have gone back to being
/// silent.
/// </para>
/// </remarks>
public sealed class SweepingSwoopTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:form-change-form.5")]
    [Fact]
    public void ItStunsAHeroAndLeavesAnAlterEgoAlone()
    {
        // "Stun **your hero**", which is not "stun you".
        // `rr:form-change-form.5`: "while a player is in alter-ego form, card
        // abilities that interact with their hero do not interact with their
        // identity."
        var hero = Deal();
        hero.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        Reveal(hero, AuthoredCards.SweepingSwoop);
        Assert.True(Statuses.Has(hero, hero.Seats[0].IdentityCard, Statuses.Stunned));

        var alterEgo = Deal();
        Reveal(alterEgo, AuthoredCards.SweepingSwoop);
        Assert.False(
            Statuses.Has(alterEgo, alterEgo.Seats[0].IdentityCard, Statuses.Stunned));
    }

    [Rule("rr:identity.2")]
    [Fact]
    public void ItSurgesOnlyWhileVultureIsInPlay()
    {
        // "If Vulture is in play, this card gains surge." A title and not a
        // printed id: `rr:identity.2` makes a card that "refers to a hero or
        // alter-ego by title" refer to the one with that title.
        var quiet = Deal();
        int queued = Queue(quiet).Cards.Count;
        Reveal(quiet, AuthoredCards.SweepingSwoop);
        Assert.Equal(queued, Queue(quiet).Cards.Count);

        var hunted = Deal();
        hunted.CreateCard(
            "01167", hunted.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        int waiting = Queue(hunted).Cards.Count;

        Reveal(hunted, AuthoredCards.SweepingSwoop);

        Assert.Equal(waiting + 1, Queue(hunted).Cards.Count);
    }

    [Fact]
    public void VultureInTheDeckIsNotVultureInPlay()
    {
        // "In play" is a place, not a card that exists. Spider-Man's nemesis
        // set is set aside at the deal, so a game that has not revealed Shadow
        // of the Past has Vulture on the table and out of play.
        var world = Deal();
        Assert.Contains(world.Seats[0].Nemesis.Cards, card => card.FaceId == "01167");
        int queued = Queue(world).Cards.Count;

        Reveal(world, AuthoredCards.SweepingSwoop);

        Assert.Equal(queued, Queue(world).Cards.Count);
    }

    [Rule("rr:boost-boost-icon.2")]
    [Fact]
    public void AsABoostCardItStunsWhoeverTheActivationDamages()
    {
        // The other tier. The boost ability is a delayed effect, so it is
        // registered when the card is turned faceup and resolves when the
        // attack it belongs to deals its damage.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var identity = world.Seats[0].IdentityCard;
        var card = world.CreateCard(
            AuthoredCards.SweepingSwoop, world.AreaOf(DeckType.BoostingArea));

        AuthoredCards.Runner().Boost(world, card, 0);

        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));
        Run(world);

        Assert.True(identity.Damage > 0, "the attack landed");
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:activation")]
    [Fact]
    public void TheBoostEffectDiesWithTheActivationThatMadeIt()
    {
        // **A scheme is an activation and is not an attack.** Bounded by the
        // end of an *attack*, this effect would survive a scheme activation
        // entirely and then stun somebody during the next attack, which it was
        // never about.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        var card = world.CreateCard(
            AuthoredCards.SweepingSwoop, world.AreaOf(DeckType.BoostingArea));
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        AuthoredCards.Runner().Boost(world, card, 0);

        // The alter-ego is schemed at, so the activation ends without damage.
        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Subject: villain.ObjectId, Seat: 0));
        Run(world);

        // Now a real attack, in a later round and nothing to do with the boost
        // card. It must damage and must not stun.
        identity.TurnTo(AuthoredCards.SpiderMan);
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 2, 2, Subject: villain.ObjectId, Seat: 0));
        Run(world);

        Assert.True(identity.Damage > 0, "the second attack landed");
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
    }

    private static Area Queue(World world) =>
        world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));

    private static void Reveal(World world, string faceId)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);
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

    private static World Deal() => WorldSetup.DealWithoutCardAbilities(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
        ["Spider-Man"],
        12345);
}
