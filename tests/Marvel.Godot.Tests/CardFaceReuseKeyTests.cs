using System.Text.Json;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardFaceReuseKeyTests
{
    private static BoardCardPresentation Card() =>
        new(7, 1, false, "Source", "", "UPGRADE", "READY", [new("Uses", "3")])
        {
            FaceId = "01001a", RulesText = "Authorized rules", Traits = ["Item"],
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["ATK"] = new(2, 3, "Printed", true, []) },
        };

    private static CardFaceReuseKey Key(BoardCardPresentation card) =>
        CardFaceReuseKey.From(card, CardDisplaySize.Board, InterfaceScale.Standard,
            CardControl.LayoutFor(card, CardDisplaySize.Board, InterfaceScale.Standard));

    [Fact]
    public void ANewSnapshotWithTheSameAuthorizedFactsRetainsItsFace()
    {
        BoardCardPresentation original = Card();
        var reconstructed = JsonSerializer.Deserialize<BoardCardPresentation>(JsonSerializer.Serialize(original))!;
        Assert.NotSame(original.Fields, reconstructed.Fields);
        Assert.Equal(Key(original), Key(reconstructed));
    }

    [Fact]
    public void AChangedIdentityVisibilityOrLiveFactRequiresANewFace()
    {
        BoardCardPresentation card = Card();
        BoardCardPresentation[] changed =
        [
            card with { TargetId = 8 }, card with { Concealed = true }, card with { FaceId = "01001b" },
            card with { Status = "EXHAUSTED" }, card with { Fields = [new("Uses", "2")] },
            card with { Counters = [new("Uses", "2")] }, card with { Statuses = ["Tough"] },
            card with { Damage = 1 }, card with { DamageKnown = false }, card with { RulesText = "New rules" },
            card with { RulesMarkup = "New emphasis" }, card with { PrintedStats = [new("ATK", "4")] },
            card with { EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["ATK"] = new(2, 4, "Printed", true, []) } },
            card with { EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["ATK"] = new(3, 3, "Modified", true, []) } },
        ];
        foreach (BoardCardPresentation replacement in changed) Assert.NotEqual(Key(card), Key(replacement));
    }

    [Fact]
    public void ScaleDisplayPurposeAndFittedGeometryRequireANewFace()
    {
        BoardCardPresentation card = Card();
        CardFaceReuseKey original = Key(card);
        Assert.NotEqual(original, CardFaceReuseKey.From(card, CardDisplaySize.Full,
            original.Scale, original.Layout));
        Assert.NotEqual(original, CardFaceReuseKey.From(card, original.Size,
            InterfaceScale.Percent150, original.Layout));
        Assert.NotEqual(original, CardFaceReuseKey.From(card, original.Size,
            original.Scale, original.Layout with { Width = original.Layout.Width - 1 }));
    }
}
