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

public sealed class ThorEngagementContinuationReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:ability.step.4")]
    [Rule("rr:ability.8")]
    public void PlacementWaitsForDeclinedOrSuspendedResponse(bool transfer, bool accept)
    {
        // Quickstrike is a "Forced Response (Hero)"; forced responses precede
        // optional responses. "The player who controls the card" chooses use.
        // Synthetic response text adds a nested choice to test completion ownership.
        var world = Hero("06001a", players: 2);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"06001a","abilities":[{
                "trigger":{"event":"WhenMinionEngaged","timing":"Response","subject":"you","form":"hero"},
                "effect":{"choose":{"descriptions":["Draw two cards","Draw one card"],"options":[
                  {"draw":{"player":"you","count":2}},
                  {"draw":{"player":"you","count":1}}
                ]}}}]},
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
        int responses = 0;
        int choices = 0;
        int defenses = 0;
        for (int step = 0; step < 30; step++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            Assert.Equal(0, source.Tokens.GetValueOrDefault("c_finished"));
            var answer = ObservePrompt(world, prompt, accept, ref defenses, ref responses, ref choices);
            Sequence.Answer(world, Cards, runner, prompt, answer, events);
        }
        Assert.Equal(1, responses);
        Assert.Equal(accept ? 1 : 0, choices);
        Assert.Equal(accept ? 2 : 0, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(1, source.Tokens["c_finished"]);
        Assert.Equal(PlayArea.Of(0), minion.Area.PlayArea);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Decision ObservePrompt(World world, Prompt prompt, bool accept,
        ref int defenses, ref int responses, ref int choices)
    {
        var response = prompt.Affordances.SingleOrDefault(a =>
            a.AnchorId == world.Seats[0].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
        if (prompt.Asking == Question.Defender)
        {
            defenses++;
            Assert.Equal(0, responses);
        }
        else if (prompt.Asking == Question.Option)
        {
            choices++;
            Assert.True(accept);
            Assert.Equal(1, responses);
            Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
            Assert.Empty(world.Seats[0].Hand.Cards);
            return Decision.Take(prompt.Affordances[0].Id);
        }
        else if (response is not null)
        {
            responses++;
            Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
            Assert.True(defenses > 0);
            if (accept) return Decision.Take(response.Id);
        }
        return Decision.Decline;
    }
}
