using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class WaspPublicRestartTests
{
    private const string Giant = "13001c";

    [Theory]
    [InlineData("ultron", true)]
    [InlineData("wrecking_crew_standard", false)]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:thwart.1")]
    public void GiantDividedPowerSurvivesFreshHostAndAuthorizedUndoRedo(string scenario, bool attack)
    {
        // Printed 13001c divides basic attack damage among enemies and basic thwart
        // threat removal among schemes. The rules clarification: "Each enemy is
        // considered to have been attacked" and "Each scheme is considered to have
        // been thwarted." Ultron's setup engages a Drone beside the villain;
        // the Wrecking Crew begins with four side schemes.
        var journey = new PublicRestartJourney(new GameSpecification(scenario, ["wasp"], [], 851), "wasp-restart");
        var divide = OpenDivision(journey, attack);
        int[] shares = [.. divide.Targets!.Legal.Order().Take(2)];
        var before = shares.ToDictionary(id => id, id => Amount(journey, id, attack));
        var answer = Decision.Take(divide.Id, shares, []);
        var fresh = journey.Restart();
        fresh.Reject(answer, stale: true);
        fresh.Reject(Decision.Take(divide.Id, [shares[0]], []));
        journey.Continue(fresh, answer);
        // A one-hit-point Drone is defeated by its share and leaves play.
        Assert.All(shares, id => Assert.True(Math.Abs(Amount(fresh, id, attack) - before[id]) == 1
            || attack && !DeckTypes.IsInPlay(fresh.Factory.Game.State.Cards[id].Area.Type)));
        Assert.Contains(shares, id => Math.Abs(Amount(fresh, id, attack) - before[id]) == 1);
        Assert.False(fresh.Factory.Game.State.Seats[0].IdentityCard.Ready);
        // Defeating Ultron's facedown Drone reveals its card, so that history boundary closes.
        if (attack) Assert.Equal("history_frontier", fresh.RefusedUndo());
        else fresh.UndoRedo(Giant);
        journey.EquivalentState(fresh);
        ContinueBoth(journey, fresh);
    }

    private static Affordance OpenDivision(PublicRestartJourney journey, bool attack)
    {
        journey.Answer(Decision.Take(Assert.Single(journey.Current.Prompt!.Affordances).Id));
        int identity = journey.Factory.Game.State.Seats[0].IdentityCard.ObjectId;
        var giant = Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.Verb == Game.ChangeForm && offer.Label == Giant);
        journey.Answer(Decision.Take(giant.Id));
        Assert.Equal(Giant, journey.Factory.Game.State.Seats[0].IdentityCard.FaceId);
        string verb = attack ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb;
        var power = Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.AnchorId == identity && offer.Verb == verb);
        Assert.Contains(" · Then divide the ", power.Description);
        journey.Answer(Decision.Take(power.Id, [power.Targets!.Legal[0]], []));
        while (journey.Current.Prompt!.Affordances.All(offer => offer.Verb != BasicPowerDivision.Verb))
            journey.Answer(Decision.Decline);
        var prompt = journey.Current.Prompt!;
        var divide = Assert.Single(prompt.Affordances);
        string consequence = attack ? "attacked" : "thwarted";
        Assert.Contains($" is {consequence}", prompt.Description);
        Assert.True(divide.Targets!.AllowRepeated);
        Assert.Equal(2, divide.Targets.Min);
        Assert.Equal(2, divide.Targets.Max);
        Assert.All(divide.Targets.Legal, target => Assert.StartsWith(
            char.ToUpperInvariant(consequence[0]) + consequence[1..] + " if given any", divide.Targets.Details![target]));
        return divide;
    }

    private static void ContinueBoth(PublicRestartJourney journey, PublicRestartJourney fresh)
    {
        var next = Assert.IsType<Prompt>(journey.Current.Prompt);
        var following = next.Cancellable ? Decision.Decline : Decision.Take(next.Affordances[0].Id);
        journey.Answer(following);
        fresh.Answer(following);
        Assert.Equal(JsonSerializer.Serialize(journey.Current.Events), JsonSerializer.Serialize(fresh.Current.Events));
        journey.EquivalentState(fresh);
    }

    private static long Amount(PublicRestartJourney journey, int id, bool attack)
    {
        var card = journey.Factory.Game.State.Cards[id];
        return attack ? card.Damage : card.Tokens.GetValueOrDefault("k_threat");
    }
}
