using Marvel.Rules.State;

namespace Marvel.Rules.Setup;

/// <summary>Resolves the starting face from printed identity kinds.</summary>
public static class IdentityFaces
{
    /// <summary>Places the alter-ego first without interpreting face-id spelling.</summary>
    public static string StartingSpec(string spec, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(facts);
        string[] faces = spec.Split(',');
        if (faces.Length != 2 || faces.Count(face => facts.Kind(face) == CardKind.Hero) != 1)
            throw new ArgumentException($"identity '{spec}' requires one hero and one alter-ego face", nameof(spec));
        string[] alterEgos = faces.Where(face => facts.Kind(face) == CardKind.AlterEgo).ToArray();
        if (alterEgos.Length != 1)
            throw new ArgumentException($"identity '{spec}' requires exactly one alter-ego face", nameof(spec));
        // rr:appendix-ii-setup.step.1: "Each player selects one identity,
        // placing their alter-ego side face up.". Ordering other faces is our format choice.
        return string.Join(',', alterEgos.Concat(faces.Where(face => face != alterEgos[0])));
    }
}
