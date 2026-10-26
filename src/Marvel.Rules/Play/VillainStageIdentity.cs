using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Configured same-character villain succession, including the side a stage enters on.</summary>
public static class VillainStageIdentity
{
    /// <summary>Captures the current stage and its configured same-character successors.</summary>
    public static IReadOnlyList<Card> Successors(World world, Card first)
    {
        // rr:villain-defeat.3.1: "The new stage ... is considered the same character".
        if (world.EncounterTopology is { } topology)
            return topology.Groups.Single(group => group.Stages.Contains(first)).Stages;
        var stages = new List<Card> { first };
        string face = first.FaceId;
        foreach (var next in world.AreaOf(DeckType.VillainDeck).Cards.Reverse())
        {
            string nextFace = PromotionFace(face, next) ?? next.FaceId;
            // rr:villain-defeat.4.1 distinguishes a different-title successor.
            if (world.Facts.Title(face) != world.Facts.Title(nextFace)) break;
            stages.Add(next);
            face = nextFace;
        }
        return stages;
    }

    internal static string? PromotionFace(string currentFace, Card next) =>
        currentFace is "02001a" or "02002a" or "02003a" or "02001b" or "02002b" or "02003b"
            ? next.Faces.FirstOrDefault(face => face == next.FaceId[..5] + currentFace[^1])
            : null;
}
