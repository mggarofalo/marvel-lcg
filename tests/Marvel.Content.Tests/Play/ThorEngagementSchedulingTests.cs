using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorEngagementSchedulingTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:ability.step.4")]
    public void InterpretedPlacementWaitsForQuickstrikeAndEngagementResponses(bool transfer)
    {
        // Quickstrike is a "Forced Response (Hero)"; "Forced Response" precedes
        // "Response". Synthetic following text observes the entry suspension.
        var world = Hero("06001a", players: 2);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"06001a","abilities":[{"limitPerPhase":1,
                "trigger":{"event":"WhenMinionEngaged","timing":"Response","subject":"you","form":"hero"},
                "effect":{"draw":{"player":"you","count":2}}}]},
              {"card":"06003","abilities":[{
                "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
                "effect":{"seq":[
                  {"putIntoPlay":{"card":PLACEMENT_TARGET,"where":"engagedWithYou"}},
                  {"placeCounters":{"card":"this","counter":"finished","count":1}}
                ]}}]}
            ]}
            """.Replace("PLACEMENT_TARGET", transfer ? "{\"titled\":\"Vulture\"}"
                : "{\"cardsIn\":{\"area\":\"encounterDiscardPile\",\"title\":\"Vulture\"}}", StringComparison.Ordinal)));
        for (int index = 0; index < 4; index++) world.CreateCard("01088", world.Seats[0].Deck);
        var minion = world.CreateCard("01167", transfer
            ? world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1))
            : world.AreaOf(DeckType.EncounterDiscardPile));
        var source = world.CreateCard("06003", world.AreaOf(DeckType.RevealingArea,
            PlayArea.Of(0), cardOwner: 0));
        var events = new List<GameEvent>(runner.ExecuteReveal(world, source, 0).Events);
        int defenses = 0;
        int responses = 0;
        for (int step = 0; step < 30; step++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            Assert.Equal(0, source.Tokens.GetValueOrDefault("c_finished"));
            var thor = prompt.Affordances.SingleOrDefault(a =>
                a.AnchorId == world.Seats[0].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
            if (prompt.Asking == Question.Defender)
            {
                defenses++;
                Assert.Equal(0, responses);
                Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
                Assert.Empty(world.Seats[0].Hand.Cards);
            }
            if (thor is not null)
            {
                responses++;
                Assert.True(defenses > 0);
                Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
            }
            Sequence.Answer(world, Cards, runner, prompt,
                thor is null ? Decision.Decline : Decision.Take(thor.Id), events);
        }
        Assert.Equal(1, responses);
        Assert.Equal(1, source.Tokens["c_finished"]);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(PlayArea.Of(0), minion.Area.PlayArea);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
