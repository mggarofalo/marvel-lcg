using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ForcedDamageReceiptReplayTests
{
    private const string Label = "forced-damage-receipt";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TaskmasterReceiptSurvivesPreventionReflectionAndStrictFreshHosts(bool prevent)
    {
        // Receipt association is an engine choice. Prepared Taskmaster/Energy Barrier
        // board isolates that contract without claiming a normally dealt scenario.
        var factory = new ForcedDamageReceiptReplayFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = Exchange(host, EngineRequest.OpenGame("open", Label,
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7301)));
        var flip = Assert.Single(opened.Prompt!.Affordances, option => option.Verb == Game.ChangeForm);
        var current = Resolve(host, opened, opened, Decision.Take(flip.Id), "flip");
        var receipt = Assert.Single(current.Events.OfType<AbilityWindowResult>());
        Assert.Equal(factory.Taskmaster, receipt.Source);
        Assert.False(receipt.Completed);
        Assert.Equal(0, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal("WhenDamageWouldBeDealt", current.Prompt!.Trigger);
        (host, factory, store, current) = Restart(opened, current, store);
        var barrier = Assert.Single(current.Prompt!.Affordances, option => option.AnchorId == factory.Barrier);
        current = Resolve(host, opened, current, prevent ? Decision.Take(barrier.Id) : Decision.Decline, "prevention");
        if (prevent)
        {
            Assert.Equal(0, factory.Game.State.Seats[0].IdentityCard.Damage);
            (host, factory, store, current) = Restart(opened, current, store);
            var target = Assert.Single(current.Prompt!.Affordances, option => option.Id == factory.Taskmaster);
            current = Resolve(host, opened, current, Decision.Take(target.Id), "reflection");
            var reflected = Assert.Single(current.Events.OfType<FieldSet>(), happened =>
                happened.Card == factory.Taskmaster && happened.Verb == "Deal_Damage");
            Assert.Null(reflected.AbilitySource);
            Assert.Null(reflected.AbilityOccurrence);
            Assert.Equal(1, factory.Game.State.Cards[factory.Taskmaster].Damage);
        }
        var damage = Assert.Single(current.Events.OfType<FieldSet>(), happened =>
            happened.Card == factory.Game.State.Seats[0].IdentityCard.ObjectId && happened.Verb == "Deal_Damage");
        Assert.Equal(prevent ? 2 : 3, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(receipt.Source, damage.AbilitySource);
        Assert.Equal(receipt.Occurrence, damage.AbilityOccurrence);
        Assert.Equal(AbilityType.ForcedResponse, damage.AbilityTiming);
        Assert.Equal(receipt.Ordinal, damage.AbilityOrdinal);
        Assert.DoesNotContain(current.Events, happened => happened is AbilityWindowResult);
        Assert.True(factory.Game.IsRootPrompt);
        _ = Restart(opened, current, store);
        RejectChangedAssociation(opened, store, damage);
    }

    private static void RejectChangedAssociation(EngineResponse opened, MemorySessionStore store, FieldSet damage)
    {
        string archive = StoredSessionJson.Write(Assert.Single(store.Load()));
        var stored = StoredSessionJson.Read(archive);
        var unit = stored.Save.Units[^1];
        var step = unit.Decisions[^1];
        string original = JournalJson.Event(damage).GetRawText();
        string altered = JournalJson.Event(damage with { AbilityOccurrence = damage.AbilityOccurrence + 1 }).GetRawText();
        Assert.Contains(step.Events, happened => happened.GetRawText() == original);
        var changedStep = step with { Events = step.Events.Select(happened => happened.GetRawText() == original
            ? JsonDocument.Parse(altered).RootElement.Clone() : happened).ToArray() };
        var changedUnit = unit with { Decisions = unit.Decisions.SkipLast(1).Append(changedStep).ToArray() };
        var changed = stored with { Save = stored.Save with
            { Units = stored.Save.Units.SkipLast(1).Append(changedUnit).ToArray() } };
        var rejectedStore = new MemorySessionStore();
        Assert.Null(rejectedStore.Commit(StoredSessionJson.Read(StoredSessionJson.Write(changed))));
        var host = new EngineHost(new ForcedDamageReceiptReplayFactory(), store: rejectedStore);
        var refused = host.Exchange(EngineRequest.SyncGame("tampered", Label, opened.Capability!));
        Assert.Equal("session_not_found", refused.Error?.Code);
        Assert.Equal(StoredSessionJson.Write(changed), StoredSessionJson.Write(Assert.Single(rejectedStore.Load())));
        Assert.Equal(archive, StoredSessionJson.Write(Assert.Single(store.Load())));
    }

    private static (EngineHost, ForcedDamageReceiptReplayFactory, MemorySessionStore, EngineResponse)
        Restart(EngineResponse opened, EngineResponse current, MemorySessionStore store)
    {
        string archive = StoredSessionJson.Write(Assert.Single(store.Load()));
        var restoredStore = new MemorySessionStore();
        Assert.Null(restoredStore.Commit(StoredSessionJson.Read(archive)));
        var factory = new ForcedDamageReceiptReplayFactory();
        var host = new EngineHost(factory, store: restoredStore);
        var restored = Exchange(host, EngineRequest.SyncGame("restart", Label, opened.Capability!));
        Assert.Equal(current.Revision, restored.Revision);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(current.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        Assert.Equal(archive, StoredSessionJson.Write(Assert.Single(restoredStore.Load())));
        return (host, factory, restoredStore, restored);
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current,
        Decision answer, string id) => Exchange(host,
        EngineRequest.ResolveGame(id, Label, opened.Capability!, answer, current.Revision));

    private static EngineResponse Exchange(EngineHost host, EngineRequest request)
    {
        var result = EngineJson.ReadResponse(EngineJson.Write(host.Exchange(EngineJson.ReadRequest(EngineJson.Write(request)))));
        Assert.Null(result.Error);
        return result;
    }
}
