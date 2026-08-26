using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Marvel.Rules.Harvest;

/// <summary>
/// Writing the two artefacts the dataset is: an index for machines and a
/// document per entry for everyone else.
/// </summary>
/// <remarks>
/// <b>The spelling is our choice.</b> The Rules Reference has no serialisation
/// of its own, so the layout below — two-space indent, keys in this order,
/// entries in document order — is this repository's, and the only property it
/// has to keep is holding still.
/// </remarks>
public static class Emit
{
    /// <summary>Writes the whole dataset into a directory.</summary>
    /// <param name="entries">The harvested entries.</param>
    /// <param name="into">Where to write.</param>
    /// <param name="version">The Rules Reference version.</param>
    /// <param name="icons">The glyph legend.</param>
    public static void Write(
        IReadOnlyList<Entry> entries,
        string into,
        string version,
        IReadOnlyDictionary<string, string> icons)
    {
        Directory.CreateDirectory(Path.Combine(into, "entries"));

        var known = Names(entries);
        foreach (var entry in entries)
        {
            File.WriteAllText(
                Path.Combine(into, "entries", $"{entry.Id["rr:".Length..]}.md"),
                Document(entry, version, known),
                new UTF8Encoding(false));
        }

        File.WriteAllText(
            Path.Combine(into, "index.json"), Index(entries, version, icons, known),
            new UTF8Encoding(false));
    }

    /// <summary>
    /// Which of an entry's cross-references name an entry that exists.
    /// </summary>
    /// <remarks>
    /// The ones that do not are kept rather than dropped, because what a
    /// snapshot does <b>not</b> carry is the thing a reader most needs told:
    /// the appendices are cited twenty times and are not parsed, and a silently
    /// shorter list would read as the document not citing them.
    /// </remarks>
    /// <param name="entry">An entry.</param>
    /// <param name="known">Every entry id.</param>
    public static (List<string> Resolved, List<string> Unresolved) References(
        Entry entry, Dictionary<string, string> known)
    {
        var resolved = new List<string>();
        var unresolved = new List<string>();
        foreach (string named in entry.SeeAlso)
        {
            if (known.TryGetValue(Entry.Slug(named.ToUpperInvariant()), out string? id))
            {
                resolved.Add(id);
            }
            else
            {
                unresolved.Add(named);
            }
        }

        // In the order the document prints them, which is not alphabetical:
        // "See also: Boost, Attack (Enemy Activation), Scheme (Enemy
        // Activation), Minion..." reads as the author grouped it.
        return (resolved, unresolved);
    }

    /// <summary>
    /// Every name an entry can be cited by, mapped to its id.
    /// </summary>
    /// <remarks>
    /// <b>A compound title is citable by either half.</b> The entry is titled
    /// "VILLAIN, VILLAIN DECK" and eleven entries cross-reference it as
    /// "Villain"; "CONFUSE, CONFUSED" is cited both ways. Without the aliases
    /// those references resolve to nothing, and what is meant to record the
    /// snapshot's gaps records the harvester's instead.
    /// </remarks>
    /// <param name="entries">The harvested entries.</param>
    public static Dictionary<string, string> Names(IReadOnlyList<Entry> entries)
    {
        var known = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            known[entry.Id] = entry.Id;
        }

        // Second, so that a half-title never displaces a whole one: "SCHEME
        // (CARD TYPE)" and "SCHEME (ENEMY ACTIVATION)" are two entries and
        // neither is "SCHEME".
        foreach (var entry in entries)
        {
            foreach (string half in entry.Title.Split(
                ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                known.TryAdd(Entry.Slug(half), entry.Id);
            }
        }

        return known;
    }

    private static string Document(
        Entry entry, string version, Dictionary<string, string> known)
    {
        var (resolved, unresolved) = References(entry, known);
        var written = new StringBuilder();

        written.Append("---\n");
        written.Append(CultureInfo.InvariantCulture, $"id: \"{entry.Id}\"\n");
        written.Append(CultureInfo.InvariantCulture, $"title: \"{Escaped(entry.Title)}\"\n");
        written.Append("document: \"Rules Reference\"\n");
        written.Append(CultureInfo.InvariantCulture, $"version: \"{version}\"\n");
        written.Append(CultureInfo.InvariantCulture, $"page: {entry.Page}\n");
        // **An entry that is only a pointer is not a rule.** "COUNTER / See:
        // All-Purpose Counter" has no text of its own, so it has no hash to
        // fingerprint and nothing to cross-reference -- what it has is a
        // redirect, which is a different claim from "these are related".
        bool redirect = entry.Opening.Count == 0
            && entry.Clauses.Count == 0
            && entry.Steps.Count == 0
            && entry.SeeAlso.Count > 0;

        if (redirect)
        {
            written.Append(
                CultureInfo.InvariantCulture,
                $"redirect: \"{Escaped(string.Join(", ", entry.SeeAlso))}\"\n");
            written.Append("see_also: []\n");
        }
        else
        {
            written.Append(CultureInfo.InvariantCulture, $"hash: \"{Head(entry).Hash}\"\n");
            if (entry.Steps.Count > 0)
            {
                written.Append(CultureInfo.InvariantCulture, $"steps: {entry.Steps.Count}\n");
            }

            // The front matter carries only what resolves. What does not is in
            // the index, which is where a reader goes to ask what the snapshot
            // is missing; a document is where they go to read the rule.
            written.Append(CultureInfo.InvariantCulture, $"see_also: [{Quoted(resolved)}]\n");
        }

        written.Append("---\n\n");
        written.Append(CultureInfo.InvariantCulture, $"# {entry.Title}\n");

        foreach (string paragraph in entry.Opening)
        {
            written.Append(CultureInfo.InvariantCulture, $"\n{paragraph}\n");
        }

        string anchor = entry.Id["rr:".Length..];
        foreach (var step in entry.Steps)
        {
            string at = step.Number.ToString(CultureInfo.InvariantCulture);
            written.Append(CultureInfo.InvariantCulture, $"\n<a id=\"{anchor}-step-{at}\"></a>\n");
            written.Append(CultureInfo.InvariantCulture, $"{at}. {step.Text}\n");
            for (int under = 0; under < step.Substeps.Count; under++)
            {
                char letter = (char)('a' + under);
                written.Append(
                    CultureInfo.InvariantCulture,
                    $"    <a id=\"{anchor}-step-{at}-{letter}\"></a>\n");
                written.Append(
                    CultureInfo.InvariantCulture, $"    - {step.Substeps[under]}\n");
            }
        }

        foreach (var clause in entry.Clauses)
        {
            string at = clause.Number.ToString(CultureInfo.InvariantCulture);
            written.Append(CultureInfo.InvariantCulture, $"\n<a id=\"{anchor}-{at}\"></a>\n");
            written.Append(CultureInfo.InvariantCulture, $"{at}. {clause.Text}\n");
            for (int under = 0; under < clause.Qualifications.Count; under++)
            {
                string number = (under + 1).ToString(CultureInfo.InvariantCulture);
                written.Append(
                    CultureInfo.InvariantCulture, $"    <a id=\"{anchor}-{at}-{number}\"></a>\n");
                written.Append(
                    CultureInfo.InvariantCulture, $"    - {clause.Qualifications[under]}\n");
            }
        }

        if (resolved.Count > 0 || unresolved.Count > 0)
        {
            // A redirect naming several entries is not a link, because there
            // is nowhere single to go: "ATK / See: Attack (Player Ability
            // Type), Basic Power" is telling a reader that the value is
            // described in two places, and `redirect:` in the front matter is
            // where a machine reads that.
            bool linked = !redirect || entry.SeeAlso.Count == 1;
            var links = entry.SeeAlso.Select(named =>
                linked
                && known.TryGetValue(Entry.Slug(named.ToUpperInvariant()), out string? id)
                    ? $"[{named}]({id["rr:".Length..]}.md)"
                    : named);

            written.Append(
                CultureInfo.InvariantCulture,
                $"\n{(redirect ? "See:" : "**See also:**")} {string.Join(", ", links)}\n");
        }

        return written.ToString();
    }

    private static string Index(
        IReadOnlyList<Entry> entries,
        string version,
        IReadOnlyDictionary<string, string> icons,
        Dictionary<string, string> known)
    {
        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, options))
        {
            writer.WriteStartObject();
            writer.WriteString("document"u8, "Marvel Champions: The Card Game -- Rules Reference");
            writer.WriteString("version"u8, version);
            writer.WriteString("tier"u8, "rr");
            writer.WriteNumber("entry_count"u8, entries.Count);
            writer.WriteNumber("record_count"u8, entries.Sum(entry => entry.Records().Count()));

            writer.WriteStartObject("icons"u8);
            foreach (var (glyph, named) in icons.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteString(glyph, named);
            }

            writer.WriteEndObject();

            writer.WriteStartArray("entries"u8);
            foreach (var entry in entries)
            {
                var (resolved, unresolved) = References(entry, known);
                foreach (var record in entry.Records())
                {
                    writer.WriteStartObject();
                    writer.WriteString("id"u8, record.Id);
                    writer.WriteString("title"u8, entry.Title);

                    writer.WriteStartArray("path"u8);
                    foreach (string step in record.Path)
                    {
                        writer.WriteStringValue(step);
                    }

                    writer.WriteEndArray();

                    writer.WriteNumber("page"u8, entry.Page);
                    writer.WriteString("fragment"u8, record.Fragment);
                    writer.WriteString("hash"u8, record.Hash);

                    if (record.Id == entry.Id)
                    {
                        writer.WriteStartArray("see_also"u8);
                        foreach (string id in resolved)
                        {
                            writer.WriteStringValue(id);
                        }

                        writer.WriteEndArray();

                        writer.WriteStartArray("see_also_unresolved"u8);
                        foreach (string named in unresolved)
                        {
                            writer.WriteStringValue(named);
                        }

                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        // Line feeds on every platform: `Utf8JsonWriter` indents with
        // `Environment.NewLine`, and a dataset that differs between Windows and
        // Linux is not a dataset.
        return Encoding.UTF8.GetString(buffer.WrittenSpan)
            .Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private static Record Head(Entry entry) => entry.Records().First();

    // The front matter is read by anything that reads YAML, and a curly quote
    // in a double-quoted scalar is legal but not portable -- so it is escaped
    // the way JSON escapes it, which YAML also understands.
    private static string Escaped(string text)
    {
        var written = new StringBuilder();
        foreach (char letter in text)
        {
            if (letter == '\\') { written.Append("\\\\"); }
            else if (letter == '"') { written.Append("\\\""); }
            else if (letter > '~')
            {
                written.Append(CultureInfo.InvariantCulture, $"\\u{(int)letter:x4}");
            }
            else { written.Append(letter); }
        }

        return written.ToString();
    }

    private static string Quoted(IEnumerable<string> items) =>
        string.Join(", ", items.Select(item => $"\"{Escaped(item)}\""));
}
