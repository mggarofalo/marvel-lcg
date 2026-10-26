using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Events;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class MsMarvelMeleePersistenceTests : EngineHostTestBase
{
    private const string Label = "ms-melee-public";

    [Fact]
    public void SecondEnemyChoiceRestartsFromPublicCustomDeckAndMatchesControl()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        // A bounded opening-hand probe checked seeds 1..20 in order, stopping
        // at 2, the first seed containing Melee after keeping the normal draw.
        var opened = host.Exchange(EngineRequest.OpenGame("open", Label,
            new GameSpecification("klaw", ["ms_marvel"], [], 2, [new(CustomDeck(factory))])));
        Assert.Null(opened.Error);
        var (current, minion, villain, identity, melee) = PlayThroughFirstEnemy(host, opened);
        var prefix = Assert.Single(store.Load());
        Assert.Equal("open", prefix.Save.Units[^1].Status);
        Assert.Equal(PromptJson(current.Prompt!), JsonSerializer.Serialize(prefix.Save.CurrentPrompt));
        long rng = prefix.Save.Units[^1].Decisions[^1].RngWords;
        string beforeRestart = SessionSaveJson.Write(prefix.Save);

        var restoredStore = new MemorySessionStore();
        Assert.Null(restoredStore.Commit(StoredSessionJson.Read(StoredSessionJson.Write(prefix))));
        var restarted = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: restoredStore);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", Label, opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(beforeRestart, SessionSaveJson.Write(Assert.Single(restoredStore.Load()).Save));
        Assert.Equal(PromptJson(current.Prompt!), PromptJson(restored.Prompt!));
        AssertSecondEnemy(restored.Prompt!, minion, villain);
        current = host.Exchange(EngineRequest.SyncGame("sync-control", Label, opened.Capability!));
        Assert.Null(current.Error);
        Assert.Equal(EngineJson.Write(current with { RequestId = "same" }),
            EngineJson.Write(restored with { RequestId = "same", Events = [] }));
        var rejected = restarted.Exchange(EngineRequest.ResolveGame("old-target", Label,
            opened.Capability!, Decision.Take(minion), restored.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Empty(rejected.Events);
        Assert.Equal(beforeRestart, SessionSaveJson.Write(Assert.Single(restoredStore.Load()).Save));
        var retry = restarted.Exchange(EngineRequest.SyncGame("retry", Label, opened.Capability!));
        Assert.Equal(EngineJson.Write(restored with { RequestId = "same" }),
            EngineJson.Write(retry with { RequestId = "same" }));

        var expected = Resolve(host, opened, current, ChooseVillain(current.Prompt!, villain), "second-control");
        var actual = Resolve(restarted, opened, restored, ChooseVillain(restored.Prompt!, villain), "second-restored");
        AssertEquivalent(expected, actual, store, restoredStore, rng);
        AssertEventResponse(actual, minion, villain, identity, melee);
        expected = Resolve(host, opened, expected, Decision.Decline, "decline-control");
        actual = Resolve(restarted, opened, actual, Decision.Decline, "decline-restored");
        AssertEquivalent(expected, actual, store, restoredStore, rng);
        Assert.Equal(Question.TurnOption, actual.Prompt!.Asking);
        Assert.Equal("complete", Assert.Single(restoredStore.Load()).Save.Units[^1].Status);
        Assert.Contains(actual.World!.Areas.Where(area => area.Zone == "DiscardPile" && area.Owner == 0)
            .SelectMany(area => area.Cards), card => card.Id == melee && card.Face?.Id == "05030");
    }

    private static (EngineResponse Current, int Minion, int Villain, int Identity, int Melee)
        PlayThroughFirstEnemy(EngineHost host, EngineResponse opened)
    {
        var current = Resolve(host, opened, opened, TakeOnly(opened), "keep");
        int minion = Assert.Single(current.World!.Areas.Where(area => area.Zone == "EngagedEnemiesArea")
            .SelectMany(area => area.Cards)).Id!.Value;
        int villain = Assert.Single(current.World.Areas.Single(area => area.Zone == "VillainArea").Cards).Id!.Value;
        int identity = current.World.Areas.SelectMany(area => area.Cards)
            .Single(card => card.Face?.Id == "05001b").Id!.Value;
        int melee = Hand(current, 0).First(card => card.Face?.Id == "05030").Id!.Value;
        var flip = Assert.Single(current.Prompt!.Affordances, offer => offer.Verb == "Change_Form");
        current = Resolve(host, opened, current, Decision.Take(flip.Id), "flip");
        current = Resolve(host, opened, current, PayMelee(current.Prompt!, melee), "melee");
        Assert.Equal(Question.Element, current.Prompt!.Asking);
        var first = Assert.Single(current.Prompt.Affordances, offer => offer.AnchorId == minion);
        current = Resolve(host, opened, current, Decision.Take(first.Id), "first-enemy");
        // Printed 05030: "Deal 3 damage to an enemy. Deal 3 damage to another enemy."
        Assert.Contains(current.Events.OfType<FieldSet>(), change => change.Card == minion
            && change.Field == "health" && change.From == 2 && change.To == 0);
        Assert.Contains(current.World!.Areas.Where(area => area.Zone == "EncounterDiscardPile")
            .SelectMany(area => area.Cards), card => card.Id == minion);
        Assert.Equal(0, current.World!.Areas.SelectMany(area => area.Cards).Single(card => card.Id == villain).State!.Damage);
        AssertSecondEnemy(current.Prompt!, minion, villain);
        return (current, minion, villain, identity, melee);
    }

    private static void AssertEventResponse(EngineResponse actual, int minion, int villain, int identity, int melee)
    {
        Assert.Equal(3, actual.World!.Areas.SelectMany(area => area.Cards).Single(card => card.Id == villain).State!.Damage);
        Assert.Contains(actual.Events.OfType<FieldSet>(), change => change.Card == villain
            && change.Field == "health" && change.From - change.To == 3);
        Assert.DoesNotContain(actual.Events.OfType<FieldSet>(), change => change.Card == minion && change.Field == "health");
        // Morphogenetics is an explicit optional response after the complete event.
        Assert.Equal(Question.Opportunity, actual.Prompt!.Asking);
        Assert.True(actual.Prompt.Cancellable);
        Assert.Single(actual.Prompt.Affordances, offer => offer.AnchorId == identity);
        Assert.DoesNotContain(Hand(actual, 0), card => card.Id == melee);
    }

    private static void AssertSecondEnemy(Prompt prompt, int first, int villain)
    {
        Assert.Equal(Question.Element, prompt.Asking);
        Assert.Equal("Choose another enemy", prompt.DisplayQuestion);
        Assert.DoesNotContain(prompt.Affordances, offer => offer.AnchorId == first);
        Assert.Single(prompt.Affordances, offer => offer.AnchorId == villain);
    }

    private static Decision ChooseVillain(Prompt prompt, int villain) =>
        Decision.Take(Assert.Single(prompt.Affordances, offer => offer.AnchorId == villain).Id);

    private static void AssertEquivalent(EngineResponse expected, EngineResponse actual,
        MemorySessionStore control, MemorySessionStore restored, long rng)
    {
        Assert.Equal(EngineJson.Write(expected with { RequestId = "same" }),
            EngineJson.Write(actual with { RequestId = "same" }));
        var controlSave = Assert.Single(control.Load()).Save;
        var restoredSave = Assert.Single(restored.Load()).Save;
        Assert.Equal(SessionSaveJson.Write(controlSave), SessionSaveJson.Write(restoredSave));
        Assert.Equal(rng, restoredSave.Units[^1].Decisions[^1].RngWords);
        Assert.Equal(PromptJson(actual.Prompt!), JsonSerializer.Serialize(restoredSave.CurrentPrompt));
    }

    private static string PromptJson(Prompt prompt) => JsonSerializer.Serialize(PromptRecord.From(prompt));

    private static Decision PayMelee(Prompt prompt, int card)
    {
        var action = Assert.Single(prompt.Affordances, offer => offer.AnchorId == card);
        Assert.Equal("3", Assert.Single(action.Costs!).Cost);
        var price = Assert.Single(action.CostOptions);
        for (int count = 1; count <= price.Generators.Count; count++)
        {
            int[] paying = price.Generators.Take(count).Select(generator => generator.Effect).ToArray();
            var allocation = ResourcePayment.Allocate(price, paying);
            if (allocation is not null && ResourcePayment.Allows(price, paying, null, allocation))
                return Decision.Take(action.Id, [], paying, new Dictionary<string, long>(), allocation);
        }
        throw new Xunit.Sdk.XunitException("The naturally drawn Melee has no legal payment");
    }

    private static string[] CustomDeck(DatasetGameFactory factory)
    {
        var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "ms_marvel");
        string[] deck = [.. hero.Signature,
            "05030", "05030", "05030", "01050", "01051", "01053",
            "01054", "01054", "01055", "01055", "01056", "01056", "01057", "01057",
            "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"];
        Assert.Equal(15, hero.Signature.Count);
        Assert.Equal(40, deck.Length);
        Assert.True(factory.AssessDeck(hero.Key, deck).IsValid);
        return deck;
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened,
        EngineResponse current, Decision decision, string id)
    {
        var response = host.Exchange(EngineRequest.ResolveGame(id, Label,
            opened.Capability!, decision, current.Revision));
        Assert.Null(response.Error);
        return response;
    }
}


