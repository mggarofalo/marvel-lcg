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

        PaymentSourcePresentation source = PaymentSourcePresentation.From(new(7, "GG")
        {
            AbilityName = "Offered generator",
            CostDescription = "Exhaust Visible source",
        }, world);

        Assert.Equal(discards, source.DiscardsCard);
        Assert.Equal("Visible source", source.Name);
        Assert.Equal("GG", source.Resources);
        Assert.Equal(face.RulesText, source.Reference);
        Assert.Equal(face.RulesMarkup, source.ReferenceMarkup);
        Assert.Equal("Offered generator", source.AbilityName);
        Assert.Equal("Exhaust Visible source", source.CostDescription);
    }

    [Fact]
    public void HiddenFaceAndAnAreaWithTheSameIdDoNotRevealOrInventSourceDetails()
    {
        var hidden = new CardDescriptor(7, CardBack.Player, false, true, -1, null);
        var world = new WorldDescriptor([], [new(7, "HandsArea", 1, -1, [hidden], [])], [], Outcome.Unfinished);

        PaymentSourcePresentation source = PaymentSourcePresentation.From(new(7, "G")
        {
            AbilityName = "Concealed ability",
            CostDescription = "Concealed cost",
        }, world);

        Assert.Equal("Resource ability", source.Name);
        Assert.Empty(source.Reference);
        Assert.Empty(source.ReferenceMarkup);
        Assert.False(source.DiscardsCard);
        Assert.Equal("G", source.Resources);
        Assert.Null(source.AbilityName);
        Assert.Null(source.CostDescription);
        Assert.Null(source.Controller);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(-1, null)]
    public void TheSourceIsLocatedAtTheSeatTheEngineReportsAsItsController(int controller, int? expected)
    {
        // The Milano (16142) sits in the first player's supports and any player may spend it
        // (rr:ability.8); the payment row names that seat from the published controller.
        var face = new CardFaceDescriptor("16142", "Milano", "", CardKind.Support, new Dictionary<string, long>());
        var card = new CardDescriptor(7, CardBack.Encounter, true, true, -1, face)
        {
            Location = new CardLocationDescriptor(1, "SupportsArea", controller, -1),
        };
        var world = new WorldDescriptor([], [new(1, "SupportsArea", 0, -1, [card], [])], [], Outcome.Unfinished);

        Assert.Equal(expected, PaymentSourcePresentation.From(new(7, "G"), world).Controller);
    }

    [Fact]
    public void ASourceWithoutAPublishedLocationHasNoController()
    {
        var face = new CardFaceDescriptor("16142", "Milano", "", CardKind.Support, new Dictionary<string, long>());
        var world = new WorldDescriptor([], [new(1, "SupportsArea", 0, -1,
            [new CardDescriptor(7, CardBack.Encounter, true, true, -1, face)], [])], [], Outcome.Unfinished);

        Assert.Null(PaymentSourcePresentation.From(new(7, "G"), world).Controller);
    }
}
