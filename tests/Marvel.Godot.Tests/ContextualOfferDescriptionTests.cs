using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ContextualOfferDescriptionTests
{
    [Fact]
    public void SingleResponseExplainsTheSearchCommitmentBeforeAccepting()
    {
        var offer = Offer(1) with
        {
            Description = "Search your deck for an upgrade and add it to your hand.",
            Consequence = "Choose a matching card if available, then shuffle your deck.",
        };
        var prompt = new PromptPresentation("", "", "", "", "", [offer,
            Offer(2) with { Illegal = "Unavailable" }]);
        string description = Assert.IsType<string>(ContextualOfferDescription.SingleResponse(
            prompt, PublicDecisionKind.Response));
        Assert.Contains("Search your deck", description);
        Assert.Contains("if available", description);
        Assert.Contains("shuffle", description);
        Assert.Null(ContextualOfferDescription.SingleResponse(prompt, PublicDecisionKind.PlayerAction));
    }

    [Fact]
    public void MultipleResponsesKeepTheirOwnDescriptions()
    {
        var prompt = new PromptPresentation("", "", "", "", "", [Offer(1), Offer(2)]);
        Assert.Null(ContextualOfferDescription.SingleResponse(prompt, PublicDecisionKind.Response));
        Assert.Null(ContextualOfferDescription.SingleResponse(prompt with { Affordances = [] },
            PublicDecisionKind.Response));
    }

    private static AffordancePresentation Offer(int id) => new(
        id, "Search", "Search the deck.", "Response", "Source", 55, 0, null, "", []);
}
