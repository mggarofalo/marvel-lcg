using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class DecisionAssessmentTests
{
    [Fact]
    public void EmptyAnswerValuesCannotBeMutatedAcrossGames()
    {
        var values = (IDictionary<string, long>)Decision.Take(7).DefinedValues;
        Assert.Throws<NotSupportedException>(() => values.Add("X", 1));
        Assert.Empty(Decision.Take(8).DefinedValues);
        Assert.Empty(Decision.Decline.DefinedValues);
    }

    [Theory]
    [InlineData("X")]
    [InlineData("Y")]
    public void AlternativeCostsValidateTheirOwnVariables(string secondVariable)
    {
        Prompt prompt = Ask(new Affordance(7, "Use", 10, 0, "Choose a price", Costs:
        [
            new CostOption(0, "0", Variables: [new VariableRequest("X", 1, 2)]),
            new CostOption(0, "0", Variables: [new VariableRequest(secondVariable, 3, 4)]),
        ]));
        var answer = new Decision(7, [], Values: new Dictionary<string, long> { [secondVariable] = 3 });

        Assert.True(DecisionAssessment.Assess(prompt, answer).IsAccepted);
        Assert.True(DecisionAssessment.Assess(prompt, answer, costIndex: 1).IsAccepted);
        Assert.Equal(DecisionRejection.InvalidVariables,
            DecisionAssessment.Assess(prompt, answer, costIndex: 0).Rejection);
    }

    [Fact]
    public void VariablesAndResourcesCannotBeBorrowedFromDifferentAlternatives()
    {
        Prompt prompt = Ask(new Affordance(7, "Use", 10, 0, "Choose a price", Costs:
        [
            new CostOption(0, "1", Sources: [new ResourceSource(11, "R")],
                Variables: [new VariableRequest("X", 1, 1)]),
            new CostOption(0, "1", Sources: [new ResourceSource(12, "R")],
                Variables: [new VariableRequest("Y", 1, 1)]),
        ]));
        var answer = new Decision(7, [], [12], new Dictionary<string, long> { ["X"] = 1 },
            [new ResourceAllocation(12, 0, "R")]);

        Assert.False(DecisionAssessment.Assess(prompt, answer).IsAccepted);
        Assert.True(DecisionAssessment.Assess(prompt,
            answer with { Values = new Dictionary<string, long> { ["Y"] = 1 } }).IsAccepted);
    }

    [Fact]
    public void APriceForAnUnselectedTargetCannotPayForTheChosenTarget()
    {
        Prompt prompt = Ask(new Affordance(7, "Use", 10, 0, "Choose", new TargetRequest([20, 21], 1, 1),
            [new CostOption(20, "0"), new CostOption(21, "1", Sources: [])]));

        Assert.True(DecisionAssessment.Assess(prompt, new Decision(7, [20])).IsAccepted);
        Assert.Equal(DecisionRejection.InvalidPayment,
            DecisionAssessment.Assess(prompt, new Decision(7, [21])).Rejection);
        Assert.Equal(DecisionRejection.CostTargetMismatch,
            DecisionAssessment.Assess(prompt, new Decision(7, [21]), costIndex: 0).Rejection);
    }

    [Fact]
    public void OffTurnActionsBelongToTheirOfferedOwner()
    {
        Prompt prompt = Ask(new Affordance(7, Game.ActionVerb, 10, 1, "Other player's Action"));

        Assert.True(DecisionAssessment.Assess(prompt, Decision.Take(7), actor: 1).IsAccepted);
        Assert.Equal(DecisionRejection.UnauthorizedActor,
            DecisionAssessment.Assess(prompt, Decision.Take(7), actor: 0).Rejection);
        Assert.True(DecisionAssessment.Assess(prompt with { Cancellable = true }, Decision.Decline, actor: 0).IsAccepted);
        Assert.Equal(DecisionRejection.UnauthorizedActor,
            DecisionAssessment.Assess(prompt with { Cancellable = true }, Decision.Decline, actor: 1).Rejection);
    }

    [Fact]
    public void DeclinesAndFreeActionsRejectStrayCommitments()
    {
        Prompt prompt = Ask(new Affordance(7, "Use", 10, 0, "Free")) with { Cancellable = true };
        foreach (int handle in new[] { -1, 7 })
        {
            Assert.Equal(DecisionRejection.UnexpectedAnswerData,
                DecisionAssessment.Assess(prompt, new Decision(handle, [], [11])).Rejection);
            Assert.Equal(DecisionRejection.UnexpectedAnswerData,
                DecisionAssessment.Assess(prompt, new Decision(handle, [], Allocations: [new(11, 0, "R")])).Rejection);
        }
        Assert.Equal(DecisionRejection.UnexpectedAnswerData,
            DecisionAssessment.Assess(prompt, new Decision(-1, [10])).Rejection);
    }

    [Fact]
    public void UnavailableOrAmbiguousHandlesNeverBecomeAuthority()
    {
        var option = new Affordance(7, "Use", 10, 0, "Action");
        Assert.Equal(DecisionRejection.MissingOffer,
            DecisionAssessment.Assess(Ask(option), Decision.Take(8)).Rejection);
        Assert.Equal(DecisionRejection.MissingOffer,
            DecisionAssessment.Assess(Ask(option, option), Decision.Take(7)).Rejection);
        DecisionAssessmentResult illegal = DecisionAssessment.Assess(Ask(option with { Illegal = "Exhausted" }), Decision.Take(7));
        Assert.Equal(DecisionRejection.IllegalOffer, illegal.Rejection);
        Assert.Equal("Exhausted", illegal.Reason);
    }

    private static Prompt Ask(params Affordance[] options) =>
        new(0, Question.TurnOption, TimingPriority.Untimed, "", "Choose", false, options);
}
