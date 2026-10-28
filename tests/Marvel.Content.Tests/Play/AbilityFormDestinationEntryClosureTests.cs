using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic entry contracts; these abilities do not admit printed Ant-Man content.
public sealed class AbilityFormDestinationEntryClosureTests
{
    private const string Standalone = """{"changeForm":{"player":"firstPlayer","to":{"face":"12001c"}}}""";
    private static readonly string Composed = """{"and":[CHANGE]}"""
        .Replace("CHANGE", Standalone, StringComparison.Ordinal);
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActivationEntryGuardsOnlyAnActivationThatWasMade(bool made)
    {
        var (runner, world, source) = Board("activation", Composed);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();

        var error = Record.Exception(() => runner.CompleteActivation(world,
            new EnemyActivation(source.ObjectId, 0, Attacking: true, Made: made)));

        // Engine choice: reject unsupported composition before scheduling any flip.
        if (made) Assert.Contains("composed", Assert.IsType<RulesNotImplementedException>(error).Message);
        else Assert.Null(error);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Null(world.AbilityRuntime);
    }

    [Fact]
    public void MadeActivationAcceptsAStandaloneDestination()
    {
        var (runner, world, source) = Board("activation", Standalone);

        runner.CompleteActivation(world,
            new EnemyActivation(source.ObjectId, 0, Attacking: true, Made: true));

        Assert.Equal("12001c", Assert.Single(world.Agenda.Outstanding)
            .Require<IdentityFormChangeOperation>().Destination);
    }

    [Theory]
    [InlineData("reveal")]
    [InlineData("activation")]
    public void BlankedPrintedTextDoesNotEnterAnUnsupportedComposition(string path)
    {
        var (runner, world, source) = Board(path, Composed, minion: true);
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, world.Seats[0].IdentityCard, source));
        string before = world.Digest().Canonical();

        var result = path == "reveal" ? runner.ExecuteReveal(world, source, 0)
            : runner.CompleteActivation(world,
                new EnemyActivation(source.ObjectId, 0, Attacking: true, Made: true));

        Assert.True(PrintedText.IsBlanked(world, source));
        Assert.True(result.IsComplete);
        Assert.Empty(result.Events);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Fact]
    public void ARevealInAnotherFormDoesNotEnterAnUnsupportedComposition()
    {
        var (runner, world, source) = Board("reveal", Composed, form: Forms.AlterEgo);
        Assert.True(Forms.In(world, world.Seats[0], world.Facts, Forms.Hero));

        var result = runner.ExecuteReveal(world, source, 0);

        Assert.True(result.IsComplete);
        Assert.Empty(result.Events);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal("12001a", world.Seats[0].IdentityCard.FaceId);
    }

    [Fact]
    public void CancellationPrecedesTheGuardAndRejectionPrecedesTheTreacheryReceipt()
    {
        var (runner, world, source) = Board("reveal", Composed);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "cancelWhenRevealed", Affects: source.ObjectId, Lasts: new Duration(Uses: 1)));

        var canceled = runner.ExecuteReveal(world, source, 0);

        Assert.Single(canceled.Events.OfType<WhenRevealedCanceled>());
        Assert.Empty(world.Agenda.Outstanding);
        // Engine choice: a subsequent uncanceled entry rejects before recording resolution work.
        var occurrence = new Occurrence(9, [Steps.CardRevealed], Subject: source.ObjectId, Player: 0);
        string receiptBefore = JsonSerializer.Serialize(occurrence);
        string before = world.Digest().Canonical();
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            runner.ExecuteReveal(world, source, 0, occurrence));
        Assert.Contains("composed", error.Message);
        Assert.Equal(receiptBefore, JsonSerializer.Serialize(occurrence));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static (AbilityRunner Runner, World World, Card Source) Board(
        string path, string effect, string? form = null, bool minion = false)
    {
        string face = minion ? "01181" : path == "activation" ? "01114" : "01105";
        string trigger = path == "activation"
            ? """{"event":"WhenActivationCompleted","timing":"ForcedResponse","subject":"this"}"""
            : """{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"FORM}"""
                .Replace("FORM", form is null ? "" : $",\"form\":\"{form}\"", StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse($$"""
            {"cards":[{"card":"{{face}}","abilities":[{"trigger":{{trigger}},"effect":{{effect}}}]}]}
            """));
        var world = new World(Cards, 1, 883);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        var source = world.CreateCard(face, minion
            ? world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))
            : world.AreaOf(path == "activation" ? DeckType.VillainArea : DeckType.RevealingArea));
        world.Abilities = runner;
        return (runner, world, source);
    }
}
