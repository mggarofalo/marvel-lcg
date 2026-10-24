using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class BasicAttackCostReplayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingCostRestartsPrivatelyAndProducesTheSamePaidAttack(bool stunned)
    {
        var factory = new BasicAttackCostReplayFactory(DatasetGameFactory.Load(RepositoryPaths.Root), stunned);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "cost",
            new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 12345)));
        Assert.Null(opened.Error);
        var attack = Assert.Single(opened.Prompt!.Affordances,
            offer => offer.AnchorId == factory.Ally && offer.Verb == BasicPowers.AttackVerb);
        var pending = host.Exchange(EngineRequest.ResolveGame("attack", "cost", opened.Capability!,
            new Decision(attack.Id, [attack.Targets!.Legal[0]]), opened.Revision));
        Assert.Null(pending.Error);
        var uninterrupted = factory.Game;
        Assert.True(uninterrupted.State.Cards[factory.Ally].Ready);
        var hidden = WorldProjection.For(uninterrupted.State, uninterrupted.Pending, [],
            new RestrictedVisibilityPolicy(1).Authorize(null, 2));
        Assert.Null(hidden.Prompt);
        Assert.All(hidden.World.Areas.Single(area => area.Id == uninterrupted.State.Seats[0].Hand.Id).Cards,
            card => { Assert.Null(card.Id); Assert.Null(card.Face); });
        string saved = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "cost", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(pending.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        Assert.Equal(saved, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(uninterrupted.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        var choice = Assert.Single(restored.Prompt!.Affordances);
        int payment = choice.Targets!.Legal[0];
        var answer = new Decision(choice.Id, [payment]);
        uninterrupted.Resolve(answer);
        var result = restarted.Exchange(EngineRequest.ResolveGame("pay", "cost", opened.Capability!, answer, restored.Revision));
        Assert.Null(result.Error);
        Assert.Equal(uninterrupted.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[payment].Area.Type);
        Assert.False(factory.Game.State.Cards[factory.Ally].Ready);
        Assert.Equal(stunned ? 0 : 1, factory.Game.State.TheCardIn(DeckType.VillainArea)!.Damage);
    }
}
