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
/// "When Defeated" abilities — <c>rr:when-defeated-abilities</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>255 cards in the pool have one and none of them ran.</b> The same shape
/// as the boost gap: an ability type the timing spine had a name and a tier for,
/// with nothing calling it. 140 side schemes, 58 minions, 30 player side
/// schemes, 23 villain stages.
/// </para>
/// <para>
/// <c>.2.1</c> is the part that decides where the call goes: "a defeated card
/// leaves play <b>after</b> its When Defeated ability is resolved, if any." So
/// the card is still where it was while the ability runs — which is what lets
/// one read its own tokens, or return the cards attached to it.
/// </para>
/// </remarks>
public sealed class WhenDefeatedTests
{
    /// <summary>
    /// Hydra Mercenary. It prints no "When Defeated", so the guard leaves it
    /// alone and a test can give it one without arguing with the pool.
    /// </summary>
    private const string Mercenary = "01101";

    /// <summary>Highway Robbery — a side scheme that prints one.</summary>
    private const string HighwayRobbery = "01166";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:when-defeated-abilities")]
    [Fact]
    public void ACardThatPrintsOneAndHasNoDataThrows()
    {
        // The failure this exists for, and it is the reveal bargain again: a
        // side scheme whose "When Defeated" resolved to silence would hand back
        // nothing, and the board would look right.
        var world = Deal();
        var scheme = world.CreateCard(
            HighwayRobbery, world.AreaOf(DeckType.SideSchemesArea));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Defeat.Scheme(world, Cards, scheme, "test", []));

        Assert.Contains("'When Defeated'", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(HighwayRobbery, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACardWithoutOneIsDefeatedInSilence()
    {
        // Most cards have none, and asking about them must stay free: it
        // happens every time anything dies.
        var world = Deal();
        var minion = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        var events = new List<GameEvent>();
        Defeat.Character(world, Cards, minion, "test", events);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Rule("rr:when-defeated-abilities.2.1")]
    [Fact]
    public void TheAbilityResolvesBeforeTheCardLeavesPlay()
    {
        // "A defeated card leaves play **after** its When Defeated ability is
        // resolved." Asserted on the event stream rather than the board,
        // because the board cannot show an order — and the event stream is
        // what the digest is built from.
        var world = Board(
            """
            {"cards":[{"card":"01166","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"giveStatus":{"card":{"query":"villain"},"status":"tough"}}}]}]}
            """,
            out var runner);
        var scheme = world.CreateCard(
            HighwayRobbery, world.AreaOf(DeckType.SideSchemesArea));

        var events = new List<GameEvent>();
        Defeat.Scheme(world, Cards, scheme, "test", events);

        int gave = events.FindIndex(happened => happened.Verb == "Give_Status");
        int left = events.FindIndex(
            happened => happened is CardsMoved moved
                && moved.Cards.Any(landing => landing.Card == scheme.ObjectId));

        Assert.True(gave >= 0, "the ability ran");
        Assert.True(left >= 0, "the card left play");
        Assert.True(gave < left, "the ability resolved before the card left play");
        Assert.True(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Tough));
    }

    [Rule("rr:when-defeated-abilities.2")]
    [Fact]
    public void AllOfThemResolveAndNotJustTheFirst()
    {
        // "**All** When Defeated abilities on the card resolve." A window takes
        // one at a time; this is not a window, and a card with two says two
        // things.
        var world = Board(
            """
            {"cards":[{"card":"01166","abilities":[
              {"trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
               "effect":{"giveStatus":{"card":{"query":"villain"},"status":"tough"}}},
              {"trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
               "effect":{"giveStatus":{"card":{"query":"villain"},"status":"stunned"}}}]}]}
            """,
            out _);
        var scheme = world.CreateCard(
            HighwayRobbery, world.AreaOf(DeckType.SideSchemesArea));

        Defeat.Scheme(world, Cards, scheme, "test", []);

        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.True(Statuses.Has(world, villain, Statuses.Tough));
        Assert.True(Statuses.Has(world, villain, Statuses.Stunned));
    }

    [Rule("rr:when-defeated-abilities.2")]
    [Fact]
    public void AMinionsRunsWhenItIsDefeated()
    {
        // The other half of `Defeat`: 58 minions in the pool print one, and a
        // minion dies through `Defeat.Character` rather than `Defeat.Scheme`.
        // Fabian Cortez is the shape -- "the player who defeated Fabian Cortez
        // discards cards from the encounter deck until an ACOLYTE minion is
        // discarded" -- so the ability needs the card still in play to know
        // whose it was.
        var world = Board(
            """
            {"cards":[{"card":"01101","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"giveStatus":{"card":{"query":"villain"},"status":"stunned"}}}]}]}
            """,
            out _);
        var minion = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Defeat.Character(world, Cards, minion, "test", []);


        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.True(
            Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned));
    }

    [Rule("rr:when-defeated-abilities.2")]
    [Fact]
    public void AVillainStageHasThemToo()
    {
        // The rule lists "a villain stage, side scheme, main scheme stage, ally,
        // or minion", and 23 villain stages in the pool print one. The call
        // sits before the switch on card type for exactly that reason.
        var world = Board(
            """
            {"cards":[{"card":"01094","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}}]}]}
            """,
            out _);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;

        world.Abilities.WhenDefeated(world, villain);

        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:when-defeated-abilities")]
    [Fact]
    public void AnEncounterCardsAbilityHasNoPlayerUntilTheCardSaysWhich()
    {
        // **A "When Defeated" on a minion belongs to nobody.** The cards say
        // whose it is themselves -- "the player who defeated Fabian Cortez
        // discards cards from the encounter deck..." -- and `Defeat` does not
        // carry who that was.
        //
        // So a card that asks for a resolving player it has not got is refused
        // by name. The alternative is reaching for the first player, which is
        // right one game in four and silent in the others.
        var world = Board(
            """
            {"cards":[{"card":"01094","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"giveStatus":{"card":"you","status":"stunned"}}}]}]}
            """,
            out _);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => world.Abilities.WhenDefeated(world, villain));

        Assert.Contains("asks who is resolving it", thrown.Message, StringComparison.Ordinal);
        Assert.False(
            Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

    /// <summary>The Rhino board, with an inline book behind it.</summary>
    private static World Board(string json, out AbilityRunner runner)
    {
        var world = Deal();
        runner = new AbilityRunner(AbilityCatalog.Parse(json));
        world.Abilities = runner;
        return world;
    }

    /// <summary>
    /// The Rhino board, with the real authored cards behind it.
    /// </summary>
    /// <remarks>
    /// <c>World.Abilities</c> is set here rather than left at its default,
    /// because the default is <c>NoCardAbilities</c> — a board where no card
    /// does anything, which is what a board built by hand is. A real game gets
    /// its runner from <c>Game.Begin</c>.
    /// </remarks>
    private static World Deal()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}
