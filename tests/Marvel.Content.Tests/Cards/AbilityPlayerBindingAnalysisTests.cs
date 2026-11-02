using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class AbilityPlayerBindingAnalysisTests
{
    /// <summary>
    /// "Place 2 threat on the main scheme" names no player, so an attachment's action with
    /// that cost is not restricted to its host's controller by a "you" binding.
    /// </summary>
    [Fact]
    public void AMainSchemeThreatCostNamesNoPlayer()
    {
        Assert.False(AbilityPlayerBindingAnalysis.Contains(new AbilityCost.PlaceMainSchemeThreat(2)));
        Assert.True(AbilityPlayerBindingAnalysis.Contains(new AbilityCost.Sequence(
            [new AbilityCost.PlaceMainSchemeThreat(2), new AbilityCost.Exhaust(AbilityCostCard.Identity)])));
    }
}
