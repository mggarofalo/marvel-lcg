const int MaximumLines = 1000;

var failed = false;
var visited = new HashSet<string>(StringComparer.Ordinal);
foreach (string path in Args)
{
    if (!visited.Add(path) || !File.Exists(path))
    {
        continue;
    }

    var lines = 0;
    using var reader = File.OpenText(path);
    while (lines <= MaximumLines && reader.ReadLine() is not null)
    {
        lines++;
    }

    if (lines <= MaximumLines)
    {
        continue;
    }

    Console.Error.WriteLine(
        $"{path}: file has more than {MaximumLines} lines; split it before committing");
    failed = true;
}

return failed ? 1 : 0;
