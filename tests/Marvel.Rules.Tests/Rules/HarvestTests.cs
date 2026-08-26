using System.Text.Json;
using Marvel.Rules.Harvest;
using HarvestRecord = Marvel.Rules.Harvest.Record;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Rules;

/// <summary>
/// The rules the Rules Reference harvester reads the document by.
/// </summary>
/// <remarks>
/// <para>
/// <c>tools/Marvel.Rules.Harvest</c> reads a PDF this repository does not hold
/// and cannot hold — the document is copyrighted — so nothing here runs it.
/// What is held instead is everything about it that does not need the file:
/// how a heading becomes a citation id, and how the document's emphasis
/// becomes Markdown.
/// </para>
/// <para>
/// <b>The id rule is the one that matters.</b> Every <c>[Rule("rr:…")]</c> in
/// the suite is an id, and an id is derived from a heading — so a harvester
/// that slugged one differently would renumber the citations of a rule that
/// had not changed. It is held against all 262 headings the vendored snapshot
/// carries.
/// </para>
/// </remarks>
public sealed class HarvestTests
{
    [Fact]
    public void EveryHeadingSlugsToTheIdTheSnapshotGaveIt()
    {
        using var index = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("rules-reference", "index.json")));

        int checkedIds = 0;
        foreach (var entry in index.RootElement.GetProperty("entries").EnumerateArray())
        {
            string id = entry.GetProperty("id").GetString()!;
            if (id.Contains('.', StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Equal(id, Entry.Slug(entry.GetProperty("title").GetString()!));
            checkedIds += 1;
        }

        Assert.Equal(262, checkedIds);
    }

    [Theory]
    // A parenthesis separates, so a disambiguating suffix is part of the id.
    [InlineData("ATTACK (ENEMY ACTIVATION)", "rr:attack-enemy-activation")]
    // So does an apostrophe, and so does a comma.
    [InlineData("PLAYER’S PLAY AREA", "rr:player-s-play-area")]
    [InlineData("CONFUSE, CONFUSED", "rr:confuse-confused")]
    // Quotation marks go, because the document quotes a word it is defining.
    [InlineData("“X” (VALUE)", "rr:x-value")]
    [InlineData("USES (X “TYPE”)", "rr:uses-x-type")]
    // An icon printed beside a heading names the glyph, not the entry.
    [InlineData("CRISIS ICON ([crisis])", "rr:crisis-icon")]
    public void AHeadingBecomesAnId(string title, string id) =>
        Assert.Equal(id, Entry.Slug(title));

    [Fact]
    public void EmphasisSpanningALineBreakIsOneAside()
    {
        // A paragraph is set across several lines and its emphasis does not
        // stop at the end of one, so an aside spanning three lines arrives as
        // three italic runs. Emitted separately they become three asides.
        string written = Markdown.Of(
        [
            new Run("plain ", false, false),
            new Run("an aside ", false, true),
            new Run("across lines", false, true),
        ]);

        Assert.Equal("plain *an aside across lines*", written);
    }

    [Fact]
    public void PunctuationLeftOnItsOwnIsNotEmphasised()
    {
        // An icon is set in a face of its own, so a bracketed one splits the
        // italic around it into three runs and leaves each bracket alone.
        // `*(*` is emphasis around nothing a reader can see.
        Assert.Equal(
            "consequential damage icons ([consequential-damage])",
            Markdown.Of(
            [
                new Run("consequential damage icons ", false, false),
                new Run("(", false, true),
                new Run("[consequential-damage]", false, false),
                new Run(")", false, true),
            ]));

        // Where the aside is not split, the brackets are part of it.
        Assert.Equal(
            "*(an aside)*",
            Markdown.Of(
            [
                new Run("(", false, true),
                new Run("an aside", false, true),
                new Run(")", false, true),
            ]));
    }

    [Fact]
    public void AWordSetBoldInsideAnAsideKeepsOnlyItsWeight()
    {
        // The aside is already italic on either side of it, so what the word
        // adds is the weight alone. Emitting both would close the aside and
        // reopen it around a word that is not a second aside.
        Assert.Equal(
            "*that alter-ego does* **not** *get 1 ATK*",
            Markdown.Of(
            [
                new Run("that alter-ego does ", false, true),
                new Run("not", true, true),
                new Run(" get 1 ATK", false, true),
            ]));
    }

    [Fact]
    public void ALineBreakInsideAHyphenatedWordIsNotASpace()
    {
        // "Non-" at the end of a line and "bolded" at the start of the next is
        // one word on the page.
        var joined = Markdown.Join(
            [new Run("format. Non-", false, false)],
            [new Run("bolded text", false, false)]);

        Assert.Equal("format. Non-bolded text", Markdown.Of(joined));
    }

    [Fact]
    public void ALineBreakBetweenTwoWordsIsASpace()
    {
        var joined = Markdown.Join(
            [new Run("the first line", false, false)],
            [new Run("and the second", false, false)]);

        Assert.Equal("the first line and the second", Markdown.Of(joined));
    }

    [Fact]
    public void AnOverprintedHeadingIsReadOnce()
    {
        // The document draws its section titles with the same glyphs
        // overprinted a fraction of a point apart, so what comes out of the
        // file is every letter twice.
        Assert.Equal("OVERVIEW", Pages.Struck("OOVVEERRVVIIEEWW"));

        // A word with a real double letter in it has an odd run somewhere,
        // which is what makes undoubling safe.
        Assert.Equal("ALL", Pages.Struck("ALL"));
        Assert.Equal("OFF", Pages.Struck("OFF"));
    }

    [Fact]
    public void ARecordsFragmentIsItsFirstSentence()
    {
        // What a fragment is for: making a citation legible in a diff. The
        // whole clause is what the hash is over.
        var record = new HarvestRecord(
            "rr:damage.1",
            ["DAMAGE", "clause 1"],
            "Damage on an identity or villain is tracked by a hit point dial. If such a "
            + "character takes damaged, reduce its dial by the amount of damage that it took.");

        Assert.Equal(
            "Damage on an identity or villain is tracked by a hit point dial.",
            record.Fragment);
    }

    [Fact]
    public void ARecordsHashIsOverItsTextWithoutTheEmphasis()
    {
        // So that the document re-setting a word in bold does not read as the
        // rule changing.
        var plain = new HarvestRecord("rr:x", ["X"], "A rule about not doing that.");
        var emphasised = new HarvestRecord("rr:x", ["X"], "A rule about **not** doing that.");

        Assert.Equal(plain.Hash, emphasised.Hash);
        Assert.StartsWith("sha256:", plain.Hash, StringComparison.Ordinal);
    }
}
