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
/// sixteen, and until the original investigation the sixteen were not in this repository — the
/// snapshot stopped at the glossary and Appendix II is page 51. So the engine's
/// setup had never been held against the thing it implements.
/// </para>
/// <para>
/// <b>What is unimplemented is asserted as unimplemented, by name.</b> Not all
/// of Appendix II is written yet, and a test that only checked the part that is
/// would let the rest be forgotten. Such a test fails when somebody implements
/// the step, which is the point: it tells them it is here. Steps 11 and 12 were
/// both of these, and are now <c>SetupCardTests</c> and
/// <c>SetupAbilityTests</c>.
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

    [Rule("rr:main-scheme-main-scheme-deck.step.2")]
    [Fact]
    public void AThreeStageMainSchemeDeckHasStageTwoOnTop()
    {
        // The authored setup row lists stages one, two, then three. Areas are
        // bottom-first, so the waiting cards are stored stage three then stage
        // two and the next rules-driven advance takes stage two from the top.
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(
                Dealer.DealOrder(Setup, "ultron", ["spider_man"]), Cards),
            [Setup.Hero("spider_man").Name],
            Seed);

        Assert.Equal(
            ["01139a", "01138a"],
            world.AreaOf(DeckType.MainSchemesDeck).Cards.Select(card => card.FaceId));
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

    [Rule("rr:appendix-ii-setup.step.12.a")]
    [Fact]
    public void TheMainSchemeIsFlippedByStep12bAndNotBefore()
    {
        // Appendix II flips the main scheme at step **12b**, and step **12a**
        // -- "resolve any 'Setup' abilities on main scheme card 1A" -- comes
        // first, while the A side is still showing. A setup ability that reads
        // its own side therefore has to run before the flip, or it reads the
        // wrong face.
        //
        // `SetupAbilityTests` holds the order against a card that does exactly
        // that. What is left here is the
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
