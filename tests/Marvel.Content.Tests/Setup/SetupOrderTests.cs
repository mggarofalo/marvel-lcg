using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>
/// A dealt board, held against Appendix II step by step.
/// </summary>
/// <remarks>
/// <para>
/// <c>WorldSetup.Deal</c> has seven numbered steps and the Rules Reference has
/// sixteen, and until MARVEL-241 the sixteen were not in this repository — the
/// snapshot stopped at the glossary and Appendix II is page 51. So the engine's
/// setup had never been held against the thing it implements.
/// </para>
/// <para>
/// <b>What is unimplemented is asserted as unimplemented, by name.</b> Not all
/// of Appendix II is written yet, and a test that only checked the part that is
/// would let the rest be forgotten. Such a test fails when somebody implements
/// the step, which is the point: it tells them it is here. Step 12 was one of
/// these and is now <c>SetupAbilityTests</c>; step 11 still is.
/// </para>
/// </remarks>
public sealed class SetupOrderTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:appendix-ii-setup.step.1")]
    [Fact]
    public void EachIdentityIsInPlayWithItsAlterEgoSideFaceUp()
    {
        // "Select Identities. Each player selects one identity, placing their
        // **alter-ego side face up**."
        var world = Deal("spider_man", "she_hulk");

        foreach (int seat in world.PlayerOrder)
        {
            var identity = world.Seats[seat].IdentityCard;
            Assert.Equal(DeckType.HeroArea, identity.Area.Type);
            Assert.Equal(CardKind.AlterEgo, Cards.Kind(identity.FaceId));
        }
    }

    [Rule("rr:appendix-ii-setup.step.10")]
    [Fact]
    public void TheObligationsAreInTheEncounterDeckAndNotSetAside()
    {
        // "Create the Encounter Deck. Shuffle the encounter sets listed on side
        // 1A of the main scheme card **with the obligation cards set aside
        // during setup step four** to create the encounter deck."
        //
        // Set aside at step 4 and shuffled in at step 10, so a dealt board has
        // them in the deck: being set aside is a stage of the deal rather than
        // where they end up.
        var world = Deal("spider_man", "she_hulk");
        var obligations = world.Cards
            .Where(card => Cards.Kind(card.FaceId) == CardKind.Obligation)
            .ToList();

        Assert.NotEmpty(obligations);
        Assert.All(obligations, card => Assert.Equal(DeckType.EncounterDeck, card.Area.Type));
    }

    [Rule("rr:appendix-ii-setup.step.12.b")]
    [Fact]
    public void TheMainSchemeShowsItsBSide()
    {
        // "Flip the main scheme card to side 1B and resolve any 'When Revealed'
        // abilities on that side." The one card on an opening board whose
        // showing face is not the first face of its spec.
        var world = Deal("spider_man");
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;

        Assert.Equal(scheme.Faces[^1], scheme.FaceId);
    }

    [Rule("rr:appendix-ii-setup.step.14")]
    [Fact]
    public void EachPlayerHoldsTheirHandSize()
    {
        // "Draw Cards. Each player draws cards from their deck until they have
        // cards equal in number to their hand size (including modifiers), as
        // listed near the bottom of their identity card."
        var world = Deal("spider_man", "she_hulk");

        foreach (int seat in world.PlayerOrder)
        {
            var player = world.Seats[seat];
            Assert.Equal(
                Cards.PrintedValue(player.IdentityCard.FaceId, "HS", world.Players),
                player.Hand.Cards.Count);
        }
    }

    [Rule("rr:appendix-ii-setup.step.11")]
    [Fact]
    public void PutSetupCardsIntoPlayIsNotImplemented()
    {
        // "Search each deck and the set aside area for any cards with the setup
        // keyword and put them into play." **This step does not run** --
        // MARVEL-211 -- so a setup card is still in the pile it was set aside
        // in when the deal returns.
        //
        // Asserted rather than left silent, because a step that does not happen
        // and a step that happens and finds nothing look identical on the Rhino
        // board, which has no setup card at all. So this deals a board that has
        // one.
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(
                Dealer.DealOrder(Setup, "2401_the_ground_is_lava", ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed);

        var flight = Assert.Single(world.Cards, card => card.FaceId == "40151");

        Assert.Equal(DeckType.AsideDeck, flight.Area.Type);
        Assert.True(
            Cards.PrintedValue(flight.FaceId, "Setup", world.Players) > 0,
            "the card this is about must actually carry the keyword");
    }

    [Rule("rr:appendix-ii-setup.step.12.a")]
    [Fact]
    public void TheMainSchemeIsFlippedByStep12bAndNotBefore()
    {
        // The divergence the vendoring found, now closed. Appendix II flips the
        // main scheme at step **12b**, and step **12a** -- "resolve any 'Setup'
        // abilities on main scheme card 1A" -- comes first, while the A side is
        // still showing. `WorldSetup` used to flip at its own step 4, before the
        // villain entered play and long before anything could read the A side.
        //
        // Nothing turned on it while no setup ability ran at all, which is why
        // MARVEL-242 recorded it as a divergence rather than a bug. Running one
        // is what makes it a bug, and `SetupAbilityTests` is where the order is
        // held against a card that reads its own side. What is left here is the
        // end state both orders agree on.
        var world = Deal("spider_man");
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;

        Assert.Equal(scheme.Faces[^1], scheme.FaceId);
        Assert.NotEqual(scheme.Faces[0], scheme.FaceId);
    }

    private static World Deal(params string[] heroes) => WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, Campaign, heroes), Cards),
        [.. heroes.Select(hero => Setup.Hero(hero).Name)],
        Seed);
}
