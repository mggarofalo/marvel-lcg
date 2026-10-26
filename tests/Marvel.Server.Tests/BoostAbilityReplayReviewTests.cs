using System.Security.Cryptography;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class BoostAbilityReplayReviewTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PendingBoostResponseAndDiscardedPreparationReceiptSurviveFreshDurableHosts(bool decline, bool cancelIcons)
    {
        // Synthetic rows exercise IDurableGameFactory without admitting incomplete Black Widow content.
        string storage = Path.Combine(Path.GetTempPath(), "marvel-b6-review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(storage);
        try
        {
            var factory = new BoostAbilityReplayReviewFactory();
            var host = new EngineHost(factory, store: new FileSessionStore(storage));
            const string label = "boost-replay";
            var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 7301);
            var opened = host.Exchange(EngineRequest.OpenGame("open", label, specification));
            Assert.Null(opened.Error);
            var action = Assert.Single(opened.Prompt!.Affordances, a => a.AnchorId == factory.Action);
            var pending = host.Exchange(EngineRequest.ResolveGame("attack", label, opened.Capability!,
                Decision.Take(action.Id), opened.Revision));
            Assert.Null(pending.Error);
            var numeric = Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == factory.Action);
            pending = host.Exchange(EngineRequest.ResolveGame("numeric", label, opened.Capability!,
                cancelIcons ? Decision.Take(numeric.Id) : Decision.Decline, pending.Revision));
            Assert.Null(pending.Error);
            Assert.Contains(pending.Prompt!.Affordances, a => a.AnchorId == factory.Preparation);
            Assert.Equal(0, factory.Game.State.Seats[0].IdentityCard.Damage);
            var original = factory.Game;
            var hashes = Hashes(storage);
            var freshFactory = new BoostAbilityReplayReviewFactory();
            host = new EngineHost(freshFactory, store: new FileSessionStore(storage));
            pending = host.Exchange(EngineRequest.SyncGame("restart-boost", label, opened.Capability!));
            Assert.Null(pending.Error);
            Equivalent(original, freshFactory.Game);
            Assert.NotSame(original.State, freshFactory.Game.State);
            Assert.Equal(hashes, Hashes(storage));
            var take = Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == freshFactory.Preparation);
            var answer = decline ? Decision.Decline : Decision.Take(take.Id);
            var expected = original.Resolve(answer);
            pending = host.Exchange(EngineRequest.ResolveGame("cancel", label, opened.Capability!, answer, pending.Revision));
            Assert.Null(pending.Error);
            Equivalent(original, freshFactory.Game);
            if (!decline)
            {
                Assert.Equal(DeckType.DiscardPile, freshFactory.Game.State.Cards[freshFactory.Preparation].Area.Type);
                Assert.IsType<ResolvedPreparationAbility>(freshFactory.Game.State.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
                Assert.Equal(0, freshFactory.Game.State.Seats[0].IdentityCard.Damage);
                original = freshFactory.Game;
                hashes = Hashes(storage);
                freshFactory = new BoostAbilityReplayReviewFactory();
                host = new EngineHost(freshFactory, store: new FileSessionStore(storage));
                pending = host.Exchange(EngineRequest.SyncGame("restart-receipt", label, opened.Capability!));
                Assert.Null(pending.Error);
                Equivalent(original, freshFactory.Game);
                Assert.Equal(hashes, Hashes(storage));
                var suit = Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == freshFactory.Suit);
                expected = original.Resolve(Decision.Take(suit.Id));
                pending = host.Exchange(EngineRequest.ResolveGame("suit", label, opened.Capability!,
                    Decision.Take(suit.Id), pending.Revision));
                Assert.Null(pending.Error);
                Equivalent(original, freshFactory.Game);
                Assert.True(freshFactory.Game.State.Seats[0].IdentityCard.Ready);
            }
            Assert.True(freshFactory.Game.IsRootPrompt);
            Assert.Null(freshFactory.Game.State.Activation);
            Assert.Equal(2 + (decline ? 1 : 0) + (cancelIcons ? 0 : 1), freshFactory.Game.State.Seats[0].IdentityCard.Damage);
            Assert.Equal(cancelIcons ? 0 : 1, Assert.Single(expected.Events.OfType<BoostResolved>()).Icons);
            Assert.Single(expected.Events.OfType<AttackCompleted>());
            Assert.Equal(SessionSaveJson.Write(Assert.Single(new FileSessionStore(storage).Load()).Save),
                SessionSaveJson.Write(SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(new FileSessionStore(storage).Load()).Save))));
        }
        finally { Directory.Delete(storage, recursive: true); }
    }

    private static string[] Hashes(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
        .Where(path => !path.EndsWith(".writer", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => Path.GetRelativePath(directory, path) + ":" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToArray();

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)), JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
    }
}
