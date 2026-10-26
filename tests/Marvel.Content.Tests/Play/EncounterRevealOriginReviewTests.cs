using System.Text.Json.Nodes;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EncounterRevealOriginReviewTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [Rule("ruling:7075a85e02f2203e")]
    public void OriginFlagRequiresTheExactCardAndRevealCondition(bool sameCard, bool reveal, bool expected)
    {
        // The ruling distinguishes a reveal from another zone; a receipt must
        // describe this exact triggering revelation, not another pending card.
        var world = ThorActualFixture.Board("06001a");
        var runner = DoctorStrangeAbilityFixture.WithReplacements(JsonNode.Parse("""
            [{"card":"01092","abilities":[{
             "trigger":{"event":"EVENT","timing":"Interrupt","subject":"game"},
             "when":{"revealedFromEncounterDeck":"trigger"},
             "effect":{"dealDamage":{"cards":"you","amount":1}}}]}]
            """.Replace("EVENT", reveal ? "WhenCardRevealed" : "WhenCardEntersPlay", StringComparison.Ordinal))!.AsArray());
        var observer = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("01190", world.AreaOf(DeckType.RevealingArea));
        var occurrence = new Occurrence(93, reveal ? [Steps.CardRevealed] : [Steps.CardEntersPlay], card.ObjectId, 0)
        {
            RevealOrigin = new EncounterRevealOrigin(sameCard ? card.ObjectId : observer.ObjectId,
                Places.Reference(world.AreaOf(DeckType.EncounterDeck))),
        };
        Assert.Equal(expected, runner.Waiting(world, occurrence, WindowKind.Interrupt)
            .Any(ability => ability.Card == observer.ObjectId));
        var withoutOrigin = occurrence with { RevealOrigin = null };
        Assert.DoesNotContain(runner.Waiting(world, withoutOrigin, WindowKind.Interrupt),
            ability => ability.Card == observer.ObjectId);
    }
}
