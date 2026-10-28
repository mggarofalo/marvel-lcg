namespace Marvel.Cards.Run;

// Stable identity for one projected area alternative.
internal static class AreaProjectionKey
{
    internal static string Key(AreaProjectionState state) => string.Join(
        ";",
        state.Damage.OrderBy(pair => pair.Key)
            .Select(pair => $"d{pair.Key}:{pair.Value}")
            .Concat(state.Tough.OrderBy(pair => pair.Key)
                .Select(pair => $"s{pair.Key}:{pair.Value}"))
            .Concat(state.Threat.OrderBy(pair => pair.Key)
                .Select(pair => $"t{pair.Key}:{pair.Value}"))
            .Concat(state.Status.OrderBy(pair => pair.Key.Card)
                .ThenBy(pair => pair.Key.Status, StringComparer.Ordinal)
                .Select(pair =>
                    $"x{pair.Key.Card}:{pair.Key.Status}:{pair.Value}"))
            .Concat(state.Departed.Order().Select(card => $"o{card}"))
            .Concat(state.Entered.Order().Select(card => $"i{card}"))
            .Concat(state.Hosts.OrderBy(pair => pair.Key)
                .Select(pair => $"h{pair.Key}:{pair.Value}"))
            .Concat(state.EngagedWith.OrderBy(pair => pair.Key)
                .Select(pair => $"e{pair.Key}:{pair.Value}"))
            .Concat(state.Traits.OrderBy(pair => pair.Key)
                .SelectMany(pair => pair.Value.Order(StringComparer.Ordinal)
                    .Select(trait => $"g{pair.Key}:{trait}")))
            .Concat(state.Faces.OrderBy(pair => pair.Key)
                .Select(pair => $"f{pair.Key}:{pair.Value}"))
            .Concat(state.Modifiers.OrderBy(pair => pair.Key.Card)
                .ThenBy(pair => pair.Key.Field, StringComparer.Ordinal)
                .Select(pair =>
                    $"m{pair.Key.Card}:{pair.Key.Field}:{pair.Value}"))
            .Append($"v{state.ActiveVillain}:{state.VillainAttachmentHost}")
            .Append($"r{state.SourceReferenceCurrent}"));
}
