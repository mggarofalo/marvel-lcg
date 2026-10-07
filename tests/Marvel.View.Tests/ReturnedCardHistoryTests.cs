using Xunit;

namespace Marvel.View.Tests;

public sealed class ReturnedCardHistoryTests : EventPresentationTestBase
{
    [Theory]
    [InlineData("DiscardPile", "Now in Spider-Man's discard pile: Spider-Tracer.")]
    [InlineData("EngagedEnemiesArea", "Returned Spider-Tracer to Spider-Man's discard pile.")]
    public void ReturnToDiscardNamesTheDestinationWithoutInventingAnAreaChange(string from, string expected)
    {
        // Public semantic movement, including the temporary-identity cleanup's same-area form.
        var moved = Move(7, from, "DiscardPile", "Return_To_Discard");
        Assert.Equal(expected, EventPresenter.Present(moved, NarrativeWorld()).Summary);
    }
}
