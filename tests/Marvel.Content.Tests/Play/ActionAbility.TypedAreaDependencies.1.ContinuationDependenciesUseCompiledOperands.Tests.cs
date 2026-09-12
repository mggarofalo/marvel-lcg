using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilityTypedAreaDependenciesContinuationDependenciesUseCompiledOperandsTests
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
        string suffix = DependencySuffix(operand, selection, condition, number);
        var(runner, fields) = MutableAreaSuffixRunner(suffix, allUnsafe);
        MutateDependency(operand, fields);
        Card? source = null;
        Card? inPlay = null;
        Card? discarded = null;
        var(game, world) = Playing(board =>
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
        AssertDependencyOutcome(operand, discarded, world);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}
