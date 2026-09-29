using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

public sealed class PaymentSourcePresentationTests
{
    [Theory]
    [InlineData("HandsArea", true)]
    [InlineData("UpgradesArea", false)]
    [InlineData("HeroArea", false)]
    public void OfferedSourceUsesItsVisibleLocationAndEngineOutput(string zone, bool discards)
    {
        var face = new CardFaceDescriptor("visible", "Visible source", "", CardKind.Upgrade,
            new Dictionary<string, long>()) { RulesText = "Resource: follow the printed cost.", RulesMarkup = "Resource: Generate [wild]." };
        var card = new CardDescriptor(7, CardBack.Player, true, true, -1, face);
        var world = new WorldDescriptor([], [new(1, zone, 0, -1, [card], [])], [], Outcome.Unfinished);

        PaymentSourcePresentation source = PaymentSourcePresentation.From(new(7, "GG"), world);

        Assert.Equal(discards, source.DiscardsCard);
        Assert.Equal("Visible source", source.Name);
        Assert.Equal("GG", source.Resources);
        Assert.Equal(face.RulesText, source.Reference);
        Assert.Equal(face.RulesMarkup, source.ReferenceMarkup);
    }

    [Fact]
    public void HiddenFaceAndAnAreaWithTheSameIdDoNotRevealOrInventSourceDetails()
    {
        var hidden = new CardDescriptor(7, CardBack.Player, false, true, -1, null);
        var world = new WorldDescriptor([], [new(7, "HandsArea", 1, -1, [hidden], [])], [], Outcome.Unfinished);

        PaymentSourcePresentation source = PaymentSourcePresentation.From(new(7, "G"), world);

        Assert.Equal("Resource ability", source.Name);
        Assert.Empty(source.Reference);
        Assert.Empty(source.ReferenceMarkup);
        Assert.False(source.DiscardsCard);
        Assert.Equal("G", source.Resources);
    }
}
