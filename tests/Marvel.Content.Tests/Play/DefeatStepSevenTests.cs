using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Step 7 of dealing damage — <c>rr:damage.step.7</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Abilities that trigger <i>when [character] is defeated…</i>
/// <i>(including <b>When Defeated</b> abilities)</i>." The parenthesis is the
/// whole of this file: a card's own dying ability and <b>another</b> card's
/// forced interrupt on the same defeat are one moment, and the Rules Reference
/// numbers where that moment is — after <c>.step.5</c> places the damage,
/// before <c>.step.8</c> discards the card.
/// </para>
/// <para>
/// Which is why it is not in the occurrence's interrupt window.
/// <c>rr:triggering-condition.2</c> gives the attack that damages and defeats
/// one interrupt window and one response window, and that window opened before
/// <c>.step.1</c> — when the damage had not been dealt and there was no defeat
/// to answer. <c>rr:damage</c>'s own numbering says step 7 is somewhere else,
/// and every ability there is forced, so nothing is lost by there being nobody
/// to ask.
/// </para>
/// <para>
/// The card this exists for is 45066 Genetic Experiments — "<b>Forced
/// Interrupt</b>: When attached minion is defeated, place 2 threat on Gene
/// Pool" — which names a card other than the one carrying it.
/// </para>
/// </remarks>
public sealed class DefeatStepSevenTests
{
    /// <summary>Hydra Mercenary, a minion that prints no "When Defeated".</summary>
    private const string Mercenary = "01101";

    /// <summary>Charge, an attachment. Nothing here uses its printed text.</summary>
    private const string Attachment = "01099";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    /// <summary>An attachment answering the defeat of what it hangs from.</summary>
    private static readonly string AttachmentInterrupt =
        Book((Attachment, "ForcedInterrupt", "attachedTo", 2));

    [Rule("rr:damage.step.7")]
    [Fact]
    public void AnotherCardsForcedInterruptOnTheDefeatResolves()
    {
        // The gap this closes, in one sentence: an attachment reacting to the
        // minion it is attached to dying. Nothing in the interrupt window could
        // serve it, because the window closed before the damage.
        var world = Board(AttachmentInterrupt, out var minion, out _);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens.GetValueOrDefault("k_threat");

        Agendas.Happening(world);
        Defeat.Character(world, Cards, minion, "test", []);
        Agendas.Finish(world, Cards, world.Abilities);

        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat") - before);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Fact]
    public void ItResolvesBeforeTheDefeatedCardLeavesPlay()
    {
        // `.step.7` before `.step.8`, which is also
        // `rr:when-defeated-abilities.2.1` from the other side. It matters for
        // an attachment more than for the card's own ability: an attachment
        // goes with its host, so a step 7 that ran after the discard would find
        // itself out of play and answer nothing.
        var world = Board(AttachmentInterrupt, out var minion, out _);
        var events = new List<GameEvent>();

        Agendas.Happening(world);
        Defeat.Character(world, Cards, minion, "test", events);
        events.AddRange(Agendas.Finish(world, Cards, world.Abilities));

        int placed = events.FindIndex(happened => happened.Verb == "Place_Threat");
        int left = events.FindIndex(
            happened => happened is CardsMoved moved
                && moved.Cards.Any(landing => landing.Card == minion.ObjectId));

        Assert.True(placed >= 0, "the interrupt ran");
        Assert.True(left >= 0, "the minion left play");
        Assert.True(placed < left, "step 7 resolved before step 8 discarded the card");
    }

    [Rule("rr:damage.step.7")]
    [Fact]
    public void ItAnswersOnlyTheDefeatOfTheCardItNames()
    {
        // "When **attached minion** is defeated" is a claim about which card
        // died, and the occurrence step 7 matches against has to carry it. A
        // second minion dying is not this attachment's business.
        var world = Board(AttachmentInterrupt, out _, out _);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var other = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        long before = scheme.Tokens.GetValueOrDefault("k_threat");

        Agendas.Happening(world);
        Defeat.Character(world, Cards, other, "test", []);

        Assert.Equal(before, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:triggering-condition.1")]
    [Fact]
    public void ItIsSpentOnTheOccurrenceOnTheAgendaAndNotOnACopyOfIt()
    {
        // "Each **Interrupt** ability can only be triggered once per occurrence
        // of its triggering condition." Two minions defeated by one occurrence
        // is one `WhenCardDefeated` between them -- `Occurrence.Also` is
        // idempotent for exactly that reason -- so a card answering "when a
        // minion is defeated" answers once.
        //
        // Step 7 builds its own occurrence to carry the subject, and that one
        // is made fresh on every defeat. If the bookkeeping lived there, this
        // would fire twice.
        var world = Board(
            """
            {"cards":[{"card":"@card","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"ForcedInterrupt",
                         "subject":"game"},
              "effect":{"placeAccelerationToken":1}}]}]}
            """.Replace("@card", Attachment, StringComparison.Ordinal),
            out var minion,
            out _);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var other = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        long before = scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);

        Agendas.Happening(world);
        Defeat.Character(world, Cards, other, "test", []);
        Defeat.Character(world, Cards, minion, "test", []);

        Assert.Equal(
            1,
            scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken) - before);
    }

    [Rule("rr:damage.step.7")]
    [Fact]
    public void ANonForcedInterruptIsRefusedRatherThanSilentlyDropped()
    {
        // An optional ability at step 7 has nobody to offer it to, and a card
        // carrying one would otherwise sit in the dataset looking implemented
        // and never fire. That is the failure this whole area is arranged to
        // avoid, so it is named out loud instead.
        var world = Board(
            Book((Attachment, "Interrupt", "attachedTo", 2)), out var minion, out _);

        Agendas.Happening(world);
        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Defeat.Character(world, Cards, minion, "test", []));

        Assert.Contains("non-forced interrupt", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("rr:damage.step.7", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:forced.5")]
    [Fact]
    public void TwoCardsAnsweringOneDefeatRefuseToPickAnOrder()
    {
        // "If two or more forced abilities would initiate at the same moment,
        // the **first player determines the order**." Step 7 is reached from
        // inside the damage and has nobody to ask, so it refuses rather than
        // choosing -- two effects at one moment in an order the engine invented
        // is a board that is plausible and wrong.
        var world = Board(
            Book(
                (Attachment, "ForcedInterrupt", "attachedTo", 2),
                (Mercenary, "WhenDefeated", "this", 1)),
            out var minion,
            out _);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens.GetValueOrDefault("k_threat");

        Agendas.Happening(world);
        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Defeat.Character(world, Cards, minion, "test", []));

        Assert.Contains("rr:forced.5", thrown.Message, StringComparison.Ordinal);

        // Refused before either of them ran, so the board is where it was
        // rather than half-way through an order nobody chose.
        Assert.Equal(before, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:when-defeated-abilities.2")]
    [Fact]
    public void ACardsOwnAbilityStillRunsWhenNothingElseAnswers()
    {
        // The half that already worked, kept honest. Step 7 holding two kinds
        // of card must not have cost the first kind anything.
        var world = Board(
            Book((Mercenary, "WhenDefeated", "this", 1)), out var minion, out _);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens.GetValueOrDefault("k_threat");

        Agendas.Happening(world);
        Defeat.Character(world, Cards, minion, "test", []);
        Agendas.Finish(world, Cards, world.Abilities);

        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat") - before);
    }

    [Rule("rr:ability.1")]
    [Fact]
    public void ACardThatIsNotInPlayDoesNotAnswer()
    {
        // "A card's ability functions while the card is in play." The
        // attachment is authored and its host is the minion, and it is in the
        // encounter discard pile -- so it says nothing.
        var world = Board(AttachmentInterrupt, out var minion, out var attachment);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        World.MoveToTop(attachment, world.AreaOf(DeckType.EncounterDiscardPile));
        long before = scheme.Tokens.GetValueOrDefault("k_threat");

        Agendas.Happening(world);
        Defeat.Character(world, Cards, minion, "test", []);

        Assert.Equal(before, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:damage.step.7")]
    [Fact]
    public void ASideSchemesDefeatReachesStepSevenToo()
    {
        // `Defeat.Scheme` is the other way in. A side scheme is defeated by
        // having no threat left rather than by damage, and
        // `rr:when-defeated-abilities.2` lists it among the cards this happens
        // to -- so the step is the same step.
        var world = Board(AttachmentInterrupt, out _, out var attachment);
        var side = world.CreateCard("01096", world.AreaOf(DeckType.SideSchemesArea));
        World.MoveToTop(
            attachment,
            world.AreaOf(DeckType.UpgradesArea, side.Area.PlayArea, side.ObjectId));

        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        Agendas.Happening(world);
        Defeat.Scheme(world, Cards, side, "test", []);
        Agendas.Finish(world, Cards, world.Abilities);

        Assert.Equal(2, main.Tokens.GetValueOrDefault("k_threat") - before);
    }

    /// <summary>
    /// An ability book in which each named card answers a defeat by placing
    /// threat on the main scheme.
    /// </summary>
    /// <remarks>
    /// One effect for every test here, and a deliberately dull one: what these
    /// assert is <i>whether</i> and <i>when</i> an ability at step 7 runs, and a
    /// counter on a card nothing else touches is the shortest way to see that.
    /// Written by substitution rather than as an interpolated raw string,
    /// because JSON this nested ends in runs of braces that an interpolated
    /// literal reads as escapes.
    /// </remarks>
    /// <param name="cards">The card, its bold trigger, its subject, and how much.</param>
    private static string Book(params (string Card, string Timing, string Subject, long Amount)[] cards)
    {
        const string One =
            """
            {"card":"@card","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"@timing","subject":"@subject"},
              "effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":@amount}}}]}
            """;

        var written = cards.Select(card => One
            .Replace("@card", card.Card, StringComparison.Ordinal)
            .Replace("@timing", card.Timing, StringComparison.Ordinal)
            .Replace("@subject", card.Subject, StringComparison.Ordinal)
            .Replace(
                "@amount",
                card.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.Ordinal));

        return $$"""{"cards":[{{string.Join(",", written)}}]}""";
    }

    /// <summary>The Rhino board, a minion, and an attachment hanging off it.</summary>
    private static World Board(string json, out Card minion, out Card attachment)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(json));

        minion = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        attachment = world.CreateCard(
            Attachment,
            world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        return world;
    }
}
