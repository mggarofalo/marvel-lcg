using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests;

/// <summary>
/// Traits, and how a printed word becomes a digest key.
/// </summary>
/// <remarks>
/// <para>
/// Every <c>t_</c> field in the state digest is keyed from a card's traits, and
/// the traits are printed words — "Hero for Hire", "S.H.I.E.L.D.", "Trap!".
/// <see cref="CardCatalog.TraitKey"/> is the one place the spelling is reshaped
/// and these pin what it does, because a key is a wire format and the printed
/// word is not.
/// </para>
/// <para>
/// <b>Which trait a card has is not a question this file answers.</b>
/// <c>datasets/cards/</c> is generated from the vendored MarvelSDB snapshot, so
/// a card's traits are what the printed card says; there is no second list to
/// choose between.
/// </para>
/// </remarks>
public sealed class CardCatalogTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void CounterTypesComeFromPrintedCardTextAndUses()
    {
        Assert.Equal(["web"], Cards.CounterTypes("01008"));
        Assert.Equal(["energy"], Cards.CounterTypes("01018"));
        Assert.Empty(Cards.CounterTypes("01094"));
    }

    [Theory]
    // Already upper-case in the engine's data, so nothing to resolve.
    [InlineData("CRIMINAL", "CRIMINAL")]
    // Spaces become underscores. 39 engine traits have one.
    [InlineData("HERO FOR HIRE", "HERO_FOR_HIRE")]
    [InlineData("ACTIVATION ORDER 1", "ACTIVATION_ORDER_1")]
    // Stops survive; the engine already stores these without a trailing one.
    [InlineData("S.H.I.E.L.D", "S.H.I.E.L.D")]
    [InlineData("A.I.M", "A.I.M")]
    // Hyphens survive.
    [InlineData("WEB-WARRIOR", "WEB-WARRIOR")]
    // The two that carry a bang. This is the one that a port deriving keys from
    // the printed traits gets wrong, and it is wrong on five real cards.
    [InlineData("TRAP!", "TRAP")]
    [InlineData("CHASE!", "CHASE")]
    public void ATraitKeyIsTheEnginesTwoSubstitutions(string engineTrait, string expected) =>
        Assert.Equal(expected, CardCatalog.TraitKey(engineTrait));

    [Theory]
    // Cards whose traits MarvelSDB does not record, carried by
    // `datasets/cards/supplement.json`. Without them these cards would have no
    // `t_` key at all, and a card asking for a CRIMINAL would not find them.
    [InlineData("01172", "CRIMINAL")]     // Whiplash
    [InlineData("07006", "WEAPON")]       // Magic Crowbar
    [InlineData("25032", "CONDITION")]    // Seduced
    public void ATraitTheSnapshotDoesNotRecordIsCarriedBySupplement(
        string card, string trait) =>
        Assert.Contains(trait, Cards.Traits(card));

    [Fact]
    public void TheCoreSupplementCarriesOnlyFactsPrintedOnRealFaces()
    {
        // Concussion Blasters prints an ATK +1 modifier in its stat box, and
        // Whiplash prints the CRIMINAL trait. MarvelSDB records neither.
        Assert.Equal(1, Cards.PrintedValue("01153", "ATK+", players: 1));
        Assert.Contains("CRIMINAL", Cards.Traits("01172"));

        // The core set prints Android Efficiency as 01144a, 01144b and 01144c.
        // It has no base 01144 face. A player card dealt facedown as an Ultron
        // drone also remains that card; no separate Drone Minion face exists.
        Assert.Throws<KeyNotFoundException>(() => Cards.Title("01144"));
        Assert.Throws<KeyNotFoundException>(() => Cards.Title("ultron_facedown_drone"));
    }

    [Fact]
    public void ATraitIsWhateverThePrintedCardSays()
    {
        // 39029 Supporting Actor prints THESPIAN. The dataset this replaced
        // stored THESPYAN, and every `t_` key for that card was keyed from the
        // typo -- which is the shape of the whole change: the printed card is
        // the authority and there is no longer a second list to prefer.
        Assert.Contains("THESPIAN", Cards.Traits("39029"));
        Assert.DoesNotContain("THESPYAN", Cards.Traits("39029"));

        // 42016 Taunt prints TACTIC and 50180 prints S.H.I.E.L.D. Neither had
        // one before, for the same reason.
        Assert.Contains("TACTIC", Cards.Traits("42016"));
        Assert.Contains("S.H.I.E.L.D", Cards.Traits("50180"));
    }

    [Fact]
    public void ABangTraitLosesItsBangOnTheFiveCardsThatHaveOne()
    {
        foreach (string card in (string[])["27102a", "47031", "47032", "47033"])
        {
            Assert.Contains("TRAP", Cards.Traits(card));
            Assert.DoesNotContain("TRAP!", Cards.Traits(card));
        }

        Assert.Contains("CHASE", Cards.Traits("27102b"));
    }

    [Fact]
    public void EveryCardInTheSnapshotHasItsPrintedTraits()
    {
        // The dataset this replaced held an engine-side record for 3,999 of its
        // 4,344 cards and gave the other 345 no traits at all -- 200 of them
        // with printed ones. A card the engine has never dealt is still a card
        // it can be asked to deal.
        //
        // 21180b Cosmo prints GUARDIAN; 21187a Norn Stone prints two.
        Assert.Equal(["GUARDIAN"], Cards.Traits("21180b"));
        Assert.Equal(["ASGARD", "ARTIFACT"], Cards.Traits("21187a"));
    }

    [Fact]
    public void TheRhinoBoardsTraitsAreWhatThePrintedCardsSay()
    {
        // The three cards on the opening board of the scenario the suite plays
        // most, named rather than counted: a change to the extract that swept
        // traits away entirely would pass every assertion above.
        Assert.Equal(["AVENGER"], Cards.Traits("01001a"));    // Spider-Man
        Assert.Equal(["GENIUS"], Cards.Traits("01001b"));     // Peter Parker
        Assert.Equal(["BRUTE", "CRIMINAL"], Cards.Traits("01094"));  // Rhino
    }
}
