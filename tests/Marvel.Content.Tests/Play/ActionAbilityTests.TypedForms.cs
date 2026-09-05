using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Fact]
    public void RepeatedIdentityDamageTraceKeepsTheFormThatEndedAVillainGrant()
    {
        // Predicting damage to a player also walks earlier villain-stage
        // transitions. The first player's change to alter-ego ends the live
        // hero-only villain health grant before that transition in every order.
        var runner = FormConditionalVillainGrantRunner(true, includeIdentityDamage: true);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092",
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"],
            abilities: runner, scenario: "klaw");

        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(AuthoredCards.SpiderMan, world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    public void UnchangedFormDoesNotExposeAnUnreachableAttackTarget()
    {
        // The engine's preflight must distinguish an actual form change from
        // an instruction whose destination is already the current form.
        // The unavailable minion target belongs only to the alter-ego branch.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"changeForm":{"player":"you","to":"hero"}},
              {"if":{"test":{"inForm":{"player":"you","form":"hero"}},
                "then":{"heal":{"card":"you","amount":1}},
                "else":{"attack":{"target":{"titled":"Shocker"},
                  "effect":{"dealDamage":{"cards":"chosen","amount":1}}}}
              }}
            ]}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        var result = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is CardsFlipped);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Rule("rr:form-change-form.2")]
    [Theory]
    [InlineData("direct")]
    [InlineData("choice")]
    [InlineData("repeated")]
    [InlineData("dependent")]
    public void FormChangeKeepsCompiledPlayerAndDestination(string wrapper)
    {
        // "When a player changes form, only the form changes."
        // The engine snapshots authored arguments when compiling the book.
        // Changing them later cannot redirect this ability or make a repeated
        // instruction flip an identity back out of its named destination.
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},
              "effect":{"changeForm":{"player":"firstPlayer","to":"hero"}}
            }]}]}
            """);
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["player"] = new AbilityValue.Word("firstPlayer"),
            ["to"] = new AbilityValue.Word("hero"),
        };
        static AbilityValue.Map Map(params (string Key, AbilityValue Value)[] entries) =>
            new(entries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal));
        var form = Map(("changeForm", new AbilityValue.Map(fields)));
        var effect = wrapper switch
        {
            "direct" => form,
            "choice" => Map(("choose", Map(("options", new AbilityValue.List([
                form, Map(("changeForm", Map(("player", new AbilityValue.Word("you")),
                    ("to", new AbilityValue.Word("alter-ego"))))),
            ]))))),
            "repeated" => Map(("forEach", Map(("count", new AbilityValue.Number(2)), ("effect", form)))),
            "dependent" => Map(("then", Map(("effect", form),
                ("then", Map(("heal", Map(("card", new AbilityValue.Word("you")),
                    ("amount", new AbilityValue.Number(1))))))))),
            _ => throw new ArgumentException("Unknown test wrapper", nameof(wrapper)),
        };
        var runner = new AbilityRunner(new AbilityBook(
            [parsed.Abilities[0] with { Effect = AbilityNode.Of(effect) }], parsed.Authored));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
            board.Seats[1].IdentityCard.TakeDamage(2);
            board.Seats[1].IdentityCard.Exhaust();
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        world.FirstPlayer = 1;
        fields["player"] = new AbilityValue.Word("you");
        fields["to"] = new AbilityValue.Word("alter-ego");

        var events = game.Resolve(Decision.Take(action.Id)).Events.ToList();
        if (wrapper == "choice")
        {
            Assert.Equal(Question.Option, game.Pending!.Asking);
            var option = Assert.Single(game.Pending.Affordances);
            events.AddRange(game.Resolve(Decision.Take(option.Id)).Events);
        }

        Assert.True(Forms.In(world, world.Seats[1], Cards, Forms.Hero));
        Assert.True(Forms.In(world, world.Seats[0], Cards, Forms.AlterEgo));
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Seats[1].IdentityCard.Ready);
        Assert.Equal(wrapper == "dependent" ? 0 : 1, world.Seats[0].IdentityCard.Damage);
        var flip = Assert.Single(events.OfType<CardsFlipped>());
        Assert.Equal([world.Seats[1].IdentityCard.ObjectId], flip.Cards);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
