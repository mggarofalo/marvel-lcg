using Marvel.Content.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests;

/// <summary>Catalog sharing and identity admission are engine ingestion choices.</summary>
public sealed class CatalogIsolationTests
{
    [Fact]
    public void PublishedCardCollectionsCannotChangeFactsForOtherConsumers()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        ICardFacts otherConsumer = catalog;
        string[] traits = [.. otherConsumer.Traits("01002")];
        string[] labels = [.. otherConsumer.PrintedTraits("01002")];
        string[] keywords = [.. otherConsumer.Keywords("01008")];
        string[] counters = [.. otherConsumer.CounterTypes("01008")];
        long attack = otherConsumer.PrintedValue("01002", "ATK", 1);
        PrintedStatValue stat = otherConsumer.PrintedStats("01002")["ATK"];

        RejectListMutation(catalog.Traits("01002"));
        RejectListMutation(catalog.PrintedTraits("01002"));
        RejectListMutation(catalog.Keywords("01008"));
        RejectListMutation(catalog.CounterTypes("01008"));
        RejectDictionaryMutation(catalog.Attributes("01002"), "ATK", "999");
        RejectDictionaryMutation(catalog.PrintedStats("01002"), "ATK", stat with { Value = "999" });

        Assert.Equal(traits, otherConsumer.Traits("01002"));
        Assert.Equal(labels, otherConsumer.PrintedTraits("01002"));
        Assert.Equal(keywords, otherConsumer.Keywords("01008"));
        Assert.Equal(counters, otherConsumer.CounterTypes("01008"));
        Assert.Equal(attack, otherConsumer.PrintedValue("01002", "ATK", 1));
        Assert.Equal(stat, otherConsumer.PrintedStats("01002")["ATK"]);
    }

    [Fact]
    public void PublishedSetupCollectionsCannotChangeAnotherDeal()
    {
        var catalog = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        string heroName = catalog.HeroNames.First();
        string campaignName = catalog.CampaignNames.First();
        string[] before = Dealer.DealOrder(catalog, campaignName, [heroName])
            .Select(card => card.Spec).ToArray();
        HeroSetup hero = catalog.Hero(heroName);
        CampaignSetup campaign = catalog.Campaign(campaignName);
        IReadOnlyList<string>[] recipes =
        [
            hero.Hero, hero.HeroDeck, hero.Obligations, hero.NemesisSet, hero.PlayerDeck,
            campaign.Villain, campaign.Challenges, campaign.Schemes, campaign.SetAside,
            campaign.Encounters, campaign.EncounterSets, campaign.ModularSets,
            .. catalog.EncounterSetNames.Select(catalog.EncounterSet),
        ];

        foreach (IReadOnlyList<string> recipe in recipes) RejectListMutation(recipe);

        Assert.Equal(before, Dealer.DealOrder(catalog, campaignName, [heroName])
            .Select(card => card.Spec));
    }

    private static void RejectListMutation(IReadOnlyList<string> values)
    {
        if (values is not IList<string> mutable) return;
        Assert.Throws<NotSupportedException>(() => mutable.Add("changed"));
        if (values.Count > 0)
            Assert.Throws<NotSupportedException>(() => mutable[0] = "changed");
    }

    private static void RejectDictionaryMutation<T>(
        IReadOnlyDictionary<string, T> values, string key, T replacement)
    {
        if (values is IDictionary<string, T> mutable)
            Assert.Throws<NotSupportedException>(() => mutable[key] = replacement);
    }
}
