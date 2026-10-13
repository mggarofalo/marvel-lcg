using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

public sealed class WindowContractVisibilityTests
{
    [Fact]
    public void WindowChoicesCannotDiscloseOtherPlayersHiddenRecipient()
    {
        World world = VisibilityFixture.Board();
        Card publicSource = world.TheCardIn(DeckType.VillainArea)!;
        Card secret = world.Seats[1].Hand.Cards[0];
        var offer = new Affordance(7, "Response", publicSource.ObjectId, 0, "Response")
        {
            Timing = AbilityType.Response,
            WindowChoices = [new WindowChoice(publicSource.ObjectId, "Public choice", null)
                { RecipientIds = [publicSource.ObjectId] },
                new WindowChoice(secret.ObjectId, "Secret name", null) { RecipientIds = [secret.ObjectId] }],
        };
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Response, "test", "Response?", true, [offer]);
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, 2);
        var result = WorldProjection.For(world, prompt, [], scope);
        var safe = Assert.Single(result.Prompt!.Affordances);
        Assert.Equal("Public choice", Assert.Single(safe.WindowChoices).Label);
        Assert.Null(WorldProjection.For(world, prompt, [], ViewScope.None).Prompt);
        var presented = PromptPresentation.From(result.Prompt, result.World);
        Assert.Equal(AbilityType.Response, Assert.Single(presented.Affordances).Timing);
    }

    [Fact]
    public void ForcedReceiptsDropHiddenSourcesAndPreserveTypedPublicSuspension()
    {
        World world = VisibilityFixture.Board();
        Card source = world.TheCardIn(DeckType.VillainArea)!;
        Card secret = world.Seats[1].Hand.Cards[0];
        var receipt = new AbilityWindowResult(source.ObjectId, AbilityType.ForcedInterrupt, 0, 17,
            "Grant overkill", false);
        var hidden = receipt with { Source = secret.ObjectId, Description = "Secret instruction" };
        var result = WorldProjection.For(world, null, [hidden, receipt], ViewScope.None);
        var safe = Assert.IsType<AbilityWindowResult>(Assert.Single(result.Events));
        var notification = EventPresenter.Present(safe, result.World).ForcedAbilityNotification!;
        Assert.Equal(source.ObjectId, notification.SourceId);
        Assert.Contains("public-villain", notification.Title);
        Assert.Contains("Forced Interrupt", notification.Title);
        Assert.Contains("awaits a decision", notification.Summary);
        Assert.False(notification.Completed);
        Assert.Equal(17, notification.Occurrence);
    }
}
