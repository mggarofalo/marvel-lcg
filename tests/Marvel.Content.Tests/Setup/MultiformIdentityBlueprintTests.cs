using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class MultiformIdentityBlueprintTests
{
    [Rule("rr:appendix-ii-setup.step.1")]
    [Fact]
    public void PrintedAntManFacesProduceOneAlterEgoFirstIdentityBlueprint()
    {
        // "Each player selects one identity, placing their alter-ego side face up."
        // pack:mc12:ant-man-hero-pack#foldable-cards identifies two hero sides.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var creation = new Creation("12001a,12001b,12001c", CreationSource.Identity, 0);

        var blueprint = Assert.Single(Blueprints.From([creation], cards));
        Assert.Equal(new CardBlueprint("12001b,12001a,12001c", SetupSlot.Identity, 0), blueprint);
    }
}
