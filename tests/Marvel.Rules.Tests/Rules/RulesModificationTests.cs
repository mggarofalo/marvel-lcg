using Marvel.Rules.Index;
using Marvel.Tests;
using Xunit;
using RuleRecord = Marvel.Rules.Index.Record;

namespace Marvel.Rules.Tests.Rules;

/// <summary>The citable modification layer over the vendored Rules Reference.</summary>
public sealed class RulesModificationTests
{
    [Rule("ruling:121d5b7b5dcec377")]
    [Fact]
    public void ACurrentModificationResolvesInsteadOfItsBaseText()
    {
        // "If an attachment does not have 'attach to' text, it must instead
        // attach once its 'When Revealed' ability triggers."
        var corpus = Corpus.Read();

        RuleRecord current = corpus.Resolve("rr:reveal.1", "1.8");

        Assert.Equal("ruling:121d5b7b5dcec377", current.Id);
        Assert.Contains("must instead attach once", current.Fragment, StringComparison.Ordinal);
        Modification provenance = Assert.Single(
            corpus.Modifications,
            item => item.Id == current.Id);
        Assert.Equal("Alex Werner (FFG Game Rules Specialist)", provenance.Source);
        Assert.Equal("1.7-1.8", provenance.Scope);
        Assert.Equal("2026-02", provenance.Observed);
    }

    [Rule("ruling:6cb0530e539c7915")]
    [Fact]
    public void AModificationCarriesTheRrgScopeAndPublicationMonth()
    {
        // "You are always permitted to generate resources beyond a card's
        // cost ('overpaying') and in doing so you are 'spending' resources."
        var corpus = Corpus.Read();

        RuleRecord current = corpus.Resolve("rr:cost.4", "1.8");
        Modification provenance = Assert.Single(
            corpus.Modifications,
            item => item.Id == current.Id);

        Assert.Equal("ruling:6cb0530e539c7915", current.Id);
        Assert.Equal("1.5", provenance.Scope);
        Assert.Equal("2023-07", provenance.Observed);
        Assert.Equal(
            "sha256:b9fcb7392fc2213edfb91460a57b4c39ed9fed57bc516068869e56460dd5beab",
            provenance.SupersedesHash);
    }

    [Fact]
    public void AnAbsorbedModificationRemainsCitableButIsNotCurrent()
    {
        var corpus = Corpus.Read();

        RuleRecord current = corpus.Resolve("rr:move.5", "1.8");
        RuleRecord absorbed = Assert.IsType<RuleRecord>(corpus.Find("ruling:1bf319f7418a86d7"));
        Modification provenance = Assert.Single(
            corpus.Modifications,
            item => item.Id == absorbed.Id);

        Assert.Equal("rr:move.5", current.Id);
        Assert.Equal("modification", absorbed.Kind);
        Assert.Equal("1.8", provenance.AbsorbedIn);
        Assert.Contains("moved damage is considered to be dealt", absorbed.Fragment, StringComparison.Ordinal);
        Assert.Contains(absorbed.Id, RuleCitations.Citable);
    }

    [Fact]
    public void ABaseCitationSeesTheModificationThatSupersedesIt()
    {
        var corpus = Corpus.Read();

        Edge edge = Assert.Single(
            corpus.ReferencedBy("rr:reveal.1"),
            item => item.From == "ruling:121d5b7b5dcec377");

        Assert.Equal("rr:reveal.1", edge.To);
        Assert.False(string.IsNullOrWhiteSpace(edge.Why));
    }

    [Fact]
    public void AStaleSupersededHashFailsClosed()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"marvel-rules-modifications-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            string graph = File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json"))
                .Replace(
                    "sha256:5282b33dc7863472de025b93a538b6183ec42fe8c54814c606637b4117930208",
                    "sha256:0000000000000000000000000000000000000000000000000000000000000000",
                    StringComparison.Ordinal);
            string graphPath = Path.Combine(root, "rules-graph.json");
            File.WriteAllText(graphPath, graph);

            var error = Assert.Throws<InvalidDataException>(() => Corpus.Read(
                RepositoryPaths.Dataset("rules-reference", "index.json"),
                graphPath,
                RepositoryPaths.Dataset("rulings", "rulings.json")));

            Assert.Contains("not sha256:5282b33d", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ARevisedRulingAnswerRequiresTheRelationshipToBeReaudited()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"marvel-rules-modifications-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            string rulings = File.ReadAllText(
                RepositoryPaths.Dataset("rulings", "rulings.json"))
                .Replace(
                    "sha256:29a9e917bc50dd403854ff5a449b3eed5987b6110d4d159279ff8230fc129a9c",
                    "sha256:0000000000000000000000000000000000000000000000000000000000000000",
                    StringComparison.Ordinal);
            string rulingsPath = Path.Combine(root, "rulings.json");
            File.WriteAllText(rulingsPath, rulings);

            var error = Assert.Throws<InvalidDataException>(() => Corpus.Read(
                RepositoryPaths.Dataset("rules-reference", "index.json"),
                RepositoryPaths.Dataset("rules-graph.json"),
                rulingsPath));

            Assert.Contains("pins ruling sha256:29a9e917", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void AVersionWithoutAVendoredBaseIsNotGuessed()
    {
        var corpus = Corpus.Read();

        var error = Assert.Throws<InvalidOperationException>(
            () => corpus.Resolve("rr:reveal.1", "1.7"));

        Assert.Equal(
            "Rules Reference v1.8 is the only vendored base; cannot resolve v1.7",
            error.Message);
    }

    [Fact]
    public void TheLatestRrgScopeWinsWithoutUsingPublicationOrder()
    {
        string? selected = Corpus.SelectCurrent(
            [Modification("ruling:old", "1.5"), Modification("ruling:new", "1.7-1.8")],
            "1.8",
            "rr:example");

        Assert.Equal("ruling:new", selected);
    }

    [Fact]
    public void AbsorbingTheLatestModificationDoesNotResurrectAnOlderOne()
    {
        string? selected = Corpus.SelectCurrent(
            [
                Modification("ruling:old", "1.5"),
                Modification("ruling:new", "1.7-1.8", absorbedIn: "1.8"),
            ],
            "1.8",
            "rr:example");

        Assert.Null(selected);
    }

    [Fact]
    public void AnAbsorbedPeerDoesNotMakeTheLiveModificationAmbiguous()
    {
        string? selected = Corpus.SelectCurrent(
            [
                Modification("ruling:absorbed", "1.7-1.8", absorbedIn: "1.8"),
                Modification("ruling:live", "1.7-1.8"),
            ],
            "1.8",
            "rr:example");

        Assert.Equal("ruling:live", selected);
    }

    [Fact]
    public void TwoAbsorbedPeersReturnAuthorityToTheBase()
    {
        string? selected = Corpus.SelectCurrent(
            [
                Modification("ruling:first", "1.7-1.8", absorbedIn: "1.8"),
                Modification("ruling:second", "1.7-1.8", absorbedIn: "1.8"),
            ],
            "1.8",
            "rr:example");

        Assert.Null(selected);
    }

    [Fact]
    public void TwoModificationsAtTheSameRrgScopeFailInsteadOfUsingFileOrder()
    {
        var error = Assert.Throws<InvalidDataException>(() => Corpus.SelectCurrent(
            [Modification("ruling:first", "1.7-1.8"), Modification("ruling:second", "1.7-1.8")],
            "1.8",
            "rr:example"));

        Assert.Equal(
            "rr:example has 2 current modifications from RRG 1.7",
            error.Message);
    }

    private static Modification Modification(
        string id,
        string scope,
        string? absorbedIn = null) => new(
            id,
            "rr:example",
            "sha256:base",
            absorbedIn,
            "because",
            "source",
            "via",
            scope,
            "2026-01",
            "sha256:ruling");
}
