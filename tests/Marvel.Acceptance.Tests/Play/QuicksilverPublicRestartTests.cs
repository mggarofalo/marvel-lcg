using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class QuicksilverPublicRestartTests
{
    [Fact]
    [Rule("rr:basic-power.1.1")]
    [Rule("rr:limit")]
    public void SuperSpeedSurvivesFreshHostAndAuthorizedUndoRedo()
    {
        // Printed 14001a: "Response: After you use one of Quicksilver's basic powers (THW,
        // ATK, or DEF), ready him. (Limit once per phase.)" Quicksilver attacks Rhino
        // on his first turn and the response readies him. The starter
        // begins as Pietro Maximoff and changes to Quicksilver first. rr:basic-power.1.1:
        // the attack power "can be used by a character to perform a basic attack".
        var journey = new PublicRestartJourney(new GameSpecification("rhino", ["quicksilver"], [], 851), "quicksilver-restart");
        journey.Answer(Decision.Take(Assert.Single(journey.Current.Prompt!.Affordances).Id));
        int identity = journey.Factory.Game.State.Seats[0].IdentityCard.ObjectId;
        var hero = Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.Verb == Game.ChangeForm);
        journey.Answer(Decision.Take(hero.Id));
        Assert.Equal("14001a", journey.Factory.Game.State.Seats[0].IdentityCard.FaceId);
        var thwart = Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.AnchorId == identity && offer.Verb == BasicPowers.AttackVerb);
        journey.Answer(Decision.Take(thwart.Id, [thwart.Targets!.Legal[0]], []));
        while (journey.Current.Prompt!.Affordances.All(offer => offer.AnchorId != identity || offer.Verb == BasicPowers.AttackVerb))
            journey.Answer(Decision.Decline);
        var superSpeed = Assert.Single(journey.Current.Prompt!.Affordances, offer => offer.AnchorId == identity);
        Assert.True(journey.Current.Prompt.Cancellable);
        Assert.False(journey.Factory.Game.State.Seats[0].IdentityCard.Ready);
        var answer = Decision.Take(superSpeed.Id);

        var fresh = journey.Restart();
        fresh.Reject(answer, stale: true);
        journey.Continue(fresh, answer);
        Assert.True(fresh.Factory.Game.State.Seats[0].IdentityCard.Ready);
        fresh.UndoRedo("14001a");
        journey.EquivalentState(fresh);

        // The shared once-per-phase limit withholds Super Speed after a second basic power.
        var again = Assert.Single(fresh.Current.Prompt!.Affordances,
            offer => offer.AnchorId == identity && offer.Verb == BasicPowers.AttackVerb);
        fresh.Answer(Decision.Take(again.Id, [again.Targets!.Legal[0]], []));
        while (fresh.Current.Prompt is { } prompt && prompt.Cancellable && prompt.Asking != Question.TurnOption)
        {
            Assert.DoesNotContain(prompt.Affordances, offer => offer.AnchorId == identity);
            fresh.Answer(Decision.Decline);
        }
        Assert.False(fresh.Factory.Game.State.Seats[0].IdentityCard.Ready);
    }
}
