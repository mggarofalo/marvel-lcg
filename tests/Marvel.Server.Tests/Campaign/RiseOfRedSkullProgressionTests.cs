using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;
namespace Marvel.Server.Tests;

/// <summary>Real host progression from arranged canonical terminal boards, not five seeded wins.</summary>
public sealed class RiseOfRedSkullProgressionTests
{
    [Rule("rr:villain-defeat")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegalFinalAttacksCommitAllFiveResultsRewardsAndRestorableCampaign(bool expert)
    {
        // rr:villain-defeat: "If the final stage of the villain deck is defeated, the players win the game."
        var actual = DatasetGameFactory.Load(RepositoryPaths.Root);
        Assert.NotNull(actual.CampaignDefinition);
        var factory = new CampaignTerminalBoardFactory(actual);
        string directory = Path.Combine(Path.GetTempPath(), "marvel-rors-progression-" + Guid.NewGuid().ToString("N"));
        try
        {
            EngineResponse owner;
            using (var store = new FileCampaignStore(directory))
            {
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                owner = host.Exchange(new(EngineProtocol.Version, "create", EngineProtocol.Campaign, "arranged-rors",
                    Entry: EntryClaim.Create())
                {
                    Campaign = new("create") { Product = factory.CampaignDefinition!.Product, Expert = expert,
                        Seed = 42, Heroes = ["spider_man"],
                        Decks = [new PlayerDeckRecipe(actual.DiscoverDecks().Heroes.Single(hero => hero.Key == "spider_man").Starter)] },
                });
                Assert.Null(owner.Error);
            }
            foreach (var stage in Enum.GetValues<CampaignStage>())
            {
                using var store = new FileCampaignStore(directory);
                var preparedFactory = new CampaignTerminalBoardFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
                var host = new CampaignHost(preparedFactory, store, new MemorySessionStore());
                var prepared = Sync(host, owner);
                Assert.Equal(stage, prepared.Campaign!.State.Stage);
                Assert.Equal(CampaignPhase.Preparing, prepared.Campaign.State.Phase);
                prepared = RepairImprisonedAllyDeck(host, owner, prepared, actual);
                var opened = host.Exchange(Command(owner, "start", prepared.Campaign!.Revision) with { Entry = EntryClaim.Create() });
                Assert.True(opened.Error is null, $"{stage}: {opened.Error} {preparedFactory.Failure}");
                var terminal = WinWithLegalAttack(host, opened);
                Assert.Equal(Outcome.PlayersWin, terminal.World!.Outcome);
                var recorded = Sync(host, owner);
                Assert.Equal(expert, recorded.Campaign!.State.Expert);
                var saved = Assert.Single(store.Load()).Campaign!.Save;
                Assert.Equal((int)stage + 1, saved.Attempts.Length);
                Assert.Contains(saved.Journal, entry => entry.Kind == "progress" && entry.Progress?.Outcome is { Won: true }
                    && entry.Proof!.Setup.Campaign!.Attempt == (int)stage);
                while (recorded.Campaign!.Reward is { } reward)
                {
                    var choice = Assert.Single(reward.Options.Take(1));
                    recorded = host.Exchange(Command(owner, "reward", recorded.Campaign.Revision) with
                        { Campaign = new("reward") { Seat = reward.Seat, Choice = choice.Key } });
                    Assert.Null(recorded.Error);
                }
                Assert.Equal(stage == CampaignStage.RedSkull ? CampaignPhase.Complete : CampaignPhase.Preparing,
                    recorded.Campaign.State.Phase);
            }
            using (var store = new FileCampaignStore(directory))
            {
                var restored = new CampaignHost(factory, store, new MemorySessionStore());
                var complete = Sync(restored, owner).Campaign!;
                Assert.Equal(CampaignPhase.Complete, complete.State.Phase);
                Assert.Equal(5, Assert.Single(store.Load()).Campaign!.Save.Attempts.Length);
                Assert.Contains(complete.State.Players[0].Cards, card => card.Role == CampaignCardRole.Tech);
                Assert.Contains(complete.State.Players[0].Cards, card => card.Role == CampaignCardRole.Condition);
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    private static EngineResponse WinWithLegalAttack(CampaignHost host, EngineResponse opened)
    {
        var current = opened;
        for (int step = 0; step < 20 && current.World!.Outcome == Outcome.Unfinished; step++)
        {
            var prompt = Assert.IsType<Prompt>(current.Prompt);
            var keep = prompt.Affordances.FirstOrDefault(option => option.Verb == Game.ResolveMulligans);
            var flip = prompt.Affordances.FirstOrDefault(option => option.Verb == Game.ChangeForm);
            var attack = prompt.Affordances.FirstOrDefault(option => option.Verb == BasicPowers.AttackVerb && option.IsLegal);
            Decision decision;
            if (keep is not null) decision = Decision.Take(keep.Id);
            else if (prompt.Cancellable && prompt.Asking != Question.TurnOption) decision = Decision.Decline;
            else if (attack is not null)
            {
                int villain = current.World.Areas.Single(area => area.Zone == nameof(DeckType.VillainArea)).Cards.Single().Id!.Value;
                Assert.Contains(villain, attack.Targets!.Legal);
                decision = new(attack.Id, [villain]);
            }
            else if (flip is not null) decision = Decision.Take(flip.Id);
            else throw new InvalidOperationException("Unexpected final-action fixture prompt: " + opened.Campaign!.State.Stage
                + " " + prompt.Label + " " + string.Join("; ", prompt.Affordances.Select(option => option.Verb + ":" + option.Label)));
            current = host.Exchange(EngineRequest.ResolveGame("finish-" + step, opened.GameId,
                opened.Capability!, decision, current.Revision));
            Assert.Null(current.Error);
        }
        return current;
    }

    private static EngineResponse Sync(CampaignHost host, EngineResponse owner)
    {
        var response = host.Exchange(Command(owner, "sync", 0));
        Assert.Null(response.Error);
        return response;
    }

    private static EngineResponse RepairImprisonedAllyDeck(CampaignHost host, EngineResponse owner,
        EngineResponse prepared, DatasetGameFactory factory)
    {
        if (prepared.Campaign!.PreparationProblems.Count == 0) return prepared;
        var state = prepared.Campaign.State;
        Assert.Equal(CampaignStage.RedSkull, state.Stage);
        Assert.Contains("01002", state.BannedAllyFaces);
        var player = state.Players[0];
        Assert.Equal(39, player.Deck.Cards.Count);
        var quantities = factory.CampaignDeckQuantities(player.Hero, player.Deck.Cards, state.BannedAllyFaces);
        string replacement = quantities.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .First(pair => pair.Value.Contains(player.Deck.Cards.Count(face => face == pair.Key) + 1)).Key;
        var repaired = host.Exchange(Command(owner, "deck", prepared.Campaign.Revision) with
            { Campaign = new("deck") { Seat = 0, Deck = new PlayerDeckRecipe([.. player.Deck.Cards, replacement]) } });
        Assert.Null(repaired.Error);
        Assert.Empty(repaired.Campaign!.PreparationProblems);
        return repaired;
    }

    private static EngineRequest Command(EngineResponse owner, string action, long revision) =>
        new(EngineProtocol.Version, action + "-" + revision, EngineProtocol.Campaign, owner.Campaign!.Id,
            Capability: owner.Capability, ExpectedRevision: revision) { Campaign = new(action) };
}

