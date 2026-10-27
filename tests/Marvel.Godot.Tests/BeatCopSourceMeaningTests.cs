using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class BeatCopSourceMeaningTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void ActualProjectedSupportThreatRemainsInAccessibleSourceSummary(int threat)
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(catalog, 1, seed: 7);
        var seat = world.CreateSeat("P0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var cop = world.CreateCard("10029", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        BoardCardPresentation Project() => Assert.Single(BoardPresentation.From(WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World).Areas
            .SelectMany(area => area.Cards), card => card.FaceId == "10029");
        Assert.DoesNotContain(CardSourceSummary.Lines(Project()), line => line.Contains("threat here", StringComparison.Ordinal));
        cop.PlaceTokens("k_threat", threat);
        var shown = Project();
        Assert.Contains($"{threat} threat here", CardSourceSummary.Lines(shown));
        Assert.Empty(CardSourceSummary.Lines(shown with { Concealed = true }));
        Assert.DoesNotContain(CardSourceSummary.AbilityLines(shown), line => line.Contains("threat here", StringComparison.Ordinal));
    }
    [Fact]
    public void SourceNamedActionsExposeDifferentSuppliedCostsBeforeSelection()
    {
        var move = new AffordancePresentation(1, "Beat Cop", null, "Action", "Beat Cop", 8, 0, null, "", [])
        { CostDescription = "Exhaust Beat Cop." };
        var discard = move with { Id = 2, CostDescription = "Exhaust Beat Cop. Discard Beat Cop." };
        Assert.Equal("Beat Cop · Exhaust Beat Cop.", DecisionCopy.Choice(move));
        Assert.Equal("Beat Cop · Exhaust Beat Cop. Discard Beat Cop.", DecisionCopy.Choice(discard));
        Assert.Equal("Beat Cop", DecisionCopy.Choice(move with { CostDescription = null }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualActionsAndPaidTargetRetainSuppliedKnownEffectsInDraft(bool restricted)
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(catalog, 1, seed: 7) { Abilities = runner };
        var seat = world.CreateSeat("P0");
        seat.IdentityCard = world.CreateCard("10001a", seat.Hero);
        world.CreateCard("01088", seat.Deck);
        world.CreateCard("01089", seat.Deck);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea)).PlaceTokens("k_threat", 4);
        world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        var enemy = world.CreateCard("01131", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var cop = world.CreateCard("10029", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        cop.PlaceTokens("k_threat", 3);
        var actions = runner.Actions(world, 0).Where(a => a.Card == cop.ObjectId).ToArray();
        foreach (var action in actions)
        {
            var offer = runner.Describe(world, action);
            var initial = new Prompt(0, Question.Option, TimingPriority.Untimed, Steps.TurnAction,
                "Choose action", true, [offer]);
            string summary = Summary(world, initial, offer.Id, restricted);
            Assert.Contains(action.Ordinal == 0 ? "move 1 threat onto this card from" : "Current damage: 3", summary);
            Assert.Contains("Exhaust Beat Cop", summary);
            if (action.Ordinal == 1) Assert.Contains("Discard Beat Cop", summary);
        }
        runner.ExecuteAction(world, new PlayerAction(actions.Single(a => a.Ordinal == 1), [], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, catalog, runner, []));
        Assert.Equal(0, cop.Tokens["k_threat"]);
        string continuation = Summary(world, prompt, enemy.ObjectId, restricted);
        Assert.Contains("Current damage: 3 before prevention/replacement", continuation);
        Assert.Contains("Later effects may change it", continuation);
        Assert.Contains("Current damage: 3", WorldProjection.For(world, prompt, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, 1)).Prompt!.Description);
    }

    private static string Summary(World world, Prompt prompt, int id, bool restricted)
    {
        IVisibilityPolicy policy = restricted ? new RestrictedVisibilityPolicy(0) : new PermissiveVisibilityPolicy();
        var visible = WorldProjection.For(world, prompt, [], policy.Authorize(null, 1));
        var presentation = PromptPresentation.From(visible.Prompt!, visible.World);
        var composer = new DecisionComposer(visible.Prompt!);
        composer.SelectAffordance(id);
        return Assert.IsType<string>(TableDraftSummary.From(composer, presentation, visible.World, compact: true));
    }
}
