using System.Globalization;

namespace Marvel.Rules.Index;

/// <summary>
/// Queries the vendored Rules Reference, its authored relationships, and rules modifications.
/// </summary>
/// <remarks>
/// <para>
/// <c>datasets/rules-graph.json</c> is a hand-authored graph of which rule
/// qualifies which, and until this existed nothing in the repository read it —
/// not a test, not a tool, not the engine. <c>refs</c> is what it was written
/// for, and the reverse direction is the half that cannot be read off the file.
/// </para>
/// <para>
/// <c>citations</c> is the other side of <c>[Rule]</c>. Every clause of the
/// Rules Reference is enumerable and so is every citation the suite makes, so
/// "what has this engine never been held to?" is a list rather than something
/// found by mutating code and seeing what survives. It is a measurement and
/// deliberately not a gate — see <c>docs/rules-citations.md</c>.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        var rest = args.Skip(1).ToArray();
        return (args.Length == 0 ? "" : args[0]) switch
        {
            "refs" => Refs(rest),
            "resolve" => Resolve(rest),
            "citations" => Report(rest),
            _ => Usage(),
        };
    }

    private static int Usage()
    {
        Console.Error.WriteLine(
            """
            Queries the vendored Rules Reference and the authored rule graph.

              refs <rr:id>            what it names, and what names it
              refs --orphans          authored edges naming no citable rule

              resolve <rr:id> [rrg]   current text for a base rule (default: vendored RR)

              citations               how much of the Rules Reference is cited
              citations --cited       every citation, by rule
              citations --uncited     every entry nothing cites
              citations --sort        order --uncited by clause count, widest first
            """);
        return 2;
    }

    private static int Refs(string[] args)
    {
        var corpus = Corpus.Read();

        if (args.Contains("--orphans"))
        {
            // The gate this tool is not: `RulesGraphTests` fails the build on
            // these. Listed here as well because a person fixing them wants
            // them on one screen, not in an assertion message.
            var broken = corpus.Edges
                .SelectMany(edge => new[] { edge.From, edge.To })
                .Where(id => !corpus.Knows(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            foreach (string id in broken)
            {
                Console.WriteLine(id);
            }

            Console.Error.WriteLine(
                $"{broken.Count} of {corpus.Edges.Count} edges name no rule in "
                + $"Rules Reference v{corpus.Version}");
            return broken.Count == 0 ? 0 : 1;
        }

        if (args.Length == 0)
        {
            return Usage();
        }

        string wanted = args[0];
        if (corpus.Find(wanted) is not { } record)
        {
            Console.Error.WriteLine(
                $"Rules Reference v{corpus.Version} has no rule '{wanted}'");
            return 1;
        }

        Console.WriteLine($"{record.Id}  {record.Title}");
        Console.WriteLine($"  {Wrap(record.Fragment)}");
        if (record.Kind == "modification")
        {
            Modification modification = corpus.Modifications.Single(item => item.Id == record.Id);
            Console.WriteLine(
                $"  {modification.Source}; RRG {modification.Scope}; "
                + $"observed {modification.Observed ?? "unavailable"}");
            Console.WriteLine(modification.AbsorbedIn is null
                ? "  current until superseded or absorbed"
                : $"  absorbed into Rules Reference v{modification.AbsorbedIn}");
        }

        var names = corpus.References(wanted);
        var named = corpus.ReferencedBy(wanted);

        Console.WriteLine();
        Console.WriteLine(names.Count == 0
            ? "names nothing — this is a base rule"
            : "names:");
        foreach (var edge in names)
        {
            Console.WriteLine($"  -> {edge.To}  {corpus.Find(edge.To)?.Title}");
            Console.WriteLine($"     {Wrap(edge.Why, "     ")}");
        }

        Console.WriteLine();
        Console.WriteLine(named.Count == 0
            ? "named by nothing — no authored rule qualifies this"
            : "named by:");
        foreach (var edge in named)
        {
            Console.WriteLine($"  <- {edge.From}  {corpus.Find(edge.From)?.Title}");
            Console.WriteLine($"     {Wrap(edge.Why, "     ")}");
        }

        return 0;
    }

    private static int Resolve(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            return Usage();
        }

        var corpus = Corpus.Read();
        string version = args.Length == 2 ? args[1] : corpus.Version;
        try
        {
            Record record = corpus.Resolve(args[0], version);
            Console.WriteLine($"{args[0]} @ Rules Reference v{version}");
            Console.WriteLine($"current: {record.Id}");
            Console.WriteLine($"hash:    {record.Hash}");
            if (record.BaseId is not null)
            {
                Modification modification = corpus.Modifications.Single(item => item.Id == record.Id);
                Console.WriteLine($"source:  {modification.Source}");
                Console.WriteLine($"via:     {modification.Via}");
                Console.WriteLine($"scope:   {modification.Scope}");
                Console.WriteLine($"observed:{(modification.Observed is null ? " unavailable" : " " + modification.Observed)}");
            }

            Console.WriteLine();
            Console.WriteLine(Wrap(record.Fragment));
            return 0;
        }
        catch (Exception error) when (error is InvalidOperationException
            or InvalidDataException
            or KeyNotFoundException)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static int Report(string[] args)
    {
        var corpus = Corpus.Read();
        var citations = Citations.Read();
        var cited = citations
            .Select(citation => citation.Id)
            .ToHashSet(StringComparer.Ordinal);
        var citedEntries = cited
            .Where(id => id.StartsWith("rr:", StringComparison.Ordinal))
            .Select(Corpus.EntryOf)
            .ToHashSet(StringComparer.Ordinal);

        var entries = corpus.Records.Where(record => record.Clauses > 0).ToList();

        if (args.Contains("--cited"))
        {
            foreach (var group in citations
                .GroupBy(citation => citation.Id, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                string known = corpus.Knows(group.Key) ? "" : "   (no such rule)";
                Console.WriteLine($"{group.Key}{known}");
                foreach (string site in group
                    .Select(citation => citation.Site)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(site => site, StringComparer.Ordinal))
                {
                    Console.WriteLine($"    {site}");
                }
            }

            return 0;
        }

        if (args.Contains("--uncited"))
        {
            var uncited = entries.Where(entry => !citedEntries.Contains(entry.Id));
            uncited = args.Contains("--sort")
                ? uncited
                    .OrderByDescending(entry => entry.Clauses)
                    .ThenBy(entry => entry.Id, StringComparer.Ordinal)
                : uncited.OrderBy(entry => entry.Id, StringComparer.Ordinal);

            foreach (var entry in uncited)
            {
                Console.WriteLine(
                    $"{entry.Clauses,4}  {entry.Id,-44}  {entry.Title}");
            }

            return 0;
        }

        int records = corpus.Records.Count;
        var modificationIds = corpus.Modifications
            .Select(modification => modification.Id)
            .ToHashSet(StringComparer.Ordinal);
        Console.WriteLine($"Rules Reference v{corpus.Version}");
        Console.WriteLine();
        Console.WriteLine(Line("entries", citedEntries.Count, entries.Count));
        Console.WriteLine(Line("citable records", cited.Count, records));
        Console.WriteLine(Line(
            "modifications",
            cited.Count(modificationIds.Contains),
            modificationIds.Count));
        Console.WriteLine();
        Console.WriteLine($"  citations made  {citations.Count}");
        return 0;
    }

    private static string Line(string label, int cited, int total)
    {
        double share = total == 0 ? 0 : 100.0 * cited / total;
        return string.Format(
            CultureInfo.InvariantCulture,
            "  {0,-18}{1,5} / {2,-5} cited ({3:0.0}%)",
            label,
            cited,
            total,
            share);
    }

    // A fragment is a whole clause and some of them run to several lines of
    // prose. Folded rather than truncated: the point of printing it is that the
    // reader can tell whether this is the rule they meant.
    private static string Wrap(string text, string indent = "  ", int width = 76)
    {
        var lines = new List<string>();
        var line = new System.Text.StringBuilder();
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line.ToString());
                line.Clear();
            }

            if (line.Length > 0)
            {
                line.Append(' ');
            }

            line.Append(word);
        }

        if (line.Length > 0)
        {
            lines.Add(line.ToString());
        }

        return string.Join(Environment.NewLine + indent, lines);
    }
}
