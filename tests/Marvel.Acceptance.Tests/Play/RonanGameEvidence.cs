using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Natural modular encounters and decision indexes in ordinary public games.</summary>
internal sealed class RonanGameEvidence(ITestOutputHelper output)
{
    private readonly SortedSet<string> observed = new(StringComparer.Ordinal);
    private readonly HashSet<int> acting = [];
    private readonly HashSet<int> paying = [];
    private readonly HashSet<int> playing = [];
    private readonly List<string> characteristic = [];

    internal static bool IsRonan(string face) => face is "90001" or "90002" or "90003" or "90004" or "90005";

    internal void Before(Game game, Decision answer, int ordinal)
    {
        var prompt = game.Pending!;
        var chosen = prompt.Affordances.FirstOrDefault(offer => offer.Id == answer.Affordance);
        if (game.State.Seats[prompt.Player].Eliminated)
            output.WriteLine("eliminated-seat prompt: " + JsonSerializer.Serialize(new
            {
                ordinal, prompt.Player, prompt.Trigger, prompt.Label,
                AuthorizedPrompt = game.PromptFor(prompt.Player),
                Sources = prompt.Affordances.Select(offer => new
                {
                    offer.AnchorId,
                    game.State.Cards[offer.AnchorId].FaceId,
                    Area = game.State.Cards[offer.AnchorId].Area.Type.ToString(),
                }),
            }));
        if (prompt.Asking == Question.TurnOption) acting.Add(prompt.Player);
        if (answer.Spent.Count > 0) paying.Add(prompt.Player);
        if (chosen?.PlaysCard == true) playing.Add(prompt.Player);
        output.WriteLine($"decision {ordinal}: " + JsonSerializer.Serialize(new
        {
            game.Round, prompt.Player, prompt.Label, Answer = answer,
            BeforeSha256 = RonanPublicGameTests.Hash(game.State.Digest().Canonical()),
        }));
    }

    internal void Resolved(Game game, Resolution result, int ordinal)
    {
        foreach (var move in result.Events.OfType<CardsMoved>())
        foreach (var landing in move.Cards)
        {
            string face = game.State.Cards[landing.Card].FaceId;
            if (!IsRonan(face)) continue;
            observed.Add(face);
            string fact = $"{ordinal}:{face}:{move.From.Zone}->{move.To.Zone}(owner={move.To.Owner})";
            characteristic.Add(fact);
            output.WriteLine("modular " + fact);
        }
        foreach (var boost in result.Events.OfType<BoostResolved>())
        {
            string face = game.State.Cards[boost.Card].FaceId;
            if (!IsRonan(face)) continue;
            string fact = $"{ordinal}:{face}:boost(enemy={boost.Enemy},icons={boost.Icons},attack={boost.Attacking})";
            characteristic.Add(fact);
            output.WriteLine("modular " + fact);
        }
    }

    internal void Finish(Game game, int players)
    {
        Assert.Equal(Enumerable.Range(0, players), acting.Order());
        Assert.Equal(Enumerable.Range(0, players), paying.Order());
        Assert.Equal(Enumerable.Range(0, players), playing.Order());
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.NotEmpty(observed);
    }

    internal string Summary => $"observed={string.Join(',', observed)}; characteristic={string.Join(';', characteristic)}";
}
