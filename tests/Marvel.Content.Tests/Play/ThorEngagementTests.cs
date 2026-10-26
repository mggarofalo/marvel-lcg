using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic Thor bodies prove engagement independently of pack admission.</summary>
public sealed class ThorEngagementTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0, 0, true, 2)]
    [InlineData(1, 1, true, 2)]
    [InlineData(0, 1, true, 0)]
    [InlineData(1, 0, true, 0)]
    [InlineData(0, 0, false, 2)]
    [InlineData(1, 1, false, 2)]
    [Rule("rr:engage")]
    public void EntryAndRevealDrawOnlyForTheEngagedThor(int thor, int engaged, bool reveal, int drawn)
    {
        // "When a minion enters play in a player's play area, it engages that player."
        var world = World(thor);
        var runner = Runner();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        if (reveal) RevealMinion(world, minion, engaged);
        else EncounterEntry.Schedule(world, EncounterEntryRequest.For(minion, engaged,
            new CardEntryPlacement(Verb: "Put_Into_Play")), new EffectCompletion(1, 1));
        Finish(world, runner, prompt =>
        {
            if (!prompt.Affordances.Any(a => a.Timing == AbilityType.Response)) return;
            Assert.Equal(minion.ObjectId, world.Agenda.Occurrence!.Subject);
            Assert.Equal(engaged, world.Agenda.Occurrence.Player);
        });
        Assert.Equal(drawn, world.Seats[thor].Hand.Cards.Count);
        Assert.Equal(PlayArea.Of(engaged), minion.Area.PlayArea);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:engage.2")]
    [Rule("rr:engage.3")]
    [Rule("rr:limit")]
    public void TransferDrawsOncePerPhaseAndSameSeatDoesNotEngageAgain(int thor)
    {
        // "that minion is also considered to have engaged that player"; abilities cannot
        // "cause the minion to engage with the same player again". 06001a limits each phase.
        // FAQ 06014: "You cannot engage an enemy that is already engaged with you."
        var world = World(thor);
        var runner = Runner();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1 - thor)));
        minion.TakeDamage(1);
        minion.Exhaust();
        Transfer(world, runner, minion, thor);
        Assert.Equal(2, world.Seats[thor].Hand.Cards.Count);
        Assert.Equal(2, minion.Damage); // synthetic Get Over Here! dealt one before moving it
        Assert.False(minion.Ready);
        Transfer(world, runner, minion, thor, damage: false);
        Assert.Equal(2, world.Seats[thor].Hand.Cards.Count);
        Transfer(world, runner, minion, 1 - thor, damage: false);
        Transfer(world, runner, minion, thor, damage: false);
        Assert.Equal(2, world.Seats[thor].Hand.Cards.Count);
        PhaseEnd.EndPlayerPhase(world, []);
        Transfer(world, runner, minion, 1 - thor, damage: false);
        Transfer(world, runner, minion, thor, damage: false);
        Assert.Equal(4, world.Seats[thor].Hand.Cards.Count);
        PhaseEnd.EndVillainPhase(world, world.Facts, []);
        Transfer(world, runner, minion, 1 - thor, damage: false);
        Transfer(world, runner, minion, thor, damage: false);
        Assert.Equal(6, world.Seats[thor].Hand.Cards.Count);
    }

    [Fact]
    [Rule("rr:engage.3")]
    public void AnAlreadyEngagedMinionDoesNotCreateAResponseOrResetItsState()
    {
        // "as the two are already engaged" excludes a new engagement occurrence.
        var world = World(0);
        var runner = Runner();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var before = world.Digest().Canonical();
        EncounterEntry.Schedule(world, EncounterEntryRequest.For(minion, 0,
            new CardEntryPlacement()), new EffectCompletion(1, 1));
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(world.Seats[0].Hand.Cards);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:quickstrike.2")]
    public void QuickstrikeFinishesBeforeThorsOptionalResponse(bool reveal)
    {
        // "quickstrike keyword resolves after any When Revealed abilities on that minion".
        var world = World(0);
        var runner = Runner();
        var minion = world.CreateCard("01167", world.AreaOf(DeckType.EncounterDiscardPile));
        if (reveal) RevealMinion(world, minion, 0);
        else EncounterEntry.Schedule(world, EncounterEntryRequest.For(minion, 0,
            new CardEntryPlacement()), new EffectCompletion(1, 1));
        int responseDamage = -1;
        Finish(world, runner, prompt =>
        {
            if (prompt.Affordances.Any(a => a.AnchorId == world.Seats[0].IdentityCard.ObjectId
                && a.Timing == AbilityType.Response))
                responseDamage = (int)world.Seats[0].IdentityCard.Damage;
        });
        Assert.Equal(reveal ? 4 : 3, responseDamage);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void WindowQueriesLeaveStateRandomAndPendingWorkUntouched()
    {
        var world = World(0);
        var runner = Runner();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var occurrence = new Occurrence(17, [MinionEngagement.Engaged], minion.ObjectId, 0);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());
        for (int query = 0; query < 3; query++)
            Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(before.Item1, world.Digest().Canonical());
        Assert.Equal(before.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Item3, world.Agenda.Outstanding.ToArray());
        var control = World(0);
        control.CreateCard("01101", control.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        foreach (var board in new[] { world, control })
        {
            var arriving = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            EncounterEntry.Schedule(board, EncounterEntryRequest.For(arriving, 0,
                new CardEntryPlacement()), new EffectCompletion(1, 1));
            Finish(board, runner);
        }
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void GetOverHereWithoutAerialDealsDamageWithoutEngaging()
    {
        // 06014: "If you have the Aerial trait, engage that enemy."
        var world = World(0);
        var runner = Runner();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var source = world.CreateCard("06014", world.AreaOf(DeckType.RevealingArea, PlayArea.Of(0), cardOwner: 0));
        runner.ExecuteReveal(world, source, 0);
        Finish(world, runner);
        Assert.Equal(1, minion.Damage);
        Assert.Equal(PlayArea.Of(1), minion.Area.PlayArea);
        Assert.Empty(world.Seats[0].Hand.Cards);
    }

    [Fact]
    public void RevealingATreacheryDoesNotCreateAnEngagement()
    {
        var world = World(0);
        var runner = Runner();
        var card = world.CreateCard("01188", world.AreaOf(DeckType.RevealingArea));
        RevealMinion(world, card, 0);
        Finish(world, runner);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Empty(world.Seats[0].Hand.Cards);
    }

    private static World World(int thor)
    {
        var world = new World(Cards, 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == thor ? "06001a" : "01001a", seat.Hero);
        }
        for (int seat = 0; seat < 2; seat++)
            for (int index = 0; index < 12; index++)
                world.CreateCard("01088", world.Seats[seat].Deck);
        return world;
    }

    private static void RevealMinion(World world, Card minion, int player)
    {
        Marvel.Rules.State.World.MoveToTop(minion, world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 2, Subject: minion.ObjectId, Seat: player));
    }

    private static void Transfer(World world, AbilityRunner runner, Card minion, int player, bool damage = true)
    {
        if (damage)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: Traits.Granted + "AERIAL", Affects: world.Seats[player].IdentityCard.ObjectId));
        var source = world.CreateCard(damage ? "06014" : "06003",
            world.AreaOf(DeckType.RevealingArea, PlayArea.Of(player), cardOwner: player));
        runner.ExecuteReveal(world, source, player);
        Finish(world, runner);
        Assert.Equal(PlayArea.Of(player), minion.Area.PlayArea);
    }

    private static void Finish(World world, AbilityRunner runner, Action<Prompt>? observe = null)
    {
        var events = new List<GameEvent>();
        for (int step = 0; step < 40; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) { Assert.Empty(world.Agenda.Outstanding); return; }
            observe?.Invoke(prompt);
            var response = prompt.Affordances.FirstOrDefault(a => a.AnchorId == world.Seats[prompt.Player].IdentityCard.ObjectId
                && a.Timing == AbilityType.Response);
            var answer = response is not null ? Decision.Take(response.Id)
                : prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id);
            Sequence.Answer(world, world.Facts, runner, prompt, answer, events);
        }
        Assert.Fail("engagement did not finish");
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"06001a","abilities":[{"limitPerPhase":1,
            "trigger":{"event":"WhenMinionEngaged","timing":"Response","subject":"you","form":"hero"},
            "effect":{"draw":{"player":"you","count":2}}}]},
          {"card":"06014","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
            "effect":{"seq":[{"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":1}},
              {"if":{"test":{"hasTrait":{"card":"you","trait":"AERIAL"}},
                "then":{"putIntoPlay":{"card":{"titled":"Hydra Mercenary"},"where":"engagedWithYou"}}}}]}}]},
          {"card":"06003","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
            "effect":{"putIntoPlay":{"card":{"titled":"Hydra Mercenary"},"where":"engagedWithYou"}}}]},
          {"card":"01101","abilities":[]},
          {"card":"01188","abilities":[]},
          {"card":"01167","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
            "effect":{"dealDamage":{"cards":"you","amount":1}}}]}
        ]}
        """));
}
