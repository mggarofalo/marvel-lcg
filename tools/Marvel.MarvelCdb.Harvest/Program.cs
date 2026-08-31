using System.Text;
using System.Text.Json;
using Marvel.MarvelCdb.Harvest;
using Marvel.Tests;

string verb = args.Length > 0 ? args[0] : string.Empty;
string cache = args.Length > 1 ? args[1] : DefaultCache();

try
{
    switch (verb)
    {
        case "fetch":
        {
            int limit = args.Length > 2 ? int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture) : 0;
            string date = args.Length > 3 ? args[3] : DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            Snapshot snapshot = Harvest.All(
                new MarvelCdbCommand(),
                date,
                limit,
                (done, total, found) => Console.Error.WriteLine(
                    $"  {done}/{total} asked, {found} ruling(s)"));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cache))!);
            File.WriteAllText(cache, snapshot.CandidateJson(), new UTF8Encoding(false));
            Console.Error.WriteLine($"wrote candidate {cache}");
            return 0;
        }

        case "write":
        {
            string into = args.Length > 2 ? args[2] : RepositoryPaths.Dataset("marvelcdb-faq");
            string pinPath = args.Length > 3
                ? args[3]
                : RepositoryPaths.Dataset("marvelcdb-faq", "query.manifest.json");
            Snapshot snapshot = Snapshot.Read(File.ReadAllText(cache));
            if (!snapshot.CandidateComplete)
            {
                throw new InvalidDataException(
                    "the candidate is partial or lacks completion accounting; run a full `fetch`");
            }
            QueryPin.Read(pinPath).Verify(snapshot.Queried);
            Directory.CreateDirectory(into);
            string target = Path.Combine(into, "faq.json");
            File.WriteAllText(target, snapshot.Json(), new UTF8Encoding(false));
            Console.Error.WriteLine($"wrote {target}");
            return 0;
        }

        case "check":
        {
            string pinPath = args.Length > 2
                ? args[2]
                : RepositoryPaths.Dataset("marvelcdb-faq", "query.manifest.json");
            Snapshot candidate = Snapshot.Read(File.ReadAllText(cache));
            if (!candidate.CandidateComplete)
            {
                throw new InvalidDataException(
                    "the candidate is partial or lacks completion accounting; run a full `fetch`");
            }
            QueryPin.Read(pinPath).Verify(candidate.Queried);
            string target = RepositoryPaths.Dataset("marvelcdb-faq", "faq.json");
            string built = candidate.Json();
            string committed = File.ReadAllText(target);
            if (string.Equals(built, committed, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("the candidate matches datasets/marvelcdb-faq/faq.json");
                return 0;
            }

            Console.Error.WriteLine("the candidate differs from datasets/marvelcdb-faq/faq.json");
            return 1;
        }

        case "pin":
        {
            string into = args.Length > 2
                ? args[2]
                : RepositoryPaths.Dataset("marvelcdb-faq", "query.manifest.json");
            Snapshot candidate = Snapshot.Read(File.ReadAllText(cache));
            if (!candidate.CandidateComplete)
            {
                throw new InvalidDataException(
                    "only a fully accounted candidate can become the query-universe pin");
            }

            File.WriteAllText(into, QueryPin.Create(candidate.Queried).Json(), new UTF8Encoding(false));
            Console.Error.WriteLine($"pinned {candidate.Queried.Count} queried codes in {into}");
            return 0;
        }

        default:
            Console.Error.WriteLine(
                """
                Acquires MarvelCDB FAQ rulings into a local candidate, then writes the vendored snapshot offline.

                  fetch [cache] [limit] [date]   query MarvelCDB and write a complete local candidate
                  write [cache] [into] [pin]    build faq.json from a pinned candidate offline
                  check [cache] [pin]           compare a pinned candidate with the committed snapshot
                  pin [cache] [into]            approve a reviewed full query universe

                The default cache is outside the repository. `limit` is only for testing the wiring;
                never write a limited candidate into datasets/marvelcdb-faq/.
                """);
            return 2;
    }
}
catch (Exception exception) when (exception is IOException
    or InvalidDataException
    or InvalidOperationException
    or JsonException
    or FormatException
    or OverflowException
    or KeyNotFoundException)
{
    Console.Error.WriteLine($"harvest failed: {exception.Message}");
    return 1;
}

static string DefaultCache() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "marvel-lcg", "marvelcdb-faq", "candidate.json");
