using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class OverflowSourceInspectionTests
{
    [Fact]
    public void AnEngagedDroneCanInspectItsFullCurrentOffTableUpgrade()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var abilities = AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var scene = CanonicalCoreScene.Deal(new("behavior:card:01142:each-facedown-drone-minion-gets-1-atk",
            "ultron", ["spider_man"], 989), setup, catalog, new AbilityRunner(abilities));
        Card environment = scene.Find(new("01140"));
        scene.Apply(new MoveSceneCard(new("01142"), new(SceneZone.Attachment, Host: environment.ObjectId)));
        World world = scene.World;
        BoardPresentation board = BoardPresentation.From(WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World);
        BoardAreaPresentation engaged = Assert.Single(board.Areas, area => area.Zone == "EngagedEnemiesArea" && area.Cards.Count > 0);
        BoardCardPresentation drone = Assert.Single(engaged.Cards, card => card.Title == "Drone");
        BoardCardPresentation upgrade = Assert.Single(board.Areas.SelectMany(area => area.Cards), card => card.FaceId == "01142");
        BoardAreaPresentation overflow = Assert.Single(SpatialTableObjectRenderer.Unplaced(board.Areas, []),
            area => area.Cards.Contains(upgrade));
        Assert.Equal(environment.ObjectId, overflow.Host);
        var sequences = new BoardInspectorSequences(board);
        sequences.Register(engaged.Cards);
        Assert.Equal(engaged.Cards, sequences.For(drone.TargetId));
        Assert.Empty(sequences.For(upgrade.TargetId));
        CardValueSourceDescriptor source = Assert.Single(drone.EffectiveValues["ATK"].Calculation,
            step => step.Source?.CardId == upgrade.TargetId).Source!;
        BoardCardPresentation full = sequences.Source(CardValueSourceInspection.DescribeSource(source));
        Assert.Same(upgrade, full);
        Assert.Equal("ATTACHMENT", full.Kind);
        Assert.NotNull(full.Persistent);
        Assert.NotEmpty(full.Persistent.Abilities);
        Assert.Contains("Hero Action", full.RulesText);
        Assert.Equal(2, drone.EffectiveValues["ATK"].CurrentValue);
    }
}
