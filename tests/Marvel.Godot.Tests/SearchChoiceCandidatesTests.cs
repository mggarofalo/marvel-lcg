using Marvel.Rules.Prompts;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SearchChoiceCandidatesTests
{
    [Fact]
    public void InspectionUsesOrderedContextFacesAndExcludesSourceAndConcealedCards()
    {
        var board = new BoardPresentation([new(1, "Cards", "",
            [Card(7, "First"), Card(8, "Second"), Card(9, "Source"),
             Card(10, "Hidden") with { Concealed = true }], [])]);
        var prompt = new Marvel.Rules.Prompts.Prompt(0, Question.Element,
            Marvel.Rules.Timing.TimingPriority.Untimed, "test", "Inspect", false, [])
        { ContextCardIds = [8, 10, 7, 11], CauseCardIds = [9] };
        Assert.Equal([8, 7], SearchChoiceCandidates.Inspected(prompt, board).Select(card => card.TargetId));
    }

    [Fact]
    public void DuplicateTitlesKeepTheirOfferedPhysicalIdentitiesAndOrder()
    {
        BoardCardPresentation first = Card(7, "Same title"), second = Card(8, "Same title");
        var board = new BoardPresentation([new(1, "Visible", "", [first, second, Card(9, "Not offered")], [])]);
        var prompt = Prompt(Offer(81, 8), Offer(71, 7));
        IReadOnlyList<BoardCardPresentation> cards = SearchChoiceCandidates.From(prompt, board);
        Assert.Equal(2, cards.Count);
        Assert.Same(second, cards[0]);
        Assert.Same(first, cards[1]);
    }

    [Fact]
    public void ConcealedMissingRemovedAndWrongNamespaceCardsCannotEnterComparison()
    {
        var board = new BoardPresentation([new(1, "Cards", "",
            [Card(7, "Concealed") with { Concealed = true }, Card(8, "Visible"), Card(10, "Wrong namespace")],
            [Card(9, "Earlier removed")])]);
        var prompt = Prompt(Offer(7, 7), Offer(8, 8), Offer(9, 9), Offer(10, 10) with
        { AnchorKind = AffordanceAnchorKind.Unspecified }, Offer(11, 11));
        Assert.Equal(8, Assert.Single(SearchChoiceCandidates.From(prompt, board)).TargetId);
    }

    [Fact]
    public void NoOfferedCandidateProducesNoEnumerationOfOtherReadableCards()
    {
        var board = new BoardPresentation([new(1, "Public", "", [Card(7, "Visible")], [])]);
        Assert.Empty(SearchChoiceCandidates.From(Prompt(), board));
    }

    private static BoardCardPresentation Card(int id, string title) => new(id, 1, false, title, "", "UPGRADE", "", []);
    private static AffordancePresentation Offer(int id, int card) =>
        new(id, "Opaque choice", null, "Choose", "Card", card, 0, null, "", [])
        { AnchorKind = AffordanceAnchorKind.Card };
    private static PromptPresentation Prompt(params AffordancePresentation[] offers) => new("Choose", DecisionFrame.Empty, "", offers);
}
