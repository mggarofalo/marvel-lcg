using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Marvel.Tests;

namespace Marvel.Cards.Extract;

/// <summary>
/// Builds <c>datasets/cards/cards.json</c> from the vendored MarvelSDB
/// snapshot.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> <c>AGENTS.md</c> non-negotiable 8: every dataset is
/// either generated — rebuildable offline and byte-identically — or vendored,
/// copied once from a pinned upstream and read as-is. <c>datasets/cards/</c>
/// was neither. The join that produced it was Python and is gone, so a new
/// pack, an errata or a MarvelSDB refresh could not be taken up, and editing
/// the file by hand was possible and unguarded.
/// </para>
/// <para>
/// <b>Offline.</b> The only input is <c>datasets/marvelsdb/</c>, which is
/// vendored in this repository, plus <c>datasets/cards/supplement.json</c>,
/// which is authored in it. Nothing here reaches the network, which is the
/// second half of the same non-negotiable.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args) =>
        (args.Length == 0 ? "" : args[0]) switch
        {
            "write" => Write(),
            "check" => Check(),
            "diff" => Diff(),
            "propose" => Propose(),
            _ => Usage(),
        };

    private static int Usage()
    {
        Console.Error.WriteLine(
            """
            Builds datasets/cards/cards.json from the vendored MarvelSDB snapshot.

              write     regenerate the dataset
              check     regenerate in memory and fail if the committed file differs
              diff      report how the regenerated dataset differs from the committed one
              propose   print a supplement covering what the snapshot does not record
            """);
        return 2;
    }

    private static int Write()
    {
        File.WriteAllText(Path(), Build(), new UTF8Encoding(false));
        Console.Error.WriteLine($"wrote {Path()}");
        return 0;
    }

    private static int Check()
    {
        string built = Build();
        string committed = File.ReadAllText(Path());
        if (string.Equals(built, committed, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("datasets/cards/cards.json is what the generator produces");
            return 0;
        }

        Console.Error.WriteLine(
            "datasets/cards/cards.json differs from what the generator produces. "
            + "Run `dotnet run --project tools/Marvel.Cards.Extract -- write`.");
        return 1;
    }

    /// <summary>
    /// What the regenerated dataset says that the committed one does not.
    /// </summary>
    /// <remarks>
    /// For the change itself rather than for the gate: the committed dataset
    /// was produced by a join that no longer exists, and every place the two
    /// disagree is either a rule this generator has wrong or a fact the old
    /// pipeline had from somewhere other than the printed card. Both need
    /// reading, and neither is visible in a byte comparison of a six-megabyte
    /// file.
    /// </remarks>
    private static int Diff()
    {
        var built = Cards.Read(Build());
        var committed = Cards.Read(File.ReadAllText(Path()));
        return Cards.Report(committed, built);
    }

    /// <summary>
    /// A starting supplement: everything the retired dataset knew that the
    /// snapshot does not record.
    /// </summary>
    /// <remarks>
    /// Run once, while replacing the retired join, and its output hand-read
    /// and annotated before being committed. It is here rather than in a
    /// scratch file because the question it answers — what does MarvelSDB not
    /// carry — comes back every time upstream is refreshed.
    /// </remarks>
    private static int Propose()
    {
        var built = Cards.Read(Build());
        var committed = Cards.Read(File.ReadAllText(Path()));
        Console.WriteLine(Supplement.Propose(committed, built));
        return 0;
    }

    private static string Path() => RepositoryPaths.Dataset("cards", "cards.json");

    private static string Build()
    {
        var all = SdbCard.ReadAll();
        var nemeses = Printed.Nemeses(all);
        var supplement = Supplement.Read();
        var written = new List<Card>();

        foreach (string code in all.Keys.OrderBy(code => code, StringComparer.Ordinal))
        {
            if (supplement.Dropped.Contains(code))
            {
                continue;
            }

            var card = all[code].Resolved(all);
            string? kind = Printed.Kind(card.Text("type_code"));
            if (kind is null)
            {
                // A card whose type this engine has no name for. Upstream has
                // none today; the branch is here because a new pack is exactly
                // when one arrives, and a silently untyped card would deal.
                Console.Error.WriteLine($"{code}: no card type for '{card.Text("type_code")}'");
                continue;
            }

            written.Add(supplement.Apply(new Card(
                Id: code,
                Name: card.Text("name") ?? "",
                Subname: card.Text("subname") ?? "",
                Kind: kind,
                Traits: Printed.Traits(card.Text("traits")),
                Attributes: Printed.Attributes(card, kind, nemeses),
                Text: card.Text("text") ?? "",
                Pack: card.Text("pack_code") ?? "",
                Set: card.Text("set_code") ?? "")));
        }

        written.AddRange(supplement.Only);
        written.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));

        return Cards.Write(written, supplement);
    }
}
