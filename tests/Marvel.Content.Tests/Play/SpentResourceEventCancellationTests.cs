using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic event, canceler and resource response; no expansion admission.
public sealed class SpentResourceEventCancellationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:event.2")]
    public void EventCancellationDoesNotRepeatTheCompletedSpend(bool cancel)
    {
        // A canceled event's "costs remain paid" and only its effects are canceled.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
          {"cards":[
            {"card":"12006","abilities":[{
              "trigger":{"event":"WhenResourceCardSpent","timing":"Response","subject":"this","player":"you","form":"hero"},
              "effect":{"placeCounters":{"card":"you","counter":"spent_seen","count":1}}}]},
            {"card":"05003","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":{"placeCounters":{"card":"you","counter":"event_seen","count":1}}}]},
            {"card":"05010","abilities":[{
              "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
              "cost":{"exhaust":"this"},"effect":{"cancelOccurrence":"trigger"}}]}]}
          """));
        Card? played = null;
        Card? spent = null;
        Card? energy = null;
        Card? canceler = null;
        var (game, world) = Playing(board =>
        {
            played = board.CreateCard("05003", board.Seats[0].Hand);
            spent = board.CreateCard("12006", board.Seats[0].Hand);
            energy = board.CreateCard("01088", board.Seats[0].Hand);
            canceler = board.CreateCard("05010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == played!.ObjectId);
        game.Resolve(PaidDecision.Take(action, [spent!.ObjectId, energy!.ObjectId]));
        Assert.Equal(DeckType.RevealingArea, played!.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_event_seen"));
        var response = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == spent.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        var interrupt = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == canceler!.ObjectId);
        game.Resolve(cancel ? Decision.Take(interrupt.Id) : Decision.Decline);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_spent_seen"));
        Assert.Equal(cancel ? 0 : 1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_event_seen"));
        Assert.Equal(DeckType.DiscardPile, spent.Area.Type);
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}
