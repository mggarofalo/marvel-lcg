using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Wilt and Groot's nemesis set on synthetic boards.
public sealed class GrootEncounterTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:obligation.1")]
    public void WiltMayFlipThenExhaustTheAlterEgoToLeaveTheGame()
    {
        // 16025: "You may flip to alter-ego form. Choose: • Exhaust your alter-ego →
        // remove Wilt from the game."
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 4);
        var wilt = Reveal(world, "16025");
        var runner = GrootBoards.Runner(world);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[0].Id), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal("16001b", groot.FaceId);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(choice.Affordances[0].Id), []);
        Sequence.Finish(world, world.Facts, runner, []);

        Assert.False(groot.Ready);
        Assert.Equal(4, GrootBoards.Growth(groot));
        Assert.Equal(DeckType.RemovedArea, wilt.Area.Type);
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(2, 0)]
    [Rule("rr:obligation.1")]
    public void WiltOtherwiseRemovesUpToThreeGrowthCounters(int growth, long left)
    {
        // "• Remove 3 growth counters from Groot. If no growth counters were removed this
        // way, this card gains surge. Discard this obligation." The removal does as much
        // as it can. In hero form the alter-ego option has no target.
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, growth);
        var wilt = Reveal(world, "16025");
        var runner = GrootBoards.Runner(world);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[^1].Id), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(Assert.Single(choice.Affordances).Id), []);
        Sequence.Finish(world, world.Facts, runner, []);

        Assert.Equal(left, GrootBoards.Growth(groot));
        Assert.True(groot.Ready);
        Assert.Equal(12, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, wilt.Area.Type);
    }

    [Fact]
    [Rule("rr:surge")]
    public void WiltGainsSurgeWhenNoGrowthCounterWasRemoved()
    {
        // "If no growth counters were removed this way, this card gains surge."
        // rr:surge: the player reveals another encounter card.
        var world = GrootBoards.Board();
        var wilt = Reveal(world, "16025");
        var runner = GrootBoards.Runner(world);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[^1].Id), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(Assert.Single(choice.Affordances).Id), []);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(11, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, wilt.Area.Type);
    }

    [Fact]
    [Rule("rr:indirect-damage")]
    public void BlazingInfernoDealsTwoIndirectDamageToEachPlayerAsTheVillainPhaseBegins()
    {
        // 16026: "Forced Response: After the villain phase begins, deal 2 indirect damage
        // to each player." Groot is in alter-ego form, so the phase schemes rather than
        // attacks; each player takes 2.
        var world = GrootBoards.Board("16001b", players: 2);
        world.Seats[1].IdentityCard.TurnTo("01001b");
        GrootBoards.Side(world, "16026", 3);
        // Hard to Keep Down heals the damaged villain rather than surging.
        GrootBoards.Villain(world).TakeDamage(20);

        VillainPhase.Schedule(world.Agenda, 1);
        var runner = GrootBoards.Runner(world);
        for (var prompt = Sequence.Work(world, world.Facts, runner, []);
             prompt is not null && (prompt.Asking == Question.Order ? InPlayerOrder(prompt)
                : AssignTo(world.Seats[prompt.Player].IdentityCard)(prompt)) is { } answer;
             prompt = Sequence.Work(world, world.Facts, runner, []))
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);

        Assert.Equal(2, GrootBoards.Groot(world).Damage);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:activation")]
    public void FurnaxDealsTwoIndirectDamageToEachPlayerAfterItActivates(bool scheme)
    {
        // 16027: "Forced Response: After Furnax activates, deal 2 indirect damage to each
        // player." rr:activation: "Whenever an enemy attacks or schemes, it is considered
        // to have activated." Groot has no growth counters, so Flora Colossus does nothing.
        var world = GrootBoards.Board(scheme ? "16001b" : "16001a");
        var groot = GrootBoards.Groot(world);
        var furnax = GrootBoards.Engaged(world, "16027");

        KangTemporalFixture.Activate(world, furnax, scheme, 0);
        GrootBoards.Drive(world, AssignTo(groot));

        Assert.Equal(scheme ? 2 : 4, groot.Damage);
    }

    [Theory]
    [InlineData(false, false, 2)]
    [InlineData(true, false, 3)]
    [InlineData(true, true, 4)]
    [Rule("rr:indirect-damage")]
    public void FanTheFlamesAddsOneForBlazingInfernoAndOneForFurnax(bool inferno, bool furnax, long taken)
    {
        // 16028: "Take 2 indirect damage. If Blazing Inferno is in play, take 1 additional
        // indirect damage. If Furnax is in play, take 1 additional indirect damage."
        var world = GrootBoards.Board("16001b");
        var groot = GrootBoards.Groot(world);
        if (inferno) GrootBoards.Side(world, "16026", 3);
        if (furnax) GrootBoards.Engaged(world, "16027");

        Reveal(world, "16028");
        GrootBoards.Drive(world, AssignTo(groot));

        Assert.Equal(taken, groot.Damage);
    }

    // rr:each-player.1: the first player orders an unordered "each player" effect.
    private static Decision InPlayerOrder(Prompt prompt) =>
        Decision.Take(prompt.Affordances[0].Id) with { Targets = [.. prompt.Affordances[0].Targets!.Legal] };

    /// <summary>Assigns each indirect damage request entirely to <paramref name="card"/>.</summary>
    internal static Func<Prompt, Decision?> AssignTo(Card card) => prompt =>
        prompt.Affordances.FirstOrDefault(a => a.Targets is { AllowRepeated: true }) is { } assign
            ? Decision.Take(assign.Id) with { Targets = [.. Enumerable.Repeat(card.ObjectId, assign.Targets!.Max)] }
            : null;

    internal static Card Reveal(World world, string face)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        GrootBoards.Runner(world).ExecuteReveal(world, card, 0);
        return card;
    }
}
