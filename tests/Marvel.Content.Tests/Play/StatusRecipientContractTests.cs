using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Xunit;
using static Marvel.Content.Tests.Play.SpellMasteryFixture;

namespace Marvel.Content.Tests.Play;

public sealed class StatusRecipientContractTests
{
    [Fact]
    public void OfferQueriesPreserveRngPendingWorkAndUnprobedCompletion()
    {
        var (world, game, deck, runner) = BoardWithSpecial(SevenRingsRecipientTests.Special, true, "09034", 1);
        var (control, controlGame, controlDeck, _) = BoardWithSpecial(SevenRingsRecipientTests.Special, true, "09034", 1);
        string before = Snapshot(world);
        for (int i = 0; i < 3; i++)
            foreach (var action in runner.Actions(world, 1)) _ = runner.Describe(world, action);
        Assert.Equal(before, Snapshot(world));
        var actual = game.Resolve(SevenRingsRecipientTests.Payment(world, game, true, deck.PublicTop!));
        var expected = controlGame.Resolve(SevenRingsRecipientTests.Payment(control, controlGame, true, controlDeck.PublicTop!));
        Assert.Equal(JsonSerializer.Serialize(expected.Events), JsonSerializer.Serialize(actual.Events));
        before = Snapshot(world);
        for (int i = 0; i < 3; i++)
            _ = runner.Choosing(world, world.Agenda.Current!.Value);
        Assert.Equal(before, Snapshot(world));
        var offer = Assert.Single(game.Pending!.Affordances);
        var controlOffer = Assert.Single(controlGame.Pending!.Affordances);
        actual = game.Resolve(new Decision(offer.Id, offer.Targets!.Legal.Reverse().ToArray()));
        expected = controlGame.Resolve(new Decision(controlOffer.Id, controlOffer.Targets!.Legal.ToArray()));
        Assert.Equal(Snapshot(control), Snapshot(world));
        Assert.Equal(JsonSerializer.Serialize(expected.Events), JsonSerializer.Serialize(actual.Events));
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData("{\"from\":{\"query\":\"characters\"},\"status\":\"ready\",\"maximum\":3}")]
    [InlineData("{\"from\":{\"query\":\"characters\"},\"status\":\"tough\",\"maximum\":0}")]
    [InlineData("{\"from\":{\"query\":\"characters\"},\"status\":\"tough\",\"maximum\":-1}")]
    [InlineData("{\"from\":{\"query\":\"characters\"},\"status\":\"tough\"}")]
    [InlineData("{\"from\":{\"query\":\"characters\"},\"status\":\"tough\",\"maximum\":3,\"repeated\":1}")]
    public void LoweringRejectsMalformedOrUnboundedStatusSelections(string argument)
    {
        string json = """
            {"cards":[{"card":"synthetic","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"chooseStatusRecipients":ARGUMENT}}]}]}
            """.Replace("ARGUMENT", argument, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
        Assert.Contains("chooseStatusRecipients", error.Message);
    }
}
