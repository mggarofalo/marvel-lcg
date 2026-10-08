using Marvel.Rules.Events;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckReturnClientValidationTests : LocalGameClientTestBase
{
    [Theory]
    [InlineData(-1, 1, "Vibranium", false)]
    [InlineData(0, -1, "Vibranium", false)]
    [InlineData(0, 0, "Vibranium", false)]
    [InlineData(0, 1, "", false)]
    [InlineData(0, 1, "Vibranium", true)]
    public async Task ReceiptAdmissionRequiresConsistentPublicFacts(int player, int count, string title, bool accepted)
    {
        // Synthetic wire shapes exercise admission, not a played card ability.
        var receipt = new CardsShuffledIntoDeck(player, count, [title]);
        ClientStartupResult result = await OpenWithReceipt(receipt);
        Assert.Equal(accepted, result.Succeeded);
    }

    [Fact]
    public async Task MissingNamesAreRejectedWhileAnExplicitCountOnlyReceiptIsAccepted()
    {
        Assert.False((await OpenWithReceipt(new CardsShuffledIntoDeck(0, 2, null!))).Succeeded);
        Assert.True((await OpenWithReceipt(new CardsShuffledIntoDeck(0, 2, []))).Succeeded);
    }

    private static async Task<ClientStartupResult> OpenWithReceipt(CardsShuffledIntoDeck receipt)
    {
        EngineResponse opened = Host().Exchange(
            EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        return await new LocalGameClient(new FixedTransport(opened with { Events = [receipt] }))
            .OpenAsync(Specification(), TestContext.Current.CancellationToken);
    }
}
