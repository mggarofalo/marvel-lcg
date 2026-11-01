using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class RocketPublicRestartTests
{
    private const string BatteryPack = "16034";
    private const string RocketsPistol = "16038";

    [Fact]
    [Rule("rr:all-purpose-counter")]
    public void BatteryPackCounterMoveSurvivesFreshHostAndAuthorizedUndoRedo()
    {
        // Printed 16034: "Enters play with 2 charge counters on it. Action: Exhaust
        // Battery Pack → move a charge counter from this card to another [tech] upgrade
        // you control." Printed 16038 enters play with 3. Seed 1637 deals both to the
        // opening hand; the starter begins in alter-ego form.
        var journey = new PublicRestartJourney(new GameSpecification("rhino", ["rocket_raccoon"], [], 1637), "rocket-restart");
        journey.Answer(Decision.Take(Assert.Single(journey.Current.Prompt!.Affordances).Id));
        var battery = PlayFromHand(journey, BatteryPack);
        var pistol = PlayFromHand(journey, RocketsPistol);
        battery = journey.Factory.Game.State.Cards[battery.ObjectId];
        Assert.Equal(2, battery.Tokens.GetValueOrDefault("c_charge"));
        Assert.Equal(3, pistol.Tokens.GetValueOrDefault("c_charge"));

        var action = Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.AnchorId == battery.ObjectId && offer.Verb != "Play");
        journey.Answer(Decision.Take(action.Id));
        var choice = Assert.Single(journey.Current.Prompt!.Affordances, offer => offer.AnchorId == pistol.ObjectId);
        var answer = Decision.Take(choice.Id);

        var fresh = journey.Restart();
        fresh.Reject(answer, stale: true);
        journey.Continue(fresh, answer);
        var state = fresh.Factory.Game.State;
        Assert.Equal(1, state.Cards[battery.ObjectId].Tokens.GetValueOrDefault("c_charge"));
        Assert.Equal(4, state.Cards[pistol.ObjectId].Tokens.GetValueOrDefault("c_charge"));
        Assert.False(state.Cards[battery.ObjectId].Ready);
        fresh.UndoRedo("16029b");
        journey.EquivalentState(fresh);
    }

    private static Card PlayFromHand(PublicRestartJourney journey, string face)
    {
        var game = journey.Factory.Game;
        var card = game.State.Seats[0].Hand.Cards.First(candidate => candidate.FaceId == face);
        var offer = Assert.Single(journey.Current.Prompt!.Affordances,
            option => option.AnchorId == card.ObjectId && option.Verb == "Play");
        // Pay with one card that is neither upgrade the journey plays.
        int[] payment = [offer.CostOptions[0].Generators.Select(source => source.Effect)
            .First(id => game.State.Cards[id].FaceId is not (BatteryPack or RocketsPistol))];
        var values = GoblinPolicyPayment.Values(offer);
        // The upgrade's one legal target is the identity it extends.
        journey.Answer(Decision.Take(offer.Id, [Assert.Single(offer.Targets!.Legal)], payment, values,
            GoblinPolicyPayment.Allocations(offer, payment, values)));
        while (journey.Current.Prompt is { Cancellable: true } window && window.Affordances.All(option => option.Verb != "Play"))
            journey.Answer(Decision.Decline);
        var played = journey.Factory.Game.State.Cards[card.ObjectId];
        Assert.Equal(DeckType.UpgradesArea, played.Area.Type);
        return played;
    }
}
