using System.Text.Json;
using Marvel.Behavior.Index;
using Marvel.Rules.Index;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Rules;

/// <summary>The closed authority universe and its JCS fingerprint wire format.</summary>
public sealed class BehaviorAuthorityTests
{
    [Theory]
    [InlineData("card", "01001a", "141d34bb4dde86154de845afad8526562ee296ada91bea42cd2c3fb2a24b0993")]
    [InlineData("setup", "iron_man", "68764d8f0751996d173c63635b6a60ed31035a9de22795278320e7ff381e87b7")]
    [InlineData("faq", "01001a", "50c91638ef5792206844d0232bf5d623242141daebaef1e9ea1f84c4270d883f")]
    public void JcsAuthorityVectorsArePinned(string kind, string id, string expected)
    {
        using var document = kind switch
        {
            "card" => Read("cards", "cards.json"),
            "setup" => Read("setup", "setup.json"),
            "faq" => Read("marvelcdb-faq", "faq.json"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        JsonElement source = kind switch
        {
            "card" => document.RootElement.GetProperty("cards").EnumerateArray()
                .Single(card => card.GetProperty("card_id").GetString() == id),
            "setup" => document.RootElement.GetProperty("heroes").GetProperty(id),
            "faq" => document.RootElement.GetProperty("entries").EnumerateArray()
                .Single(entry => entry.GetProperty("code").GetString() == id),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        Assert.Equal("sha256:" + expected, CanonicalJson.Hash(source));
    }

    [Fact]
    public void EveryAuthorityUniverseIsEnumeratedInContractOrder()
    {
        var sources = AuthoritySources.Read();

        Assert.Equal(1218, sources.Count(source => source.Kind == "rule"));
        Assert.Equal(209, sources.Count(source => source.Kind == "card"));
        Assert.Equal(63, sources.Count(source => source.Kind == "faq"));
        Assert.Equal(1100, sources.Count(source => source.Kind == "ruling"));
        Assert.Equal(18, sources.Count(source => source.Kind == "setup"));
        Assert.Equal(sources.Count, sources.Select(source => source.Id).Distinct().Count());

        var kinds = sources.Select(source => source.Kind).Distinct().ToList();
        Assert.Equal(["rule", "card", "faq", "ruling", "setup"], kinds);
        foreach (var group in sources.GroupBy(source => source.Kind))
        {
            Assert.Equal(
                group.Select(source => source.Id).Order(StringComparer.Ordinal),
                group.Select(source => source.Id));
        }
    }

    [Fact]
    public void JcsEscapingUsesLiteralUnicodeAndRequiredControlEscapes()
    {
        using var document = JsonDocument.Parse(
            "{\"z\":\"<b>—\\n\",\"a\":\"\\u0001\\\"\\\\\"}");

        Assert.Equal(
            "{\"a\":\"\\u0001\\\"\\\\\",\"z\":\"<b>—\\n\"}",
            CanonicalJson.Serialize(document.RootElement));
    }

    [Fact]
    public void ReviewedCatalogAccountsForEveryAuthorityAndObligation()
    {
        var catalog = Catalog.Build();

        Assert.Equal(2, catalog.Version);
        Assert.Equal(2608, catalog.Sources.Count);
        Assert.Equal(4352, catalog.Sources.Sum(source => source.Obligations.Count));
        Assert.All(catalog.Sources, source => Assert.NotEmpty(source.Obligations));
        Assert.Equal(
            4352,
            catalog.Sources.SelectMany(source => source.Obligations)
                .Select(obligation => obligation.Id)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.DoesNotContain(
            catalog.Sources.SelectMany(source => source.Obligations),
            obligation => obligation.Disposition == "unreviewed");
    }

    [Fact]
    public void AbilityEntryRetainsEveryIndependentNormativeStatement()
    {
        var catalog = Catalog.Build();

        var sentenceOrder = Assert.Single(
            catalog.Sources, source => source.Id == "rr:ability.4");
        Assert.Contains(sentenceOrder.Obligations, obligation =>
            obligation.Id == "behavior:rr:ability.4:sentence-order"
            && obligation.Disposition == "executable");
        Assert.Contains(sentenceOrder.Obligations, obligation =>
            obligation.Id == "behavior:rr:ability.4:alteration-review"
            && obligation.Disposition == "no-independent-behavior");

        var constantLifetime = Assert.Single(
            catalog.Sources, source => source.Id == "rr:ability.8.2");
        Assert.Contains(constantLifetime.Obligations, obligation =>
            obligation.Id == "behavior:rr:ability.8.2:constant-active-while-in-play"
            && obligation.Disposition == "executable");
    }

    [Fact]
    public void UpToCostBranchesStartAtOneRatherThanZero()
    {
        var catalog = Catalog.Build();
        var legalPractice = Assert.Single(
            catalog.Sources, source => source.Id == "card:01023");

        Assert.Equal(
            3,
            legalPractice.Obligations.Count(obligation =>
                obligation.Id.Contains("choose-and-discard-up-5", StringComparison.Ordinal)));
        Assert.DoesNotContain(legalPractice.Obligations, obligation =>
            obligation.Id.EndsWith("-zero", StringComparison.Ordinal)
            || obligation.Id.EndsWith("-one", StringComparison.Ordinal)
            || obligation.Id.EndsWith("-multiple", StringComparison.Ordinal));
    }

    [Fact]
    public void InvasiveAiComesFromItsPrintedCoreStateRatherThanAnInventedDeck()
    {
        var catalog = Catalog.Build();
        var invasiveAi = Assert.Single(catalog.Sources, source => source.Id == "card:01149");
        Assert.Contains(
            invasiveAi.Obligations,
            obligation => obligation.Id.EndsWith(
                ":each-player-discards-top-3-cards-their-one-player",
                StringComparison.Ordinal));
        Assert.Contains(
            invasiveAi.Obligations,
            obligation => obligation.Id.EndsWith(
                ":each-player-discards-top-3-cards-their-multiple-players",
                StringComparison.Ordinal));

        var ironMan = Assert.Single(
            catalog.Sources,
            source => source.Id == "setup:hero:iron_man");
        var spiderMan = Assert.Single(
            catalog.Sources,
            source => source.Id == "setup:hero:spider_man");
        Assert.DoesNotContain(ironMan.Obligations, obligation => obligation.Summary.Contains(
            "01006",
            StringComparison.Ordinal));
        Assert.Contains(spiderMan.Obligations, obligation => obligation.Summary.Contains(
            "01006",
            StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyPlayerDeckContinuesTheDrawAndDoesNotEliminateThePlayer()
    {
        var catalog = Catalog.Build();
        var rule = Assert.Single(catalog.Sources, source => source.Id == "rr:player-deck.2");
        var obligation = Assert.Single(rule.Obligations);

        Assert.Contains("continues to draw", obligation.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("eliminat", obligation.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MixedRulingBranchesRetainIndependentDispositions()
    {
        var catalog = Catalog.Build();
        var ruling = Assert.Single(
            catalog.Sources,
            source => source.Id == "ruling:81a47ee5901551a4");

        Assert.Equal("mixed", ruling.Disposition);
        Assert.Contains(ruling.Obligations, obligation => obligation.Disposition == "executable");
        Assert.Contains(ruling.Obligations, obligation => obligation.Disposition == "outside-core");
    }

    [Fact]
    public void GeneratedCatalogIsCurrent()
    {
        Catalog.Check();
    }

    [Fact]
    public void CatalogSerializationUsesLfOnEveryPlatform()
    {
        string serialized = Catalog.Serialize(new { First = "one", Second = "two" });

        Assert.DoesNotContain('\r', serialized);
        Assert.EndsWith("\n", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void SkeletonsNameExactNegativeTranscriptExceptions()
    {
        var catalog = Catalog.Build();
        var unsupported = catalog.Sources.SelectMany(source => source.Obligations)
            .First(obligation => obligation.Implementation == "unimplemented");
        using var output = new StringWriter();

        Catalog.Skeletons(catalog, output);

        Assert.Contains($"@{unsupported.Id}", output.ToString(), StringComparison.Ordinal);
        Assert.Contains(
            $"Then {unsupported.Exception} is raised",
            output.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void EveryCodeRuleCitationMapsBackToAnAuthorityDerivedObligation()
    {
        var catalog = Catalog.Build();
        var bySource = catalog.Sources.ToDictionary(source => source.Id, StringComparer.Ordinal);
        var missing = Citations.Read()
            .Select(citation => citation.Id)
            .Distinct(StringComparer.Ordinal)
            .Where(id => !bySource.TryGetValue(id, out var source)
                || source.Obligations.Count == 0)
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void AuthorityChangesCannotRebindOldAdjudications()
    {
        var thrown = Assert.Throws<InvalidDataException>(() =>
            Catalog.EnsureReviewedFingerprint(
                "card:01149",
                "sha256:reviewed",
                "sha256:changed"));

        Assert.Contains("re-adjudicate", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ChoiceAndMaximumBranchesAreIndependentObligations()
    {
        var catalog = Catalog.Build();
        var vision = Assert.Single(catalog.Sources, source => source.Id == "card:01068");
        Assert.Contains(
            vision.Obligations,
            obligation => obligation.Id.EndsWith(
                ":choose-thw-plus-two-until-end-phase",
                StringComparison.Ordinal));
        Assert.Contains(
            vision.Obligations,
            obligation => obligation.Id.EndsWith(
                ":choose-atk-plus-two-until-end-phase",
                StringComparison.Ordinal));

        var energyChannel = Assert.Single(
            catalog.Sources,
            source => source.Id == "card:01018");
        Assert.Contains(
            energyChannel.Obligations,
            obligation => obligation.Id.EndsWith(":below-damage-cap", StringComparison.Ordinal));
        Assert.Contains(
            energyChannel.Obligations,
            obligation => obligation.Id.EndsWith(":at-damage-cap", StringComparison.Ordinal));
        Assert.Contains(
            energyChannel.Obligations,
            obligation => obligation.Id.EndsWith(":above-damage-cap", StringComparison.Ordinal));

        var hydraBomber = Assert.Single(
            catalog.Sources,
            source => source.Id == "card:01110");
        Assert.Contains(
            hydraBomber.Obligations,
            obligation => obligation.Id.EndsWith(":when-revealed-take-two-damage", StringComparison.Ordinal));
        Assert.Contains(
            hydraBomber.Obligations,
            obligation => obligation.Id.EndsWith(":when-revealed-place-one-threat", StringComparison.Ordinal));

        var nickFury = Assert.Single(catalog.Sources, source => source.Id == "card:01084");
        Assert.Contains(
            nickFury.Obligations,
            obligation => obligation.Id.EndsWith(":after-entering-play-remove-two-threat", StringComparison.Ordinal));
        Assert.Contains(
            nickFury.Obligations,
            obligation => obligation.Id.EndsWith(":after-entering-play-draw-three-cards", StringComparison.Ordinal));
        Assert.Contains(
            nickFury.Obligations,
            obligation => obligation.Id.EndsWith(":after-entering-play-deal-four-damage", StringComparison.Ordinal));
    }

    [Fact]
    public void IndependentFaqAnswersAndMixedScopeRemainSeparate()
    {
        var catalog = Catalog.Build();
        var webbedUp = Assert.Single(catalog.Sources, source => source.Id == "faq:01009");
        Assert.Equal(2, webbedUp.Obligations.Count);
        Assert.Contains(
            webbedUp.Obligations,
            obligation => obligation.Id.EndsWith(":prevents-two-attacks", StringComparison.Ordinal));
        Assert.Contains(
            webbedUp.Obligations,
            obligation => obligation.Id.EndsWith(
                ":replaced-attack-does-not-trigger-spider-sense",
                StringComparison.Ordinal));

        var makeTheCall = Assert.Single(catalog.Sources, source => source.Id == "faq:01071");
        Assert.Equal("mixed", makeTheCall.Disposition);
        Assert.Contains(
            makeTheCall.Obligations,
            obligation => obligation.Disposition == "executable");
        Assert.Contains(
            makeTheCall.Obligations,
            obligation => obligation.Disposition == "outside-core");

        var jenniferWalters = Assert.Single(
            catalog.Sources,
            source => source.Id == "faq:01019a");
        Assert.Equal(2, jenniferWalters.Obligations.Count);

        var pepperPotts = Assert.Single(catalog.Sources, source => source.Id == "faq:01033");
        Assert.Equal(3, pepperPotts.Obligations.Count);

        var focusedRage = Assert.Single(catalog.Sources, source => source.Id == "faq:01027");
        Assert.Equal("outside-core", focusedRage.Disposition);
        var legalPractice = Assert.Single(catalog.Sources, source => source.Id == "faq:01023");
        Assert.Equal("narrower", legalPractice.Disposition);
        var nova = Assert.Single(catalog.Sources, source => source.Id == "faq:01135");
        Assert.Contains(
            nova.Obligations,
            obligation => obligation.Id.EndsWith(":published-clarification-3", StringComparison.Ordinal)
                && obligation.Disposition == "outside-core");
    }

    [Fact]
    public void ConflictingHistoricalAnswersNameTheirCurrentReplacement()
    {
        var catalog = Catalog.Build();
        var historicalUltron = Assert.Single(
            catalog.Sources,
            source => source.Id == "ruling:5afa90a922165fcc");
        Assert.Contains(
            historicalUltron.Obligations,
            obligation => obligation.Disposition == "superseded"
                && obligation.Target ==
                    "behavior:ruling:3db19283592b0b90:original-target-receives-drone");

        var historicalStrength = Assert.Single(
            catalog.Sources,
            source => source.Id == "faq:01028");
        Assert.Contains(
            historicalStrength.Obligations,
            obligation => obligation.Disposition == "superseded"
                && obligation.Target ==
                    "behavior:ruling:7f35317bac86b8c4:already-stunned-enemy");

        var historicalMadameHydra = Assert.Single(
            catalog.Sources,
            source => source.Id == "ruling:6d7c80f97619194b");
        Assert.Contains(
            historicalMadameHydra.Obligations,
            obligation => obligation.Disposition == "superseded"
                && obligation.Target ==
                    "behavior:ruling:3893e8921b39cf65:madame-hydra-one-legions-scheme");
    }

    [Fact]
    public void CoreReachabilityAndEffectiveRuleOverlaysAreAdjudicatedPerBranch()
    {
        var catalog = Catalog.Build();
        var rhino = Assert.Single(
            catalog.Sources,
            source => source.Id == "ruling:074448583686e795");
        Assert.Equal("executable", rhino.Disposition);

        var reveal = Assert.Single(catalog.Sources, source => source.Id == "rr:reveal.1");
        Assert.Equal("mixed", reveal.Disposition);
        Assert.Contains(reveal.Obligations, obligation => obligation.Disposition == "executable");
        Assert.Contains(reveal.Obligations, obligation => obligation.Disposition == "outside-core");

        var forEach = catalog.Sources
            .Where(source => source.Id == "rr:for-each"
                || source.Id.StartsWith("rr:for-each.", StringComparison.Ordinal))
            .SelectMany(source => source.Obligations)
            .Where(obligation => obligation.Disposition == "executable");
        Assert.DoesNotContain(forEach, obligation => obligation.Implementation == "unimplemented");

        var defeatSteps = catalog.Sources
            .Where(source => source.Id is "rr:damage.step.6" or "rr:damage.step.7")
            .SelectMany(source => source.Obligations);
        Assert.DoesNotContain(
            defeatSteps,
            obligation => obligation.Implementation == "unimplemented");

        var singleTargets = Assert.Single(
            catalog.Sources,
            source => source.Id == "ruling:0870bc11f295fbb0");
        Assert.Contains(
            singleTargets.Obligations,
            obligation => obligation.Id.EndsWith(":legal-practice-one-scheme", StringComparison.Ordinal));
        Assert.Contains(
            singleTargets.Obligations,
            obligation => obligation.Id.EndsWith(":energy-channel-one-enemy", StringComparison.Ordinal));

        var splitRulings = new[]
        {
            "ruling:2c4a1030e6225759",
            "ruling:3216d926a2bbbadb",
            "ruling:82d66e85735baf99",
        };
        Assert.All(
            splitRulings,
            id => Assert.Equal(
                2,
                Assert.Single(catalog.Sources, source => source.Id == id).Obligations.Count));
    }

    private static JsonDocument Read(params string[] parts) =>
        JsonDocument.Parse(File.ReadAllBytes(RepositoryPaths.Dataset(parts)));
}
