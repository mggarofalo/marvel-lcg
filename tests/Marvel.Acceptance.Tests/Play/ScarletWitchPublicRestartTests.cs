using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class ScarletWitchPublicRestartTests
{
    private const string Hero = "15001a";

    [Fact]
    [Rule("rr:would")]
    [Rule("rr:limit")]
    public void ChaosControlSurvivesFreshHostAtTheWouldCountWindow()
    {
        // Printed 15001a: "Interrupt: When boost icons on an encounter card would be
        // counted, discard the top card of the encounter deck and count the number of
        // boost icons on that card instead. (Limit once per phase.)" Ruling
        // 6962a015e8597900: boost icons are counted whenever a game step determines
        // the exact number on an encounter card, as when a boost card is turned faceup.
        var journey = new PublicRestartJourney(new GameSpecification("rhino", ["scarlet_witch"], [], 1501), "scarlet-witch-restart");
        var chaos = OpenChaosControl(journey);
        Assert.Equal(0, journey.Current.Prompt!.Player);
        var answer = Decision.Take(chaos.Id);
        int discarded = Discarded(journey);

        var fresh = journey.Restart();
        fresh.Reject(answer, stale: true);
        journey.Continue(fresh, answer);
        Assert.Equal(discarded + 1, Discarded(fresh));
        // Chaos Control resolves inside the villain's activation, whose dependent
        // decisions are still pending, so history refuses to move and keeps the session.
        Assert.Equal("history_open", fresh.RefusedUndo());
        journey.EquivalentState(fresh);

        var next = Assert.IsType<Prompt>(journey.Current.Prompt);
        var following = next.Cancellable ? Decision.Decline : Decision.Take(next.Affordances[0].Id);
        journey.Answer(following);
        fresh.Answer(following);
        Assert.Equal(JsonSerializer.Serialize(journey.Current.Events), JsonSerializer.Serialize(fresh.Current.Events));
        journey.EquivalentState(fresh);
    }

    private static Affordance OpenChaosControl(PublicRestartJourney journey)
    {
        journey.Answer(Decision.Take(Assert.Single(journey.Current.Prompt!.Affordances).Id));
        var hero = Assert.Single(journey.Current.Prompt!.Affordances, offer => offer.Verb == Game.ChangeForm);
        journey.Answer(Decision.Take(hero.Id));
        Assert.Equal(Hero, journey.Factory.Game.State.Seats[0].IdentityCard.FaceId);
        int identity = journey.Factory.Game.State.Seats[0].IdentityCard.ObjectId;
        var policy = new CoreGamePolicy(journey.Factory.Game.State.Facts);
        for (int step = 0; step < 200; step++)
        {
            var prompt = journey.Current.Prompt!;
            var chaos = prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == identity
                && offer.Label == "Chaos Control");
            if (chaos is not null && journey.Factory.Game.State.Seats[0].IdentityCard.FaceId == Hero) return chaos;
            journey.Answer(policy.Answer(journey.Factory.Game));
        }
        throw new InvalidOperationException("Chaos Control was not offered");
    }

    private static int Discarded(PublicRestartJourney journey) =>
        journey.Factory.Game.State.AreaOf(DeckType.EncounterDiscardPile).Cards.Count;
}
