using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowNickFuryCardTests
{
    [Theory]
    [InlineData("Remove 2 threat from a scheme")]
    [InlineData("Draw 3 cards")]
    [InlineData("Deal 4 damage to an enemy")]
    [Rule("rr:delayed-effect.1")]
    public void ActualEntryOffersEachPrintedEffectThenDiscardsThisIncarnation(string choice)
    {
        // Delayed effects "resolve automatically" at their specified timing point.
        var (_, world) = Playing(_ => { }, hero: true);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var nick = world.CreateCard("08019", seat.Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        long threat = scheme.Tokens["k_threat"], damage = villain.Damage;
        var paying = new[] { world.CreateCard("08020", seat.Hand), world.CreateCard("08021", seat.Hand) };
        CardPlay.Play(world, world.Facts, runner, seat, nick, paying.Select(c => c.ObjectId).ToArray(), []);
        int hand = seat.Hand.Cards.Count;
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(3, prompt.Affordances.Count);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.Description?.StartsWith(choice, StringComparison.Ordinal) == true).Id), []);
        if (choice != "Draw 3 cards")
        {
            prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            int target = choice.StartsWith("Remove", StringComparison.Ordinal) ? scheme.ObjectId : villain.ObjectId;
            Sequence.Answer(world, world.Facts, runner, prompt,
                Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == target).Id), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(threat - (choice.StartsWith("Remove", StringComparison.Ordinal) ? 2 : 0), scheme.Tokens["k_threat"]);
        Assert.Equal(damage + (choice.StartsWith("Deal", StringComparison.Ordinal) ? 4 : 0), villain.Damage);
        Assert.Equal(hand + (choice == "Draw 3 cards" ? 3 : 0), seat.Hand.Cards.Count);
        DelayedEffects.Occur(world, "WhenRoundEnds", []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, nick.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void AnOldEntryCannotDiscardTheReenteredPhysicalCard()
    {
        // A card returning to play "is considered to be a new copy of the card."
        var (_, world) = Playing(_ => { });
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var nick = world.CreateCard("08019", seat.Hand);
        var paying = new[] { world.CreateCard("08020", seat.Hand), world.CreateCard("08021", seat.Hand) };
        CardPlay.Play(world, world.Facts, runner, seat, nick, paying.Select(c => c.ObjectId).ToArray(), []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.Description == "Draw 3 cards").Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        var area = nick.Area;
        Discard.Card(world, nick, "test departure", []);
        World.MoveToTop(nick, area);
        DelayedEffects.Occur(world, "WhenRoundEnds", []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Same(area, nick.Area);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
