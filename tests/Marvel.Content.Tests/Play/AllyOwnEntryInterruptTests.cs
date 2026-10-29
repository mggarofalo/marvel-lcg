using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic entry ability; this does not admit a new card to the ability book.
public sealed class AllyOwnEntryInterruptTests
{
    private static AbilityRunner Runner(string timing = "Interrupt") => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"06012","abilities":[{
          "trigger":{"event":"WhenCardEntersPlay","timing":"TIMING","subject":"this"},
          "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}
        }]}]}
        """.Replace("TIMING", timing, StringComparison.Ordinal)));

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Rule("rr:initiating-abilities.1")]
    [Rule("rr:ability.11")]
    public void OwnEntryInterruptIsOptionalAndCountersSurviveEntry(bool play, bool take)
    {
        // An interrupt "may be initiated just before that triggering condition becomes true";
        // unless forced, interrupts are optional.
        var runner = Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        var energy = world.CreateCard("06022", seat.Hand);
        var physical = world.CreateCard("01003", seat.Hand);
        int originIncarnation = ally.Incarnation;
        if (play)
            CardPlay.Play(world, world.Facts, runner, seat, ally,
                [energy.ObjectId, physical.ObjectId], []);
        else
            CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);

        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.False(DeckTypes.IsInPlay(ally.Area.Type));
        Assert.Equal(originIncarnation, ally.Incarnation);
        var offer = Assert.Single(prompt.Affordances, option => option.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt,
            take ? Decision.Take(offer.Id) : Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(originIncarnation + 1, ally.Incarnation);
        Assert.Equal(take ? 1 : 0, ally.Tokens.GetValueOrDefault("c_test"));
        Assert.Empty(world.Agenda.Outstanding);
    }
    [Fact]
    public void ExistingResponseControlRunsAfterCompletedEntry()
    {
        var runner = Runner("Response");
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var ally = world.CreateCard("06012", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        var offer = Assert.Single(prompt.Affordances, option => option.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, ally.Tokens["c_test"]);
        Assert.Empty(world.Agenda.Outstanding);
    }
    [Fact]
    public void OutOfPlayAllyCannotUseAnUnrelatedEntryInterrupt()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"06012","abilities":[{
              "trigger":{"event":"WhenCardEntersPlay","timing":"Interrupt","subject":"game"},
              "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}
            }]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var ally = world.CreateCard("06012", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, ally.Tokens.GetValueOrDefault("c_test"));
    }
}
