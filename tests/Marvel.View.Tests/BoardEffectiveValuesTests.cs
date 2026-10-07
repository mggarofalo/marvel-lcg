using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class BoardEffectiveValuesTests
{
    [Fact]
    public void AReadableBoardFaceRetainsItsAuthorizedCurrentValueAndSourceExplanation()
    {
        var face = new CardFaceDescriptor("hero", "Hero", "", CardKind.Hero, new Dictionary<string, long>())
        {
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            {
                ["ATK"] = new(2, 3, "Printed", true,
                    [new("Add", 1, new("training", "Training", 2, false), null)]),
            },
        };
        BoardCardPresentation card = Present(new(1, CardBack.Player, true, true, -1, face));
        CardEffectiveValue attack = Assert.Single(card.EffectiveValues).Value;
        Assert.Equal(2, attack.BaseValue);
        Assert.Equal(3, attack.CurrentValue);
        Assert.True(attack.IsModified);
        Assert.Equal("Training", Assert.Single(attack.Calculation).Source!.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(12)]
    public void NeitherAnAnonymousPileNorAnAddressableConcealedCardGainsEffectiveFacts(int? id)
    {
        BoardCardPresentation card = Present(new(id, CardBack.Player, false, true, -1, null));
        Assert.True(card.Concealed);
        Assert.Empty(card.EffectiveValues);
        Assert.Empty(card.PrintedStats);
        Assert.Null(card.FaceId);
    }

    [Fact]
    public void AFaceDownPublicReplacementRetainsOnlyTheReplacementValues()
    {
        var face = new CardFaceDescriptor("effective-drone", "Drone", "", CardKind.Minion, new Dictionary<string, long>())
        {
            ArtFaceId = null,
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            {
                ["ATK"] = new(1, 1, "Replacement", false, []),
            },
        };
        BoardCardPresentation card = Present(new(1, CardBack.Player, false, true, -1, face));
        Assert.False(card.Concealed);
        Assert.Null(card.FaceId);
        Assert.Equal("Drone", card.Title);
        Assert.Equal("Replacement", card.EffectiveValues["ATK"].BaseKind);
        Assert.Equal(1, card.EffectiveValues["ATK"].CurrentValue);
    }

    private static BoardCardPresentation Present(CardDescriptor card) =>
        Assert.Single(Assert.Single(BoardPresentation.From(new WorldDescriptor(
            [], [new AreaDescriptor(1, "HeroArea", 0, -1, [card], [])], [], Outcome.Unfinished)).Areas).Cards);
}
