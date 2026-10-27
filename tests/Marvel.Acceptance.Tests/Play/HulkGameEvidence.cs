using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Accepted Hulk sources and physical hand discards in unarranged public games.</summary>
internal sealed class HulkGameEvidence(ITestOutputHelper output, int hulkSeat)
{
    private readonly SortedSet<string> played = new(StringComparer.Ordinal);
    private readonly SortedSet<string> abilities = new(StringComparer.Ordinal);
    private readonly HashSet<int> acting = [];
    private readonly HashSet<int> paying = [];
    private readonly HashSet<int> playing = [];
    private HashSet<int>? endingHand;
    private int basicAttacks;
    private int signaturePlays;
    private int research;
    private int handDiscards;
    private int discardedCards;

    internal Action Before(Game game, Decision answer, int ordinal)
    {
        var prompt = game.Pending!;
        var chosen = prompt.Affordances.FirstOrDefault(offer => offer.Id == answer.Affordance);
        string? face = chosen is { AnchorId: >= 0 } && chosen.AnchorId < game.State.Cards.Count
            ? game.State.Cards[chosen.AnchorId].FaceId : null;
        if (prompt.Asking == Question.TurnOption) acting.Add(prompt.Player);
        if (answer.Spent.Count > 0) paying.Add(prompt.Player);
        CaptureTurnEnd(game, chosen);
        output.WriteLine($"decision {ordinal}: " + JsonSerializer.Serialize(new
        {
            game.Round, prompt.Player, prompt.Label, Source = face, Answer = answer,
            BeforeSha256 = HulkPublicGameTests.Hash(game.State.Digest().Canonical()),
        }));
        return () => Count(chosen, face, prompt.Player);
    }

    private void CaptureTurnEnd(Game game, Affordance? chosen)
    {
        var prompt = game.Pending!;
        if (prompt.Asking != Question.TurnOption || chosen is not null || prompt.Player != hulkSeat)
            return;
        var seat = game.State.Seats[hulkSeat];
        if (seat.IdentityCard.FaceId != "10001a" || seat.Hand.Cards.Count == 0) return;
        Assert.Null(endingHand);
        endingHand = seat.Hand.Cards.Select(card => card.ObjectId).ToHashSet();
    }

    private void Count(Affordance? chosen, string? face, int player)
    {
        if (chosen is null || face is null) return;
        if (chosen.PlaysCard)
        {
            played.Add(face);
            playing.Add(player);
            if (IsSignature(face)) signaturePlays++;
        }
        if (chosen.Verb == BasicPowers.AttackVerb && face == "10001a") basicAttacks++;
        if (chosen.Timing is null) return;
        abilities.Add(face);
        if (face == "10001b") research++;
    }

    private static bool IsSignature(string face) =>
        StringComparer.Ordinal.Compare(face, "10002") >= 0
        && StringComparer.Ordinal.Compare(face, "10010") <= 0;

    internal void Resolved(Game game, Resolution result)
    {
        if (endingHand is null) return;
        foreach (var move in result.Events.OfType<CardsMoved>().Where(move =>
                     move.From.Zone == nameof(DeckType.HandsArea) && move.From.Owner == hulkSeat
                     && move.To.Zone == nameof(DeckType.DiscardPile)))
            foreach (var card in move.Cards)
                if (endingHand.Remove(card.Card))
                {
                    Assert.Equal(DeckType.DiscardPile, game.State.Cards[card.Card].Area.Type);
                    discardedCards++;
                }
        if (endingHand.Count == 0)
        {
            handDiscards++;
            endingHand = null;
        }
    }

    internal void Finish(Game game, int players)
    {
        Assert.Equal(Enumerable.Range(0, players), acting.Order());
        Assert.Equal(Enumerable.Range(0, players), paying.Order());
        Assert.Equal(Enumerable.Range(0, players), playing.Order());
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.True(basicAttacks > 0);
        Assert.True(signaturePlays > 0);
        Assert.True(research > 0);
        Assert.True(handDiscards > 0);
        Assert.Null(endingHand);
    }

    internal string Summary => $"basicAttacks={basicAttacks}, signaturePlays={signaturePlays}, "
        + $"research={research}, handDiscards={handDiscards}, discardedCards={discardedCards}; "
        + $"played={string.Join(',', played)}; abilities={string.Join(',', abilities)}";
}
