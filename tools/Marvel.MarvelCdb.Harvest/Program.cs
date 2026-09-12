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
            return Fetch(args, cache);

        case "write":
            return Write(args, cache);

        case "check":
            return Check(args, cache);

        case "pin":
            return Pin(args, cache);

        default:
            Console.Error.WriteLine(
                """
                Acquires MarvelCDB FAQ rulings into a local candidate, then writes the vendored snapshot offline.

                  fetch [cache] [limit] [date]   query MarvelCDB and write a complete local candidate
                  write [cache] [into] [pin]    build faq.json from a pinned candidate offline
                  check [cache] [pin]           compare a pinned candidate with the committed snapshot
                  pin [cache] [into]            pin the reviewed candidate bytes and query universe

                The default cache is outside the repository. `limit` is only for testing the wiring;
                never write a limited candidate into datasets/marvelcdb-faq/.
                """);
            return 2;
    }
}
catch (Exception exception) when (Expected(exception))
{
    Console.Error.WriteLine($"harvest failed: {exception.Message}");
    return 1;
}

static int Fetch(string[] arguments, string cachePath)
{
    int limit = arguments.Length > 2
        ? int.Parse(arguments[2], System.Globalization.CultureInfo.InvariantCulture) : 0;
    string date = arguments.Length > 3
        ? arguments[3]
        : DateTime.UtcNow.ToString(
            "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    Snapshot snapshot = Harvest.All(
        new MarvelCdbCommand(), date, limit,
        (done, total, found) => Console.Error.WriteLine(
            $"  {done}/{total} asked, {found} ruling(s)"));
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cachePath))!);
    File.WriteAllText(cachePath, snapshot.CandidateJson(), new UTF8Encoding(false));
    Console.Error.WriteLine($"wrote candidate {cachePath}");
    return 0;
}

static int Write(string[] arguments, string cachePath)
{
    string into = arguments.Length > 2
        ? arguments[2] : RepositoryPaths.Dataset("marvelcdb-faq");
    var (bytes, snapshot) = ReadCandidate(cachePath);
    AcquisitionPin.Read(PinPath(arguments, 3)).Verify(bytes, snapshot);
    snapshot.VerifyPublishable();
    Directory.CreateDirectory(into);
    string target = Path.Combine(into, "faq.json");
    File.WriteAllText(target, snapshot.Json(), new UTF8Encoding(false));
    Console.Error.WriteLine($"wrote {target}");
    return 0;
}

static int Check(string[] arguments, string cachePath)
{
    var (bytes, candidate) = ReadCandidate(cachePath);
    AcquisitionPin.Read(PinPath(arguments, 2)).Verify(bytes, candidate);
    candidate.VerifyPublishable();
    string committed = File.ReadAllText(
        RepositoryPaths.Dataset("marvelcdb-faq", "faq.json"));
    bool equal = string.Equals(candidate.Json(), committed, StringComparison.Ordinal);
    Console.Error.WriteLine(equal
        ? "the candidate matches datasets/marvelcdb-faq/faq.json"
        : "the candidate differs from datasets/marvelcdb-faq/faq.json");
    return equal ? 0 : 1;
}

static int Pin(string[] arguments, string cachePath)
{
    string into = PinPath(arguments, 2);
    var (bytes, candidate) = ReadCandidate(cachePath);
    File.WriteAllText(
        into, AcquisitionPin.Create(bytes, candidate).Json(),
        new UTF8Encoding(false));
    Console.Error.WriteLine($"pinned reviewed acquisition bytes in {into}");
    return 0;
}

static (byte[] Bytes, Snapshot Snapshot) ReadCandidate(string path)
{
    byte[] bytes = File.ReadAllBytes(path);
    return (bytes, Snapshot.Read(Encoding.UTF8.GetString(bytes)));
}

static string PinPath(string[] arguments, int index) => arguments.Length > index
    ? arguments[index]
    : RepositoryPaths.Dataset("marvelcdb-faq", "acquisition.manifest.json");

static bool Expected(Exception exception) => exception is IOException
    or InvalidDataException
    or InvalidOperationException
    or JsonException
    or FormatException
    or OverflowException
    or KeyNotFoundException;

static string DefaultCache() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "marvel-lcg", "marvelcdb-faq", "candidate.json");
