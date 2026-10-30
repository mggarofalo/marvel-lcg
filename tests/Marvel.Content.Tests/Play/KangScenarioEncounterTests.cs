using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Kang scenario encounter faces and the full authored book on a
// nonpublic Rhino board; Kang's public admission stays closed.
public sealed class KangScenarioEncounterTests : CoreCardTestBase
{
    private const string Rampage = "11025";
    private const string ManipulatedTimestream = "11027";
    private const string TimeTravelTactics = "11028";

    // Maria Hill, an ally, and Energy, a resource.
    private static readonly string[] NotEvents = ["01067", "01088"];

    // Hard to Keep Down and Stampede, neither a minion.
    private static readonly string[] NoMinion = ["01104", "01106"];

    [Fact]
    [Rule("rr:acceleration-icon")]
    public void RampageEntersWithFourThreatAndAnAccelerationIcon()
    {
        // 11025 prints "Starting Threat 4" and Acceleration 1. rr:acceleration-icon:
        // "place X additional threat on the main scheme, where X is the number of
        // acceleration icons in play."
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var rampage = f.Reveal(Rampage, player: 1);

        f.Finish();

        Assert.Equal(DeckType.SideSchemesArea, rampage.Area.Type);
        Assert.Equal(4, rampage.Tokens["k_threat"]);
        Assert.Equal(1, MainScheme.Acceleration(world, Cards));
        Assert.Equal(0, Deal.HazardIcons(world, Cards));
    }

    [Fact]
    [Rule("rr:when-defeated-abilities.2")]
    [Rule("rr:when-revealed-abilities.2")]
    public void DefeatedRampageEngagesTheFirstDiscardedMinionWithTheDefeatingPlayer()
    {
        // 11025: "When Defeated: Discard cards from the top of the encounter deck
        // until a minion is discarded. Put that minion into play engaged with the
        // player who defeated this scheme." rr:when-defeated-abilities.2: "When a ...
        // side scheme ... is defeated, all 'When Defeated' abilities on the card
        // resolve." Player 1, not the first player, defeats it.
        // rr:when-revealed-abilities.2: "If an encounter card with a 'When Revealed'
        // ability is put into play without being revealed, the 'When Revealed'
        // ability does not trigger", so Shocker deals no damage.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var below = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var shocker = world.CreateCard("01103", world.AreaOf(DeckType.EncounterDeck));
        var above = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var rampage = InPlay(world);

        BasicThwartPowers.BasicThwart(world, Cards, 1, rampage, []);
        f.Finish();

        Assert.Equal([shocker], world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.Equal(new[] { above.ObjectId, rampage.ObjectId }.Order(),
            world.AreaOf(DeckType.EncounterDiscardPile).Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([below], world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    public void DefeatedRampageWithoutAMinionStopsWhenTheEncounterDeckEmpties()
    {
        // rr:encounter-deck.2: "If the encounter deck is emptied this way, that card
        // ability is considered to be fulfilled. Do not continue the discard effect
        // with the newly shuffled encounter deck." The minion already in the
        // discard pile is shuffled in, not put into play; rr:encounter-deck.1 places
        // an acceleration token as the discard pile is shuffled in.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var old = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        var deck = NoMinion
            .Select(face => world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck))).ToArray();
        var rampage = InPlay(world);

        BasicThwartPowers.BasicThwart(world, Cards, 1, rampage, []);
        f.Finish();

        Assert.Equal(deck.Append(old).Select(card => card.ObjectId).Order(),
            world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([rampage], world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:you-your.8")]
    public void ManipulatedTimestreamDiscardsEachEventFromTheRevealingPlayersHandWithoutSurge(int events)
    {
        // 11027: "Discard each event from your hand. If no events are discarded this
        // way, this card gains surge." rr:you-your.8: "If a game step or card ability
        // discards cards from 'your' hand, the player resolving that game step or
        // card ability discards the cards from their hand." Player 1, not the first
        // player, reveals; their ally and resource stay, as does player 0's event.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var kept = NotEvents.Select(face => world.CreateCard(face, world.Seats[1].Hand)).ToList();
        var discarded = Enumerable.Range(0, events)
            .Select(_ => world.CreateCard("01005", world.Seats[1].Hand)).ToList();
        var other = world.CreateCard("01005", world.Seats[0].Hand);
        var timestream = f.Reveal(ManipulatedTimestream, player: 1);

        f.Finish();

        Assert.Equal(kept.Select(card => card.ObjectId).Order(),
            world.Seats[1].Hand.Cards.Select(card => card.ObjectId).Order());
        Assert.All(discarded, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Equal([other], world.Seats[0].Hand.Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, timestream.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:surge.1")]
    public void ManipulatedTimestreamSurgesWhenTheRevealingPlayerHasNoEvent(bool emptyHand)
    {
        // rr:surge.1: "Deal yourself 1 facedown encounter card." Player 1 holds no
        // event, though player 0 does; player 1 is dealt the Hydra Mercenary on top.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var mercenary = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var kept = emptyHand ? [] : NotEvents.Select(face => world.CreateCard(face, world.Seats[1].Hand)).ToList();
        var other = world.CreateCard("01005", world.Seats[0].Hand);
        f.Reveal(ManipulatedTimestream, player: 1);

        f.Finish();

        Assert.Equal(kept.Select(card => card.ObjectId).Order(),
            world.Seats[1].Hand.Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([other], world.Seats[0].Hand.Cards);
        Assert.Equal([mercenary], world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [Rule("rr:each-player")]
    [Rule("rr:indirect-damage.1")]
    [Rule("rr:obligation.5")]
    public void TimeTravelTacticsDealsEachPlayerIndirectDamageForTheirOwnObligations(int first, int second)
    {
        // 11028: "Each player takes 1 indirect damage for each obligation in their
        // play area." rr:each-player: "each player resolves that effect one at a
        // time", so "their" is the resolving player. rr:obligation.5: a revealing
        // player "places that obligation into their play area". rr:indirect-damage.1:
        // "divided as that player chooses among characters under their control";
        // each controls only an identity. Player 1, not the first player, reveals.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        KangTemporalFixture.Obligations(world, 0, first);
        KangTemporalFixture.Obligations(world, 1, second);
        f.Reveal(TimeTravelTactics, player: 1);

        f.Finish(First);

        Assert.Equal(first, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(second, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:surge.1")]
    public void TimeTravelTacticsSurgesToTheRevealingPlayer()
    {
        // 11028 prints Surge. rr:surge.1: "Deal yourself 1 facedown encounter
        // card": the revealing player, player 1, is dealt the Hydra Mercenary on top.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var mercenary = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var tactics = f.Reveal(TimeTravelTactics, player: 1);

        f.Finish(First);

        Assert.Equal([mercenary], world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, tactics.Area.Type);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:boost-boost-icon")]
    [Rule("rr:attack-enemy-activation.step.3.c")]
    [Rule("rr:you-your")]
    public void TimeTravelTacticsBoostGainsAnIconForEachObligationOfTheAttackedPlayer(int obligations)
    {
        // 11028: "Boost: This card gains [boost] for each obligation in your play
        // area." It prints one boost icon. rr:boost-boost-icon: "Add the number of
        // boost icons on each card to the enemy's ATK value"; step.3.c counts them
        // after the Boost ability. rr:you-your: "you" is the player the activation
        // is against: Rhino (ATK 2) attacks player 1, not the first player, and
        // player 0's three obligations do not count. Rhino's own ATK is unchanged.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var tactics = world.CreateCard(TimeTravelTactics, world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Obligations(world, 0, 3);
        KangTemporalFixture.Obligations(world, 1, obligations);
        KangTemporalFixture.Attack(world, f.Villain, 1);

        var events = f.Finish();

        var boost = Assert.Single(events.OfType<BoostResolved>());
        Assert.Equal(1 + obligations, boost.Icons);
        Assert.Equal(2 + 1 + obligations, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, tactics.Area.Type);
        Assert.Equal(2, Modified(world, f.Villain, "attack"));
        Assert.False(world.Agenda.IsBusy);
    }

    private static Decision First(Prompt prompt) => Decision.Take(prompt.Affordances[0].Id);

    private static Card InPlay(World world)
    {
        var rampage = world.CreateCard(Rampage, world.AreaOf(DeckType.SideSchemesArea));
        rampage.PlaceTokens("k_threat", 1);
        return rampage;
    }
}
