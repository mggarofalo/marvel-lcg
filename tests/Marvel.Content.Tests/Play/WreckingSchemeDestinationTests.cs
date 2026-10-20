using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingSchemeDestinationTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void EveryDeclaredStageNamesItsOwnSchemeRegardlessOfActiveCounter(int index, bool next)
    {
        // Each canonical Wrecking villain face prints: "When [name] schemes,
        // place the threat on his side scheme instead of the main scheme."
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var group = groups[index];
        if (next)
        {
            World.MoveToTop(group.Villain, world.AreaOf(DeckType.RemovedArea));
            World.MoveToTop(group.Stages[1], world.AreaOf(DeckType.VillainArea));
        }
        var enemy = group.CurrentVillain!;
        var program = Program(enemy.FaceId);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        Assert.Same(group.Scheme, AbilitySchemeDestinations.Resolve(world, program, enemy));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }

    [Fact]
    public void TopologyAloneDoesNotRedirectAnUndeclaredEnemy()
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var main = world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        Assert.Same(main, AbilitySchemeDestinations.Resolve(world, AbilityLowering.Book(AbilityBook.None),
            groups[2].Villain));
        var minion = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Same(main, AbilitySchemeDestinations.Resolve(world, AbilityLowering.Book(AbilityBook.None), minion));
    }

    [Fact]
    public void APrintedFaceWithoutItsPhysicalGroupCannotUseAnotherCopiesScheme()
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var copy = world.CreateCard(groups[2].Villain.FaceId, world.AreaOf(DeckType.VillainArea));
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilitySchemeDestinations.Resolve(world, Program(copy.FaceId), copy));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MissingSchemeOrDepartedEnemyCannotSilentlyFallBackToAnotherGroup(bool removeScheme)
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var group = groups[2];
        var program = Program(group.Villain.FaceId);
        World.MoveToTop(removeScheme ? group.Scheme : group.Villain, world.AreaOf(DeckType.RemovedArea));
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilitySchemeDestinations.Resolve(world, program, group.Villain));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("7")]
    [InlineData("\"activeSideScheme\"")]
    [InlineData("{\"query\":\"sideSchemes\"}")]
    public void UnknownOrAmbiguousDestinationDeclarationsFailAtParsing(string declaration)
    {
        Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            "{\"cards\":[{\"card\":\"07032\",\"abilities\":[],\"schemeDestination\":" + declaration + "}]}"));
    }

    [Fact]
    public void DestinationMetadataDoesNotDeclareTheRestOfACardImplemented()
    {
        Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """{"cards":[{"card":"07032","schemeDestination":"correspondingSideScheme"}]}"""));
        var malformed = new AbilityBook([], new HashSet<string> { "07032" },
            SchemeDestinations: new Dictionary<string, AbilitySchemeDestination>
            { ["07032"] = (AbilitySchemeDestination)999 });
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(malformed));
        var unauthored = malformed with
        {
            Authored = new HashSet<string>(),
            SchemeDestinations = new Dictionary<string, AbilitySchemeDestination>
            { ["07032"] = AbilitySchemeDestination.CorrespondingSideScheme },
        };
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(unauthored));
    }

    private static AbilityProgram Program(string face) => AbilityLowering.Book(AbilityCatalog.Parse(
        "{\"cards\":[{\"card\":\"" + face
        + "\",\"abilities\":[],\"schemeDestination\":\"correspondingSideScheme\"}]}"));
}
