using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class GrootPublicRestartTests
{
    [Fact]
    [Rule("rr:max-maximum.6")]
    [Rule("rr:limit")]
    public void GrowthSpurtSurvivesFreshHostAndAuthorizedUndoRedo()
    {
        // Printed 16001b: "Action: Place 2 growth counters on Groot (to a maximum of 10).
        // (Limit once per round.)" The starter begins in alter-ego form, where Growth
        // Spurt is printed. rr:limit: a limited ability cannot be used again in the
        // same round.
        var journey = new PublicRestartJourney(new GameSpecification("rhino", ["groot"], [], 1601), "groot-restart");
        journey.Answer(Decision.Take(Assert.Single(journey.Current.Prompt!.Affordances).Id));
        var groot = journey.Factory.Game.State.Seats[0].IdentityCard;
        Assert.Equal("16001b", groot.FaceId);
        var spurt = Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.AnchorId == groot.ObjectId && offer.Verb != Game.ChangeForm);
        var answer = Decision.Take(spurt.Id);

        var fresh = journey.Restart();
        fresh.Reject(answer, stale: true);
        journey.Continue(fresh, answer);
        var restored = fresh.Factory.Game.State.Seats[0].IdentityCard;
        Assert.Equal(2, restored.Tokens.GetValueOrDefault("c_growth"));
        fresh.UndoRedo("16001b");
        journey.EquivalentState(fresh);

        Assert.DoesNotContain(fresh.Current.Prompt!.Affordances,
            offer => offer.AnchorId == groot.ObjectId && offer.Verb != Game.ChangeForm);
    }
}
