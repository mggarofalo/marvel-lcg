using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class WakandaDamageMeaningTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SelectedClawsKeepsItsCurrentAmountAndSequenceStep(bool finalStep, bool restricted)
    {
        var (world, runner, claws, target) = Board();
        runner.ResolveSpecial(world, claws, 0, finalStep);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        string before = world.Digest().Fingerprint();
        string summary = SelectedSummary(world, prompt, target, restricted);

        Assert.Contains($"Current attack damage: {(finalStep ? 4 : 2)} before prevention and replacement", summary);
        Assert.Equal(finalStep, summary.Contains("Final step", StringComparison.Ordinal));
        Assert.Contains("2/4 → 0/4 HP", summary);
        Assert.Contains("would be defeated", summary);
        Assert.Contains("Later effects can change the result", summary);
        Assert.Equal(before, world.Digest().Fingerprint());
        Assert.Equal(2, target.Damage);
        Assert.Null(WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(1).Authorize(null, 2)).Prompt);

        var events = new List<GameEvent>();
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(target.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(DeckType.EncounterDiscardPile, target.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DamageAmountDoesNotPromiseDamageThroughStatusReplacement(bool stunned)
    {
        var (world, runner, claws, target) = Board();
        Card affected = stunned ? world.Seats[0].IdentityCard : target;
        string status = stunned ? Statuses.Stunned : Statuses.Tough;
        Statuses.Give(world, affected, status);
        runner.ResolveSpecial(world, claws, 0, finalStep: true);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        string before = world.Digest().Fingerprint();
        string summary = SelectedSummary(world, prompt, target, restricted: true);

        Assert.Contains("Final step", summary);
        Assert.DoesNotContain("would be defeated", summary);
        if (stunned)
        {
            Assert.Contains("Stunned cancels this attack; no damage will be dealt", summary);
            Assert.DoesNotContain("Current attack damage", summary);
        }
        else
        {
            Assert.Contains("Current attack damage: 4 before prevention and replacement", summary);
            Assert.Contains("Tough prevents the damage and is discarded", summary);
            Assert.Contains("2/4 → 2/4 HP", summary);
        }
        Assert.Equal(before, world.Digest().Fingerprint());
        Assert.True(Statuses.Has(world, affected, status));

        var events = new List<GameEvent>();
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(target.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(2, target.Damage);
        Assert.False(Statuses.Has(world, affected, status));
    }

    [Fact]
    public void LastRemainingClawsAutomaticallyEntersTargetChoiceWithItsFinalStepMeaning()
    {
        // Daggers first leaves this enemy at two HP before the final Claws choice.
        var (world, runner, claws, target) = Board(targetDamage: 1);
        Card daggers = world.CreateCard("01046", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Card wakanda = world.CreateCard("01043a", world.Seats[0].Hand);
        Card payment = world.CreateCard("01044", world.Seats[0].Hand);
        runner.Act(world, new PendingAbility(wakanda.ObjectId, AbilityType.Action, 0), [payment.ObjectId], []);
        Prompt next = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(PublicDecisionKind.SpecialAbilityNext, next.PublicKind);
        Sequence.Answer(world, Cards, runner, next, Decision.Take(daggers.ObjectId), []);
        Prompt player = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, player, Decision.Take(world.Seats[0].IdentityCard.ObjectId), []);
        Prompt clawsTarget = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));

        Assert.NotEqual(PublicDecisionKind.SpecialAbilityNext, clawsTarget.PublicKind);
        Assert.Contains(claws.ObjectId, clawsTarget.ContextCardIds);
        string summary = SelectedSummary(world, clawsTarget, target, restricted: true);
        Assert.Contains("Final step", summary);
        Assert.Contains("Current attack damage: 4 before prevention and replacement", summary);
        Assert.Contains("2/4 → 0/4 HP", summary);
    }

    [Theory]
    [InlineData("01046", false, "Deal 1 damage to the villain and 1 to each enemy")]
    [InlineData("01046", true, "Deal 2 damage to the villain and 2 to each enemy")]
    [InlineData("01048", false, "Current thwart amount: 1 threat")]
    [InlineData("01048", true, "Current thwart amount: 2 threat")]
    [InlineData("01049", false, "Move 1 damage from Black Panther")]
    [InlineData("01049", true, "Move 2 damage from Black Panther")]
    public void OtherSelectableSpecialsRetainTheirStepAndAmount(string face, bool finalStep, string expected)
    {
        var (world, runner, _, enemy) = Board();
        world.Seats[0].IdentityCard.TakeDamage(2);
        Card source = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Card scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 1);
        Card target = face switch
        {
            "01046" => world.Seats[0].IdentityCard,
            "01048" => scheme,
            _ => enemy,
        };
        runner.ResolveSpecial(world, source, 0, finalStep);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        string before = world.Digest().Fingerprint();
        string summary = SelectedSummary(world, prompt, target, restricted: true, sourceTitle: Cards.Title(face));
        Assert.Contains(expected, summary);
        Assert.Equal(finalStep, summary.Contains("Final step", StringComparison.Ordinal));
        Assert.Equal(before, world.Digest().Fingerprint());
    }

    [Fact]
    public void FinalTacticalGeniusExplainsConfusedInsteadOfPromisingThreatRemoval()
    {
        var (world, runner, _, _) = Board();
        Card source = world.CreateCard("01048", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Card scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 1);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        runner.ResolveSpecial(world, source, 0, finalStep: true);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        string summary = SelectedSummary(world, prompt, scheme, restricted: true, sourceTitle: "Tactical Genius");
        Assert.Contains("Final step", summary);
        Assert.Contains("Confused cancels this thwart; no threat will be removed", summary);
        Assert.DoesNotContain("Current thwart amount", summary);
        var events = new List<GameEvent>();
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(scheme.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(1, scheme.Tokens["k_threat"]);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
    }

    private static string SelectedSummary(World world, Prompt prompt, Card target, bool restricted, string sourceTitle = "Panther Claws")
    {
        IVisibilityPolicy policy = restricted ? new RestrictedVisibilityPolicy(0) : new PermissiveVisibilityPolicy();
        VisibleResult visible = WorldProjection.For(world, prompt, [], policy.Authorize(null, 2));
        PromptPresentation presentation = PromptPresentation.From(visible.Prompt!, visible.World);
        var composer = new DecisionComposer(visible.Prompt!);
        var offer = Assert.Single(presentation.Affordances, value => value.Id == target.ObjectId);
        composer.SelectAffordance(offer.Id);
        Assert.True(composer.Progress().IsReady);
        Assert.Contains(sourceTitle, presentation.Heading);
        Assert.False(string.IsNullOrWhiteSpace(offer.CommitLabel));
        var hidden = Assert.Single(Assert.Single(visible.World.Areas,
            area => area.Id == world.Seats[1].Hand.Id).Cards);
        if (restricted) Assert.Null(hidden.Face);
        return Assert.IsType<string>(TableDraftSummary.From(composer, presentation, visible.World, compact: true));
    }

    private static (World World, AbilityRunner Runner, Card Claws, Card Target) Board(long targetDamage = 2)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(Cards, players: 2) { Abilities = runner };
        Seat seat = world.CreateSeat("Black Panther");
        seat.IdentityCard = world.CreateCard("01040a", seat.Hero);
        Seat other = world.CreateSeat("Spider-Man");
        other.IdentityCard = world.CreateCard("01001a", other.Hero);
        world.CreateCard("01002", other.Hand);
        world.CreateCard("01088", seat.Deck);
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        Card claws = world.CreateCard("01047", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Card target = world.CreateCard("01143", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        target.TakeDamage(targetDamage);
        return (world, runner, claws, target);
    }
}
