using System.Text.Json.Nodes;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic resource and interrupt rows exercise the bounded 09003 cost body.
public sealed class AstralSchemeCostReviewTests
{
    [Theory]
    [InlineData("unchanged", false)]
    [InlineData("departed", false)]
    [InlineData("reentered", false)]
    [InlineData("unchanged", true)]
    [InlineData("departed", true)]
    [InlineData("reentered", true)]
    [InlineData("unchanged", false, true)]
    [InlineData("departed", false, true)]
    [InlineData("reentered", false, true)]
    [InlineData("departed", true, true)]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.7")]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:ability.1")]
    [Rule("rr:confuse-confused.5")]
    public void PaymentSuspensionRetainsTheChosenSchemeIncarnation(string movement, bool confused, bool independent = false)
    {
        // "Pay the cost(s)" precedes resolution. A departing card is a "new
        // copy"; abilities "only interact with cards that are in play".
        // Step 7 says the event "is then placed in its owner's discard pile".
        // When confused, costs "must still be paid" before cancellation.
        var runner = Runner(independent);
        Card source = null!, generator = null!, chosen = null!, other = null!;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("09003", board.Seats[0].Hand);
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea,
                PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            chosen = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            other = board.CreateCard("01107", chosen.Area);
            chosen.PlaceTokens("k_threat", 7);
            other.PlaceTokens("k_threat", 9);
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        if (confused) Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        var action = Assert.Single(runner.Actions(world, 0), entry => entry.Card == source.ObjectId);
        var result = runner.ExecuteAction(world,
            new PlayerAction(action, [generator.ObjectId], [chosen.ObjectId]));
        var events = result.Events.ToList();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Opportunity, prompt.Asking);
        Assert.Equal(0, prompt.Player);
        Assert.False(result.IsComplete);
        Assert.Equal(7, chosen.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        var schemeArea = chosen.Area;
        int incarnation = chosen.Incarnation;
        if (movement != "unchanged")
        {
            World.MoveToTop(chosen, world.AreaOf(DeckType.EncounterDiscardPile));
            if (movement == "reentered")
            {
                World.MoveToTop(chosen, schemeArea);
                chosen.PlaceTokens("k_threat", 7);
                Assert.NotEqual(incarnation, chosen.Incarnation);
            }
        }
        int handBefore = world.Seats[0].Hand.Cards.Count;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(movement == "unchanged" ? confused ? 7 : 4 : movement == "reentered" ? 7 : 0,
            chosen.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(handBefore + (independent && !confused ? 1 : 0), world.Seats[0].Hand.Cards.Count);
        Assert.Equal(9, other.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.DoesNotContain(events, entry => entry.Verb == "Choose");
    }

    private static AbilityRunner Runner(bool independent)
    {
        var rows =
        JsonNode.Parse("""
        [
          {"card":"09003","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
            "labels":["thwart"],"cost":{"chooseScheme":"chosen"},
            "effect":{"thwart":{"target":"chosen","effect":{"removeThreat":{"scheme":"chosen","amount":3}}}}
          }]},
          {"card":"01030","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
            "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"GG"}
          }]},
          {"card":"01092","abilities":[{
            "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
            "effect":{"draw":{"player":"you","count":1}}
          }]}
        ]
        """)!.AsArray();
        if (independent)
        {
            var power = rows[0]!["abilities"]![0]!["effect"]!["thwart"]!;
            var removal = power["effect"]!.DeepClone();
            power["effect"] = new JsonObject { ["seq"] = new JsonArray(removal,
                JsonNode.Parse("""{"draw":{"player":"you","count":1}}""")) };
        }
        return DoctorStrangeAbilityFixture.WithReplacements(rows);
    }
}
