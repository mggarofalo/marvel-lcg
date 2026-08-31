using Marvel.Rules.Packs.Harvest;
using Marvel.Tests;

string verb = args.Length > 0 ? args[0] : string.Empty;
string library = args.Length > 1 ? args[1] : Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Documents", "Marvel Champions LCG");
string output = args.Length > 2 ? args[2] : RepositoryPaths.Dataset("rules-packs");

try
{
    IReadOnlyList<string> paths = Harvest.Sources(library);
    if (paths.Count == 0)
    {
        Console.Error.WriteLine(
            $"no pack documents under {library}. The copyrighted PDFs are not in this repository; "
            + "point this tool at your local library.");
        return 2;
    }

    if (verb == "list")
    {
        foreach (string path in paths)
        {
            var (code, kind) = Harvest.Classify(Path.GetFileName(path))!.Value;
            Console.WriteLine($"  {code,-8} {kind,-14} {Path.GetFileName(path)}");
        }

        Console.WriteLine($"\n{paths.Count} document(s)");
        return 0;
    }

    if (verb == "pin")
    {
        IReadOnlyDictionary<string, byte[]> committed = Emit.ReadTreeBytes(output);
        string manifest = Emit.Manifest(paths, committed);
        File.WriteAllText(
            Path.Combine(output, "sources.manifest.json"),
            manifest,
            new System.Text.UTF8Encoding(false));
        Console.Error.WriteLine($"pinned {paths.Count} local PDFs against {output}");
        return 0;
    }

    if (verb == "check")
    {
        return Emit.VerifyManifest(paths, output) ? 0 : 1;
    }

    var documents = paths.Select(Harvest.Read).ToList();
    var tree = new Dictionary<string, string>(Emit.Build(documents), StringComparer.Ordinal);
    tree["sources.manifest.json"] = Emit.Manifest(paths, tree);
    if (verb == "write")
    {
        Emit.Write(tree, output);
        Console.Error.WriteLine(
            $"wrote {tree.Count - 2} sections from {documents.Count} documents to {output}");
        return 0;
    }

    if (verb == "diff")
    {
        IReadOnlyDictionary<string, string> committed = Emit.ReadTree(output);
        var added = tree.Keys.Except(committed.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var removed = committed.Keys.Except(tree.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var changed = tree.Keys.Intersect(committed.Keys, StringComparer.Ordinal)
            .Where(path => !string.Equals(tree[path], committed[path], StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToList();
        foreach (string path in added) Console.WriteLine($"  + {path}");
        foreach (string path in removed) Console.WriteLine($"  - {path}");
        foreach (string path in changed) Console.WriteLine($"  ~ {path}");
        if (added.Count == 0 && removed.Count == 0 && changed.Count == 0)
        {
            Console.Error.WriteLine(
                $"{output} is up to date ({documents.Count} documents, {tree.Count - 1} sections)");
            return 0;
        }

        Console.Error.WriteLine(
            $"{output} is stale: {added.Count} added, {removed.Count} removed, {changed.Count} changed");
        return 1;
    }

    Console.Error.WriteLine(
        """
        Reads local expansion PDFs into datasets/rules-packs/.

          list [library]                 list the local documents and their classifications
          pin [library] [against]        pin the current vendored snapshot to the local PDF bytes
          write [library] [into]         harvest and write the vendored dataset
          check [library] [against]      verify the local PDFs and committed snapshot against their pins
          diff [library] [against]       re-harvest and report candidate file differences

        This is a manual tool. The PDFs are copyrighted and deliberately absent from CI.
        """);
    return 2;
}
catch (Exception exception) when (exception is IOException or InvalidDataException)
{
    Console.Error.WriteLine($"harvest failed: {exception.Message}");
    return 1;
}
