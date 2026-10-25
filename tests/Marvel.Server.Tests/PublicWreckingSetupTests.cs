using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicWreckingSetupTests
{
    [Fact]
    public void EveryAdmittedRecipeCreatesItsPhysicalGroupsAndRestartsAfterKeepingAHand()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var choices = factory.DiscoverSetup().Scenarios.Where(choice => choice.Name == "Wrecking Crew").ToArray();
        Assert.Equal(19, choices.Length);
        string[] heroes = ["captain_america", "spider_man", "captain_marvel", "she_hulk"];
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var excluded = heroes.SelectMany(hero => setup.Hero(hero).Obligations.Concat(setup.Hero(hero).NemesisSet))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var choice in choices)
        foreach (int seats in new[] { 1, 2, 4 })
        {
            var specification = new GameSpecification(choice.Key, heroes.Take(seats).ToArray(), [], 8123);
            var world = factory.Create(specification).Game.State;
            AssertPhysicalGroups(world, choice.Mode, choice.Key);
            Assert.DoesNotContain(world.Cards, card => excluded.Contains(card.FaceId));
            var store = new MemorySessionStore();
            var host = new EngineHost(factory, store: store);
            var opened = EngineJson.Snapshot(host.Exchange(EngineRequest.OpenGame("open", "crew", specification)));
            Assert.Null(opened.Error);
            Assert.Equal(4, opened.World!.EncounterGroups.Count);
            Assert.Single(opened.World.EncounterGroups, group => group.Active);
            foreach (var group in opened.World.EncounterGroups)
            {
                Assert.NotNull(group.Villain);
                Assert.NotNull(group.Scheme);
                Assert.Equal(15, opened.World.Areas.Single(area => area.Id == group.Deck).Cards.Count);
                Assert.Empty(opened.World.Areas.Single(area => area.Id == group.Discard).Cards);
            }
            var mulligan = Assert.Single(Assert.IsType<Prompt>(opened.Prompt).Affordances);
            Assert.Equal(Game.ResolveMulligans, mulligan.Verb);
            var kept = host.Exchange(EngineRequest.ResolveGame("keep", "crew", opened.Capability!,
                new Decision(mulligan.Id, []), opened.Revision));
            Assert.Null(kept.Error);
            Assert.Equal(1, kept.Revision);
            var before = host.Exchange(EngineRequest.SyncGame("sync", "crew", opened.Capability!));
            var saved = Assert.Single(store.Load());
            Assert.Single(saved.Save.Units);
            var restarted = new EngineHost(factory, store: store);
            var after = restarted.Exchange(EngineRequest.SyncGame("sync", "crew", opened.Capability!));
            Assert.Null(after.Error);
            Assert.Equal(EngineJson.Write(before), EngineJson.Write(after));
            Assert.Equal(JsonSerializer.Serialize(saved), JsonSerializer.Serialize(Assert.Single(store.Load())));
        }
    }

    private static void AssertPhysicalGroups(World world, string? mode, string scenario)
    {
        var topology = Assert.IsType<EncounterTopology>(world.EncounterTopology);
        Assert.Equal(4, topology.Groups.Count);
        Assert.Equal(4, world.AreaOf(DeckType.VillainArea).Cards.Count);
        Assert.Equal(4, world.AreaOf(DeckType.SideSchemesArea).Cards.Count);
        Assert.Equal(mode == "extreme" ? 4 : 0, world.AreaOf(DeckType.VillainDeck).Cards.Count);
        Assert.Same(topology.Groups[0], topology.Active);
        string[][] stages = [["07002", "07003"], ["07017", "07018"], ["07032", "07033"], ["07046", "07047"]];
        string[] schemes = ["07004", "07019", "07034", "07048"];
        string variants = mode == "expert" ? "bbbb" : mode == "custom" ? scenario.Split('_')[3] : "aaaa";
        for (int index = 0; index < 4; index++)
        {
            var group = topology.Groups[index];
            Assert.Equal(mode == "extreme" ? stages[index] : [stages[index][variants[index] == 'b' ? 1 : 0]],
                group.Stages.Select(card => card.FaceId));
            Assert.Equal(schemes[index], group.Scheme.FaceId);
            Assert.Equal(DeckType.SideSchemesArea, group.Scheme.Area.Type);
            Assert.Equal(15, group.Deck.Cards.Count);
            Assert.Equal(group.Members.OrderBy(card => card.ObjectId), group.Deck.Cards.OrderBy(card => card.ObjectId));
            Assert.All(group.Members, card => Assert.Same(group, topology.ForCard(card)));
            Assert.Empty(group.Discard.Cards);
        }
        Assert.DoesNotContain(world.Cards, card => world.Facts.Kind(card.FaceId) == CardKind.Obligation);
    }
}

