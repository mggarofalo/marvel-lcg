using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Temporal faces and the full authored book on a nonpublic Rhino board;
// Kang's public admission stays closed.
public sealed class KangTemporalCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData("11030", false, true)]
    [InlineData("11030", true, true)]
    [InlineData("01101", false, false)]
    [InlineData("01101", true, false)]
    [Rule("rr:boost-boost-icon")]
    [Rule("rr:boost-boost-icon.2")]
    public void AncientWarriorBoostStunsOnlyThePlayerTheVillainActivatesAgainst(
        string boost, bool scheme, bool stunned)
    {
        // rr:boost-boost-icon: "If the boost field has a star icon ... resolve the
        // 'Boost' ability when the card is turned face up." 11030: "Boost: You are stunned."
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var card = world.CreateCard(boost, world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(scheme ? new EnemySchemeOperation() : new EnemyAttackOperation(),
            1, 2, Index: 1, Subject: f.Villain.ObjectId, Seat: 1));

        var events = f.Finish();

        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Equal(stunned, Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(scheme ? 0 : 1, events.OfType<AttackCompleted>().Count());
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:quickstrike")]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:boost-boost-icon.2")]
    public void RevealedAncientWarriorQuickstrikesAHeroWithoutItsBoostText(bool hero)
    {
        // rr:quickstrike.1: "Forced Response (Hero): After this minion engages a
        // player, it attacks that player." rr:boost-boost-icon.2: "Only the ability
        // text beneath the divider line is active on a card that is resolving as a
        // boost card", so a revealed Ancient Warrior does not stun.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        if (!hero) Forms.Change(world.Seats[0], Cards);
        var identity = world.Seats[0].IdentityCard;
        var warrior = f.Reveal("11030");

        var events = f.Finish();

        Assert.Equal(DeckType.EngagedEnemiesArea, warrior.Area.Type);
        Assert.Equal(hero ? 2 : 0, identity.Damage);
        Assert.Equal(hero ? 1 : 0, events.OfType<AttackCompleted>().Count(e => e.Enemy == warrior.ObjectId));
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("11032", 3)]
    [InlineData("01101", 0)]
    [Rule("rr:piercing")]
    [Rule("rr:piercing.1")]
    [Rule("rr:tough.1")]
    public void TyrannosaurusRexAttacksDiscardToughBeforeDamage(string enemy, int damage)
    {
        // 11032: "Tyrannosaurus Rex's attacks gain piercing." rr:piercing.1: "Before
        // this attack deals damage to a character, discard each tough status card
        // from that character." rr:tough.1: "When this character would take damage,
        // remove a tough status card from it instead", which stops the control.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var identity = world.Seats[0].IdentityCard;
        var attacker = KangTemporalFixture.Engaged(world, enemy, 0);
        Statuses.Give(world, identity, Statuses.Tough);
        KangTemporalFixture.Attack(world, attacker, 0);

        f.Finish();

        Assert.Equal(damage > 0, Keywords.Has(world, attacker, "piercing", Cards));
        Assert.False(Statuses.Has(world, identity, Statuses.Tough));
        Assert.Equal(damage, identity.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:toughness.1")]
    public void RevealedTyrannosaurusRexEntersWithATough()
    {
        // rr:toughness.1: "Forced Response: After this character enters play, give
        // it a tough status card."
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var rex = f.Reveal("11032");

        f.Finish();

        Assert.Equal(DeckType.EngagedEnemiesArea, rex.Area.Type);
        Assert.Single(Statuses.On(world, rex, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:per-player-icon")]
    [Rule("rr:hazard-icon")]
    [Rule("rr:defeat")]
    [Rule("rr:replacement-effect")]
    public void TimePortalEntersPerPlayerAddsAHazardAndReturnsToTheEncounterDeck(int players)
    {
        // 11033 prints "Starting Threat 2[per_hero]" and Hazard 1. rr:per-player-icon:
        // "multiplies that value by the number of players who started the scenario."
        // rr:hazard-icon:
        // "for each hazard icon on cards in play, deal one player one additional card."
        // rr:defeat: "if a side scheme has no threat on it, it is defeated." 11033:
        // "shuffle it into the encounter deck instead of discarding it."
        var (world, f) = KangTemporalFixture.Board(players);
        world.FirstPlayer = players - 1;
        var portal = f.Reveal("11033");
        f.Finish();
        Assert.Equal(DeckType.SideSchemesArea, portal.Area.Type);
        Assert.Equal(2 * players, portal.Tokens["k_threat"]);
        Assert.Equal(1, Deal.HazardIcons(world, Cards));

        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.DealEncounterCards), 1, 3));
        f.Finish();
        for (int player = 0; player < players; player++)
            Assert.Equal(player == world.FirstPlayer ? 2 : 1,
                world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards.Count);

        // Captain Marvel's THW 2 removes all the threat only in a one-player game.
        BasicThwartPowers.BasicThwart(world, Cards, 0, portal, []);
        var events = f.Finish();
        bool defeated = players == 1;
        Assert.Equal(defeated ? DeckType.EncounterDeck : DeckType.SideSchemesArea, portal.Area.Type);
        Assert.Equal(defeated ? 0 : 2, portal.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(defeated ? 1 : 0, events.OfType<CardsMoved>().Count(e =>
            e.Verb == "Shuffle_Into" && e.Cards.Any(c => c.Card == portal.ObjectId)));
        Assert.Equal(defeated ? 0 : 1, Deal.HazardIcons(world, Cards));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:defeat")]
    public void ShuffledTimePortalIsStillADefeatedSideScheme()
    {
        // 11033 replaces only the discard; the scheme is still defeated. Skilled
        // Investigator (04047): "Hero Response: After a side scheme is defeated,
        // exhaust Skilled Investigator → draw 1 card."
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var identity = world.Seats[0].IdentityCard;
        var investigator = world.CreateCard("04047",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), identity.ObjectId, cardOwner: 0));
        var portal = f.Reveal("11033");
        f.Finish();
        int hand = world.Seats[0].Hand.Cards.Count;

        BasicThwartPowers.BasicThwart(world, Cards, 0, portal, []);
        int responses = 0;
        var events = new List<GameEvent>();
        for (var prompt = Sequence.Work(world, Cards, world.Abilities, events); prompt is not null;
             prompt = Sequence.Work(world, Cards, world.Abilities, events))
        {
            Assert.True(responses < 2, prompt.Description);
            var take = prompt.Affordances.SingleOrDefault(a => a.AnchorId == investigator.ObjectId);
            if (take is not null) responses++;
            Sequence.Answer(world, Cards, world.Abilities, prompt,
                take is not null ? Decision.Take(take.Id) : Decision.Decline, events);
        }

        Assert.Equal(1, responses);
        Assert.Equal(DeckType.EncounterDeck, portal.Area.Type);
        Assert.Equal(hand + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(investigator.Ready);
        Assert.False(world.Agenda.IsBusy);
    }
}
