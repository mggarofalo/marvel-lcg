using System.Collections.Immutable;
using System.Text.Json;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Rules.Tests.Play;

internal static class DroneProfileFixture
{
    internal static EffectiveCardProfile Profile { get; } = Read();

    private static EffectiveCardProfile Read()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var profile = data.RootElement.GetProperty("cards").EnumerateArray()
            .Single(card => card.GetProperty("card").GetString() == "01140")
            .GetProperty("abilities")[0].GetProperty("effect").GetProperty("defineProfile");
        return new(profile.GetProperty("id").GetString()!, profile.GetProperty("title").GetString()!,
            CardKind.Minion,
            [.. profile.GetProperty("traits").EnumerateArray().Select(trait => trait.GetString()!)],
            profile.GetProperty("baseValues").EnumerateObject()
                .ToImmutableDictionary(pair => pair.Name, pair => pair.Value.GetInt64(), StringComparer.Ordinal));
    }

    internal static IReadOnlyList<Card> InPlay(World world, int? player = null) =>
        [.. world.Cards.Where(card => card.InstanceState.Profile?.Id == Profile.Id
            && (player is null || card.Area.PlayArea == PlayArea.Of(player.Value)))];
}
