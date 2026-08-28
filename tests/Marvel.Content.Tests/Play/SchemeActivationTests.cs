using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// A scheme activation, step by step — <c>rr:scheme-enemy-activation</c>.
/// </summary>
/// <remarks>
/// <para>
/// The rule lists three steps and the engine resolved all three in one call.
/// That is fine until step 2 stops to ask: a <b>Boost</b> ability offering the
/// player a choice suspends, and the threat went onto the main scheme while the
/// question was still on the table — so whatever they chose arrived after the
/// number it was meant to change.
/// </para>
/// <para>
/// The attack activation has the same shape. <c>Steps.FlipBoostCards</c> is
/// step 3 and <c>Steps.CalculateAttackDamage</c> is step 4, so a boost card's
/// question is answered between them. This is the same split one activation
/// over.
/// </para>
/// </remarks>
public sealed class SchemeActivationTests
{
    private const string Campaign = "unus";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:scheme-enemy-activation.step.3")]
    [Fact]
    public void TheThreatIsPlacedInAStepOfItsOwn()
    {
        // The claim, in the plainest form the agenda can make it: resolving the
        // scheme leaves something outstanding, and that something is step 3.
        var (world, runner, unus) = Board();
        var events = new List<GameEvent>();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Index: 0, Subject: unus.ObjectId, Seat: 0));
        VillainPhase.Take(world, Cards, runner, world.Agenda.Current!.Value, events);

        Assert.Contains(
            world.Agenda.Outstanding, step => step.What == Steps.SchemeThreat);
        Assert.Equal(before, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:scheme-enemy-activation.step.2")]
    [Rule("rr:scheme-enemy-activation.step.2.b")]
    [Fact]
    public void ABoostCardsQuestionIsAnsweredBeforeTheThreatIsPlaced()
    {
        // Step 2b says to "resolve any 'Boost' abilities" before step 3 reads
        // the modified SCH. A boost ability that *asks* is the case that can tell the
        // two apart: resolved inline, the player's answer landed after the
        // threat had already gone on.
        //
        // Hand-written, because the card that needs it is the next one in and
        // the ordering is a property of the step rather than of any card.
        var (world, _, unus) = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        var deck = world.AreaOf(DeckType.EncounterDeck);
        var top = deck.Cards[^1];
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{top.FaceId}}", "abilities": [ {
                "trigger": { "event": "WhenCardRevealed", "timing": "Boost", "subject": "this" },
                "effect": { "choose": { "options": [
                    { "grantUntil": { "card": { "query": "villain" }, "keyword": "scheme",
                                      "amount": 2, "until": "EndOfActivation" } },
                    { "gainSurge": 1 } ] } } }
            ] } ] }
            """));

        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Index: 0, Subject: unus.ObjectId, Seat: 0));

        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, runner, events);

        // The question is put, and nothing has been placed to answer it after.
        Assert.NotNull(asked);
        Assert.Equal(Question.Option, asked.Asking);
        Assert.Equal(before, main.Tokens.GetValueOrDefault("k_threat"));

        Sequence.Answer(world, Cards, runner, asked, Decision.Take(0), events);
        Agendas.Finish(world, Cards, runner);

        // Unus's first stage prints SCH 1, and the option the player took is
        // worth two more.
        Assert.Equal(3, main.Tokens.GetValueOrDefault("k_threat") - before);
    }

    [Rule("rr:scheme-enemy-activation.step.2")]
    [Fact]
    public void TheBoostIconsSurviveTheStepBoundaryAsAModifier()
    {
        // ".c: increase the scheming enemy's SCH value by one for each boost
        // icon on the card" — a modifier, and registered as one. A local number
        // added at the end of the call would not survive the step boundary that
        // a suspending choice needs.
        var (world, runner, unus) = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        // `01186` Advance prints no boost icon and `01101` Hydra Mercenary
        // prints one, so the difference between them is exactly the icon.
        var deck = world.AreaOf(DeckType.EncounterDeck);
        World.MoveToTop(world.CreateCard("01101", deck), deck);

        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Index: 0, Subject: unus.ObjectId, Seat: 0));
        Agendas.Finish(world, Cards, runner);

        Assert.Empty(events);
        Assert.Equal(2, main.Tokens.GetValueOrDefault("k_threat") - before);
    }

    [Rule("rr:activation.6")]
    [Fact]
    public void TheActivationEndsAtStepThreeAndNotBeforeIt()
    {
        // A modifier bounded by "this activation" has to survive until the
        // number it modifies has been read. Expiring it at the end of step 2
        // would make it worth nothing at all.
        var (world, runner, unus) = Board();
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(
            Marvel.Rules.Timing.EffectSource.LastingEffect,
            Kind: "scheme",
            Amount: 5,
            Card: unus.ObjectId,
            Affects: unus.ObjectId,
            Lasts: Marvel.Rules.Timing.Duration.UntilEndOf(
                Marvel.Rules.Timing.TimingPoints.EndOfActivation)));

        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Index: 0, Subject: unus.ObjectId, Seat: 0));
        Agendas.Finish(world, Cards, runner);

        Assert.Equal(6, main.Tokens.GetValueOrDefault("k_threat") - before);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Kind == "scheme");
    }

    private static (World World, AbilityRunner Runner, Card Unus) Board()
    {
        var runner = AuthoredCards.Runner();
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed,
            runner);

        return (world, runner, world.TheCardIn(DeckType.VillainArea)!);
    }
}
