using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreLastingEffectCardsTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:lasting-effects.4")]
    [Rule("rr:target.5")]
    [Fact]
    public void LeadFromTheFrontAffectsTheChosenPlayersCurrentAndLaterCharacters()
    {
        var world = Board();
        var eventCard = world.CreateCard("01070", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var existing = world.CreateCard(
            "01076", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(eventCard.ObjectId, AbilityType.Action, 0),
            [payment.ObjectId], []);
        AnswerCardChoice(world, runner, eventCard, world.Seats[1].IdentityCard);

        var later = world.CreateCard(
            "01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        Assert.Equal(3, StateFields.Modified(world, existing, "attack", Cards, world.Players));
        Assert.Equal(2, StateFields.Modified(world, later, "thwart", Cards, world.Players));
        Assert.Equal(
            3,
            StateFields.Modified(
                world, world.Seats[1].IdentityCard, "attack", Cards, world.Players));
        Assert.Equal(
            2,
            StateFields.Modified(
                world, world.Seats[0].IdentityCard, "attack", Cards, world.Players));
    }

    [Rule("rr:lasting-effects.1")]
    [Rule("rr:target.5")]
    [Fact]
    public void HelicarrierReducesOnlyTheChosenPlayersNextCard()
    {
        var world = Board();
        var support = world.CreateCard(
            "01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var mine = world.CreateCard("01083", world.Seats[0].Hand);
        var theirs = world.CreateCard("01083", world.Seats[1].Hand);
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(support.ObjectId, AbilityType.Action, 0), [], []);
        AnswerCardChoice(world, runner, support, world.Seats[1].IdentityCard);

        Assert.False(support.Ready);
        Assert.Equal(3, CardPlay.CostOf(world, Cards, world.Seats[0], mine).Amount);
        Assert.Equal(2, CardPlay.CostOf(world, Cards, world.Seats[1], theirs).Amount);
    }

    private static void AnswerCardChoice(
        World world, Marvel.Cards.Run.AbilityRunner runner, Card source, Card chosen)
    {
        var step = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, source, player: 0, step.Index,
            Decision.Take(chosen.ObjectId), step.Tier);
    }

    private static World Board()
    {
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            seat.IdentityCard.TurnTo("01001a");
        }

        return world;
    }
}
