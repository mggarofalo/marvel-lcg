using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// "Kang" and "each obligation in play" on a prepared stage-II split, where each
// player has their own game area and Kang II. Nonpublic composition; Kang's setup
// and product admission stay closed.
public sealed class KangGameAreaReferenceTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:reveal.5")]
    public void PinnedDownInTheSplitCountsOnlyItsRevealersGameArea(int player)
    {
        // 11024: "When Revealed: Place 2 threat here for each obligation in play."
        // MARVEL-894 (Q15): a side scheme revealed during the split enters the
        // revealing player's game area, and (Q4) the count reads only that area.
        // Player 0 holds one obligation and player 1 two; counting both areas would
        // give 2 + 2 x 3.
        var (world, f, _) = Split();
        KangTemporalFixture.Obligations(world, 0, 1);
        KangTemporalFixture.Obligations(world, 1, 2);
        var pinned = f.Reveal("11024", player);

        f.Finish();

        Assert.Equal(DeckType.SideSchemesArea, pinned.Area.Type);
        Assert.Equal(PlayArea.Of(player), pinned.Area.PlayArea);
        Assert.Same(world.GameAreaOf(PlayArea.Of(player)), Places.GameAreaOf(world, pinned));
        Assert.Equal(2 + 2 * (player + 1), pinned.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:reveal.5")]
    [Rule("rr:cannot")]
    public void ARevealedDominionProtectsOnlyItsRevealersKang(int player)
    {
        // 11023: "Kang cannot take damage." Revealed during the split it enters the
        // revealer's game area (MARVEL-894 Q15), where "Kang" is that area's Kang
        // (Q2); the other Kang II still takes damage.
        var (world, f, villains) = Split();
        var dominion = f.Reveal("11023", player);
        f.Finish();
        Assert.Same(world.GameAreaOf(PlayArea.Of(player)), Places.GameAreaOf(world, dominion));
        foreach (int seat in new[] { 0, 1 })
        {
            world.Seats[seat].IdentityCard.TurnTo("01001a");
            BasicPowerInitiation.BasicAttack(world, world.Facts, seat, villains[seat], []);
            f.Finish();
        }

        Assert.Equal(0, villains[player].Damage);
        Assert.Equal(2, villains[1 - player].Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:attach-to")]
    public void TemporalShieldAttachesToTheRevealersKang(int player)
    {
        // MARVEL-894 (Q2): "Kang" is the Kang villain in the card's game area.
        var (world, f, villains) = Split();
        var shield = f.Reveal("11014", player);

        f.Finish();

        Assert.Equal(villains[player].ObjectId, shield.Area.Host);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:boost-boost-icon")]
    public void MacrobotsToughensTheActivatingAreasKang(int player)
    {
        // Ruling 5ecfc71ddff64d39 keeps a Kang boost in its own game area. Macrobots
        // is the boost card of the Kang activating against the player whose area it is.
        var (world, f, villains) = Split();
        world.CreateCard("11017", world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Attack(world, villains[player], player);

        f.Finish();

        Assert.True(Statuses.Has(world, villains[player], Statuses.Tough));
        Assert.False(Statuses.Has(world, villains[1 - player], Statuses.Tough));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:reveal.4")]
    public void EnergyBlastIsTheRevealersKangAttackingThem(int player)
    {
        // 11026: "When Revealed (Hero): Kang attacks you." Each local Kang II has ATK 2;
        // the encounter deck's Hard to Keep Down prints no boost icons.
        var (world, f, villains) = Split();
        world.Seats[player].IdentityCard.TurnTo("01001a");

        f.Reveal("11026", player);

        var events = f.Finish();

        var attack = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(villains[player].ObjectId, attack.Enemy);
        Assert.Equal(2, world.Seats[player].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1 - player].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ACardInNoGameAreaCannotChooseBetweenTwoKangs()
    {
        // A side scheme in the detached central villain area is in no game area and
        // reaches both, so its "Kang" would be either local Kang II. Asking whether a
        // Kang can take damage raises rather than choosing one.
        var (world, f, villains) = Split();
        var dominion = world.CreateCard("11023", world.AreaOf(DeckType.SideSchemesArea));
        world.Seats[0].IdentityCard.TurnTo("01001a");

        Assert.Null(Places.GameAreaOf(world, dominion));
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villains[0], []);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => f.Finish());
        Assert.Contains("11023", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, villains[0].Damage);
    }

    [Fact]
    public void TwoVillainsInOneGameAreaAreNotOneKang()
    {
        // The reference names one villain; with Rhino and Kang I both in the only game
        // area, Temporal Shield raises rather than choosing whom to attach to.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        world.CreateCard("11001", world.AreaOf(DeckType.VillainArea));
        var shield = f.Reveal("11014");

        var thrown = Assert.Throws<RulesNotImplementedException>(() => f.Finish());

        Assert.Contains("11014", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(-1, shield.Area.Host);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AnObligationCountFromAPlayersAreaReadsOnlyThatGameArea(int player)
    {
        // Synthetic: Shocker's printed reveal is replaced by "take 1 damage for each
        // obligation in this game area" to observe the count from an engaged minion,
        // which is in the revealer's game area. Player 0 holds one obligation, player 1 two.
        var (world, f, _) = Split(Synthetic("""
            {"card":"01103","abilities":[{"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"dealDamage":{"cards":"you","amount":{"count":{"query":"obligationsInThisGameArea"}}}}}]}
            """));
        KangTemporalFixture.Obligations(world, 0, 1);
        KangTemporalFixture.Obligations(world, 1, 2);
        f.Reveal("01103", player);

        f.Finish();

        Assert.Equal(player + 1, world.Seats[player].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1 - player].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    private static AbilityRunner Synthetic(string row)
    {
        using var book = System.Text.Json.JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray()
            .Where(card => card.GetProperty("card").GetString() != "01103")
            .Select(card => card.GetRawText()).Append(row);
        return new AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""{"cards":[{{string.Join(",", rows)}}]}"""));
    }

    private static (World World, RedSkullFixture Fixture, Card[] Villains) Split(AbilityRunner? abilities = null)
    {
        var world = new World(Cards, 2, 42);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            seat.IdentityCard.TurnTo("01001b");
            for (int i = 0; i < 6; i++) world.CreateCard("01088", seat.Deck);
            world.Join(PlayArea.Of(player), world.CreateGameArea(), "prepared split", []);
        }
        var central = world.CreateCard("11008a,11008b", world.AreaOf(DeckType.MainSchemesArea));
        central.TurnTo("11008b");
        world.Detach(PlayArea.Villains, "prepared split", []);
        var villains = new Card[2];
        for (int player = 0; player < 2; player++)
        {
            var local = PlayArea.Of(player);
            world.CreateCard(player == 0 ? "11009b" : "11010b", world.AreaOf(DeckType.MainSchemesArea, local));
            villains[player] = world.CreateCard(player == 0 ? "11002" : "11003", world.AreaOf(DeckType.VillainArea, local));
        }
        for (int i = 0; i < 4; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var runner = abilities ?? AuthoredCards.Runner();
        world.Abilities = runner;
        return (world, new RedSkullFixture(world, runner), villains);
    }
}
