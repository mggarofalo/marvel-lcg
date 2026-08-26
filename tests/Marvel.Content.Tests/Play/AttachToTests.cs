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
/// "Attach to" — <c>rr:attach-to</c>, a rule about a phrase.
/// </summary>
/// <remarks>
/// <para>
/// "If a card uses the phrase 'attach to', it must be attached to <i>(placed
/// beneath and slightly overlapped by)</i> the specified game element <b>as it
/// enters play</b>." So it is not an ability, and the difference is not
/// cosmetic: the dataset modelled it as a "When Revealed", which reads
/// correctly for a card revealed off the encounter deck and is wrong everywhere
/// else. <c>rr:when-revealed-abilities.2</c> — "if an encounter card with a
/// 'When Revealed' ability is put into play <b>without being revealed</b>, the
/// 'When Revealed' ability does not trigger" — and a setup attachment is put
/// into play without being revealed, which is what blocked MARVEL-211.
/// </para>
/// <para>
/// Reading it as the rule it is also fixed something quieter.
/// <c>Reveal.Resolve</c> sent every attachment to nowhere, so
/// <c>Reveal.EnterPlay</c> never ran for one — and eleven attachments in the
/// pool print <c>uses X</c>, which is a keyword that fires on entering play.
/// Each of them had been arriving with an empty counter pool and an ability
/// that spends from it.
/// </para>
/// </remarks>
public sealed class AttachToTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    /// <summary>"Size Increase", an attachment printing <c>uses 3 size</c>.</summary>
    private const string SizeIncrease = "12028";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:attach-to")]
    [Fact]
    public void AnAttachmentAttachesAsItEntersPlay()
    {
        // Charge's own printed line, "Attach to Rhino", resolved by the route
        // into play rather than by an ability.
        var world = Deal(AuthoredCards.Runner());
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var charge = world.Cards.First(card => card.FaceId == AuthoredCards.Charge);

        Reveal.Resolve(world, Cards, charge, 0, []);

        Assert.Equal(DeckType.UpgradesArea, charge.Area.Type);
        Assert.Equal(villain.ObjectId, charge.Area.Host);
    }

    [Rule("rr:attach-to")]
    [Rule("rr:when-revealed-abilities.2")]
    [Fact]
    public void ThePhraseIsNotAnAbilityAndDoesNotFireFromAWhenRevealedWindow()
    {
        // The distinction, made observable. Asking the interpreter to resolve
        // Charge's "When Revealed" attaches nothing, because Charge has no
        // "When Revealed" — the attach is a property of the card, which is
        // exactly what lets it work on a path that has no reveal in it.
        var world = Deal(AuthoredCards.Runner());
        var charge = world.Cards.First(card => card.FaceId == AuthoredCards.Charge);
        var before = charge.Area.Type;

        AuthoredCards.Runner().WhenRevealed(world, charge, 0);

        Assert.Equal(before, charge.Area.Type);
    }

    [Rule("rr:uses-x-type")]
    [Rule("rr:enters-play")]
    [Fact]
    public void AnAttachmentEnteringPlayGetsTheKeywordsItPrints()
    {
        // "When a card with this keyword enters play, place X all-purpose
        // counters from the token pool on the card." Size Increase prints
        // `uses 3 size`, and an attachment that never entered play never got
        // them — the card arrived attached and empty, with an ability that
        // spends from an empty pool.
        var world = Deal(Attaching(SizeIncrease, """{ "query": "villain" }"""));
        var size = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, Cards, size, 0, []);

        Assert.Equal(DeckType.UpgradesArea, size.Area.Type);
        Assert.Equal(3, size.Tokens.GetValueOrDefault("c_size"));
    }

    [Rule("rr:attach-to.3")]
    [Fact]
    public void AnAttachmentWhoseElementIsNotThereStaysWhereItWas()
    {
        // "The 'attach to' phrase is checked for legality when the card would
        // be attached [...] if the initial check does not pass, the card is not
        // able to be attached, so it remains in its prior state or game area."
        //
        // Named by title, so this is a card that names something no board in
        // this scenario holds — and the rest of the clause, "if such a card
        // cannot remain in its prior state or game area, discard it", is the
        // reveal's own step 4 discarding it from the table.
        var world = Deal(Attaching(SizeIncrease, """{ "titled": "Ultron" }"""));
        var size = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, Cards, size, 0, []);

        Assert.Equal(DeckType.RevealingArea, size.Area.Type);
        Assert.Equal(0, size.Tokens.GetValueOrDefault("c_size"));
    }

    [Rule("rr:reveal.1")]
    [Fact]
    public void AnAttachmentPrintingNoSuchPhraseIsNotPutIntoPlayEither()
    {
        // The control, and the reason the answer is nullable rather than a
        // throw: an attachment that names nothing and one whose named element
        // is absent end in the same place, because `rr:attach-to.3` says so.
        var world = Deal(Book($$"""{ "card": "{{SizeIncrease}}", "abilities": [] },"""));
        var size = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, Cards, size, 0, []);

        Assert.Equal(DeckType.RevealingArea, size.Area.Type);
    }

    [Fact]
    public void AnAttachToNamingTwoThingsIsRefused()
    {
        // The same strictness the effect tree has. "Attach to" names one game
        // element — `rr:attach-to` is singular throughout — so a value carrying
        // two operations is a card that has not been read properly rather than
        // one the engine should pick from.
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            $$"""
            { "cards": [ {
                "card": "{{SizeIncrease}}",
                "attachTo": { "query": "villain", "titled": "Ultron" },
                "abilities": []
            } ] }
            """));

        Assert.Contains("is not a node", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A book in which one card attaches to something, and the scenario's own
    /// three cards are read and silent.
    /// </summary>
    /// <remarks>
    /// The deal asks the main scheme and the villain for their setup text
    /// (<c>rr:appendix-ii-setup.step.12</c>), and a card nobody has read stops
    /// it — so a stub book has to say it read them.
    /// </remarks>
    private static AbilityRunner Attaching(string card, string element) =>
        Book($$"""{ "card": "{{card}}", "attachTo": {{element}}, "abilities": [] },""");

    private static AbilityRunner Book(string cards) =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [
                {{cards}}
                { "card": "01097a", "abilities": [] },
                { "card": "01097b", "abilities": [] },
                { "card": "01094", "abilities": [] }
            ] }
            """));

    private static World Deal(ICardAbilities abilities) => WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
        ["Spider-Man"],
        Seed,
        abilities);
}
