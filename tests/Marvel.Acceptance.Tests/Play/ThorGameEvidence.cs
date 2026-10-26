using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Captures public-game observations and distinguishes reached Thor mechanics.</summary>
internal sealed class ThorGameEvidence(ITestOutputHelper output)
{
    private readonly SortedSet<string> played = new(StringComparer.Ordinal);
    private readonly SortedSet<string> abilities = new(StringComparer.Ordinal);
    private readonly SortedSet<string> stages = new(StringComparer.Ordinal);
    private readonly HashSet<int> acting = [];
    private int payments;
    private int attacks;
    private int turns;
    private int engagementDraws;
    private int energyUses;
    private int aerialInstalls;

    internal string Summary => $"distinctPlayed={played.Count}, payments={payments}, attacks={attacks}, "
        + $"stages={string.Join(',', stages)}, turns={turns}, engagementDraws={engagementDraws}, "
        + $"godOfThunderUses={energyUses}, aerialInstalls={aerialInstalls}";

    internal string Cards => $"played={string.Join(',', played)}; abilities={string.Join(',', abilities)}";

    internal Action Before(Game game, Decision answer, int ordinal)
    {
        var prompt = game.Pending!;
        var chosen = prompt.Affordances.FirstOrDefault(offer => offer.Id == answer.Affordance);
        string? face = SourceFace(game, chosen);
        int handBefore = game.State.Seats[prompt.Player].Hand.Cards.Count;
        Count(game, prompt, chosen, answer);
        RecordSource(chosen, face);
        output.WriteLine($"decision {ordinal}: " + JsonSerializer.Serialize(new
        {
            game.Round, prompt.Player, prompt.Label, Source = face, Answer = answer,
            BeforeSha256 = ThorPublicGameTests.Hash(game.State.Digest().Canonical()),
        }));
        return () => After(game, prompt.Player, chosen, face, handBefore);
    }

    private static string? SourceFace(Game game, Affordance? chosen) =>
        chosen is { AnchorId: >= 0 } && chosen.AnchorId < game.State.Cards.Count
            ? game.State.Cards[chosen.AnchorId].FaceId : null;

    private void Count(Game game, Prompt prompt, Affordance? chosen, Decision answer)
    {
        if (prompt.Asking == Question.TurnOption) acting.Add(prompt.Player);
        if (game.State.TheCardIn(DeckType.VillainArea) is { } villain) stages.Add(villain.FaceId);
        if (answer.Spent.Count > 0) payments++;
        if (chosen?.Verb == BasicPowers.AttackVerb) attacks++;
        if (answer.IsDecline && prompt.Asking == Question.TurnOption) turns++;
        energyUses += answer.Spent.Count(id => game.State.Cards[id].FaceId == "06008");
    }

    private void RecordSource(Affordance? chosen, string? face)
    {
        if (face is null || chosen is null) return;
        if (chosen.PlaysCard) played.Add(face);
        if (chosen.Verb == Game.ActionVerb || chosen.Timing is not null)
            abilities.Add($"{face}:{chosen.Verb}");
    }

    private void After(Game game, int player, Affordance? chosen, string? face, int handBefore)
    {
        if (face == "06001a" && chosen?.Timing == Marvel.Rules.Timing.AbilityType.Response)
        {
            Assert.Equal(handBefore + 2, game.State.Seats[player].Hand.Cards.Count);
            engagementDraws++;
        }
        if (face == "06009" && chosen?.PlaysCard == true) aerialInstalls++;
        AssertAerial(game);
    }

    private static void AssertAerial(Game game)
    {
        var thor = game.State.Seats[0].IdentityCard;
        if (thor.FaceId == "06001a" && game.State.Cards.Any(card => card.Owner == 0
            && card.FaceId == "06009" && DeckTypes.IsInPlay(card.Area.Type)))
            Assert.True(Traits.Has(game.State, thor, "AERIAL", game.State.Facts));
    }

    internal void Finish(Game game, int players)
    {
        Assert.Equal(Enumerable.Range(0, players), acting.Order());
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.NotEmpty(played);
        Assert.Contains("06009", played);
        Assert.True(payments > 0);
        Assert.True(attacks > 0);
    }
}
