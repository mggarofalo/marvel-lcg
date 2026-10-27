using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Accepted sources and physical Invocation movements in unarranged public games.</summary>
internal sealed class DoctorStrangeGameEvidence(ITestOutputHelper output)
{
    private readonly SortedSet<string> played = new(StringComparer.Ordinal);
    private readonly SortedSet<string> abilities = new(StringComparer.Ordinal);
    private readonly SortedSet<string> invocationDiscards = new(StringComparer.Ordinal);
    private readonly HashSet<int> acting = [];
    private readonly HashSet<int> paying = [];
    private readonly HashSet<int> playing = [];
    private int spellMastery;
    private int naturalTalent;
    private int master;

    internal Action Before(Game game, Decision answer, int ordinal)
    {
        var prompt = game.Pending!;
        var chosen = prompt.Affordances.FirstOrDefault(offer => offer.Id == answer.Affordance);
        string? face = chosen is { AnchorId: >= 0 } && chosen.AnchorId < game.State.Cards.Count
            ? game.State.Cards[chosen.AnchorId].FaceId : null;
        if (prompt.Asking == Question.TurnOption) acting.Add(prompt.Player);
        if (answer.Spent.Count > 0) paying.Add(prompt.Player);
        output.WriteLine($"decision {ordinal}: " + JsonSerializer.Serialize(new
        {
            game.Round, prompt.Player, prompt.Label, Source = face, Answer = answer,
            Top = game.State.Seats[prompt.Player].InvocationDeck?.PublicTop?.FaceId,
            BeforeSha256 = DoctorStrangePublicGameTests.Hash(game.State.Digest().Canonical()),
        }));
        return () => Count(chosen, face, prompt.Player);
    }

    private void Count(Affordance? chosen, string? face, int player)
    {
        if (chosen is null || face is null) return;
        if (chosen.PlaysCard)
        {
            played.Add(face);
            playing.Add(player);
            if (face == "09005") master++;
        }
        if (chosen.Timing is null) return;
        abilities.Add(face);
        if (face == "09001a") spellMastery++;
        if (face == "09001b") naturalTalent++;
    }

    internal void Resolved(Game game, Resolution result)
    {
        foreach (var move in result.Events.OfType<CardsMoved>()
                     .Where(move => move.To.Zone == nameof(DeckType.AdditionalDiscardPile)))
            foreach (var card in move.Cards)
                invocationDiscards.Add(game.State.Cards[card.Card].FaceId);
    }

    internal void Finish(Game game, int players)
    {
        Assert.Equal(Enumerable.Range(0, players), acting.Order());
        Assert.Equal(Enumerable.Range(0, players), paying.Order());
        Assert.Equal(Enumerable.Range(0, players), playing.Order());
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.True(spellMastery > 0);
        Assert.True(naturalTalent > 0);
        Assert.NotEmpty(invocationDiscards);
    }

    internal string Summary => $"spellMastery={spellMastery}, naturalTalent={naturalTalent}, master={master}; "
        + $"played={string.Join(',', played)}; abilities={string.Join(',', abilities)}; "
        + $"invocationDiscards={string.Join(',', invocationDiscards)}";
}

