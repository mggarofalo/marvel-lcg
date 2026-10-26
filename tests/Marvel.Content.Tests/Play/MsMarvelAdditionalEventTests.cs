using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual authored event rows in prepared nonpublic engine games.</summary>
public sealed class MsMarvelAdditionalEventTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:lasting-effects")]
    public void MoraleBoostAffectsOnlySelectedHeroUntilRoundEnd(int recipient)
    {
        // "Until the end of the round" keeps all three printed bonuses alive through phase end.
        Card? played = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            played = board.CreateCard("05032", board.Seats[0].Hand);
            payment = board.CreateCard("01003", board.Seats[0].Hand);
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        string[] fields = ["thwart", "attack", "defense"];
        long Value(int player, string field) => StateFields.Modified(world, world.Seats[player].IdentityCard,
            field, world.Facts, world.Players);
        var before = world.Seats.Select((_, i) => fields.Select(f => Value(i, f)).ToArray()).ToArray();
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == played!.ObjectId);
        game.Resolve(PaidDecision.Take(action, [payment!.ObjectId]));
        game.Resolve(Decision.Take(world.Seats[recipient].IdentityCard.ObjectId));
        for (int i = 0; i < 2; i++)
            Assert.Equal(before[i].Select(v => v + (i == recipient ? 1 : 0)), fields.Select(f => Value(i, f)));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(before[recipient].Select(v => v + 1), fields.Select(f => Value(recipient, f)));
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Equal(before[recipient], fields.Select(f => Value(recipient, f)));
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.True(game.IsRootPrompt);
    }
}
