using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttachmentSurgeContinuationTests
{
    [Fact]
    [Rule("rr:first-player.1")]
    [Rule("rr:surge.1")]
    public void TargetChoiceCompletesBeforeSurgeInterruptAndCannotBeAnsweredAgain()
    {
        // First player resolves target ties; surge says "Deal yourself 1 facedown encounter card."
        var f = new EspionageSurgeFixture();
        f.World.Agenda.Abandon();
        World.MoveToTop(f.Encounter, f.World.AreaOf(DeckType.EncounterDiscardPile));
        var first = f.World.CreateCard("01167", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var second = f.World.CreateCard("01167", first.Area);
        var attachment = f.World.CreateCard("01185", f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: attachment.ObjectId, Seat: 1));
        var target = f.Work();
        Assert.Equal(Steps.ChooseAttachmentTarget, target.Trigger);
        Assert.Equal(f.World.FirstPlayer, target.Player);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, target.Affordances.Select(offer => offer.Id));
        string unchosen = f.World.Digest().Canonical();
        var waiting = f.World.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(f.World, f.Cards, f.Runner,
            target, Decision.Take(int.MaxValue), f.Events));
        Assert.Equal(unchosen, f.World.Digest().Canonical());
        Assert.Equal(waiting, f.World.Agenda.Outstanding.ToArray());
        Sequence.Answer(f.World, f.Cards, f.Runner, target, Decision.Take(second.ObjectId), f.Events);

        var surge = f.Work();
        Assert.Equal("WhenSurgeWouldResolve", surge.Trigger);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
        Assert.Equal(second.ObjectId, attachment.Area.Host);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        string before = f.World.Digest().Canonical();
        var pending = f.World.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(f.World, f.Cards, f.Runner,
            target, Decision.Take(first.ObjectId), f.Events));
        Assert.Equal(before, f.World.Digest().Canonical());
        Assert.Equal(pending, f.World.Agenda.Outstanding.ToArray());

        f.Answer(surge, f.Espionage[0].ObjectId);
        f.Finish();
        Assert.Equal(second.ObjectId, attachment.Area.Host);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(f.World.Agenda.Outstanding);
    }
}
