using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PreparationContinuationIndependentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:resolve.2")]
    public void FalsePresenceBranchAndChainedReadsRunAfterRecipientAndSourceReentry(bool reenter)
    {
        // A departing card "is considered to be a new copy of the card".
        // Synthetic tail: both false branches must observe the retained incarnation.
        var f = new PreparationPrintedFixture("01182", tail: """
            {"seq":[
              {"if":{"test":{"isInPlay":"chosen"},
                "then":{"giveStatus":{"card":"chosen","status":"stunned"}},
                "else":{"draw":{"player":"you","count":1}}}},
              {"if":{"test":{"not":{"isInPlay":"chosen"}},
                "then":{"draw":{"player":"you","count":1}}}}
            ]}
            """);
        var observer = f.World.CreateCard("08023",
            f.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var entry = Entry(f);
        int recipientIncarnation = f.Minion.Incarnation;
        int sourceIncarnation = f.Bite.Incarnation;
        f.Minion.TakeDamage(2);
        Answer(f, entry, f.Bite.ObjectId);
        Answer(f, Work(f), observer.ObjectId);
        var choice = Work(f);
        // Exercise legal movement while the damage procedure is suspended. The
        // source may finish its already paid ability after changing controller.
        World.MoveToTop(f.Minion, f.World.AreaOf(DeckType.EncounterDiscardPile));
        if (reenter) World.MoveToTop(f.Minion,
            f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        World.MoveToTop(f.Bite,
            f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        int firstHand = f.World.Seats[0].Hand.Cards.Count;
        int secondHand = f.World.Seats[1].Hand.Cards.Count;
        Sequence.Answer(f.World, f.Cards, f.Runner, choice,
            Decision.Take(choice.Affordances[0].Id), f.Events);
        var responses = Work(f);
        Assert.Equal(recipientIncarnation + (reenter ? 1 : 0), f.Minion.Incarnation);
        Assert.True(f.Bite.Incarnation > sourceIncarnation);
        Assert.Equal(firstHand, f.World.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHand + 2, f.World.Seats[1].Hand.Cards.Count);
        Assert.False(Statuses.Has(f.World, f.Minion, Statuses.Stunned));
        var receipt = Assert.IsType<ResolvedPreparationAbility>(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal(sourceIncarnation, receipt.Incarnation);
        Assert.Equal(1, receipt.Controller);
        Assert.Equal(1, responses.Player);
        Assert.Equal(2, responses.Affordances.Count);
        Sequence.Answer(f.World, f.Cards, f.Runner, responses, Decision.Decline, f.Events);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.True(f.Suit.Ready);
        Assert.False(f.Hero.Ready);
    }

    [Fact]
    [Rule("rr:response.4")]
    public void DecliningPreparationNeverPublishesCompletion()
    {
        // Once all players decline, "responses to that instance of that
        // triggering condition cannot be used."
        var f = new PreparationPrintedFixture();
        Sequence.Answer(f.World, f.Cards, f.Runner, Entry(f), Decision.Decline, f.Events);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.Equal(DeckType.UpgradesArea, f.Bite.Area.Type);
        Assert.Equal(0, f.Minion.Damage);
        Assert.True(f.Suit.Ready);
        Assert.False(f.Hero.Ready);
        Assert.DoesNotContain(f.World.Agenda.Outstanding,
            step => step.Operation is PreparationAbilityResolvedOperation);
    }

    private static Prompt Entry(PreparationPrintedFixture f)
    {
        for (int i = 0; i < 12; i++)
        {
            var prompt = Work(f);
            if (prompt.Affordances.Any(a => a.AnchorId == f.Bite.ObjectId)) return prompt;
            Assert.True(prompt.Cancellable);
            Sequence.Answer(f.World, f.Cards, f.Runner, prompt, Decision.Decline, f.Events);
        }
        throw new InvalidOperationException("entry response was not reached");
    }

    private static Prompt Work(PreparationPrintedFixture f) =>
        Assert.IsType<Prompt>(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));

    private static void Answer(PreparationPrintedFixture f, Prompt prompt, int source) =>
        Sequence.Answer(f.World, f.Cards, f.Runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == source).Id), f.Events);
}
