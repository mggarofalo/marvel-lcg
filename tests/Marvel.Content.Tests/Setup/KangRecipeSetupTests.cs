using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

// Direct production-dealer evidence for The Once and Future Kang recipes. Public
// admission stays closed (KangRecipeAdmissionTests); these deal the declared catalog.
public sealed class KangRecipeSetupTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly string[] Heroes = ["spider_man", "captain_marvel", "she_hulk", "iron_man"];
    private static readonly string[] Modulars = ["temporal", "mot", "anachronauts"];
    private static readonly bool[] Modes = [false, true];

    /// <summary>Every recommended combination: both modes, each modular set, one to four players.</summary>
    public static IEnumerable<object[]> Combinations() =>
        from expert in Modes
        from modular in Modulars
        from players in Enumerable.Range(1, 4)
        select new object[] { expert, modular, players };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecipesMatchPrintedContentsAndPinnedPhysicalQuantities(bool expert)
    {
        // 11007a Contents: "Kang (I), each Kang (II), and Kang (III). Kang and Standard
        // encounter sets. One modular encounter set (Temporal)." Setup: "Set each Kang (II),
        // Kang (III), and Kang's Dominion side scheme aside. remove each player's obligation
        // cards from the game." pack:mc11:adjustable-difficulty: Expert replaces "all six
        // villains in the Kang encounter set with the six villains from the Expert Kang set
        // and add[s] the Expert encounter set".
        var recipe = Setup.Campaign(expert ? "kang_expert" : "kang");
        var pinned = Pinned();
        int first = expert ? 11034 : 11001;
        Assert.Equal("Kang", recipe.Name);
        Assert.Equal(expert, recipe.Expert);
        Assert.True(recipe.RemovesHeroObligations);
        Assert.Equal([Code(first)], recipe.Villain);
        // MARVEL-894 Q13: stages 1, 2 and 4 are the main scheme deck in that order.
        Assert.Equal(["11007a,11007b", "11008a,11008b", "11013a,11013b"], recipe.Schemes);
        Assert.Equal(Enumerable.Range(first + 1, 5).Select(Code)
            .Concat(["11009a,11009b", "11010a,11010b", "11011a,11011b", "11012a,11012b"])
            .Concat(Enumerable.Repeat("11023", pinned["11023"].Quantity)), recipe.SetAside);
        Assert.Equal(expert ? ["standard", "expert"] : ["standard"], recipe.EncounterSets);
        Assert.Equal(["temporal"], recipe.ModularSets);
        // Every other Kang-set card is an encounter card at its pinned physical quantity.
        var scenario = pinned.Where(item => item.Value.Set == "kang"
                && item.Value.Type is not ("villain" or "main_scheme") && item.Key != "11023")
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .SelectMany(item => Enumerable.Repeat(item.Key, item.Value.Quantity));
        Assert.Equal(scenario, recipe.Encounters);
        Assert.Equal(26, recipe.Encounters.Count);
        Assert.Equal(4, pinned["11023"].Quantity);
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    [Rule("rr:appendix-ii-setup.step.12.b")]
    public void EveryRecommendedCombinationDealsThroughTheProductionDealer(bool expert, string modular, int players)
    {
        // "Flip the main scheme card to side 1B and resolve any 'When Revealed' abilities
        // on that side." 11007b: "When Revealed: Deal each player an encounter card."
        var heroes = Heroes.Take(players).ToArray();
        var events = new List<GameEvent>();
        var world = Deal(expert, modular, heroes, 11, events);
        var game = Game.Begin(world, Cards, world.Abilities);
        int first = expert ? 11034 : 11001;

        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(Code(first), Assert.Single(world.AreaOf(DeckType.VillainArea).Cards).FaceId);
        Assert.Empty(world.AreaOf(DeckType.VillainDeck).Cards);
        var scheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        Assert.Equal("11007b", scheme.FaceId);
        Assert.Equal(["11013a", "11008a"], world.AreaOf(DeckType.MainSchemesDeck).Cards.Select(card => card.FaceId));
        Assert.Equal(Enumerable.Range(first + 1, 5).Select(Code)
                .Concat(["11009a", "11010a", "11011a", "11012a", "11023", "11023", "11023", "11023"])
                .Order(StringComparer.Ordinal),
            world.AreaOf(DeckType.AsideDeck).Cards.Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(world.Cards, card => card.FaceId == Code(expert ? 11001 : 11034));
        Assert.All(world.Seats, seat => Assert.Single(
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards));
        var expected = Setup.Campaign(expert ? "kang_expert" : "kang").Encounters
            .Concat(Setup.EncounterSet("standard"))
            .Concat(expert ? Setup.EncounterSet("expert") : [])
            .Concat(Setup.EncounterSet(modular));
        var dealt = world.AreaOf(DeckType.EncounterDeck).Cards
            .Concat(world.Seats.SelectMany(seat => world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards))
            .Select(card => card.FaceId);
        Assert.Equal(expected.Order(StringComparer.Ordinal), dealt.Order(StringComparer.Ordinal));
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void HeroObligationsAreRemovedFromTheGameWhileNemesisSetsStayAside(int players)
    {
        // 11007a: "remove each player's obligation cards from the game." Removed cards are
        // still cards in the removed-from-game area: 27135 Loose Ends searches it for your
        // obligation (ruling 78ee6578c6efb661). 13B searches "the encounter deck, discard
        // pile, and set-aside area for their nemesis minion", so each nemesis set is still
        // set aside (rr:appendix-ii-setup.step.5).
        var heroes = Heroes.Take(players).ToArray();
        var world = Deal(false, "temporal", heroes, 3, []);
        // The rules pseudo-card shares the removed-from-game area.
        var removed = world.AreaOf(DeckType.RemovedArea).Cards.Select(card => card.FaceId)
            .Where(face => face != "rule_a");
        var expected = heroes.SelectMany(hero => Setup.Hero(hero).Obligations).Select(spec => spec.Split(',')[0]);
        Assert.Equal(expected.Order(StringComparer.Ordinal), removed.Order(StringComparer.Ordinal));
        Assert.DoesNotContain(world.AreaOf(DeckType.EncounterDeck).Cards, card => expected.Contains(card.FaceId));
        for (int seat = 0; seat < players; seat++)
        {
            var hero = Setup.Hero(heroes[seat]);
            Assert.Equal(hero.NemesisSet.Order(StringComparer.Ordinal), world.Seats[seat].Nemesis.Cards
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        }
        // A control without the flag still deals each obligation into the encounter deck.
        var rhino = Dealer.DealOrder(Setup, "rhino", heroes, facts: Cards);
        Assert.Equal(players, rhino.Count(card => card.Source == Marvel.Rules.Setup.CreationSource.Obligation));
        Assert.Equal(players, Dealer.DealOrder(Setup, "kang", heroes, facts: Cards)
            .Count(card => card.Source == Marvel.Rules.Setup.CreationSource.RemovedObligation));
    }

    [Fact]
    public void TheObligationFlagMustBeABooleanAndIsFalseWhenAbsent()
    {
        Assert.All(Setup.CampaignNames.Where(name => !name.StartsWith("kang", StringComparison.Ordinal)),
            name => Assert.False(Setup.Campaign(name).RemovesHeroObligations));
        string text = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
        string malformed = text.Replace("\"remove_hero_obligations\": true", "\"remove_hero_obligations\": \"yes\"",
            StringComparison.Ordinal);
        Assert.NotEqual(text, malformed);
        Assert.Throws<JsonException>(() => SetupCatalog.Parse(malformed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheSameSeedDealsTheSameBoard(bool expert)
    {
        // Determinism: a seed names a game, including the stage-3 pool's later choice.
        var first = Deal(expert, "mot", Heroes, 29, []);
        var second = Deal(expert, "mot", Heroes, 29, []);
        Assert.Equal(first.Digest().Canonical(), second.Digest().Canonical());
        Assert.Equal(first.Random.Generator.GetState().Index, second.Random.Generator.GetState().Index);
        Assert.Equal(first.Random.Generator.GetState().Words, second.Random.Generator.GetState().Words);
    }

    internal static World Deal(bool expert, string modular, IReadOnlyList<string> heroes, uint seed, List<GameEvent> events)
    {
        string scenario = expert ? "kang_expert" : "kang";
        var plan = ScenarioDealer.Prepare(Setup, new DealRequest(scenario, heroes, [modular]), Cards);
        return WorldSetup.Deal(Cards, plan.Blueprints, heroes.Select(hero => Setup.Hero(hero).Name).ToArray(), seed,
            AuthoredCards.Runner(), events, expert);
    }

    private static string Code(int code) => code.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Physical quantities, sets and kinds from the pinned MarvelSDB snapshot, by card.</summary>
    private static Dictionary<string, (string Set, string Type, int Quantity)> Pinned()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("marvelsdb", "pack", "toafk_encounter.json")));
        return document.RootElement.EnumerateArray()
            .Where(card => !card.GetProperty("code").GetString()!.EndsWith('b'))
            .ToDictionary(card => card.GetProperty("code").GetString()!.TrimEnd('a'),
                card => (card.GetProperty("set_code").GetString()!, card.GetProperty("type_code").GetString()!,
                    card.GetProperty("quantity").GetInt32()), StringComparer.Ordinal);
    }
}
