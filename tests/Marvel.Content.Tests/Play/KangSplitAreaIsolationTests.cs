using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
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
        // Beyond the Kang pool: encounter cards whose text selects every player.
        "01148", "01166", "01169",
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
    [Rule("rr:each-player.1")]
    public void EachPlayerMeansBothPlayersOfAJoinedGroupAndNotTheThird()
    {
        // Ruling 4799b99ecc0e5dea after player 0 joins player 1: Corrupted
        // Timestream asks both of them, ordered by the first player, and not
        // player 2. With empty hands each places 2 beside the starting 2.
        var events = new List<GameEvent>();
        var world = Split(3, false, events);
        Defeat(world, LocalKang(world, 0), events);
        Assert.Null(JoinAreaOf(world, EndPlayerPhase(world, 2, events), 1, events));
        for (int seat = 0; seat < 2; seat++)
            foreach (var card in world.Seats[seat].Hand.Cards.ToList()) World.MoveToTop(card, world.Seats[seat].Deck);
        string third = Snapshot(world, 2);
        bool ordered = false;

        Reveal(world, "11022", 1, events, prompt =>
        {
            ordered |= prompt.Affordances is [{ Verb: "Order" }];
            return new ActingPolicy(7, declineOneIn: 1).Answer(prompt);
        });

        Assert.True(ordered);
        var scheme = Assert.Single(world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(1)).Cards);
        Assert.Equal(6, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(third, Snapshot(world, 2));
    }

    [Fact]
    [Rule("rr:each-player.1")]
    public void MariaHillDrawsOnlyForThePlayersInHerGameArea()
    {
        // 01067: "Response: After Maria Hill enters play, each player draws 1 card."
        // Ruling 4799b99ecc0e5dea limits "each player" to her game area.
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        var hill = world.CreateCard("01067", world.Seats[1].Hand);
        string theirs = Snapshot(world, 0);
        int mine = world.Seats[1].Hand.Cards.Count;
        Agendas.Happening(world);
        bool responded = false;

        CardPlay.PutAllyIntoPlay(world, world.Facts, world.Abilities, hill, 1, "prepared entry", events);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 20, prompt.Description);
            var use = prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == hill.ObjectId);
            responded |= use is not null;
            Sequence.Answer(world, world.Facts, world.Abilities, prompt,
                use is not null ? Decision.Take(use.Id) : new ActingPolicy(7, declineOneIn: 1).Answer(prompt), events);
            prompt = Sequence.Work(world, world.Facts, world.Abilities, events);
        }

        Assert.True(responded);
        Assert.Equal(DeckType.AlliesArea, hill.Area.Type);
        Assert.Equal(mine, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(theirs, Snapshot(world, 0));
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
    public void TheMainSchemeQueryNamesTheRevealersOwnScheme()
    {
        // 01178: "When Revealed: Place 1 threat on the main scheme."
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        var central = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        long centralThreat = central.Tokens.GetValueOrDefault("k_threat");
        long mine = LocalScheme(world, 1).Tokens.GetValueOrDefault("k_threat");
        long theirs = LocalScheme(world, 0).Tokens.GetValueOrDefault("k_threat");

        Reveal(world, "01178", 1, events);

        Assert.Equal(mine + 1, LocalScheme(world, 1).Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(theirs, LocalScheme(world, 0).Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(centralThreat, central.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void AReferenceNeverReachesAnotherGameAreasCard()
    {
        // pack:mc11:game-areas: with no main scheme in the source's own game area
        // and none central, the other area's scheme is still out of reach.
        var world = Split(2, false, []);
        var removed = world.AreaOf(DeckType.RemovedArea);
        World.MoveToTop(LocalScheme(world, 1), removed);
        World.MoveToTop(Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards), removed);

        Assert.Null(CardReferences.TheCardIn(world, DeckType.MainSchemesArea, world.Seats[1].IdentityCard));
        Assert.Same(LocalScheme(world, 0),
            CardReferences.TheCardIn(world, DeckType.MainSchemesArea, world.Seats[0].IdentityCard));
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
        Hero(world, 1);
        int start = events.Count;

        Reveal(world, "01189", 1, events);

        var attacks = events.Skip(start).OfType<AttackBoundary>().Where(attack => attack.Started).ToList();
        Assert.DoesNotContain(attacks, attack => attack.Source == theirs.ObjectId);
        Assert.Contains(attacks, attack => attack.Source == mine.ObjectId && attack.Target == identity.ObjectId);
    }

    [Theory]
    [Rule("rr:amplify-icon.1")]
    [InlineData(0, 0)]
    [InlineData(1, 2)]
    public void AmplifyCountsOnlyIconsInTheActivatingEnemysGameArea(int holder, int added)
    {
        // "Add one additional boost icon to that card for each amplify icon in
        // play"; MARVEL-894 Q14 puts the boost card in the activating enemy's
        // area, and pack:mc11:game-areas keeps another area's icons out of reach.
        Assert.Equal(AssaultDamage(null) + added, AssaultDamage(holder));
    }

    [Fact]
    [Rule("rr:overkill.1")]
    public void OverkillSpillsOntoTheAttackersOwnKang()
    {
        // "Deal the damage beyond its hit points to the villain": the Kang in the
        // attacking character's game area.
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        string theirs = Snapshot(world, 0);
        var mine = LocalKang(world, 1);
        long health = Damage.Health(world, world.Facts, mine);
        bool tough = Statuses.Has(world, mine, Statuses.Tough);
        Agendas.Happening(world);
        var identity = world.Seats[1].IdentityCard;

        DamageAttacks.Resolve(world, world.Facts,
            new AttackDamageRequest(identity, identity, minion, 9, "prepared overkill", "Attack") { GrantedOverkill = true },
            events);
        Sequence.Finish(world, world.Facts, world.Abilities, events);

        Assert.Equal(theirs, Snapshot(world, 0));
        Assert.True(Damage.Health(world, world.Facts, mine) < health || tough && !Statuses.Has(world, mine, Statuses.Tough));
    }

    [Fact]
    public void MainSchemeCountersRefuseWhileTheTableIsSeparated()
    {
        // A captured live amount names no referring card, so it cannot choose
        // between the separate game areas' main schemes.
        var world = Split(2, false, []);

        Assert.Throws<RulesNotImplementedException>(() =>
            new ContinuousAmount.MainSchemeCounters("test").Evaluate(world));
    }

    // Player 1's own Kang attacks them through Assault (01187), boosted once.
    private static long AssaultDamage(int? amplifyHolder)
    {
        var events = new List<GameEvent>();
        var world = Split(2, false, events);
        if (amplifyHolder is int seat)
            world.Effects.Register(new(EffectSource.LastingEffect, "amplify", 2, Affects: LocalKang(world, seat).ObjectId));
        Hero(world, 1);
        long before = world.Seats[1].IdentityCard.Damage;
        Reveal(world, "01187", 1, events);
        return world.Seats[1].IdentityCard.Damage - before;
    }

    private static void Hero(World world, int seat)
    {
        var identity = world.Seats[seat].IdentityCard;
        if (!Forms.In(world, world.Seats[seat], world.Facts, Forms.Hero))
            identity.TurnTo(identity.FaceId.Replace('b', 'a'));
    }

    // Everything belonging to one seat: its game area, seat state (eliminated,
    // form, identity health and statuses, pile sizes), every card in its play
    // area with zone, order, face, readiness, damage and tokens, and every
    // registered effect on one of those cards or scoped to the seat itself.
    private static string Snapshot(World world, int seat)
    {
        var area = PlayArea.Of(seat);
        var owner = world.Seats[seat];
        var identity = owner.IdentityCard;
        string state = string.Join(':', owner.Eliminated,
            Forms.In(world, owner, world.Facts, Forms.Hero), Damage.Health(world, world.Facts, identity),
            string.Join(',', new[] { Statuses.Stunned, Statuses.Confused, Statuses.Tough }
                .Where(status => Statuses.Has(world, identity, status))),
            owner.Hand.Cards.Count, owner.Deck.Cards.Count);
        var cards = world.Cards.Where(card => card.Area.PlayArea == area)
            .OrderBy(card => card.ObjectId)
            .Select(card => string.Join(':', card.ObjectId, card.Area.Type, card.Area.Id,
                card.Area.Cards.ToList().IndexOf(card), card.FaceId, card.FaceUp, card.Ready, card.Damage,
                string.Join(',', card.Tokens.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => $"{pair.Key}={pair.Value}"))));
        var effects = world.Effects.Registered
            .Where(effect => effect.Scope == ContinuousEffect.CharactersControlledBy
                ? effect.Affects == seat
                : effect.Affects is int target && world.Cards[target].Area.PlayArea == area
                    || effect.Lasts?.NextTurnSeat == seat)
            .Select(effect => $"{effect.Source}:{effect.Kind}:{effect.Amount}>{effect.Affects}:{effect.Scope}");
        return string.Join('\n', [$"area {world.GameAreaOf(area)?.Id}", state, .. cards, .. effects]);
    }
}
