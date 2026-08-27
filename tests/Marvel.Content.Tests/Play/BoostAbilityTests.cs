using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// "Boost" abilities — <c>rr:boost-boost-icon.2</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>419 cards in the pool have one and every one of them was ignored.</b>
/// Step 2b of both activations is "resolve any <b>Boost</b> abilities,
/// indicated by the star icon in the boost area", and the engine went straight
/// from turning the card faceup to counting its icons.
/// </para>
/// <para>
/// <b>Why it stayed hidden.</b> The printed <c>Boost</c> attribute counts icons,
/// and <c>rr:boost-boost-icon.1</c> makes a star icon "not a boost icon" that
/// adds nothing — so a card with an ability and a card with none carry the same
/// number. The engine had no way to tell them apart, and a comment in
/// <c>Attack.FlipBoostCards</c> said exactly that. The star survives only in the
/// text box, which is where <c>HasBoostAbility</c> reads it.
/// </para>
/// </remarks>
public sealed class BoostAbilityTests
{
    /// <summary>"Sonic Boom" — a treachery with a Boost ability.</summary>
    private const string SonicBoom = "01123";

    /// <summary>"Advance" — a treachery with none.</summary>
    private const string Advance = "01186";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:boost-boost-icon.1")]
    [Fact]
    public void TheStarIsInTheTextBoxAndNowhereElse()
    {
        // The measurement the fix rests on. A card that prints a Boost ability
        // and one that does not carry the same `Boost` number, so the number
        // cannot be the test -- and both of these print 2.
        Assert.Equal(
            Cards.PrintedValue(Advance, "Boost", 1),
            Cards.PrintedValue(SonicBoom, "Boost", 1));

        Assert.True(Cards.HasBoostAbility(SonicBoom));
        Assert.False(Cards.HasBoostAbility(Advance));
    }

    [Rule("rr:boost-boost-icon.2")]
    [Fact]
    public void ABoostAbilityNobodyHasWrittenThrowsRatherThanBeingSkipped()
    {
        // The failure this exists for. An unwritten boost ability that resolves
        // to silence is a villain who schemed for two instead of two and an
        // exhausted hero, and nothing anywhere says so.
        var world = Deal();
        const string unauthoredBoost = "02007";
        var card = world.CreateCard(unauthoredBoost, world.AreaOf(DeckType.BoostingArea));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => AuthoredCards.Runner().Boost(world, card, 0));

        Assert.Contains("no ability data", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(unauthoredBoost, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACardWithoutOneResolvesInSilence()
    {
        // Most boost cards are only icons, and asking about a card that has no
        // ability must stay free -- it happens twice a round.
        var world = Deal();
        var card = world.CreateCard(Advance, world.AreaOf(DeckType.BoostingArea));

        Assert.Empty(AuthoredCards.Runner().Boost(world, card, 0));
    }

    [Rule("rr:boost-boost-icon.2")]
    [Fact]
    public void AWrittenBoostAbilityRuns()
    {
        // Authored inline rather than in the dataset, because the card this
        // uses needs a shape the interpreter has not got yet -- "if this
        // activation deals damage to a character, stun that character". What is
        // under test is the tier and the wiring, not the card.
        var book = AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01123","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"giveStatus":{"card":"you","status":"stunned"}}}]}]}
            """);

        var world = Deal();
        var card = world.CreateCard(SonicBoom, world.AreaOf(DeckType.BoostingArea));

        new AbilityRunner(book).Boost(world, card, 0);

        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

    [Fact]
    public void HalfAWrittenCardIsNotAWrittenBoostAbility()
    {
        // The guard asks whether **this half** is written, not whether the card
        // is. `01168` Sweeping Swoop is the card it was tightened for: it says
        // one thing when revealed and another as a boost card, and a card
        // authored for the first would otherwise pass here and go back to being
        // silent for the second.
        var book = AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01123","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"giveStatus":{"card":"you","status":"confused"}}}]}]}
            """);

        var world = Deal();
        var card = world.CreateCard(SonicBoom, world.AreaOf(DeckType.BoostingArea));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => new AbilityRunner(book).Boost(world, card, 0));

        Assert.Contains("'Boost' ability", thrown.Message, StringComparison.Ordinal);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
    }

    [Fact]
    public void ItIsTheBoostTierAndNotTheRevealOne()
    {
        // A card may carry both, and matching on the condition alone would run
        // the wrong one. "When Revealed" and "Boost" are two abilities on one
        // card in `01168` Sweeping Swoop, and they say different things.
        var book = AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01123","abilities":[
              {"trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
               "effect":{"giveStatus":{"card":"you","status":"stunned"}}},
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"giveStatus":{"card":"you","status":"confused"}}}]}]}
            """);

        var world = Deal();
        var card = world.CreateCard(SonicBoom, world.AreaOf(DeckType.BoostingArea));
        var identity = world.Seats[0].IdentityCard;

        new AbilityRunner(book).Boost(world, card, 0);

        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
        Assert.False(Statuses.Has(world, identity, Statuses.Confused));
    }

    private static World Deal() => WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
        ["Spider-Man"],
        12345);
}
