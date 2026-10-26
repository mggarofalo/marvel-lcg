using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class MutagenVictoryTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("mutagen_formula", 3u)]
    public void PublicSetupWinsWithExactDecisionReplay(string scenario, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var starter = Assert.Single(factory.DiscoverDecks().Heroes, hero => hero.Key == "captain_america").Starter;
        Assert.Equal(40, starter.Count);
        Assert.True(factory.AssessDeck("captain_america", starter).IsValid);
        Assert.False(Assert.Single(factory.DiscoverSetup().Scenarios, choice => choice.Key == scenario).Expert);
        string[] cards = ["03002", "03003", "03003", "03004", "03004", "03004", "03005", "03005",
            "03006", "03006", "03007", "03008", "03009", "03010", "03010", "03011", "03012", "03013",
            "03014", "01067", "01068", "03020", "01084", "03016", "03016", "03016", "01070", "01070",
            "01070", "01086", "01086", "01086", "01091", "01092", "03018", "03018", "03021", "03022", "03023", "01074"];
        Assert.Equal(40, cards.Length);
        Assert.True(factory.AssessDeck("captain_america", cards).IsValid);
        var specification = new GameSpecification(scenario, ["captain_america"], null, seed, [new PlayerDeckRecipe(cards)]);
        var game = factory.Create(specification).Game;
        var replay = factory.Create(specification).Game;
        Assert.Equal("02014", game.State.TheCardIn(DeckType.VillainArea)!.FaceId);
        Assert.Equal("02017b", game.State.TheCardIn(DeckType.MainSchemesArea)!.FaceId);
        Assert.Equal("03001b", Assert.Single(game.State.Seats).IdentityCard.FaceId);
        var policy = new MutagenTacticalPolicy(game.State.Facts);
        var stages = new HashSet<string>(StringComparer.Ordinal);
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is not null)
        {
            Assert.True(policy.Answered < 2000, $"{scenario}: {game.Pending.Label}");
            if (game.State.TheCardIn(DeckType.VillainArea) is { } villain) stages.Add(villain.FaceId);
            var beforeAnswer = Observe(game);
            var answer = policy.Answer(game);
            Assert.Equal(beforeAnswer, Observe(game));
            output.WriteLine("decision=" + JsonSerializer.Serialize(answer));
            var identity = game.State.Seats[0].IdentityCard;
            output.WriteLine($"{policy.Answered}: r{game.Round} {identity.FaceId} dmg={identity.Damage}; "
                + $"{game.Pending.Label}; {game.Pending.Affordances.FirstOrDefault(option => option.Id == answer.Affordance)?.Label ?? "decline"}; "
                + $"targets={string.Join(',', answer.Targets)}; resources={string.Join(',', answer.Resources ?? [])}; threat={game.State.TheCardIn(DeckType.MainSchemesArea)?.Tokens.GetValueOrDefault("k_threat")}; hand={string.Join(',', game.State.Seats[0].Hand.Cards.Select(card => game.State.Facts.Title(card.FaceId)))}");
            output.WriteLine("board=" + string.Join(';', game.State.Areas
                .Where(area => area.Type is DeckType.EngagedEnemiesArea or DeckType.VillainArea or DeckType.SideSchemesArea)
                .SelectMany(area => area.Cards).Select(card => $"{card.ObjectId}:{card.FaceId}:damage={card.Damage}")));
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"{scenario}, seed={seed}, decision={policy.Answered}, "
                    + $"prompt={game.Pending.Label}, answer={JsonSerializer.Serialize(answer)}", failure);
            }
            var replayed = replay.Resolve(answer);
            Assert.Equal(Observe(game, result), Observe(replay, replayed));
        }
        output.WriteLine($"{scenario}, seed={seed}: {game.State.Result}, round={game.Round}, "
            + $"decisions={policy.Answered}, recoveries={policy.Recoveries}, allyDefenses={policy.AllyDefenses}");
        Assert.Equal(Outcome.PlayersWin, game.State.Result);
        Assert.Equal(["02014", "02015"], stages.Order());
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(replay.Pending);
    }

    private static string Observe(Game game, Resolution? result = null) => JsonSerializer.Serialize(new
    {
        State = game.State.Digest().Canonical(),
        RngWords = game.State.Random.Generator.WordsConsumed,
        game.State.Result,
        game.Phase,
        game.Round,
        Prompt = game.Pending is { } prompt ? PromptRecord.From(prompt) : null,
        result?.Events,
        result?.Information,
    });
}
