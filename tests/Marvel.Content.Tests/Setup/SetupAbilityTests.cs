using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>
/// The deal running card text — <c>rr:appendix-ii-setup.step.12</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Resolve Scenario Setup and When Revealed Abilities" is three sub-steps in a
/// stated order, and the order is the whole of the rule: 12a resolves the main
/// scheme's "Setup" abilities, 12b then flips the card to 1B and resolves what
/// is on <i>that</i> side, and 12c does the villain. So the A side is showing
/// while its own ability runs, and a dealer that flipped first would be asking
/// a card about a face it no longer has.
/// </para>
/// <para>
/// <c>WorldSetup</c> flipped at its own step 4, before the villain entered play
/// and long before anything could read side 1A. Nothing turned on it while no
/// setup ability ran at all — MARVEL-242 pinned it as a divergence rather than
/// a bug for exactly that reason — and running one is what makes it a bug.
/// </para>
/// </remarks>
public sealed class SetupAbilityTests
{
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:appendix-ii-setup.step.12.a")]
    [Rule("rr:appendix-ii-setup.step.12.b")]
    [Fact]
    public void TheMainSchemesSetupAbilityRunsWhileItsASideIsStillShowing()
    {
        // The ordering, made observable. The stub's Setup ability places a
        // threat on whatever card it is printed on, and it is printed on the A
        // side — so a deal that flipped first would resolve nothing, because
        // `book.On` is asked about the *showing* face.
        //
        // `01097a` is the real card behind the shape: its printed text is
        // "Setup: Advance to stage 1B", so a card that reads its own side at
        // setup is not hypothetical.
        var world = Deal("rhino", Book(
            "01097a",
            """{ "placeThreat": { "scheme": "this", "amount": 3 } }""",
            "Setup"));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;

        Assert.Equal(3, scheme.Tokens.GetValueOrDefault("k_threat"));

        // And it is on 1B by the time the deal is done — 12a does not stop 12b.
        Assert.Equal(scheme.Faces[^1], scheme.FaceId);
    }

    [Rule("rr:appendix-ii-setup.step.12.b")]
    [Fact]
    public void TheMainSchemesWhenRevealedIsAskedOfTheBSideAndNotTheA()
    {
        // The other half of 12b: the flip comes first and the "When Revealed"
        // is resolved on the side that is now showing. A stub on the A side
        // must not fire, because by the time When Revealed abilities are asked
        // for, that face is gone.
        var world = Deal("rhino", Book(
            "01097a",
            """{ "placeThreat": { "scheme": "this", "amount": 3 } }""",
            "WhenRevealed",
            "\"event\": \"WhenCardRevealed\","));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;

        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:appendix-ii-setup.step.12.c")]
    [Fact]
    public void TheExpertVillainsWhenRevealedResolvesDuringTheDeal()
    {
        // Not a stub. `rr:appendix-ii-setup.step.12.c` — "resolve any 'Setup'
        // and 'When Revealed' abilities on the villain" — and the expert Rhino
        // deck opens on stage II, whose text is "**When Revealed:** Search the
        // encounter deck and discard pile for the Breakin' & Takin' side scheme
        // and reveal it."
        //
        // So the expert scenario is supposed to begin with that side scheme
        // already on the table, and until step 12 ran it did not: the board was
        // materially the wrong board, and every expert game in the suite was
        // played on it.
        var world = Deal("rhino_expert", AuthoredCards.Runner());

        Assert.Equal(AuthoredCards.RhinoTwo, world.TheCardIn(DeckType.VillainArea)!.FaceId);

        var scheme = Assert.Single(
            world.Cards, card => card.FaceId == AuthoredCards.BreakinAndTakin);

        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Rule("rr:appendix-ii-setup.step.12.c")]
    [Fact]
    public void TheStandardVillainHasNoSuchTextAndTheBoardIsUnchanged()
    {
        // The control. Rhino I is authored and prints nothing at all, so 12c
        // finds nothing — which is what makes the expert board above a
        // difference in the *card* rather than in the step.
        var world = Deal("rhino", AuthoredCards.Runner());

        Assert.Equal("01094", world.TheCardIn(DeckType.VillainArea)!.FaceId);
        Assert.DoesNotContain(
            world.Cards, card => card.FaceId == AuthoredCards.BreakinAndTakin
                && DeckTypes.IsInPlay(card.Area.Type));
    }

    [Rule("rr:setup-triggered-ability.1")]
    [Fact]
    public void ASetupAbilityIsResolvedRatherThanOffered()
    {
        // "Setup abilities are mandatory." There is nobody to ask during setup
        // — no game has begun and no player has priority — so the events come
        // back from the deal rather than through a prompt.
        var events = new List<GameEvent>();
        Deal(
            "rhino",
            Book("01097a", """{ "placeThreat": { "scheme": "this", "amount": 3 } }""", "Setup"),
            events);

        Assert.Contains(events, happened => happened is FieldSet { Field: "k_threat" });
    }

    [Fact]
    public void AChallengeStopsBeforeItsUnimplementedSetupCanDealAPlausibleBoard()
    {
        // The Ground is Lava modifies setup and play. Leaving its Challenge
        // card inert in RemovedArea would let the scenario continue as a
        // different game, so the product boundary refuses before setup begins.
        var refused = Assert.Throws<RulesNotImplementedException>(
            () => Deal("2401_the_ground_is_lava", AuthoredCards.Runner()));

        Assert.Contains("Challenge", refused.Message, StringComparison.Ordinal);
        Assert.Contains("setup and rules modifiers", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASetupAbilityCarryingATriggeringConditionIsRefused()
    {
        // `rr:setup-triggered-ability.2` times these to a step of setup rather
        // than to something happening in the game, so there is no occurrence to
        // name and no condition in `Steps.EveryCondition` that names one.
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01097a", "abilities": [ {
                "trigger": { "event": "WhenCardRevealed", "timing": "Setup" },
                "effect": { "placeThreat": { "scheme": "this", "amount": 1 } }
            } ] } ] }
            """));

        Assert.Contains("is 'Setup' and triggers on", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>One card, one ability, for holding a step against its order.</summary>
    /// <remarks>
    /// The scheme's other face and the villain are authored-and-silent, because
    /// step 12 asks all three and a card nobody has read stops the deal — which
    /// is the point of the test two along from here.
    /// </remarks>
    private static AbilityRunner Book(
        string card, string effect, string timing, string when = "") =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [
                { "card": "{{card}}", "abilities": [ {
                    "trigger": { {{when}} "timing": "{{timing}}" },
                    "effect": {{effect}}
                } ] },
                { "card": "01097b", "abilities": [] },
                { "card": "01094", "abilities": [] }
            ] }
            """));

    private static World Deal(
        string campaign, ICardAbilities abilities, List<GameEvent>? events = null) =>
        WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed,
            abilities,
            events);
}
