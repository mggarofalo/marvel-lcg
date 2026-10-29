using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicAntManFormIndependentTests : EngineHostTestBase
{
    private static readonly string[] Destinations = ["12001a", "12001c"];

    [Theory]
    [InlineData("12001a", "TINY")]
    [InlineData("12001c", "GIANT")]
    public void OpeningTurnOffersBothFormsAndKeepsPhysicalIdentity(string destination, string trait)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var opened = host.Exchange(EngineRequest.OpenGame("open", "ant-form",
            new GameSpecification("rhino", ["ant_man"], [], 851)));
        Assert.Null(opened.Error);
        var identity = Assert.Single(opened.World!.Areas.SelectMany(area => area.Cards),
            card => card.Face?.Id == "12001b");
        var turn = host.Exchange(EngineRequest.ResolveGame("keep", opened.GameId, opened.Capability!,
            TakeOnly(opened), opened.Revision));
        Assert.Null(turn.Error);
        var changes = turn.Prompt!.Affordances.Where(offer => offer.Verb == Game.ChangeForm).ToArray();
        Assert.Equal(Destinations, changes.Select(offer => offer.Label).Order());
        var change = Assert.Single(changes, offer => offer.Label == destination);
        Assert.Contains(trait, change.DisplayLabel);
        var result = host.Exchange(EngineRequest.ResolveGame("change", opened.GameId, opened.Capability!,
            Decision.Take(change.Id), turn.Revision));
        Assert.Null(result.Error);
        var changed = Assert.Single(result.World!.Areas.SelectMany(area => area.Cards),
            card => card.Face?.Id == destination);
        Assert.Equal(identity.Id, changed.Id);
        Assert.Contains(trait, changed.Face!.Traits);
        Assert.DoesNotContain(result.World.Areas.SelectMany(area => area.Cards), card => card.Face?.Id == "12001b");
    }
}
