using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public abstract class LifecycleRulesTestBase
{
    protected static World Board(Facts facts, int players = 1)
    {
        var world = new World(facts, players, seed: 7);
        for (int player = 0; player < players; player++)
        {
            world.CreateSeat($"p{player}");
            world.Seats[player].IdentityCard = world.CreateCard("alterego,hero", world.Seats[player].Hero);
            // Keep ordinary discards from satisfying rr:player-deck.4 and
            // immediately returning to an otherwise empty deck.
            world.CreateCard("resource", world.Seats[player].Deck);
        }

        return world;
    }

    protected sealed class Facts : ICardFacts
    {
        public Dictionary<string, string> Sets { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Forms { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Uses { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Classes { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> Maxima { get; } = new(StringComparer.Ordinal);

        public CardKind Kind(string faceId) => faceId switch
        {
            "alterego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "ally" => CardKind.Ally,
            "event" => CardKind.Event,
            "resource" => CardKind.Resource,
            "upgrade" or "gamma" or "limited" => CardKind.Upgrade,
            "permanent" or "same" or "other" or "quiver" => CardKind.Support,
            "shooter" => CardKind.Upgrade,
            "sideA" or "sideB" => CardKind.EncounterSideScheme,
            "minion" => CardKind.Minion,
            "environment" => CardKind.Environment,
            "status" => CardKind.Status,
            "treachery" => CardKind.Treachery,
            _ => CardKind.Unknown,
        };
        public string EncounterSet(string faceId) => Sets.GetValueOrDefault(faceId, string.Empty);
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId)
        {
            var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Cost"] = "0",
            };
            if (Uses.TryGetValue(faceId, out string? uses))
            {
                attributes["Uses"] = uses;
            }

            if (Classes.TryGetValue(faceId, out string? printedClass))
            {
                attributes["Class"] = printedClass;
            }

            if (Maxima.ContainsKey(faceId))
            {
                attributes["MaxPerUnitKind"] = "player";
            }

            return attributes;
        }

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => Maxima.TryGetValue(faceId, out long maximum) && attribute == "MaxPerUnit" ? maximum : faceId == "permanent" && attribute == "Permanent" ? 1 : attribute == "Cost" ? 0 : fallback;
        public string? FormKeyword(string faceId) => Forms.GetValueOrDefault(faceId);
    }
}
