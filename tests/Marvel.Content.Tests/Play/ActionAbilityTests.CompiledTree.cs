using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData("choice:option:0")]
    [InlineData("if:then")]
    [InlineData("forEach:0:2")]
    public void AContinuationFrameMustIdentifyAChildOfTheCompiledInstruction(string frame)
    {
        // Continuation paths are an engine-owned format. A frame for a different
        // instruction must raise the supported failure before any sibling runs.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[{"exhaust":"this"},{"heal":{"card":"you","amount":1}}]}
            """);
        var (world, source) = FixedCountBoard(runner);
        world.Seats[0].IdentityCard.TakeDamage(1);
        var forged = new PhaseStep(
            Steps.ResumeAbility, 1, 2, Subject: source.ObjectId, Seat: 0,
            Tier: AbilityType.Action, AbilityOrdinal: 0,
            AbilityPath: [frame], AbilityFace: source.FaceId,
            AbilityHasContinuation: true);

        var error = Assert.Throws<RulesNotImplementedException>(() => runner.ResumeAbility(world, forged));

        Assert.Contains("continuation path", error.Message, StringComparison.Ordinal);
        Assert.Contains(frame, error.Message, StringComparison.Ordinal);
        Assert.True(source.Ready);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:choose-option.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayerQualifiedDiscardChoiceChecksTheSelectedDeck(bool selectedDeckEmpty)
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"chooseCard":{"from":{"query":"identities"},"effect":{"choose":{"options":[
              {"discardTop":{"from":"yourDeck","player":"chosenPlayer","count":1}},
              {"heal":{"card":"you","amount":1}}
            ]}}}}
            """, cost: """{"exhaust":"this"}""");
        var (world, source) = FixedCountBoard(runner, players: 2);
        world.Seats[0].IdentityCard.TakeDamage(1);
        var populated = world.Seats[selectedDeckEmpty ? 0 : 1].Deck;
        var bottom = world.CreateCard("01002", populated);
        var top = world.CreateCard("01003", populated);

        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var selection = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, selection,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), []);
        var options = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));

        // "When a player card requires a player to choose an option, they cannot
        // choose an option that cannot be at least partially resolved."
        // The selected player's deck controls whether discarding can apply;
        // the resolver's oppositely populated deck cannot supply that answer.
        Assert.Equal(!selectedDeckEmpty, options.Affordances.Any(option => option.Id == 0));
        Assert.Contains(options.Affordances, option => option.Id == 1);
        Sequence.Answer(world, Cards, runner, options, Decision.Take(selectedDeckEmpty ? 1 : 0), []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(selectedDeckEmpty ? new[] { bottom, top } : new[] { bottom }, populated.Cards);
        Assert.Equal(selectedDeckEmpty ? DeckType.PlayerDeck : DeckType.DiscardPile, top.Area.Type);
        Assert.Equal(selectedDeckEmpty ? 0 : 1, world.Seats[0].IdentityCard.Damage);
        Assert.False(source.Ready);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompiledContinuationDoesNotRetainTheSuppliedBookOrSequence(bool afterSuspension)
    {
        // The program is an engine-owned snapshot, including structural
        // children and the ability index used after a player decision.
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"seq":[
                {"choose":{"options":[{"heal":{"card":"you","amount":1}},{"heal":{"card":"you","amount":2}}]}},
                {"choose":{"options":[{"heal":{"card":"you","amount":3}},{"heal":{"card":"you","amount":4}}]}}
              ]}
            }]}]}
            """);
        var steps = ((AbilityValue.List)parsed.Abilities[0].Effect.Argument).Values.ToList();
        var rows = new List<CardAbility>
        {
            parsed.Abilities[0] with { Effect = new AbilityNode("seq", new AbilityValue.List(steps)) },
        };
        var authored = parsed.Authored.ToHashSet(StringComparer.Ordinal);
        var runner = new AbilityRunner(new AbilityBook(rows, authored));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        void ChangeInput()
        {
            rows.Clear();
            steps.Clear();
            authored.Clear();
        }
        if (!afterSuspension) ChangeInput();

        game.Resolve(Decision.Take(action.Id));
        if (afterSuspension) ChangeInput();
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal([0, 1], game.Pending.Affordances.Select(option => option.Id));
        game.Resolve(Decision.Take(0));
        Assert.Equal(8, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal([0, 1], game.Pending.Affordances.Select(option => option.Id));
        game.Resolve(Decision.Take(1));

        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.False(source!.Ready);
        Assert.Contains(AuthoredCards.AuntMay, runner.Authored);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
