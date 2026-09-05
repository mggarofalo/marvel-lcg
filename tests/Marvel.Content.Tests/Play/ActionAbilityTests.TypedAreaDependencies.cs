using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData("selector", false)]
    [InlineData("condition", true)]
    [InlineData("number", true)]
    [InlineData("condition", false)]
    [InlineData("number", false)]
    public void ContinuationDependenciesUseCompiledOperands(string operand, bool allUnsafe)
    {
        // Snapshotting authored operands is an engine contract. A later
        // singular lookup must still exclude the discard option after the
        // caller changes the syntax that supplied its dependency.
        const string selection = """{"cardsIn":{"area":"encounterDiscardPile","title":"Hydra Mercenary"}}""";
        string condition = $$$$$$"""{"isKind":{"card":{{{{{{selection}}}}}},"kind":"minion"}}""";
        string number = $$$$$$"""{"add":[{"mul":[{"min":[{"if":{"test":{"not":{"or":[{"and":[{{{{{{condition}}}}}}]}]}},"then":1,"else":1}}]}]}]}""";
        string suffix = operand switch
        {
            "selector" => $$$$$$"""{"removeFromGame":{{{{{{selection}}}}}}} """,
            "condition" => $$$$$$"""{"if":{"test":{{{{{{condition}}}}}},"then":{"heal":{"card":"you","amount":1}}}}""",
            _ => $$$$$$"""{"heal":{"card":"you","amount":{{{{{{number}}}}}}}}""",
        };
        var (runner, fields) = MutableAreaSuffixRunner(suffix, allUnsafe);
        if (operand == "selector")
            fields["cardsIn"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                ["area"] = new AbilityValue.Word("encounterDeck"),
                ["title"] = new AbilityValue.Word("Hydra Mercenary"),
            });
        else if (operand == "condition")
            fields["test"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                ["inForm"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
                {
                    ["player"] = new AbilityValue.Word("you"),
                    ["form"] = new AbilityValue.Word("hero"),
                }),
            });
        else
            fields["amount"] = new AbilityValue.Number(1);

        Card? source = null;
        Card? inPlay = null;
        Card? discarded = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            inPlay = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            discarded = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Seats[0].IdentityCard.TakeDamage(2);
        }, hero: true, abilities: runner);
        if (allUnsafe)
        {
            Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
            Assert.True(source!.Ready);
            Assert.Equal(DeckType.EngagedEnemiesArea, inPlay!.Area.Type);
            Assert.Equal(DeckType.EncounterDiscardPile, discarded!.Area.Type);
            return;
        }
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Id == 0);
        Assert.Contains(game.Pending.Affordances, option => option.Id == 1);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, discarded!.Area.Type);
        Assert.False(source!.Ready);

        game.Resolve(Decision.Take(1));

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(operand == "selector" ? DeckType.RemovedArea : DeckType.EncounterDiscardPile,
            discarded.Area.Type);
        Assert.Equal(operand == "selector" ? 2 : 1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableAreaSuffixRunner(
        string suffix, bool allUnsafe)
    {
        string alternative = allUnsafe ? """{"discard":{"titled":"Hydra Mercenary"}}"""
            : """{"draw":{"player":"you","count":1}}""";
        var parsed = AbilityCatalog.Parse($$$$$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"seq":[
                {"choose":{"options":[{"discard":{"titled":"Hydra Mercenary"}},{{{{{{alternative}}}}}}]}},
                {{{{{{suffix}}}}}}
              ]}
            }]}]}
            """);
        var sequence = (AbilityValue.List)parsed.Abilities[0].Effect.Argument;
        var leaf = AbilityNode.Of(sequence.Values[1]);
        var fields = ((AbilityValue.Map)leaf.Argument).Entries
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var effect = new AbilityNode("seq", new AbilityValue.List([
            sequence.Values[0],
            new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                [leaf.Kind] = new AbilityValue.Map(fields),
            }),
        ]));
        return (new AbilityRunner(new AbilityBook(
            [parsed.Abilities[0] with { Effect = effect }], parsed.Authored)), fields);
    }
}
