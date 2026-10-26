using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class HeimdallNativeSceneTests
{
    [Fact]
    [Rule("rr:look-looked-at")]
    public void PublicThorFixturePreservesActingSeatPrivacyAndPhysicalOrderedReturn()
    {
        // "only the player who is resolving the ability can look at those cards".
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        Assert.Contains(factory.DiscoverSetup().Heroes, hero => hero.Key == "thor");
        Game game = HeimdallNativeScene.Create(factory,
            new GameSpecification("rhino", ["thor", "spider_man"], null, 1)).Game;
        var ally = game.State.Cards.Single(card => card.FaceId == "06020");
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == ally.ObjectId).Id));
        Assert.Empty(game.Pending!.ContextCardIds);
        Assert.All(Project(0).World.Areas.Single(area => area.Zone == "EncounterDeck").Cards,
            card => Assert.Null(card.Face));
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        int[] group = game.Pending!.ContextCardIds.ToArray();
        Assert.Equal(3, group.Length);
        Assert.Equal(3, group.Distinct().Count());
        Assert.Equal(3, Project(0).World.Areas.Single(area => area.Zone == "EncounterDeck").Cards.Count(card => card.Face is not null));
        var other = Project(1);
        Assert.Null(other.Prompt);
        Assert.All(other.World.Areas.Single(area => area.Zone == "EncounterDeck").Cards,
            card => { Assert.Null(card.Id); Assert.Null(card.Face); });
        game.Resolve(Decision.Take(group[1]));
        Assert.Equal(2, Project(0).World.Areas.Single(area => area.Zone == "EncounterDeck").Cards.Count(card => card.Face is not null));
        Assert.Null(Project(1).Prompt);
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, [group[2], group[0]]));
        Assert.Equal(DeckType.EncounterDiscardPile, game.State.Cards[group[1]].Area.Type);
        Assert.Equal(new[] { group[2], group[0] }, game.State.AreaOf(DeckType.EncounterDeck).Cards.Reverse().Take(2).Select(card => card.ObjectId));
        Assert.All(Project(0).World.Areas.Single(area => area.Zone == "EncounterDeck").Cards,
            card => Assert.Null(card.Face));

        VisibleResult Project(int seat) => WorldProjection.For(game.State, game.Pending, [],
            new RestrictedVisibilityPolicy(seat).Authorize(null, game.State.Players));
    }
}
