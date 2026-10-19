using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Synthetic offered contracts test menu composition, never game-rule derivation.</summary>
public sealed class CardMenuChoicesTests
{
    [Fact]
    public void MenuGroupsOnlyOfferedTargetsByEngagementRatherThanController()
    {
        var offer = new Affordance(3, "Attack", 10, 0, "Attack", new TargetRequest([20, 21, 22], 1, 1));
        var composer = Composer(offer);
        var world = World() with { Relationships = [
            new(RelationshipKind.Engagement, 21, null, 1),
            new(RelationshipKind.Engagement, 22, null, 0),
            new(RelationshipKind.Engagement, 99, null, 0)] };
        CardMenuEntry action = Assert.Single(CardMenuChoices.Build(composer, world, 10, [Visible(offer)]));
        Assert.Null(composer.Selected);
        Assert.Null(action.Choice);
        Assert.Equal(["Rhino", "Player 1 minions", "Player 2 minions"], action.Children.Select(entry => entry.Label));
        Assert.Equal(22, Assert.Single(action.Children[1].Children).Choice!.Target);
        Assert.Equal(21, Assert.Single(action.Children[2].Children).Choice!.Target);
        Assert.All(Leaves(action), entry => Assert.True(entry.Choice!.Commit));
    }

    [Fact]
    public void SameNamedSourcesKeepExactOffersAndUnavailableReasons()
    {
        var first = new Affordance(3, "Action", 10, 0, "Use support");
        var second = new Affordance(4, "Action", 11, 0, "Use support");
        var unavailable = new Affordance(5, "Action", 10, 0, "Other action", Illegal: "Exhausted");
        var composer = Composer(first, second, unavailable);
        var menu = CardMenuChoices.Build(composer, World(), 10, [Visible(first), Visible(second), Visible(unavailable)]);
        Assert.Equal(2, menu.Count);
        Assert.Equal(3, menu[0].Choice!.Offer);
        Assert.True(menu[1].Disabled);
        Assert.Equal("Exhausted", menu[1].Detail);
        Assert.Null(menu[1].Choice);
        Assert.Null(composer.Selected);
    }

    [Fact]
    public void SelectingTargetCommitsOnlyTheNamedCurrentAnswer()
    {
        var composer = Composer(new Affordance(3, "Attack", 10, 0, "Attack", new TargetRequest([20, 21], 1, 1)));
        Assert.True(CardMenuChoices.Select(composer, Binding(composer), new(3, 21, true)));
        Assert.True(composer.TryBuild(out var decision, out _));
        Assert.Equal(3, decision!.Affordance);
        Assert.Equal([21], decision.Targets);
    }

    [Theory]
    [InlineData(false, 21)]
    [InlineData(true, 99)]
    public void StaleOrForeignChoicePreservesExistingDraft(bool current, int target)
    {
        var composer = Composer(new Affordance(3, "Attack", 10, 0, "Attack", new TargetRequest([20, 21], 1, 1)));
        composer.SelectAffordance(3);
        composer.SelectTargets([20]);
        var binding = new TableDraftBinding(composer, 1, 1, (_, _) => current);
        Assert.False(CardMenuChoices.Select(composer, binding, new(3, target, true)));
        Assert.Equal([20], composer.Targets);
    }

    [Fact]
    public void PaymentLeafContinuesCompositionAndCannotPretendToCommit()
    {
        var offer = new Affordance(3, "Play", 10, 0, "Event", new TargetRequest([20, 21], 1, 1),
            [new CostOption(10, "1", Sources: [new(7, "Y")])]);
        var composer = Composer(offer);
        var action = Assert.Single(CardMenuChoices.Build(composer, World(), 10, [Visible(offer)]));
        var leaf = action.Children[0];
        Assert.False(leaf.Choice!.Commit);
        Assert.False(CardMenuChoices.Select(composer, Binding(composer), leaf.Choice with { Commit = true }));
        Assert.Null(composer.Selected);
        Assert.True(CardMenuChoices.Select(composer, Binding(composer), leaf.Choice));
        Assert.Equal([20], composer.Targets);
        Assert.Empty(composer.Resources);
        Assert.False(composer.TryBuild(out _, out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GroupedAndRepeatedTargetsKeepTheCompleteComposer(bool grouped)
    {
        var request = grouped ? new TargetRequest([20, 21], 2, 2, [[20, 21]])
            : new TargetRequest([20, 21], 2, 2, AllowRepeated: true);
        var offer = new Affordance(3, "Action", 10, 0, "Allocate", request);
        var composer = Composer(offer);
        var action = Assert.Single(CardMenuChoices.Build(composer, World(), 10, [Visible(offer)]));
        Assert.Empty(action.Children);
        Assert.False(action.Choice!.Commit);
        Assert.Null(action.Choice.Target);
    }

    [Fact]
    public void DirectRecipientUsesExplicitSelectionEvenWhenDecliningRecipientIsLegal()
    {
        var offer = new Affordance(3, "Action", 10, 0, "Helicarrier", new TargetRequest([20, 21], 0, 1))
            { DirectRecipientCommit = true };
        var composer = Composer(offer);
        var action = Assert.Single(CardMenuChoices.Build(composer, World(), 10, [Visible(offer)]));
        Assert.Null(action.Choice);
        Assert.Equal(2, action.Children.Count);
        Assert.True(CardMenuChoices.Select(composer, Binding(composer), action.Children[1].Choice!));
        Assert.Equal([21], composer.Targets);
    }

    [Fact]
    public void HandPlayKeepsPaymentAndTargetsInExistingComposer()
    {
        var offer = new Affordance(3, "Play", 10, 0, "Support", new TargetRequest([20], 1, 1),
            [new CostOption(10, "1", Sources: [new(7, "Y")])]);
        var composer = Composer(offer);
        var action = Assert.Single(CardMenuChoices.Build(composer, World(), 10, [Visible(offer) with { PlaysCard = true }]));
        Assert.Empty(action.Children);
        Assert.False(action.Choice!.Commit);
        Assert.Null(action.Choice.Target);
        Assert.Null(composer.Selected);
    }

    [Fact]
    public void RepeatedDescriptionsOfOneTargetAreNotDifferentCopies()
    {
        var offer = new Affordance(3, "Attack", 10, 0, "Attack", new TargetRequest([20], 1, 1));
        var world = World();
        world = world with { Areas = [.. world.Areas, .. world.Areas] };
        var action = Assert.Single(CardMenuChoices.Build(Composer(offer), world, 10, [Visible(offer)]));
        Assert.Equal("Rhino", Assert.Single(action.Children).Label);
    }

    private static IEnumerable<CardMenuEntry> Leaves(CardMenuEntry entry) => entry.Children
        .SelectMany(child => child.Children.Count == 0 ? [child] : Leaves(child));
    private static DecisionComposer Composer(params Affordance[] offers) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", false, offers));
    private static TableDraftBinding Binding(DecisionComposer composer) => new(composer, 1, 1, (_, _) => true);
    private static AffordancePresentation Visible(Affordance offer) => new(offer.Id, offer.Label,
        offer.Description, offer.Verb, offer.Label, offer.AnchorId, offer.AnchorPlayer, offer.Illegal, "", [])
        { AnchorKind = AffordanceAnchorKind.Card, TargetRequest = offer.Targets,
            DirectRecipientCommit = offer.DirectRecipientCommit };
    private static WorldDescriptor World() => new([], [new(1, "Visible", 0, -1,
        [Card(20, "Rhino"), Card(21, "Minion A"), Card(22, "Minion B")], [])], [], Outcome.Unfinished);
    private static CardDescriptor Card(int id, string name) => new(id, CardBack.Encounter, true, true, -1,
        new("test", name, "", CardKind.Minion, new Dictionary<string, long>()));
}
