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
        // 7,026 references across 135 scenarios, 63 heroes and 184 encounter
        // sets. One that does not resolve is a `KeyNotFoundException` thrown
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
        // one, and all 135 records carry it. A villain is the other half and
        // is *not* universal: see below.
        foreach (var (name, campaign) in Campaigns())
        {
            Assert.True(campaign.Schemes.Count > 0, $"campaigns.{name} has no main scheme");
        }
    }

    [Fact]
    public void TheScenariosWithNoVillainDeckAreTheOnesThatHaveSeveral()
    {
        // Sixteen records name no villain, and they are not incomplete. The
        // Wrecking Crew, the Sinister Six and the rest put several enemies in
        // play from their encounter sets rather than one villain deck, so
        // `villain` is empty and the encounter set carries them.
        //
        // Pinned by name because "some scenarios have no villain" is exactly
        // the shape a dropped field would take.
        string[] several =
        [
            "2423_the_wreckoning", "four_horsemen", "four_horsemen_expert", "loki",
            "loki_expert", "mansion_attack", "mansion_attack_expert", "morlock_siege",
            "morlock_siege_expert", "on_the_run", "on_the_run_expert", "sinister_six",
            "sinister_six_expert", "the_wrecking_crew", "the_wrecking_crew_expert", "wild",
        ];

        Assert.Equal(
            several,
            Campaigns()
                .Where(entry => entry.Campaign.Villain.Count == 0)
                .Select(entry => entry.Name)
                .OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void AMainSchemeIsAMainSchemeUnlessItsOtherSideIsAPlace()
    {
        // The kind is read from `datasets/cards/`, so this holds one dataset
        // against the other: a scenario whose scheme list named a side scheme
        // would deal a board that is plausible and wrong. 516 faces.
        //
        // Four of them are not main schemes and are right anyway. Venom
        // Goblin's four stages are double-sided cards whose backs are the
        // locations the scenario moves between, and `rr:environment` makes an
        // environment a card type of its own.
        string[] places = ["27116b", "27117b", "27118b", "27119b"];

        foreach (var (name, campaign) in Campaigns())
        {
            foreach (string face in campaign.Schemes.SelectMany(Faces))
            {
                var expected = places.Contains(face, StringComparer.Ordinal)
                    ? CardKind.Environment
                    : CardKind.MainScheme;

                Assert.True(
                    Cards.Kind(face) == expected,
                    $"campaigns.{name} lists '{face}' as a main scheme, "
                    + $"and it is a {Cards.Kind(face)}");
            }
        }
    }

    [Fact]
    public void EveryVillainStageFunctionsAsAVillain()
    {
        // `pack:mc56:leaders`: leaders are their own printed card type, are
        // used in place of villains in Civil War scenarios, and every game
        // rule and card ability affecting villains affects leaders.
        var leaders = new List<string>();
        foreach (var (name, campaign) in Campaigns())
        {
            foreach (string face in campaign.Villain.SelectMany(Faces))
            {
                if (Cards.Kind(face) == CardKind.Leader)
                {
                    leaders.Add($"{name}:{face}");
                }

                Assert.True(
                    CardKinds.IsVillain(Cards.Kind(face)),
                    $"campaigns.{name} lists '{face}' as a villain stage, "
                    + $"and it is a {Cards.Kind(face)}");
            }
        }

        Assert.Equal(
            [
                "captain_america:56137", "captain_america:56138",
                "captain_marvel:56092", "captain_marvel:56093",
                "iron_man:56059", "iron_man:56060",
                "spider_woman:56168", "spider_woman:56169",
            ],
            leaders.OrderBy(entry => entry, StringComparer.Ordinal));
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

            // `rr:form-change-form` -- most identities are one card with two
            // sides, and four are not. Angel's third face is Archangel; Ant-Man
            // and Wasp each print a third form; Ironheart is three cards.
            int expected = name switch
            {
                "angel" or "ant_man" or "wasp" => 3,
                "ironheart" => 6,
                _ => 2,
            };

            Assert.True(
                faces.Count == expected,
                $"heroes.{name} has {faces.Count} identity faces, not {expected}");

            // Hero side first. `rr:appendix-ii-setup.step.2` has each player
            // "choose a hero and place it hero-side faceup", so the order the
            // dataset writes them in is the order they are dealt.
            Assert.Equal(CardKind.Hero, Cards.Kind(faces[0]));

            // One hero has no alter-ego side. Upstream types SP//dr Suit's
            // back as a support and names both faces the same, which is either
            // a card that really is shaped that way or a gap in the
            // transcription -- MARVEL-256 is where that gets read off a card.
            // Either way `rr:identity` gives every hero two identities and the
            // engine would find only one.
            Assert.Equal(
                string.Equals(name, "sp_dr", StringComparison.Ordinal)
                    ? CardKind.Support
                    : CardKind.AlterEgo,
                Cards.Kind(faces[1]));
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
    public void TheNamesTwoGroupsShareAreTheOnesACharacterIsBothSidesOf()
    {
        // A scenario, a hero and an encounter set are looked up in three
        // separate tables, so sharing a name is not ambiguous *here*. It is
        // ambiguous to anything that flattens them -- and eleven characters
        // are both a hero and the villain of their own scenario, which is a
        // fact about the game rather than a defect in the file.
        //
        // Pinned so that a twelfth is something somebody looked at.
        string[] shared =
        [
            "black_widow", "captain_america", "captain_marvel", "enchantress", "iron_man",
            "magneto", "maria_hill", "nebula", "spider_man", "spider_woman", "venom",
        ];

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

        Assert.Equal(shared, collisions.OrderBy(name => name, StringComparer.Ordinal));
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
