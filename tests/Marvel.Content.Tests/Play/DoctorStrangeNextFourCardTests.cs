using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual next-four programs on prepared nonpublic boards.</summary>
public sealed class DoctorStrangeNextFourCardTests
{
    private static readonly string[] AddedFaces = ["09006", "09010", "09032", "09034"];

    [Fact]
    public void InventoryAppendsFourExactFacesWithoutStartingCounters()
    {
        using var json = JsonDocument.Parse(AuthoredCards.Text);
        var rows = json.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(672, rows.Length);
        Assert.Equal(AddedFaces,
            rows.Skip(667).Take(4).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(667).Take(4), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(27, AuthoredCards.Book.Authored.Count(face => face.StartsWith("09", StringComparison.Ordinal)));
        Assert.DoesNotContain("09003", AuthoredCards.Book.Authored);
    }

    [Fact]
    [Rule("rr:ability.13")]
    public void ActualStudiesRequiresAlterEgoWithOtherwisePayableControl()
    {
        // An alter-ego ability requires "the specified form".
        var world = PrintedSetSearchFixture.Board();
        var seat = world.Seats[1];
        var source = world.CreateCard("09006", seat.Hand);
        world.CreateCard("01093", seat.Hand);
        world.CreateCard("09007", seat.Deck);
        var runner = world.Abilities!;
        Assert.Contains(runner.Actions(world, 1), action => action.Card == source.ObjectId);
        seat.IdentityCard.TurnTo("09001a");
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == source.ObjectId);
        seat.IdentityCard.TurnTo("09001b");
        Assert.Contains(runner.Actions(world, 1), action => action.Card == source.ObjectId);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:ownership-and-control.2.1")]
    [Rule("rr:forced.1")]
    public void EnhancementsFollowsRecipientHeroAndMandatorilyDiscardsInEitherForm(int recipient, bool alterEgo)
    {
        // Upgrades attached to another player's card "are controlled by that other player".
        // Forced abilities "must be resolved when their triggering conditions are met".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var owner = world.Seats[0];
        var hero = world.Seats[recipient].IdentityCard;
        var source = world.CreateCard("09010", owner.Hand);
        var payment = world.CreateCard("01093", owner.Hand);
        string[] stats = ["thwart", "attack", "defense"];
        long[] Values(Card card) => stats.Select(stat => StateFields.Modified(world, card, stat,
            world.Facts, world.Players)).ToArray();
        var baseline = world.Seats.Select(seat => Values(seat.IdentityCard)).ToArray();
        Assert.Contains(hero.ObjectId, runner.AttachmentTargets(world, source)!);
        CardPlay.Play(world, world.Facts, runner, owner, source, [payment.ObjectId], [], [hero.ObjectId]);
        Assert.Equal(0, source.Owner);
        Assert.Equal(recipient, source.Area.PlayArea.Player);
        Assert.Equal(hero.ObjectId, source.Area.Host);
        for (int player = 0; player < 2; player++)
            Assert.Equal(baseline[player].Select(value => value + (player == recipient ? 1 : 0)),
                Values(world.Seats[player].IdentityCard));
        string heroFace = hero.FaceId;
        hero.TurnTo(recipient == 0 ? "06001b" : "01001b");
        Assert.All(Values(hero), value => Assert.Equal(0, value));
        hero.TurnTo(heroFace);
        Assert.Equal(baseline[recipient].Select(value => value + 1), Values(hero));
        if (alterEgo) hero.TurnTo(recipient == 0 ? "06001b" : "01001b");
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 1, 5));
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(0, source.Area.PlayArea.Player);
        Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == source.ObjectId));
        hero.TurnTo(heroFace);
        Assert.Equal(baseline[recipient], Values(hero));
        Assert.Empty(world.Agenda.Outstanding);
    }
}
