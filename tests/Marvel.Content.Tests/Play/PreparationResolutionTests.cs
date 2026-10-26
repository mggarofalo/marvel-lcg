using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic synthetic rows exercising the Preparation completion contract.</summary>
public sealed class PreparationResolutionTests
{
    [Rule("rr:resolve.2")]
    [Rule("rr:response.3")]
    [Rule("ruling:420bd5b1763b9506")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedPreparationOffersBothResponsesInEitherOrder(bool suitFirst)
    {
        // "An ability is resolved when it is triggered and one or more of its
        // effects resolve." The current clarification applies "resolve" to both cards.
        var f = new PreparationResolutionFixture();
        int incarnation = f.Preparation.Incarnation;
        var initial = f.Work();
        f.Answer(initial, f.Preparation.ObjectId);
        Assert.Equal(DeckType.DiscardPile, f.Preparation.Area.Type);
        Assert.DoesNotContain(f.World.Agenda.Outstanding, step => step.Operation is PreparationAbilityResolvedOperation);
        var choice = f.Work();
        Assert.Equal(Question.Option, choice.Asking);
        Assert.Equal(1, choice.Player);
        var digest = System.Text.Json.JsonSerializer.Serialize(f.World.Digest());
        Assert.Throws<ArgumentException>(() => Sequence.Answer(f.World, f.Cards, f.Runner, choice, Decision.Take(int.MaxValue), f.Events));
        Assert.Equal(digest, System.Text.Json.JsonSerializer.Serialize(f.World.Digest()));
        if (suitFirst) World.MoveToTop(f.Preparation,
            f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Sequence.Answer(f.World, f.Cards, f.Runner, choice, Decision.Take(choice.Affordances[0].Id), f.Events);
        var responses = f.Work();
        var occurrence = f.World.Agenda.Occurrence!;
        var receipt = Assert.IsType<ResolvedPreparationAbility>(occurrence.ActorFacts?.PreparationAbility);
        Assert.Equal(f.Preparation.ObjectId, receipt.Source);
        Assert.Equal("08010", receipt.Face);
        Assert.Equal(incarnation, receipt.Incarnation);
        Assert.Equal(1, receipt.Controller);
        Assert.Equal(1, receipt.Player);
        Assert.Contains("PREPARATION", receipt.Traits);
        Assert.Equal(1, responses.Player);
        Assert.Equal(2, responses.Affordances.Count);
        f.Answer(responses, suitFirst ? f.Suit.ObjectId : f.Hero.ObjectId);
        var second = f.Work();
        Assert.Same(occurrence, f.World.Agenda.Occurrence);
        f.Answer(second, suitFirst ? f.Hero.ObjectId : f.Suit.ObjectId);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.False(f.Suit.Ready);
        Assert.True(f.Hero.Ready);
        Assert.Equal(1, f.Villain.Damage);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Rule("rr:resolve.5")]
    [Rule("rr:resolve.7")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoAppliedEffectOrConstantDoesNotPublishAReceipt(bool constant)
    {
        // "Constant abilities are never considered resolved." "If all of the
        // effects of an ability are canceled", it is not considered resolved.
        var f = new PreparationResolutionFixture();
        var cause = new Occurrence(98, [Steps.CardEntersPlay], Player: 1);
        var ability = new PendingAbility(f.Preparation.ObjectId,
            constant ? AbilityType.Constant : AbilityType.Response, 1);
        var invocation = f.World.Agenda.Executions.Begin(cause, ability);
        cause.Resolution.Begin(ability);
        PreparationAbilityCompletion.Track(f.World, invocation, f.Preparation, 1);
        if (constant) cause.Resolution.Resolve(ability);
        invocation.Finish([]);
        Assert.DoesNotContain(f.World.Agenda.Outstanding, step => step.Operation is PreparationAbilityResolvedOperation);
        Assert.Equal(ResolutionStatus.Unresolved, cause.Resolution.StatusOf(ability));
    }

    [Rule("rr:resolve.6")]
    [Fact]
    public void MerePlayCreatesNoCompletedAbilityCondition()
    {
        // "Card types other than events and treacheries are not considered resolved."
        var f = new PreparationResolutionFixture();
        var occurrence = new Occurrence(99, [Steps.CardPlayed], f.Preparation.ObjectId, 1);
        Assert.DoesNotContain(f.Runner.Waiting(f.World, occurrence, WindowKind.Response),
            ability => ability.Card == f.Hero.ObjectId || ability.Card == f.Suit.ObjectId);
    }
}
