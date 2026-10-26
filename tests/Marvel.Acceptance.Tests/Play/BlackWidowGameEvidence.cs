using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Records accepted Black Widow activations and public-game observations.</summary>
internal sealed class BlackWidowGameEvidence(ITestOutputHelper output)
{
    private readonly SortedSet<string> played = new(StringComparer.Ordinal);
    private readonly SortedSet<string> abilities = new(StringComparer.Ordinal);
    private readonly SortedSet<string> seen = new(StringComparer.Ordinal);
    private readonly HashSet<int> acting = [];
    private readonly HashSet<int> paying = [];
    private readonly HashSet<int> playing = [];
    private int preparationPlays;
    private int preparationAbilities;
    private int missionPrep;
    private int widowmaker;

    internal Action Before(Game game, Decision answer, int ordinal)
    {
        var prompt = game.Pending!;
        var chosen = prompt.Affordances.FirstOrDefault(offer => offer.Id == answer.Affordance);
        string? face = Source(game, chosen);
        int hand = game.State.Seats[prompt.Player].Hand.Cards.Count;
        if (prompt.Asking == Question.TurnOption) acting.Add(prompt.Player);
        if (answer.Spent.Count > 0) paying.Add(prompt.Player);
        CountSource(game, chosen, face, prompt.Player);
        output.WriteLine($"decision {ordinal}: " + JsonSerializer.Serialize(new
        {
            game.Round, prompt.Player, prompt.Label, Source = face, Answer = answer,
            BeforeSha256 = BlackWidowPublicGameTests.Hash(game.State.Digest().Canonical()),
        }));
        return () => After(game, face, chosen, prompt.Player, hand);
    }

    private static string? Source(Game game, Affordance? chosen) =>
        chosen is { AnchorId: >= 0 } && chosen.AnchorId < game.State.Cards.Count
            ? game.State.Cards[chosen.AnchorId].FaceId : null;

    private void CountSource(Game game, Affordance? chosen, string? face, int player)
    {
        if (face is null || chosen is null) return;
        CountPlay(game, chosen, face, player);
        if (chosen.Timing is null) return;
        abilities.Add(face);
        if (game.State.Facts.Traits(face).Contains("PREPARATION")) preparationAbilities++;
        if (face == "08001a") widowmaker++;
    }

    private void CountPlay(Game game, Affordance chosen, string face, int player)
    {
        if (chosen.PlaysCard)
        {
            played.Add(face);
            playing.Add(player);
            if (game.State.Facts.Traits(face).Contains("PREPARATION")) preparationPlays++;
        }
    }

    private void After(Game game, string? face, Affordance? chosen, int player, int hand)
    {
        foreach (var card in game.State.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)))
            seen.Add(card.FaceId);
        if (face == "08001b" && chosen?.Timing is not null)
        {
            Assert.Equal(hand + 1, game.State.Seats[player].Hand.Cards.Count);
            missionPrep++;
        }
    }

    internal void Finish(Game game, int players, bool custom)
    {
        Assert.Equal(Enumerable.Range(0, players), acting.Order());
        Assert.Equal(Enumerable.Range(0, players), paying.Order());
        Assert.Equal(Enumerable.Range(0, players), playing.Order());
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.True(preparationPlays > 0);
        Assert.True(preparationAbilities > 0);
        Assert.True(widowmaker > 0);
        if (custom) Assert.Contains("08030", played);
    }

    internal string Summary => $"preparationPlays={preparationPlays}, preparationAbilities={preparationAbilities}, "
        + $"missionPrep={missionPrep}, widowmaker={widowmaker}; played={string.Join(',', played)}; "
        + $"abilities={string.Join(',', abilities)}; seenInPlay={string.Join(',', seen)}";
}
