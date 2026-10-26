using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RapidResponseIdentityTests
{
    [Theory]
    [InlineData("hand", false)]
    [InlineData("roundtrip", true)]
    [InlineData("reorder", true)]
    [InlineData("reentry", false)]
    [Rule("rr:leaves-play.1")]
    public void MovementBeforeAnswerRequiresTheSameCopyInYourDiscard(string movement, bool eligible)
    {
        // "When a card leaves play ... it is considered to be a new copy".
        // Printed 08031 also requires "from your discard pile" at resolution.
        var f = new RapidResponseFixture();
        var prompt = f.Defeat();
        var discard = f.Ally.Area;
        World.MoveToTop(f.Ally, movement == "reorder" ? discard : movement == "reentry"
            ? f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)
            : f.World.Seats[0].Hand);
        if (movement != "hand") World.MoveToTop(f.Ally, discard);
        if (eligible)
        {
            f.Answer(prompt, f.Rapid.ObjectId);
            f.Work();
            Assert.Equal(DeckType.AlliesArea, f.Ally.Area.Type);
            Assert.Equal(1, f.Ally.Damage);
            return;
        }
        string before = f.World.Digest().Canonical();
        var pending = f.World.Agenda.Outstanding.ToArray();
        var rng = f.World.Random.Generator.GetState();
        Assert.Throws<RulesNotImplementedException>(() => f.Answer(prompt, f.Rapid.ObjectId));
        Assert.Equal(before, f.World.Digest().Canonical());
        Assert.Equal(pending, f.World.Agenda.Outstanding);
        Assert.Equal(rng.Words, f.World.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, f.World.Random.Generator.GetState().Index);
        Assert.Equal(DeckType.UpgradesArea, f.Rapid.Area.Type);
    }

    [Fact]
    public void OrdinaryDiscardDoesNotCreateAResponse()
    {
        var f = new RapidResponseFixture();
        Discard.Card(f.World, f.Ally, "Fixture", f.Events);
        Assert.Null(Sequence.Work(f.World, f.World.Facts, f.Runner, f.Events));
        Assert.Equal(DeckType.UpgradesArea, f.Rapid.Area.Type);
        Assert.Equal(DeckType.DiscardPile, f.Ally.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidAnswerOrUnavailableDiscardCostPreservesTheExactPendingWork(bool payment)
    {
        var f = new RapidResponseFixture();
        var prompt = f.Defeat();
        var sourceArea = f.Rapid.Area;
        if (payment) World.MoveToTop(f.Rapid, f.World.Seats[0].Hand);
        string before = f.World.Digest().Canonical();
        var pending = f.World.Agenda.Outstanding.ToArray();
        var occurrence = f.World.Agenda.Occurrence;
        var rng = f.World.Random.Generator.GetState();
        long words = f.World.Random.Generator.WordsConsumed;
        int count = f.Events.Count;
        int offer = prompt.Affordances.Single(a => a.AnchorId == f.Rapid.ObjectId).Id;
        var invalid = Decision.Take(payment ? offer : int.MaxValue);
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt, invalid, f.Events));
        Assert.Equal(before, f.World.Digest().Canonical());
        Assert.Equal(pending, f.World.Agenda.Outstanding);
        Assert.Same(occurrence, f.World.Agenda.Occurrence);
        Assert.Equal(rng.Words, f.World.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, f.World.Random.Generator.GetState().Index);
        Assert.Equal(words, f.World.Random.Generator.WordsConsumed);
        Assert.Equal(count, f.Events.Count);
        if (payment) World.MoveToTop(f.Rapid, sourceArea);
        f.Answer(prompt, f.Rapid.ObjectId);
        f.Work();
        var control = new RapidResponseFixture();
        control.Answer(control.Defeat(), control.Rapid.ObjectId);
        control.Work();
        Assert.Equal(control.World.Digest().Canonical(), f.World.Digest().Canonical());
        Assert.Equal(control.World.Random.Generator.GetState().Words, f.World.Random.Generator.GetState().Words);
        Assert.Equal(control.World.Agenda.Outstanding.Select(step => (step.What, step.Subject, step.Seat, step.OccurrenceId)),
            f.World.Agenda.Outstanding.Select(step => (step.What, step.Subject, step.Seat, step.OccurrenceId)));
    }
}
