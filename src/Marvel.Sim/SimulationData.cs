using Marvel.Cards.Dsl;
using Marvel.Content;
using Marvel.Content.Setup;

namespace Marvel.Sim;

internal sealed record SimulationData(
    SetupCatalog Setup,
    CardCatalog Cards,
    AbilityBook Abilities)
{
    public static SimulationData Load(string? requestedRoot)
    {
        string root = RepositoryRoot(requestedRoot);
        return new SimulationData(
            SetupCatalog.Parse(File.ReadAllText(
                Path.Combine(root, "datasets", "setup", "setup.json"))),
            CardCatalog.Parse(File.ReadAllText(
                Path.Combine(root, "datasets", "cards", "cards.json"))),
            AbilityCatalog.Parse(File.ReadAllText(
                Path.Combine(root, "datasets", "abilities", "abilities.json"))));
    }

    private static string RepositoryRoot(string? requested)
    {
        if (requested is not null)
        {
            string root = Path.GetFullPath(requested);
            if (!File.Exists(Path.Combine(root, "Marvel.slnx")))
            {
                throw new SimulationUsageException(
                    $"--repo-root does not contain Marvel.slnx: {root}");
            }

            return root;
        }

        for (DirectoryInfo? directory = new(Environment.CurrentDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Marvel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new SimulationUsageException(
            "could not find Marvel.slnx; run from the repository or pass --repo-root");
    }
}
