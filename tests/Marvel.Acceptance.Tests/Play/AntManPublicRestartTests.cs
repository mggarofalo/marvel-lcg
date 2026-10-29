using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class AntManPublicRestartTests
{
    [Fact]
    [Rule("rr:ability.11")]
    public void GiantResponseSurvivesFreshHostAndAuthorizedUndoRedo()
    {
        // rr:ability.11: "all interrupt and response abilities are optional" unless forced.
        var journey = new AntManRestartJourney(new GameSpecification("rhino", ["ant_man"], [], 851));
        var opening = Assert.Single(journey.Current.Prompt!.Affordances);
        journey.Answer(Decision.Take(opening.Id));
        var identity = journey.Factory.Game.State.Seats[0].IdentityCard;
        int identityId = identity.ObjectId;
        var giant = Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.Verb == Game.ChangeForm && offer.Label == "12001c");
        journey.Answer(Decision.Take(giant.Id));
        Assert.Equal("12001c", journey.Factory.Game.State.Seats[0].IdentityCard.FaceId);
        Assert.Equal(identityId, journey.Factory.Game.State.Seats[0].IdentityCard.ObjectId);
        Assert.True(journey.Current.Prompt!.Cancellable);
        var response = Assert.Single(journey.Current.Prompt.Affordances, offer => offer.AnchorId == identityId);
        int villain = journey.Factory.Game.State.TheCardIn(DeckType.VillainArea)!.ObjectId;
        Assert.Contains(villain, response.Targets!.Legal);
        long before = journey.Factory.Game.State.Cards[villain].Damage;
        var answer = Decision.Take(response.Id, [villain], []);
        var fresh = journey.Restart();
        fresh.Reject(answer, stale: true);
        fresh.Reject(Decision.Take(response.Id, [identityId], []));
        journey.Continue(fresh, answer);
        // Printed 12001c: "After you change to this form, deal 1 damage to an enemy."
        Assert.Equal(before + 1, fresh.Factory.Game.State.Cards[villain].Damage);
        Assert.True(fresh.Factory.Game.IsRootPrompt);
        fresh.UndoRedo();
        journey.EquivalentState(fresh);
        var attack = Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.AnchorId == identityId && offer.Verb == BasicPowers.AttackVerb);
        var next = Decision.Take(attack.Id, [villain], []);
        journey.Answer(next);
        fresh.Answer(next);
        Assert.Equal(JsonSerializer.Serialize(journey.Current.Events), JsonSerializer.Serialize(fresh.Current.Events));
        journey.EquivalentState(fresh);
    }
}
