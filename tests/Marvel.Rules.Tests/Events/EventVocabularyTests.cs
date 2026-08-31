using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Marvel.Rules.Events;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Events;

/// <summary>
/// The event vocabulary, held against the document that declares it —
/// <c>docs/event-stream.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The document is the contract, so the document is what the code is checked
/// against.</b> It carries a table of nine kinds and the payload each one
/// spells. The derivable and emitted-only tables make different claims, and a
/// table nobody verifies is a table that drifts: a field renamed in C# and not
/// in the prose leaves two contracts, both confident.
/// </para>
/// <para>
/// <b>None of this is a rule.</b> The Rules Reference says nothing about wire
/// formats, so every claim here is the engine's own choice. What makes them
/// worth pinning is not authority but stability — a client reads these names,
/// and a name that moves silently breaks it.
/// </para>
/// </remarks>
public sealed partial class EventVocabularyTests
{
    private const string DerivableHeading = "### Derivable events";
    private const string EmittedOnlyHeading = "### Emitted-only events";

    /// <summary>The keys every event carries whatever its kind.</summary>
    private static readonly string[] Universal = ["kind", "trigger", "verb"];

    /// <summary>One of each corpus-derived kind.</summary>
    private static readonly GameEvent[] Derivable =
    [
        new CardsCreated(new AreaRef("HandsArea", 0, -1, "a1"), [new CreatedCard(7, "01001a")]),
        new CardsMoved(
            new AreaRef("PlayerDeck", 0, -1, "a2"),
            new AreaRef("HandsArea", 0, -1, "a1"),
            [new Landing(7, 3)]),
        new AreaReordered(new AreaRef("PlayerDeck", 0, -1, "a2"), [3, 1, 2]),
        new CardFormChanged(7, "01001a", "01001b"),
        new CardsFlipped([7, 8], true),
        new CardAttached(9, 7),
        new CardDetached(9, 7),
        new ControlChanged(9, 0, 1),
        new FieldSet(7, "health", 10, 8),
    ];

    /// <summary>One of each kind the engine emits but a digest cannot derive.</summary>
    private static readonly GameEvent[] EmittedOnly =
    [
        new PlayAreaJoined(1, 4),
    ];

    /// <summary>Every serialisable kind, with distinguishable payload values.</summary>
    private static readonly GameEvent[] OneOfEach = [.. Derivable, .. EmittedOnly];

    [Fact]
    public void TheDerivableKindsAreExactlyTheMeasuredNine()
    {
        // Exact equality preserves the measured claim. Moving an emitted-only
        // kind into this table would say the corpus produced something it
        // cannot see; dropping one of the nine would lose an observed shape.
        var documented = Documented(DerivableHeading).Keys.ToHashSet(StringComparer.Ordinal);
        var tested = Derivable.Select(Kind).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(9, tested.Count);
        Assert.Equal(tested, documented);
    }

    [Fact]
    public void TheEmittedOnlyKindsAreExactlyTheDocumentedOnes()
    {
        // This class is a separate assertion: PlayAreaJoined is justified by
        // the engine operation and published scenario rule, not by the frozen
        // corpus. It must not weaken the nine-kind claim above.
        var documented = Documented(EmittedOnlyHeading).Keys.ToHashSet(StringComparer.Ordinal);
        var tested = EmittedOnly.Select(Kind).ToHashSet(StringComparer.Ordinal);

        Assert.Single(tested);
        Assert.Equal(tested, documented);
    }

    [Fact]
    public void TheSerialisedUnionIsExactlyTheDeclaredHierarchy()
    {
        // `JsonDerivedType` is the set the serialiser dispatches on. The union
        // of the two documented classes is the public wire vocabulary.
        var documented = Documented().Keys.ToHashSet(StringComparer.Ordinal);
        var tested = OneOfEach.Select(Kind).ToHashSet(StringComparer.Ordinal);
        var declared = typeof(GameEvent)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(attribute => (string)attribute.TypeDiscriminator!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(tested, documented);
        Assert.Equal(tested, declared);
    }

    [Fact]
    public void EachKindSerialisesTheKeysTheDocumentSpells()
    {
        // Payload for payload, not just kind for kind. This is the half that
        // rots quietly: renaming `host` to `attached_to` in C# would leave the
        // document describing a field no event carries.
        var documented = Documented();

        foreach (var happened in OneOfEach)
        {
            string kind = Kind(happened);
            var written = JsonSerializer.Deserialize<JsonElement>(EventJson.Write(happened));

            var payload = written.EnumerateObject()
                .Select(property => property.Name)
                .Where(name => !Universal.Contains(name, StringComparer.Ordinal))
                .ToHashSet(StringComparer.Ordinal);

            Assert.Equal(documented[kind], payload);
        }
    }

    [Fact]
    public void EveryEventCarriesKindAndTriggerAndVerb()
    {
        // "Every event also carries `kind`, plus `trigger` and `verb` — the
        // engine's own names for why the transition happened. Those are the
        // half a digest can never show."
        foreach (var happened in OneOfEach)
        {
            var written = JsonSerializer.Deserialize<JsonElement>(EventJson.Write(happened));

            foreach (string key in Universal)
            {
                Assert.True(
                    written.TryGetProperty(key, out _),
                    $"{Kind(happened)} carries no '{key}'");
            }
        }
    }

    [Fact]
    public void EveryKindSurvivesARoundTrip()
    {
        // Write then read gives back an equal record. Records compare by value,
        // so this covers the payload as well as the discriminator -- a `from`
        // and `to` read back the wrong way round would fail here.
        foreach (var happened in OneOfEach)
        {
            var again = EventJson.Read(EventJson.Write(happened with
            {
                Trigger = "WhenPlayerInTurn",
                Verb = "Play",
            }));

            Assert.Equal("WhenPlayerInTurn", again.Trigger);
            Assert.Equal("Play", again.Verb);
            Assert.Equal(Kind(happened), Kind(again));
        }
    }

    [Fact]
    public void KeysAreSnakeCaseAndNotCamel()
    {
        // `face_up`, not `faceUp`. One key is enough to pin the policy, and
        // `CardsFlipped` is the only kind whose payload has two words in it.
        string written = EventJson.Write(new CardsFlipped([7], true));

        Assert.Contains("\"face_up\"", written, StringComparison.Ordinal);
        Assert.DoesNotContain("faceUp", written, StringComparison.Ordinal);
    }

    [Fact]
    public void AFieldThatIsGoneIsNotAFieldThatIsZero()
    {
        // "A field that is gone means the card no longer registers it at all,
        // which is how a granted trait expires; serialising that as `0` would
        // say the card still has the trait, at zero."
        string gone = EventJson.Write(new FieldSet(7, "t_GENIUS", 1, null));
        string zero = EventJson.Write(new FieldSet(7, "t_GENIUS", 1, 0));

        Assert.NotEqual(gone, zero);
        Assert.Contains("\"to\":null", gone, StringComparison.Ordinal);
        Assert.Contains("\"to\":0", zero, StringComparison.Ordinal);

        Assert.Null(Assert.IsType<FieldSet>(EventJson.Read(gone)).To);
        Assert.Equal(0, Assert.IsType<FieldSet>(EventJson.Read(zero)).To);
    }

    [Fact]
    public void ReadingSomethingThatIsNotAnEventSaysSoRatherThanReturningNull()
    {
        Assert.Throws<JsonException>(() => EventJson.Read("null"));
    }

    /// <summary>The kind discriminator the serialiser writes for this event.</summary>
    private static string Kind(GameEvent happened) =>
        JsonSerializer.Deserialize<JsonElement>(EventJson.Write(happened))
            .GetProperty("kind")
            .GetString()!;

    /// <summary>
    /// The vocabulary table in <c>docs/event-stream.md</c>, as
    /// <c>kind -&gt; payload keys</c>.
    /// </summary>
    /// <remarks>
    /// Parsed rather than transcribed. A copy here would be a third statement
    /// of the contract, free to drift from both the document and the code.
    /// </remarks>
    private static Dictionary<string, HashSet<string>> Documented() =>
        Documented(DerivableHeading)
            .Concat(Documented(EmittedOnlyHeading))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    /// <summary>One named vocabulary table in the event-stream document.</summary>
    private static Dictionary<string, HashSet<string>> Documented(string heading)
    {
        string document = File.ReadAllText(
            Path.Combine(RepositoryPaths.Root, "docs", "event-stream.md"));
        int start = document.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"the '{heading}' section was not found in the document");
        start += heading.Length;
        int end = document.IndexOf("\n### ", start, StringComparison.Ordinal);
        string section = end < 0 ? document[start..] : document[start..end];

        var table = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (Match row in Row().Matches(section))
        {
            table[row.Groups[1].Value] =
            [
                .. row.Groups[2].Value
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(cell => cell.Trim('`')),
            ];
        }

        Assert.True(table.Count > 0, $"the vocabulary table under '{heading}' was not found");
        return table;
    }

    // A row of the vocabulary table: one backticked kind, then a cell of
    // backticked keys. `| kind | payload |` and the `|---|---|` rule do not
    // match, because neither has a backtick in the first cell.
    [GeneratedRegex(@"^\| `(\w+)` \| ([^|]+) \|$", RegexOptions.Multiline)]
    private static partial Regex Row();
}
