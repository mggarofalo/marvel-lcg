using System.Text.Json;
using Marvel.Content;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

// Projection fixtures isolate authorization; these are not native gameplay evidence.
public sealed class PublicPendingSituationTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    public void PublicContextOmitsPrivateAndFacedownCausesEvenWhenTheViewerCanReadThem(int viewer, bool handFaceUp)
    {
        World world = Board();
        Card publicCard = world.CreateCard("01108", world.AreaOf(DeckType.RevealingArea));
        Card hand = world.CreateCard("01005", world.Seats[0].Hand);
        // Synthetic visibility fixture: a face flag cannot grant a private
        // hand card public audience, even when its owner can read that face.
        if (handFaceUp) hand.TurnFaceUp();
        Card facedown = world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck));
        Prompt prompt = Choice([publicCard.ObjectId, hand.ObjectId, facedown.ObjectId]);
        ViewScope scope = Scope(world, viewer);

        VisibleResult visible = WorldProjection.For(world, prompt, [], scope);

        Assert.Equal(0, visible.World.Table!.PromptOwner);
        Assert.Equal(PublicDecisionKind.Choice, visible.World.Table.PendingSituation!.Kind);
        Assert.Equal([publicCard.ObjectId], visible.World.Table.PendingSituation.SourceCardIds);
        Assert.Contains("Resolving", PendingSituationPresentation.Context(visible.World));
        Assert.DoesNotContain("Swinging Web Kick", PendingSituationPresentation.Context(visible.World));
        if (viewer != 0) Assert.Null(visible.Prompt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public void PrivateOptionsCostsAndSearchExposureCannotChangeSerializedPublicSituation(int viewer)
    {
        World world = Board();
        Card secret = world.CreateCard("01005", world.Seats[0].Hand);
        Prompt first = Choice([secret.ObjectId]);
        Prompt changed = first with
        {
            Label = "secret label", DisplayQuestion = "private question",
            Description = "private resolution", Cancellable = true,
            ExposesConcealedCandidates = true,
            Affordances = [new Affordance(secret.ObjectId, "Choose", secret.ObjectId, 0, "private card",
                new TargetRequest([secret.ObjectId], 1, 1), [new CostOption(secret.ObjectId, "3")])
                { CostDescription = "Private payment" }],
        };

        string before = JsonSerializer.Serialize(WorldProjection.For(world, first, [], Scope(world, viewer)).World.Table);
        string after = JsonSerializer.Serialize(WorldProjection.For(world, changed, [], Scope(world, viewer)).World.Table);

        Assert.Equal(before, after);
        Assert.DoesNotContain("private question", after, StringComparison.Ordinal);
        Assert.DoesNotContain(secret.ObjectId.ToString(), JsonSerializer.Serialize(
            WorldProjection.For(world, changed, [], Scope(world, viewer)).World.Table!.PendingSituation));
    }

    [Fact]
    public void PrimaryOwnerSurvivesASeparateAuthorizedOffTurnMenuWithoutLeakingDefenderMembership()
    {
        World world = Board();
        Card defender = world.Seats[0].IdentityCard;
        Prompt primary = Choice([defender.ObjectId]) with
        {
            PublicKind = PublicDecisionKind.Defense,
            Affordances = [new Affordance(1, Marvel.Rules.Play.Attack.DefenseVerb, defender.ObjectId, 0, "Defend")],
        };
        Prompt scoped = Choice([]) with { Player = 1 };

        VisibleResult other = WorldProjection.For(world, scoped, [], Scope(world, 1), activePlayer: 0, publicPrompt: primary);

        Assert.Equal(1, other.Prompt!.Player);
        Assert.Equal(0, other.World.Table!.PromptOwner);
        Assert.Equal(PublicDecisionKind.Defense, other.World.Table.PendingSituation!.Kind);
        Assert.All(other.World.PlayerSummaries, summary => Assert.Empty(summary.OfferedDefenders));
        Assert.Contains("Spider-Man", PendingSituationPresentation.Heading(other.World));
        VisibleResult completed = WorldProjection.For(world, null, [], Scope(world, 1));
        Assert.Null(completed.World.Table!.PromptOwner);
        Assert.Null(completed.World.Table.PendingSituation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public void ActivationCauseHasItsOwnRoleAndCannotExposeAnotherSeatsHand(int viewer)
    {
        World world = Board();
        Card assault = world.CreateCard("01187", world.AreaOf(DeckType.RevealingArea));
        Card privateSource = world.CreateCard("01005", world.Seats[1].Hand);
        privateSource.TurnFaceUp();
        Prompt prompt = Choice([world.Seats[0].IdentityCard.ObjectId]) with
        { CauseCardIds = [assault.ObjectId, privateSource.ObjectId], Description = "Enemy attack" };
        VisibleResult visible = WorldProjection.For(world, prompt, [], Scope(world, viewer));
        Assert.Equal([assault.ObjectId], visible.World.Table!.PendingSituation!.CauseCardIds);
        Assert.Contains("Cause: Assault", PendingSituationPresentation.Context(visible.World));
        Assert.DoesNotContain("Swinging Web Kick", PendingSituationPresentation.Context(visible.World));
        if (viewer == 0)
        {
            Assert.Equal([assault.ObjectId], visible.Prompt!.CauseCardIds);
            PromptPresentation presentation = PromptPresentation.From(visible.Prompt, visible.World);
            Assert.Contains("Cause: Assault", presentation.Resolution);
            Assert.Contains("Resolving Spider-Man", presentation.Context);
            Assert.DoesNotContain("Swinging Web Kick", presentation.Resolution);
        }
    }

    [Fact]
    public void MinionOrderingExplainsAChoiceWithoutClaimingTheFirstMinionIsResolving()
    {
        World world = Board();
        Card minion = world.CreateCard("01107", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Prompt prompt = Choice([minion.ObjectId]) with { PublicKind = PublicDecisionKind.MinionActivationOrder };
        VisibleResult visible = WorldProjection.For(world, prompt, [], Scope(world, 0));
        Assert.Contains("Choosing minion activation order", PromptPresentation.From(visible.Prompt!, visible.World).Context);
        Assert.DoesNotContain("Resolving", PendingSituationPresentation.Context(visible.World));
    }

    private static Prompt Choice(IReadOnlyList<int> causes) => new(
        0, Question.Element, TimingPriority.Untimed, "choice", "choice", false, [])
    { ContextCardIds = causes };

    private static ViewScope Scope(World world, int viewer) => viewer < 0 ? ViewScope.None
        : new RestrictedVisibilityPolicy(viewer).Authorize(null, world.Players);

    private static World Board()
    {
        var world = new World(Cards, players: 2);
        Seat zero = world.CreateSeat("Spider-Man");
        Seat one = world.CreateSeat("Captain Marvel");
        zero.IdentityCard = world.CreateCard("01001a", zero.Hero);
        one.IdentityCard = world.CreateCard("01010a", one.Hero);
        return world;
    }
}
