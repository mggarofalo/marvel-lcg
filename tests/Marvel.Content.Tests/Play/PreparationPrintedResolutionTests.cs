using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PreparationPrintedResolutionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:resolve.2")]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:quickstrike.2")]
    [Rule("ruling:420bd5b1763b9506")]
    public void PrintedBiteCompletesBeforeBothResponseOrders(bool suitFirst, bool reveal)
    {
        // Quickstrike is a "Forced Response (Hero)". "An ability is resolved when
        // it is triggered and one or more of its effects resolve."
        var f = new PreparationPrintedFixture(reveal: reveal);
        var entry = EntryResponse(f);
        Assert.Equal(3, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, f.Minion.Damage);
        Answer(f, entry, f.Bite.ObjectId);
        var responses = Work(f);
        Assert.Equal(2, f.Minion.Damage);
        Assert.True(Statuses.Has(f.World, f.Minion, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, f.Bite.Area.Type);
        Assert.Equal(1, responses.Player);
        Assert.Equal(2, responses.Affordances.Count);
        Answer(f, responses, suitFirst ? f.Suit.ObjectId : f.Hero.ObjectId);
        if (!suitFirst) ChooseEnemy(f);
        Answer(f, Work(f), suitFirst ? f.Hero.ObjectId : f.Suit.ObjectId);
        if (suitFirst) ChooseEnemy(f);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.True(f.Hero.Ready);
        Assert.False(f.Suit.Ready);
        Assert.Equal(1, f.Villain.Damage);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:resolve.7")]
    public void StunnedPrintedBiteDoesNotOfferCompletionResponses()
    {
        // "If all of the effects of an ability are canceled", it is not resolved.
        var f = new PreparationPrintedFixture();
        Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        Answer(f, EntryResponse(f), f.Bite.ObjectId);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, f.Bite.Area.Type);
        Assert.Equal(0, f.Minion.Damage);
        Assert.False(Statuses.Has(f.World, f.Minion, Statuses.Stunned));
        Assert.False(f.Hero.Ready);
        Assert.True(f.Suit.Ready);
        Assert.Equal(0, f.Villain.Damage);
    }

    private static Prompt Work(PreparationPrintedFixture f) =>
        Assert.IsType<Prompt>(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:resolve.2")]
    [Rule("rr:damage.step.7")]
    public void LethalBiteWaitsForDefeatAndSuspendedInterruptBeforeItsReceipt(bool nested)
    {
        // "If a character has zero or fewer remaining hit points, it is defeated."
        // "An ability is resolved when it is triggered and one or more of its effects resolve."
        var f = new PreparationPrintedFixture("01182");
        var observer = nested ? f.World.CreateCard("08023",
            f.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1)) : null;
        var entry = EntryResponse(f);
        f.Minion.TakeDamage(2);
        Answer(f, entry, f.Bite.ObjectId);
        var prompt = Work(f);
        if (nested)
        {
            Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == f.Suit.ObjectId || a.AnchorId == f.Hero.ObjectId);
            Answer(f, prompt, observer!.ObjectId);
            var choice = Work(f);
            Assert.Equal(Question.Option, choice.Asking);
            Assert.DoesNotContain(f.World.Agenda.Outstanding, s => s.Operation is PreparationAbilityResolvedOperation);
            string before = f.World.Digest().Canonical();
            var pending = f.World.Agenda.Outstanding.ToArray();
            var random = f.World.Random.Generator.GetState();
            Assert.Throws<ArgumentException>(() => Sequence.Answer(f.World, f.Cards, f.Runner, choice,
                Decision.Take(int.MaxValue), f.Events));
            Assert.Equal(before, f.World.Digest().Canonical());
            Assert.Equal(pending, f.World.Agenda.Outstanding.ToArray());
            Assert.Equal(random.Words, f.World.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, f.World.Random.Generator.GetState().Index);
            Sequence.Answer(f.World, f.Cards, f.Runner, choice, Decision.Take(choice.Affordances[0].Id), f.Events);
            prompt = Work(f);
        }
        Assert.Equal(DeckType.EncounterDiscardPile, f.Minion.Area.Type);
        Assert.False(Statuses.Has(f.World, f.Minion, Statuses.Stunned));
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(2, prompt.Affordances.Count);
        var receiptWindow = f.World.Agenda.Occurrence;
        Assert.IsType<ResolvedPreparationAbility>(receiptWindow!.ActorFacts?.PreparationAbility);
        string beforeResponse = f.World.Digest().Canonical();
        var responsePending = f.World.Agenda.Outstanding.ToArray();
        var responseRandom = f.World.Random.Generator.GetState();
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(f.World, f.Cards, f.Runner,
            prompt, Decision.Take(int.MaxValue), f.Events));
        Assert.Equal(beforeResponse, f.World.Digest().Canonical());
        Assert.Equal(responsePending, f.World.Agenda.Outstanding.ToArray());
        Assert.Equal(responseRandom.Words, f.World.Random.Generator.GetState().Words);
        Assert.Equal(responseRandom.Index, f.World.Random.Generator.GetState().Index);
        Answer(f, prompt, f.Hero.ObjectId);
        ChooseEnemy(f);
        var suitResponse = Work(f);
        Assert.Same(receiptWindow, f.World.Agenda.Occurrence);
        Assert.Single(suitResponse.Affordances);
        Answer(f, suitResponse, f.Suit.ObjectId);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.True(f.Hero.Ready);
        Assert.False(f.Suit.Ready);
        Assert.Equal(1, f.Villain.Damage);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:resolve.2")]
    public void FinalStunCannotFollowADepartedOrReenteredRecipient(bool reenter)
    {
        // A departing card "is considered to be a new copy of the card".
        // Damage already applied still resolves the Preparation ability.
        string movement = reenter
            ? """{"seq":[{"discard":"trigger.subject"},{"putIntoPlay":{"card":"trigger.subject","where":"engagedWithYou"}}]}"""
            : """{"discard":"trigger.subject"}""";
        var f = new PreparationPrintedFixture("01182", interruptEffect:
            "{\"choose\":{\"options\":[" + movement + ", {\"giveStatus\":{\"card\":\"you\",\"status\":\"confused\"}}]}}");
        var observer = f.World.CreateCard("08023",
            f.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var entry = EntryResponse(f);
        int incarnation = f.Minion.Incarnation;
        f.Minion.TakeDamage(2);
        Answer(f, entry, f.Bite.ObjectId);
        Answer(f, Work(f), observer.ObjectId);
        var choice = Work(f);
        Sequence.Answer(f.World, f.Cards, f.Runner, choice, Decision.Take(choice.Affordances[0].Id), f.Events);
        var response = Work(f);
        Assert.Equal(reenter ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, f.Minion.Area.Type);
        if (reenter) Assert.True(f.Minion.Incarnation > incarnation);
        Assert.False(Statuses.Has(f.World, f.Minion, Statuses.Stunned));
        Assert.Equal(0, f.Minion.Damage);
        Assert.Equal(2, response.Affordances.Count);
        Sequence.Answer(f.World, f.Cards, f.Runner, response, Decision.Decline, f.Events);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonterminalReadAfterOpaqueDefeatStillRefusesBeforePayment(bool nested)
    {
        // Engine boundary: a later board read needs a supported projected state.
        var f = new PreparationPrintedFixture("01182", boardRead: true);
        if (nested) f.World.CreateCard("08023",
            f.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var entry = EntryResponse(f);
        f.Minion.TakeDamage(2);
        string before = f.World.Digest().Canonical();
        var pending = f.World.Agenda.Outstanding.ToArray();
        var random = f.World.Random.Generator.GetState();
        Assert.Throws<RulesNotImplementedException>(() => Answer(f, entry, f.Bite.ObjectId));
        Assert.Equal(before, f.World.Digest().Canonical());
        Assert.Equal(pending, f.World.Agenda.Outstanding.ToArray());
        Assert.Equal(random.Words, f.World.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, f.World.Random.Generator.GetState().Index);
        Assert.Equal(DeckType.UpgradesArea, f.Bite.Area.Type);
    }

    private static Prompt EntryResponse(PreparationPrintedFixture f)
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

    private static void Answer(PreparationPrintedFixture f, Prompt prompt, int card)
    {
        var offered = Assert.Single(prompt.Affordances, a => a.AnchorId == card);
        Sequence.Answer(f.World, f.Cards, f.Runner, prompt, Decision.Take(offered.Id), f.Events);
    }

    private static void ChooseEnemy(PreparationPrintedFixture f) =>
        Sequence.Answer(f.World, f.Cards, f.Runner, Work(f), Decision.Take(f.Villain.ObjectId), f.Events);
}
