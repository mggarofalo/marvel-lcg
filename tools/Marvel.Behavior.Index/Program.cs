namespace Marvel.Behavior.Index;

/// <summary>Builds and checks the authority-derived behavioral catalog.</summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            return args.Length == 1 ? args[0] switch
            {
                "write" => Write(),
                "check" => Check(),
                "report" => Report(),
                "scaffold" => Scaffold(),
                "skeletons" => Skeletons(),
                _ => Usage(),
            } : Usage();
        }
        catch (Exception error) when (error is InvalidDataException
            or InvalidOperationException
            or System.Text.Json.JsonException
            or KeyNotFoundException
            or IOException)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static int Write()
    {
        Catalog.Write();
        Console.WriteLine("wrote specs/behavior/catalog.json");
        return 0;
    }

    private static int Check()
    {
        Catalog.Check();
        Console.WriteLine("behavior catalog is current");
        return 0;
    }

    private static int Report()
    {
        var catalog = Catalog.Build();
        foreach (var kind in catalog.Sources.GroupBy(source => source.Kind, StringComparer.Ordinal))
        {
            Console.WriteLine($"{kind.Key,-8} {kind.Count(),5}");
        }

        Console.WriteLine($"{"sources",-8} {catalog.Sources.Count,5}");
        Console.WriteLine(
            $"{"obligations",-8} {catalog.Sources.Sum(source => source.Obligations.Count),5}");
        return 0;
    }

    private static int Scaffold()
    {
        Catalog.Scaffold();
        Console.WriteLine("wrote specs/behavior/adjudications.json for review");
        return 0;
    }

    private static int Skeletons()
    {
        Catalog.Skeletons(Catalog.Build(), Console.Out);
        return 0;
    }

    private static int Usage()
    {
        Console.Error.WriteLine(
            "usage: Marvel.Behavior.Index write|check|report|scaffold|skeletons");
        return 2;
    }
}
