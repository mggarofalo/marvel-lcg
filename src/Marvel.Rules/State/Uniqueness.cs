namespace Marvel.Rules.State;

/// <summary>Matching unique cards — <c>rr:unique-icon</c>.</summary>
public static class Uniqueness
{
    /// <summary>Whether two cards represent the same unique person, place, or thing.</summary>
    public static bool Matches(ICardFacts facts, Card left, Card right)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return IsUnique(facts, left) && IsUnique(facts, right)
            && Matches(facts, left.Faces, right.Faces);
    }

    /// <summary>Whether a card is prevented from entering play by a matching card.</summary>
    public static bool IsBlocked(World world, ICardFacts facts, Card entering)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(entering);

        return world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Any(inPlay => !ReferenceEquals(inPlay, entering)
                && Places.CanAffect(world, entering, inPlay)
                && Matches(facts, entering, inPlay));
    }

    /// <summary>Whether two card specs match for deckbuilding or identity selection.</summary>
    public static bool Matches(
        ICardFacts facts, IReadOnlyList<string> leftFaces, IReadOnlyList<string> rightFaces)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(leftFaces);
        ArgumentNullException.ThrowIfNull(rightFaces);

        if (!IsUnique(facts, leftFaces) || !IsUnique(facts, rightFaces))
        {
            return false;
        }

        var left = Names(facts, leftFaces);
        var right = Names(facts, rightFaces);

        // `rr:unique-icon.1.1`: title alone matches only when neither card has
        // a subtitle or alter-ego title.
        if (left.Qualifiers.Count == 0 && right.Qualifiers.Count == 0
            && left.Titles.Overlaps(right.Titles))
        {
            return true;
        }

        // `rr:unique-icon.1.2`: a subtitle or alter-ego title on either card
        // matches any title, subtitle, or alter-ego title on the other.
        return left.Qualifiers.Overlaps(right.All) || right.Qualifiers.Overlaps(left.All);
    }

    /// <summary>Whether a printed card carries the unique icon.</summary>
    public static bool IsUnique(ICardFacts facts, IReadOnlyList<string> faces) =>
        faces.Any(face => facts.Attributes(face).ContainsKey("Unique"));

    private static bool IsUnique(ICardFacts facts, Card card) => IsUnique(facts, card.Faces);

    private static CardNames Names(ICardFacts facts, IReadOnlyList<string> faces)
    {
        var titles = new HashSet<string>(StringComparer.Ordinal);
        var qualifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (string face in faces)
        {
            Add(titles, facts.Title(face));
            Add(qualifiers, facts.Subtitle(face));
            if (facts.Kind(face) == CardKind.AlterEgo)
            {
                Add(qualifiers, facts.Title(face));
            }
        }

        var all = new HashSet<string>(titles, StringComparer.Ordinal);
        all.UnionWith(qualifiers);
        return new CardNames(titles, qualifiers, all);
    }

    private static void Add(HashSet<string> names, string name)
    {
        if (name.Length > 0)
        {
            names.Add(name);
        }
    }

    private sealed record CardNames(
        HashSet<string> Titles, HashSet<string> Qualifiers, HashSet<string> All);
}
