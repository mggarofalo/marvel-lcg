using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicMsMarvelSetupTests : EngineHostTestBase
{
    [Fact]
    public void DiscoveryAndProductionSetupIncludeExactStarterAtOneThroughFourSeats()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var discovery = new EngineHost(factory).Exchange(EngineRequest.ReadSetup("setup"));
        Assert.Null(discovery.Error);
        Assert.Equal(17, discovery.Setup!.Heroes.Count);
        Assert.Contains(discovery.Setup.Heroes, hero => hero.Key == "ms_marvel");
        var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "ms_marvel");
        Assert.Equal(40, hero.Starter.Count);
        Assert.True(factory.AssessDeck(hero.Key, hero.Starter).IsValid);
        foreach (string scenario in new[] { "rhino", "rhino_expert", "absorbing_man", "absorbing_man_expert" })
        foreach (int count in Enumerable.Range(1, 4))
        {
            string[] heroes = ["ms_marvel", "captain_marvel", "spider_man", "hawkeye"];
            var game = factory.Create(new GameSpecification(scenario, heroes.Take(count).ToArray(), null, 693)).Game;
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Equal(count, game.State.Seats.Count);
            var seat = game.State.Seats[0];
            Assert.Equal("05001b", seat.IdentityCard.FaceId);
            Assert.Equal(hero.Starter.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
            Assert.Single(game.State.Cards, card => card.FaceId == "05025");
            Assert.Equal(5, game.State.Cards.Count(card => card.FaceId is "05026" or "05027" or "05028" or "05029"));
        }
        string[] custom = [.. hero.Starter.SkipLast(1), "05033"];
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        Assert.NotNull(factory.Create(new GameSpecification("rhino", [hero.Key], null, 693, [new(custom)])));
    }

    [Fact]
    [Rule("rr:event")]
    public void MorphogeneticsPendingResponseRestartsAndMatchesUninterruptedControl()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        // A bounded setup probe examined seeds 1..20 in order with the normal starter,
        // keep-hand and Teen Spirit; 3 is the first naturally containing Big Hands.
        var opened = host.Exchange(EngineRequest.OpenGame("open", "ms-public",
            new GameSpecification("rhino", ["ms_marvel"], [], 3)));
        Assert.Null(opened.Error);
        var current = Resolve(host, opened, opened, TakeOnly(opened), "keep");
        int identity = current.World!.Areas.SelectMany(area => area.Cards).Single(card => card.Face?.Id == "05001b").Id!.Value;
        var spirit = Assert.Single(current.Prompt!.Affordances, offer => offer.AnchorId == identity && offer.Verb != "Change_Form");
        current = Resolve(host, opened, current, Decision.Take(spirit.Id), "teen-spirit");
        var flip = Assert.Single(current.Prompt!.Affordances, offer => offer.Verb == "Change_Form");
        current = Resolve(host, opened, current, Decision.Take(flip.Id), "flip");
        int hands = Hand(current, 0).First(card => card.Face?.Id == "05003").Id!.Value;
        current = Resolve(host, opened, current, PayEvent(current.Prompt!, hands), "big-hands");
        // rr:event: "then places the card in its owner's discard pile after
        // those effects resolve". Recovery ruling 7f69f006cddce07e uses this window.
        Assert.Contains(current.Prompt!.Affordances, offer => offer.AnchorId == identity);
        Assert.Equal(4, current.World!.Areas.SelectMany(area => area.Cards)
            .Single(card => card.Face?.Id == "01094").Face!.Damage);
        Assert.DoesNotContain(Hand(current, 0), card => card.Id == hands);
        string beforeRestart = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "ms-public", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(beforeRestart, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(PromptRecord.From(current.Prompt)),
            System.Text.Json.JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        var recovery = Assert.Single(restored.Prompt!.Affordances, offer => offer.AnchorId == identity);
        var answer = Decision.Take(recovery.Id);
        // Both hosts continue their independently reconstructed live game from the same saved prefix.
        var expected = Resolve(host, opened, current, answer, "control-recover");
        string control = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var result = Resolve(restarted, opened, restored, answer, "recover");
        Assert.Equal(control, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Contains(Hand(result, 0), card => card.Id == hands && card.Face!.Id == "05003");
        Assert.False(result.World!.Areas.SelectMany(area => area.Cards).Single(card => card.Id == identity).Ready);
        Assert.Equal(Hand(expected, 0).Select(card => card.Id), Hand(result, 0).Select(card => card.Id));
    }

    private static Decision PayEvent(Prompt prompt, int card)
    {
        var action = Assert.Single(prompt.Affordances, offer => offer.AnchorId == card);
        var price = Assert.Single(action.CostOptions);
        for (int count = 1; count <= price.Generators.Count; count++)
        {
            int[] paying = price.Generators.Take(count).Select(generator => generator.Effect).ToArray();
            var allocation = ResourcePayment.Allocate(price, paying);
            if (allocation is not null && ResourcePayment.Allows(price, paying, null, allocation))
                return Decision.Take(action.Id, [action.Targets!.Legal[0]], paying,
                    new Dictionary<string, long>(), allocation);
        }
        throw new Xunit.Sdk.XunitException("The naturally drawn event has no legal payment");
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current, Decision decision, string id)
    {
        var response = host.Exchange(EngineRequest.ResolveGame(id, "ms-public", opened.Capability!, decision, current.Revision));
        Assert.Null(response.Error);
        return response;
    }
}
