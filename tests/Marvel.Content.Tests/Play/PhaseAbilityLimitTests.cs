using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic actions isolate printed period and sharing semantics.</summary>
public sealed class PhaseAbilityLimitTests
{
    [Rule("rr:limit")]
    [Rule("rr:end-of-player-phase.step.4")]
    [Rule("rr:villain-phase.step.6")]
    [Fact]
    public void EachPhaseEndRestoresTheSamePhysicalAbility()
    {
        // Each copy may be used X times per the specified period, per instance.
        var runner = Runner("limitPerPhase");
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01007"), abilities: runner);
        for (int phase = 0; phase < 3; phase++)
        {
            var offered = Assert.Single(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
            int before = world.Seats[0].Hand.Cards.Count;
            runner.ExecuteAction(world, new PlayerAction(offered, [], []));
            Assert.Equal(before + 1, world.Seats[0].Hand.Cards.Count);
            Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(offered, [], [])));
            if (phase == 0) PhaseEnd.EndPlayerPhase(world, []);
            else PhaseEnd.EndVillainPhase(world, world.Facts, []);
        }
    }

    [Rule("rr:limit")]
    [Rule("rr:max-maximum")]
    [Theory]
    [InlineData("limitPerPhase", true)]
    [InlineData("maxPerPhase", false)]
    public void LimitBelongsToEachCopyButMaximumIsSharedAcrossPlayers(string property, bool otherAvailable)
    {
        // Limit is per instance; maximum is across all copies by title for all players.
        var runner = Runner(property);
        Card? first = null;
        Card? second = null;
        Card? third = null;
        var (_, world) = Playing(board =>
        {
            first = InPlay(board, "01007");
            second = InPlay(board, "01007");
            third = board.CreateCard("01007", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 0), item => item.Card == first!.ObjectId), [], []));
        Assert.Equal(otherAvailable, runner.Actions(world, 0).Any(item => item.Card == second!.ObjectId));
        Assert.Equal(otherAvailable, runner.Actions(world, 1).Any(item => item.Card == third!.ObjectId));
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.Contains(runner.Actions(world, 1), item => item.Card == third!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 1), item => item.Card == third!.ObjectId), [], []));
        PhaseEnd.EndVillainPhase(world, world.Facts, []);
        Assert.Contains(runner.Actions(world, 1), item => item.Card == third!.ObjectId);
    }

    [Rule("rr:limit")]
    [Fact]
    public void RoundLimitSurvivesThePlayerPhaseBoundary()
    {
        // The specified round period includes both phases.
        var runner = Runner("limitPerRound");
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01007"), abilities: runner);
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 0), item => item.Card == source!.ObjectId), [], []));
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        PhaseEnd.EndVillainPhase(world, world.Facts, []);
        Assert.Contains(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
    }

    [Theory]
    [InlineData("\"limitPerPhase\":0")]
    [InlineData("\"limitPerPhase\":-1")]
    [InlineData("\"limitPerPhase\":null")]
    [InlineData("\"limitPerPhase\":1.5")]
    [InlineData("\"limitPerPhase\":\"1\"")]
    [InlineData("\"limitPerPhase\":1,\"limitPerRound\":1")]
    public void InvalidPeriodEnvelopeFailsAdmission(string fields)
    {
        Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(Json(fields)));
    }

    [Fact]
    public void ProgrammaticPeriodCannotBypassLowering()
    {
        var book = AbilityCatalog.Parse(Json("\"limitPerPhase\":1"));
        var ability = book.Abilities[0];
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(book with
        { Abilities = [ability with { LimitPeriod = (AbilityLimitPeriod)99 }] }));
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(book with
        { Abilities = [ability with { Limit = null }] }));
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(book with
        { Abilities = [ability with { Limit = 0 }] }));
    }

    private static AbilityRunner Runner(string property) => new(AbilityCatalog.Parse(Json($"\"{property}\":1")));

    private static string Json(string fields) => $$$$"""
        {"cards":[{"card":"01007","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
          {{{{fields}}}},"effect":{"draw":{"player":"you","count":1}}}]}]}
        """;
}
