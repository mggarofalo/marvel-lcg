using Marvel.Core.Digest;

namespace Marvel.Rules.State;

/// <summary>Serializes complete deterministic card positions and registered fields.</summary>
internal static class WorldDigest
{
    /// <summary>Builds one record per physical card in creation order.</summary>
    public static StateDigest Create(World world)
    {
        var positions = new Dictionary<int, (string Zone, int Index)>();
        foreach (var area in world.Areas)
        {
            string zone = area.Type.ToString();
            for (int index = 0; index < area.Cards.Count; index++)
            {
                positions[area.Cards[index].ObjectId] = (zone, index);
            }

            for (int index = 0; index < area.Removed.Count; index++)
            {
                positions[area.Removed[index].ObjectId] = (zone + "/removed", index);
            }
        }

        var records = new List<CardRecord>(world.Cards.Count);
        foreach (var card in world.Cards)
        {
            // `/absent` should not happen. It is emitted rather than raised
            // because a digest that can crash while computing itself is worse
            // than one with a visible anomaly in it.
            var (zone, index) = positions.TryGetValue(card.ObjectId, out var found)
                ? found
                : (card.Area.Type + "/absent", -1);

            records.Add(CardDigestRecord.For(world, card, zone, index));
        }

        return new StateDigest(records);
    }
}
