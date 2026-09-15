using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DecisionVocabularyTests
{
    [Fact]
    public void MockingbirdNamesTheSourceAndStunEffectInItsChoice()
    {
        Card? mockingbird = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            mockingbird = board.CreateCard(
                "01083",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var occurrence = new Occurrence(
            51, ["WhenCardEntersPlay"], Subject: mockingbird!.ObjectId, Player: 0);
        PendingAbility pending = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Response));

        runner.Resolve(world, occurrence, pending, [], []);
        Prompt prompt = Assert.IsType<Prompt>(
            Sequence.Work(world, CardCatalogData, runner, []));

        Assert.Equal("Mockingbird: choose an enemy to stun", prompt.DisplayQuestion);
        Assert.Equal("Choose an enemy to stun", prompt.Description);
        Assert.Contains(prompt.Affordances,
            option => option.Description == "Stun Rhino");
    }

    [Fact]
    public void ForJusticeAdvertisesItsOneProvablyBetterDeclaration()
    {
        Card? forJustice = null;
        Card? powerOfJustice = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            forJustice = board.CreateCard("01060", board.Seats[0].Hand);
            powerOfJustice = board.CreateCard("01062", board.Seats[0].Hand);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        }, hero: true, abilities: runner);

        PendingAbility action = Assert.Single(
            runner.Actions(world, 0), candidate => candidate.Card == forJustice!.ObjectId);
        CostOption cost = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.Equal("B", cost.PreferredResourceTypes);
        Assert.Contains(cost.Generators,
            source => source.Effect == powerOfJustice!.ObjectId
                && source.Generates == "GG");
    }

    [Fact]
    public void AQualitativePaidResourceBranchDoesNotInventAPreference()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
            board.CreateCard(
                AuthoredCards.Shocker,
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);

        PendingAbility action = Assert.Single(
            runner.Actions(world, 0), candidate => candidate.Card == card!.ObjectId);
        CostOption cost = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.True(cost.DeclarationSensitive);
        Assert.Empty(cost.PreferredResourceTypes);
    }

    [Fact]
    public void AnEqualNumericPaidResourceBranchDoesNotInventAPreference()
    {
        const string forJustice = "01060";
        Card? card = null;
        var runner = Runner(forJustice, "Action", """
            { "removeThreat": {
                "scheme": { "query": "mainScheme" },
                "amount": { "if": {
                    "test": { "paidWithResource": "B" },
                    "then": 3,
                    "else": 3
                } }
            } }
            """);
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(forJustice, board.Seats[0].Hand);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        }, hero: true, abilities: runner);

        PendingAbility action = Assert.Single(
            runner.Actions(world, 0), candidate => candidate.Card == card!.ObjectId);
        CostOption cost = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.True(cost.DeclarationSensitive);
        Assert.Empty(cost.PreferredResourceTypes);
    }
}
