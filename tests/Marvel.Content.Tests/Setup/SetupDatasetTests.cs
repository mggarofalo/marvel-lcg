using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>
/// The setup dataset holds together — <c>AGENTS.md</c> non-negotiable 8.
/// </summary>
/// <remarks>
/// <para>
/// <c>datasets/setup/setup.json</c> is <b>authored</b>: what it records is
/// printed in rules inserts and on the backs of product boxes rather than on
/// any card, so there is nothing to generate it from and no upstream to vendor
/// it from. What the non-negotiable actually asks of a dataset is that it
/// cannot drift unnoticed, and for this one that is a gate rather than a
/// regeneration.
/// </para>
/// <para>
/// <b>Every failure here is one a game would otherwise meet at the table.</b>
/// A card id that resolves against nothing throws while a scenario is being
/// dealt, halfway through a board; a modular set nobody defines is a scenario
/// that deals fewer cards than it should and plays to the end looking fine.
/// The dataset it points at, <c>datasets/cards/</c>, is now regenerated from
/// an upstream that moves, so these two can come apart without either being
/// edited.
/// </para>
/// </remarks>
public sealed class SetupDatasetTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static readonly string CardsText =
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));

    private static readonly string SetupText =
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));

    private static readonly SetupCatalog Setup = SetupCatalog.Parse(SetupText);

    /// <summary>The keys whose arrays hold card ids rather than names.</summary>
    private static readonly string[] CardLists =
    [
        "encounters", "schemes", "set_aside", "villain",
        "hero", "hero_deck", "nemesis_set", "obligations", "player_deck",
    ];

    [Fact]
    public void EveryCardTheSetupNamesIsACardTheCatalogHas()
    {
        // Every reference in the supported Core Set boundary resolves before a
        // game is dealt. One that does not is a `KeyNotFoundException` thrown
        // partway through a deal, on whichever board happens to reach it.
        var missing = new List<string>();
        foreach (var (section, name, key, id) in Referenced())
        {
            if (!Cards.Has(id))
            {
                missing.Add($"{section}.{name}.{key} names card '{id}'");
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void EveryEncounterSetTheSetupNamesIsOneItDefines()
    {
        // `encounter_sets` and `modular_sets` on a scenario are names, and a
        // name is resolved against the same file's third group. A scenario
        // naming a set nobody defines deals fewer cards than the rules insert
        // says and never says so.
        var missing = new List<string>();
        foreach (var (name, campaign) in Campaigns())
        {
            foreach (string set in campaign.EncounterSets.Concat(campaign.ModularSets))
            {
                if (!Setup.EncounterSetNames.Contains(set, StringComparer.Ordinal))
                {
                    missing.Add($"campaigns.{name} names encounter set '{set}'");
                }
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void EveryScenarioHasAMainScheme()
    {
        // `rr:main-scheme-main-scheme-deck` -- every game is played against
        // one, and all six supported scenario modes carry it.
        foreach (var (name, campaign) in Campaigns())
        {
            Assert.True(campaign.Schemes.Count > 0, $"campaigns.{name} has no main scheme");
        }
    }

    [Fact]
    public void EverySupportedScenarioHasAVillainDeck()
    {
        foreach (var (name, campaign) in Campaigns())
        {
            Assert.True(campaign.Villain.Count > 0, $"campaigns.{name} has no villain deck");
        }
    }

    [Fact]
    public void EveryMainSchemeFaceIsAMainScheme()
    {
        // The kind is read from `datasets/cards/`, so this holds the authored
        // Core Set setup against the complete generated card catalog.
        foreach (var (name, campaign) in Campaigns())
        {
            foreach (string face in campaign.Schemes.SelectMany(Faces))
            {
                Assert.True(
                    Cards.Kind(face) == CardKind.MainScheme,
                    $"campaigns.{name} lists '{face}' as a main scheme, "
                    + $"and it is a {Cards.Kind(face)}");
            }
        }
    }

    [Fact]
    public void EveryVillainStageFunctionsAsAVillain()
    {
        foreach (var (name, campaign) in Campaigns())
        {
            foreach (string face in campaign.Villain.SelectMany(Faces))
            {
                Assert.Equal(
                    CardKind.EncounterVillain,
                    Cards.Kind(face));
                Assert.True(
                    CardKinds.IsVillain(CardKind.EncounterVillain),
                    $"campaigns.{name} lists '{face}' as a villain stage, "
                    + $"and it is a {Cards.Kind(face)}");
            }
        }
    }

    [Fact]
    public void AHeroOpensAsAnIdentityWithTwoSides()
    {
        // `rr:identity` -- "each hero has two identities, a hero form and an
        // alter-ego form", printed as the two sides of one card. The dataset
        // writes them as one comma-joined entry, which is what
        // `World.CreateCard` splits.
        foreach (var (name, hero) in Heroes())
        {
            var faces = hero.Hero.SelectMany(Faces).ToList();

            Assert.True(
                faces.Count == 2,
                $"heroes.{name} has {faces.Count} identity faces, not 2");

            // Hero side first. `rr:appendix-ii-setup.step.2` has each player
            // "choose a hero and place it hero-side faceup", so the order the
            // dataset writes them in is the order they are dealt.
            Assert.Equal(CardKind.Hero, Cards.Kind(faces[0]));

            Assert.Equal(CardKind.AlterEgo, Cards.Kind(faces[1]));
        }
    }

    [Fact]
    public void AHeroOpensWithAnObligationAndANemesisSet()
    {
        // `rr:obligation` and `rr:nemesis-encounter-set`: both are shuffled
        // into the encounter deck when that hero is in the game, so a hero
        // missing either plays a different game from the one printed.
        foreach (var (name, hero) in Heroes())
        {
            Assert.True(hero.Obligations.Count > 0, $"heroes.{name} has no obligation");
            Assert.True(hero.NemesisSet.Count > 0, $"heroes.{name} has no nemesis set");
        }
    }

    [Rule("rr:classifications.1")]
    [Rule("rr:classifications.2")]
    [Rule("rr:classifications.3")]
    [Rule("rr:identity-specific-card.1")]
    [Rule("rr:identity-specific-card.2")]
    [Rule("rr:identity-specific-card.3.1")]
    [Rule("rr:nemesis-encounter-set.1")]
    [Rule("rr:nemesis-encounter-set.2")]
    [Rule("rr:obligation.3")]
    [Rule("rr:basic-card.1")]
    [Rule("rr:basic-card.3")]
    [Fact]
    public void EverySupportedStarterDeckPassesProductIndependentConstructionRules()
    {
        // The authored setup names signature and customizable cards; the
        // generated card dataset supplies their printed class and set icon.
        // Validate every starter rather than proving one hand-picked deck.
        foreach (string hero in Setup.HeroNames)
        {
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", [hero], facts: Cards), Cards);
        }
    }

    [Rule("rr:unique-icon")]
    [Fact]
    public void TheGeneratedDatasetCarriesEveryPrintedUniqueIcon()
    {
        // The icon is vendored as MarvelSDB's `is_unique` boolean and emitted
        // as the generated `Unique` fact. Pin the complete current pool so a
        // dropped extractor branch cannot silently disable every rule using it.
        using var document = JsonDocument.Parse(CardsText);
        Assert.Equal(
            1340,
            document.RootElement.GetProperty("cards").EnumerateArray().Count(card =>
                card.GetProperty("attributes").TryGetProperty("Unique", out _)));
    }

    [Fact]
    public void TheHeaderCountsWhatTheFileHolds()
    {
        // The counts are the file's own claim about itself, and a claim nobody
        // checks is how a group quietly stops covering a scenario.
        using var document = JsonDocument.Parse(SetupText);
        var counts = document.RootElement.GetProperty("counts");

        Assert.Equal(Setup.CampaignNames.Count, counts.GetProperty("campaigns").GetInt32());
        Assert.Equal(Setup.HeroNames.Count, counts.GetProperty("heroes").GetInt32());
        Assert.Equal(
            Setup.EncounterSetNames.Count, counts.GetProperty("encounter_sets").GetInt32());
    }

    [Fact]
    public void RuntimeNamesDoNotCollideAcrossSetupGroups()
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        var collisions = new List<string>();
        foreach (var (group, names) in new (string, IEnumerable<string>)[]
        {
            ("campaigns", Setup.CampaignNames),
            ("heroes", Setup.HeroNames),
            ("encounter_sets", Setup.EncounterSetNames),
        })
        {
            foreach (string name in names)
            {
                if (!seen.TryAdd(name, group))
                {
                    collisions.Add(name);
                }
            }
        }

        Assert.Empty(collisions);
    }

    [Fact]
    public void TheRuntimeBoundaryIsExactlyTheCoreSet()
    {
        // The complete card catalog remains available as printed facts, but
        // setup is the product-selection API. Its keys therefore define the
        // product the engine claims can be played.
        Assert.Equal(
            ["rhino", "rhino_expert", "klaw", "klaw_expert", "ultron", "ultron_expert"],
            Setup.CampaignNames);
        Assert.Equal(
            ["spider_man", "captain_marvel", "she_hulk", "iron_man", "black_panther"],
            Setup.HeroNames);
        Assert.Equal(
            [
                "standard", "expert", "bomb_scare", "masters_of_evil", "under_attack",
                "legions_of_hydra", "the_doomsday_chair",
            ],
            Setup.EncounterSetNames);

        Assert.Throws<KeyNotFoundException>(() => Setup.Campaign("unus"));
        Assert.Throws<KeyNotFoundException>(() => Setup.Hero("sp_dr"));
        Assert.Throws<KeyNotFoundException>(() => Setup.EncounterSet("sinister_syndicate"));
    }

    /// <summary>Every card id the dataset names, with where it names it.</summary>
    private static IEnumerable<(string Section, string Name, string Key, string Card)>
        Referenced()
    {
        using var document = JsonDocument.Parse(SetupText);
        foreach (string section in (string[])["campaigns", "heroes", "encounter_sets"])
        {
            foreach (var record in document.RootElement.GetProperty(section).EnumerateObject())
            {
                foreach (var field in record.Value.EnumerateObject())
                {
                    if (!CardLists.Contains(field.Name, StringComparer.Ordinal)
                        || field.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var entry in field.Value.EnumerateArray())
                    {
                        foreach (string id in Faces(entry.GetString() ?? string.Empty))
                        {
                            yield return (section, record.Name, field.Name, id);
                        }
                    }
                }
            }
        }
    }

    // A double-sided card is written as its two faces joined by a comma, which
    // is what `World.CreateCard` splits on.
    private static IEnumerable<string> Faces(string entry) =>
        entry.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<(string Name, CampaignSetup Campaign)> Campaigns() =>
        Setup.CampaignNames.Select(name => (name, Setup.Campaign(name)));

    private static IEnumerable<(string Name, HeroSetup Hero)> Heroes() =>
        Setup.HeroNames.Select(name => (name, Setup.Hero(name)));
}
