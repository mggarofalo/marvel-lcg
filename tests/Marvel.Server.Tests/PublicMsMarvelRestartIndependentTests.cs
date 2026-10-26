using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicMsMarvelRestartIndependentTests : EngineHostTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:event")]
    public void SavedResponseAcceptAndDeclineMatchAnIndependentUninterruptedStore(bool recover)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var controlStore = new MemorySessionStore();
        var control = new EngineHost(factory, store: controlStore);
        // Normal published starter and seed; no explicit deck or prepared world.
        var opened = control.Exchange(EngineRequest.OpenGame("open", "ms-independent",
            new GameSpecification("rhino", ["ms_marvel"], [], 3)));
        Assert.Null(opened.Error);
        var current = Resolve(control, opened, opened, TakeOnly(opened), "keep");
        int identity = current.World!.Areas.SelectMany(area => area.Cards)
            .Single(card => card.Face?.Id == "05001b").Id!.Value;
        int[] beforeSpirit = Hand(current, 0).Select(card => card.Id!.Value).ToArray();
        var spirit = Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == identity && offer.Verb != "Change_Form");
        current = Resolve(control, opened, current, Decision.Take(spirit.Id), "spirit");
        var found = Assert.Single(Hand(current, 0), card => !beforeSpirit.Contains(card.Id!.Value));
        Assert.Contains(found.Face!.Id, factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "ms_marvel").Signature);
        var flip = Assert.Single(current.Prompt!.Affordances, offer => offer.Verb == "Change_Form");
        current = Resolve(control, opened, current, Decision.Take(flip.Id), "flip");
        int eventId = Hand(current, 0).First(card => card.Face?.Id == "05003").Id!.Value;
        current = Resolve(control, opened, current, PayEvent(current.Prompt!, eventId), "event");

        // rr:event: "then places the card in its owner's discard pile after those
        // effects resolve". The exact event is now eligible for Morphogenetics.
        Assert.DoesNotContain(Hand(current, 0), card => card.Id == eventId);
        Assert.True(current.Prompt!.Cancellable);
        Assert.Single(current.Prompt.Affordances, offer => offer.AnchorId == identity);
        var prefix = Assert.Single(controlStore.Load());
        string savedPrefix = SessionSaveJson.Write(prefix.Save);
        var restartStore = new MemorySessionStore();
        restartStore.Commit(prefix);
        var restarted = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: restartStore);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "ms-independent", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(savedPrefix, SessionSaveJson.Write(Assert.Single(restartStore.Load()).Save));
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(current.Prompt)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));

        var response = Assert.Single(restored.Prompt!.Affordances, offer => offer.AnchorId == identity);
        var decision = recover ? Decision.Take(response.Id) : Decision.Decline;
        var expected = Resolve(control, opened, current, decision, "control");
        var actual = Resolve(restarted, opened, restored, decision, "restarted");

        Assert.Equal(SessionSaveJson.Write(Assert.Single(controlStore.Load()).Save),
            SessionSaveJson.Write(Assert.Single(restartStore.Load()).Save));
        Assert.Equal(JsonSerializer.Serialize(expected.Events), JsonSerializer.Serialize(actual.Events));
        Assert.Equal(recover, Hand(actual, 0).Any(card => card.Id == eventId && card.Face?.Id == "05003"));
        var identityCard = actual.World!.Areas.SelectMany(area => area.Cards).Single(card => card.Id == identity);
        Assert.Equal(!recover, identityCard.Ready);
        Assert.Equal(4, actual.World.Areas.SelectMany(area => area.Cards)
            .Single(card => card.Face?.Id == "01094").Face!.Damage);
    }

    private static Decision PayEvent(Prompt prompt, int eventId)
    {
        var action = Assert.Single(prompt.Affordances, offer => offer.AnchorId == eventId);
        var cost = Assert.Single(action.CostOptions);
        for (int count = 1; count <= cost.Generators.Count; count++)
        {
            int[] paying = cost.Generators.Take(count).Select(generator => generator.Effect).ToArray();
            var allocation = ResourcePayment.Allocate(cost, paying);
            if (allocation is not null && ResourcePayment.Allows(cost, paying, null, allocation))
                return Decision.Take(action.Id, [action.Targets!.Legal[0]], paying,
                    new Dictionary<string, long>(), allocation);
        }
        throw new Xunit.Sdk.XunitException("The normally dealt event requires legal payment");
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current,
        Decision decision, string request)
    {
        var answer = host.Exchange(EngineRequest.ResolveGame(request, "ms-independent", opened.Capability!,
            decision, current.Revision));
        Assert.Null(answer.Error);
        return answer;
    }
}
