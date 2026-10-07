using Marvel.Rules.State;
using Marvel.Rules.Play;
using Xunit;

namespace Marvel.View.Tests;

public sealed class BoardPrintedMarksTests
{
    [Theory]
    [InlineData(CardKind.Ally, "ATK", "2**", false, 2)]
    [InlineData(CardKind.Ally, "THW", "0*", false, 1)]
    [InlineData(CardKind.EncounterVillain, "HP", "14*", true, 0)]
    [InlineData(CardKind.MainScheme, "EscalationThreat", "1*", true, 0)]
    public void VisibleFaceRetainsTheMeaningOfItsPrintedMarks(
        CardKind kind, string attribute, string printed, bool perPlayer, int consequential)
    {
        var face = new CardFaceDescriptor("sample", "Sample", "", kind,
            new Dictionary<string, long>())
        {
            PrintedStats = new Dictionary<string, string> { [attribute] = printed, ["RES"] = "P" },
        };
        BoardCardPresentation card = Present(new CardDescriptor(1, CardBack.Player, true, true, -1, face));

        BoardPrintedValueMark mark = Assert.Single(card.PrintedMarks);
        Assert.Equal(attribute, mark.Attribute);
        Assert.Equal(perPlayer, mark.PerPlayer);
        Assert.Equal(consequential, mark.ConsequentialDamage);
        Assert.Contains(card.PrintedStats, value => value.Name == attribute && value.Value == printed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(12)]
    public void ConcealedCardsHaveNoPrintedMarksWhetherAddressableOrAnonymous(int? id)
    {
        BoardCardPresentation card = Present(new CardDescriptor(id, CardBack.Encounter, false, true, -1, null));

        Assert.True(card.Concealed);
        Assert.Empty(card.PrintedMarks);
        Assert.Empty(card.PrintedStats);
        Assert.Empty(card.RulesText);
        Assert.Null(card.FaceId);
    }

    private static BoardCardPresentation Present(CardDescriptor card)
    {
        var world = new WorldDescriptor([], [new AreaDescriptor(1, "HandsArea", 0, -1, [card], [])],
            [], Outcome.Unfinished);
        return Assert.Single(Assert.Single(BoardPresentation.From(world).Areas).Cards);
    }
}
