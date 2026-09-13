using System.Text.Json;
using System.Text.RegularExpressions;

namespace Marvel.MarvelCdb.Harvest;

public sealed record BatchResult(
    IReadOnlyList<JsonElement> Entries,
    IReadOnlyList<QueryOutcome> Outcomes);
