using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests;

/// <summary>
/// Traits, and which of the dataset's two trait lists the digest is built from.
/// </summary>
/// <remarks>
/// <para>
/// <c>datasets/cards/cards.json</c> carries two: <c>traits</c> is MarvelSDB's
/// printed spelling, <c>engine.traits</c> is the Python engine's own list.
/// <c>CardFace.GetInfoTraits</c> keys every <c>t_</c> field in the state digest
/// from the second, so that is the one to read — and reading the first passes
/// almost every test, because they agree on all but twelve of 3,999 cards.
/// </para>
/// <para>
/// These pin the twelve, and the five cards where the two lists agree about the
/// trait and disagree about how it is spelled as a key. MARVEL-177.
/// </para>
/// </remarks>
public sealed class CardCatalogTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

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
    // The engine has a trait the printed card does not. Reading the printed
    // list would give this card no `t_` key at all.
    [InlineData("01172", "CRIMINAL")]     // Whiplash
    [InlineData("07006", "WEAPON")]       // Magic Crowbar
    [InlineData("25032", "CONDITION")]    // Seduced
    public void ATraitTheEngineHasAndThePrintedCardDoesNotIsStillATrait(
        string card, string trait) =>
        Assert.Contains(trait, Cards.Traits(card));

    [Fact]
    public void ATraitThePrintedCardHasAndTheEngineDoesNotIsNotOne()
    {
        // 42016 Taunt prints TACTIC; the engine gives it nothing, so the digest
        // has no `t_TACTIC` for it. Whichever source is right about the card,
        // the digest is built from the engine's answer and a port reproduces
        // that or fails the byte comparison.
        Assert.Empty(Cards.Traits("42016"));
        Assert.Empty(Cards.Traits("50180"));
    }

    [Fact]
    public void TheEnginesSpellingWinsEvenWhenItIsATypo()
    {
        // 39029 Supporting Actor. The printed trait is THESPIAN; the engine
        // stores THESPYAN and keys the digest from it. Pinned deliberately: it
        // is a defect in the engine's data and it is still the contract, so a
        // port that "corrected" it would diverge. Reported as
        // `engine_traits_diverge`.
        Assert.Contains("THESPYAN", Cards.Traits("39029"));
        Assert.DoesNotContain("THESPIAN", Cards.Traits("39029"));
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
    public void ACardTheEngineHasNeverHeardOfHasNoTraits()
    {
        // The dataset is a union of both sources, so it holds 345 cards
        // MarvelSDB has and the engine does not — 200 of them with printed
        // traits. Falling back to the printed list for those would invent `t_`
        // keys for a card that can never appear in a digest, because the engine
        // cannot create a card it has never heard of.
        //
        // 21180b Cosmo prints GUARDIAN and has no engine record.
        Assert.Empty(Cards.Traits("21180b"));
        Assert.Empty(Cards.Traits("21187a"));
    }

    [Fact]
    public void TheMilestoneBoardsTraitsAreUnchangedByReadingTheEnginesList()
    {
        // The guard on the whole change. `OpeningBoardTests` proves the digest
        // is still byte-identical; this says why that is not luck — every card
        // on that board agrees between the two lists, which is exactly what
        // MARVEL-177 predicted when it said none of the divergences is on it.
        Assert.Equal(["AVENGER"], Cards.Traits("01001a"));    // Spider-Man
        Assert.Equal(["GENIUS"], Cards.Traits("01001b"));     // Peter Parker
        Assert.Equal(["BRUTE", "CRIMINAL"], Cards.Traits("01094"));  // Rhino
    }
}
