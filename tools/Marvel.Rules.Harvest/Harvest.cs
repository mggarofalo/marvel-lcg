using UglyToad.PdfPig;

namespace Marvel.Rules.Harvest;

/// <summary>
/// The glossary, read out of the document.
/// </summary>
/// <remarks>
/// <para>
/// An entry runs from its heading to the next one, and a heading is set larger
/// than the text under it. That is the only structural signal the document
/// gives: there is no table of contents to walk and no tagging in the file.
/// </para>
/// <para>
/// <b>An entry can cross a column and a page.</b> "See also" ends most of them
/// and not all, so the boundary is the next heading rather than the last
/// cross-reference — which is why the pages are read into one stream first and
/// cut afterwards.
/// </para>
/// </remarks>
public static class Harvest
{
    /// <summary>The pages the glossary occupies.</summary>
    /// <remarks>
    /// Page 4 is where the overview and the glossary begin, and page 50 is the
    /// last of it; the appendices follow. Held as numbers because the document
    /// gives nothing better — there is no marker at either boundary.
    /// </remarks>
    public const int First = 4;

    /// <summary>The last page of the glossary.</summary>
    public const int Last = 50;

    /// <summary>Reads every glossary entry.</summary>
    /// <param name="document">The Rules Reference.</param>
    public static IReadOnlyList<Entry> Read(PdfDocument document)
    {
        var entries = new List<Entry>();
        var lines = new List<(Line Line, int Page)>();

        for (int number = First; number <= Last; number++)
        {
            foreach (var line in Pages.Read(document.GetPage(number)))
            {
                // The glossary ends where the appendices begin, and the last
                // page holds both. An appendix is set as a section rather than
                // an entry, so without this its heading reads as two more
                // paragraphs of whatever entry was last open.
                if (line.Text.TrimStart().StartsWith("APPENDIX", StringComparison.Ordinal))
                {
                    goto done;
                }

                lines.Add((line, number));
            }
        }

    done:

        var current = new List<(Line Line, int Page)>();
        foreach (var entry in lines)
        {
            // The document's own section titles -- OVERVIEW, GLOSSARY -- are
            // set larger than an entry's heading and are not entries.
            if (entry.Line.Kind == Starts.Heading && entry.Line.Size < 14)
            {
                // A long heading wraps, and the second line is a heading too.
                // "PLAY RESTRICTIONS AND / PERMISSIONS" is one entry.
                if (current.Count == 1 && current[0].Line.Kind == Starts.Heading)
                {
                    var joined = current[0].Line with
                    {
                        Runs = [new Run(
                            $"{current[0].Line.Text.Trim()} {entry.Line.Text.Trim()}",
                            true,
                            false)],
                    };

                    current = [(joined, current[0].Page)];
                    continue;
                }

                if (current.Count > 0)
                {
                    entries.Add(Assemble(current));
                }

                current = [entry];
                continue;
            }

            if (current.Count > 0)
            {
                current.Add(entry);
            }
        }

        if (current.Count > 0)
        {
            entries.Add(Assemble(current));
        }

        return entries;
    }

    private static Entry Assemble(List<(Line Line, int Page)> lines)
    {
        string title = lines[0].Line.Text.Trim();
        var opening = new List<string>();
        var steps = new List<Numbered>();
        var clauses = new List<Clause>();
        var seeAlso = new List<string>();

        // What the next `More` line continues. The document wraps every kind of
        // line, so a continuation belongs to whatever last began.
        var into = Starts.More;
        var held = new List<Run>();

        // Whether the last thing to open was a step. The document runs its
        // steps after its clauses and never goes back, so this only ever turns
        // on -- but it turns on per entry, and it decides where the
        // second-level marker attaches.
        bool stepping = false;

        void Close()
        {
            string text = Markdown.Of(held).Trim();
            held = [];
            if (text.Length == 0)
            {
                return;
            }

            switch (into)
            {
                case Starts.Clause:
                    clauses.Add(new Clause(clauses.Count + 1, text, []));
                    break;

                // The second-level marker means "one level down from whatever
                // we are in": a qualification under a clause, and a lettered
                // step under a numbered one. `rr:attack-enemy-activation`'s
                // step 3 has five.
                case Starts.SubClause when stepping && steps.Count > 0:
                    var above = steps[^1];
                    steps[^1] = above with { Substeps = [.. above.Substeps, text] };
                    break;

                case Starts.SubClause when clauses.Count > 0:
                    var last = clauses[^1];
                    clauses[^1] = last with { Qualifications = [.. last.Qualifications, text] };
                    break;

                case Starts.Step:
                    steps.Add(new Numbered(steps.Count + 1, text, []));
                    break;

                case Starts.SubStep when steps.Count > 0:
                    var under = steps[^1];
                    steps[^1] = under with { Substeps = [.. under.Substeps, text] };
                    break;

                case Starts.SeeAlso:
                    seeAlso.AddRange(References(text));
                    break;

                default:
                    opening.Add(text);
                    break;
            }
        }

        foreach (var (line, _) in lines.Skip(1))
        {
            // "COUNTER / **See**: All-Purpose Counter" -- an entry that is
            // nothing but a pointer at another. The pointer is a
            // cross-reference and the entry has no text of its own, which is
            // why it is caught before a plain line is folded into whatever
            // came before it.
            bool pointer = into == Starts.More
                && held.Count == 0
                && line.Runs.Count > 0
                && line.Runs[0].Bold
                && line.Runs[0].Text.TrimStart().StartsWith("See", StringComparison.Ordinal);

            if (line.Kind == Starts.More && !pointer)
            {
                held = Markdown.Join(held, line.Runs);
                continue;
            }

            Close();
            into = pointer ? Starts.SeeAlso : line.Kind;
            stepping = into switch
            {
                Starts.Step or Starts.SubStep => true,
                Starts.Clause or Starts.SeeAlso => false,
                _ => stepping,
            };

            // The marker introduces the text and is not part of it.
            held = [.. into switch
            {
                Starts.Clause => Cut(line.Runs, "•"),
                Starts.Step or Starts.SubStep => Upto(line.Runs, '.'),
                Starts.SeeAlso => Upto(line.Runs, ':'),
                _ => line.Runs,
            }];
        }

        Close();

        return new Entry(
            Entry.Slug(title), title, lines[0].Page, opening, steps, clauses, seeAlso);
    }

    // "See also: Ability, Cost Arrow Icon, Game Element" -- printed titles,
    // which become ids the same way a heading does.
    private static IEnumerable<string> References(string text) =>
        Markdown.Plain(text)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(name => name.Length > 0);

    /// <summary>
    /// The line with a leading marker taken off it.
    /// </summary>
    /// <remarks>
    /// <b>Cut from the runs and not from the marked-up text.</b> The document
    /// sets a marker in the same weight as the words beside it — "**1. Give
    /// boost card: **" is one bold run — so cutting the string leaves half an
    /// emphasis behind, and the emphasis is re-emitted from the runs afterwards
    /// instead.
    /// </remarks>
    /// <param name="runs">The line.</param>
    /// <param name="marker">What to take off the front.</param>
    private static IReadOnlyList<Run> Cut(IReadOnlyList<Run> runs, string marker)
    {
        if (runs.Count == 0)
        {
            return runs;
        }

        string head = runs[0].Text.TrimStart();
        return head.StartsWith(marker, StringComparison.Ordinal)
            ? [runs[0] with { Text = head[marker.Length..].TrimStart() }, .. runs.Skip(1)]
            : runs;
    }

    /// <summary>The line with everything up to and including a character cut.</summary>
    /// <param name="runs">The line.</param>
    /// <param name="at">The character to cut through.</param>
    private static IReadOnlyList<Run> Upto(IReadOnlyList<Run> runs, char at)
    {
        for (int run = 0; run < runs.Count; run++)
        {
            int found = runs[run].Text.IndexOf(at, StringComparison.Ordinal);
            if (found >= 0)
            {
                return
                [
                    runs[run] with { Text = runs[run].Text[(found + 1)..].TrimStart() },
                    .. runs.Skip(run + 1),
                ];
            }
        }

        return runs;
    }
}
