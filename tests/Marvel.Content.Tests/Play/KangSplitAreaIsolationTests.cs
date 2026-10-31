using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangStageFlowFixture;

namespace Marvel.Content.Tests.Play;

// Nonpublic prepared probes on a live split table; Kang's admission stays closed.
public sealed class KangSplitAreaIsolationTests
{
    // Every encounter card the Kang scenario can reveal mid-split: the Kang set
    // without its stages, villains and set-aside Kang's Dominion, then the
    // Standard and Expert sets (pack:mc11 setup).
    private static readonly string[] Pool =
    [
        "11014", "11015", "11016", "11017", "11018", "11019", "11020", "11021", "11022",
        "11024", "11025", "11026", "11027", "11028", "11029",
        "01186", "01187", "01188", "01189", "01190", "01191", "01192", "01193",
    ];

    public static TheoryData<string, int, bool> Reveals()
    {
        var data = new TheoryData<string, int, bool>();
        foreach (string face in Pool)
            for (int player = 0; player < 2; player++)
            {
                data.Add(face, player, false);
                if (face is "01191" or "01192" or "01193") data.Add(face, player, true);
            }
        return data;
    }

    [Theory]
    [MemberData(nameof(Reveals))]
    public void ARevealChangesNothingInTheOtherGameArea(string face, int revealer, bool expert)
    {
        // pack:mc11:game-areas: "Cards and components in one game area cannot
        // affect another game area (with the exception of the text on stage 2B)."
        // The shared encounter deck and 2B are central, so only they may change
        // outside the revealer's own game area.
        var events = new List<GameEvent>();
        var world = Split(2, expert, events);
        int other = 1 - revealer;
        string before = Snapshot(world, other);

        Reveal(world, face, revealer, events);

        Assert.Equal(before, Snapshot(world, other));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    [Rule("rr:each-player.1")]
    public void CorruptedTimestreamAsksOnlyThePlayersInItsGameArea()
    {
        // Ruling 4799b99ecc0e5dea: "'Each player' refers to each player in the
        // same game area. If you are the only person in your game area, then
        // 'each player' refers only to you." With empty hands, the revealer alone
        // places 2 threat beside the scheme's starting 2.
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        foreach (var seat in world.Seats)
            foreach (var card in seat.Hand.Cards.ToList()) World.MoveToTop(card, seat.Deck);

        Reveal(world, "11022", 1, events);

        var scheme = Assert.Single(world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(1)).Cards);
        Assert.Equal("11022", scheme.FaceId);
        Assert.Equal(4, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void AdvanceSchemesWithTheRevealersOwnKang()
    {
        // 01186: "The villain schemes." Each split game area has its own Kang.
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        long mine = LocalScheme(world, 1).Tokens.GetValueOrDefault("k_threat");
        long theirs = LocalScheme(world, 0).Tokens.GetValueOrDefault("k_threat");

        Reveal(world, "01186", 1, events);

        Assert.True(LocalScheme(world, 1).Tokens.GetValueOrDefault("k_threat") > mine);
        Assert.Equal(theirs, LocalScheme(world, 0).Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:incite-x")]
    public void InciteThreatsTheRevealersOwnMainScheme()
    {
        // "Place X threat on the main scheme": the revealer's stage 3B, not 2B.
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        var central = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        long centralThreat = central.Tokens.GetValueOrDefault("k_threat");
        long mine = LocalScheme(world, 1).Tokens.GetValueOrDefault("k_threat");
        long theirs = LocalScheme(world, 0).Tokens.GetValueOrDefault("k_threat");

        Reveal(world, "11029", 1, events);

        Assert.Equal(mine + 1, LocalScheme(world, 1).Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(theirs, LocalScheme(world, 0).Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(centralThreat, central.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void MasterplanFindsTheSideSchemeInTheRevealersGameArea()
    {
        // 01192: "Place 4 threat on each side scheme. If there are no side schemes
        // in play, discard cards from the top of the encounter deck until a side
        // scheme is discarded." A side scheme in another game area is out of reach.
        var events = new List<GameEvent>();
        var world = Split(2, true, events);
        var mine = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(1)));
        var theirs = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(0)));
        int deck = EncounterRouting.Deck(world).Cards.Count;

        Reveal(world, "01192", 1, events);

        Assert.Equal(4, mine.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, theirs.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(deck, EncounterRouting.Deck(world).Cards.Count);
    }

    [Fact]
    public void TimeTravelTacticsDamagesOnlyTheRevealersGameArea()
    {
        // 11028: "Each player takes 1 indirect damage for each obligation in their
        // play area." Only the revealer is in the revealer's game area.
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        for (int seat = 0; seat < 2; seat++)
            world.CreateCard("11018", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(seat)));
        long theirs = world.Seats[0].IdentityCard.Damage;
        long mine = world.Seats[1].IdentityCard.Damage;

        Reveal(world, "11028", 1, events);

        Assert.Equal(theirs, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(mine + 1, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    public void GangUpAttacksWithTheRevealersOwnKang()
    {
        // 01189: "The villain and each minion engaged with you attacks you."
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        var mine = LocalKang(world, 1);
        var theirs = LocalKang(world, 0);
        var identity = world.Seats[1].IdentityCard;
        if (!Forms.In(world, world.Seats[1], world.Facts, Forms.Hero)) identity.TurnTo(identity.FaceId.Replace('b', 'a'));
        int start = events.Count;

        Reveal(world, "01189", 1, events);

        var attacks = events.Skip(start).OfType<AttackBoundary>().Where(attack => attack.Started).ToList();
        Assert.DoesNotContain(attacks, attack => attack.Source == theirs.ObjectId);
        Assert.Contains(attacks, attack => attack.Source == mine.ObjectId && attack.Target == identity.ObjectId);
    }

    private static string Snapshot(World world, int seat)
    {
        var area = PlayArea.Of(seat);
        var cards = world.Cards.Where(card => card.Area.PlayArea == area)
            .OrderBy(card => card.ObjectId)
            .Select(card => string.Join(':', card.ObjectId, card.Area.Type, card.Area.Id,
                card.Area.Cards.ToList().IndexOf(card), card.FaceId, card.FaceUp, card.Damage,
                string.Join(',', card.Tokens.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => $"{pair.Key}={pair.Value}"))));
        var effects = world.Effects.Registered
            .Where(effect => effect.Affects is int target && world.Cards[target].Area.PlayArea == area)
            .Select(effect => $"{effect.Kind}>{effect.Affects}");
        return string.Join('\n', [$"area {world.GameAreaOf(area)?.Id}", .. cards, .. effects]);
    }
}
