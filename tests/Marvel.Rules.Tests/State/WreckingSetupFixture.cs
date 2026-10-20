using Marvel.Rules.State;

namespace Marvel.Rules.Tests.State;

/// <summary>Blueprint-only setup fixture; complete card-text admission is tested separately.</summary>
internal sealed class WreckingSetupFixture
{
    internal WreckingSetupFixture(bool extreme)
    {
        Cards.Add(new("hero", SetupSlot.Identity, 0));
        Cards.Add(new("main-a,main-b", SetupSlot.MainScheme, -1));
        var groups = new List<EncounterGroupSetup>();
        for (int group = 0; group < 4; group++)
        {
            var stages = new List<int> { Cards.Count };
            Cards.Add(new($"villain-{group}-a", SetupSlot.Villain, -1));
            if (extreme)
            {
                stages.Add(Cards.Count);
                Cards.Add(new($"villain-{group}-b", SetupSlot.Villain, -1));
            }
            int scheme = Cards.Count;
            Cards.Add(new($"side-{group}", SetupSlot.SetAside, -1));
            var encounters = new List<int>();
            for (int index = 0; index < 15; index++)
            {
                encounters.Add(Cards.Count);
                Cards.Add(new($"encounter-{group}-{index}", SetupSlot.Encounter, -1));
            }
            groups.Add(new(stages, scheme, encounters));
        }
        Plan = new(groups, groups[0].Stages[0]);
    }

    internal List<CardBlueprint> Cards { get; } = [];
    internal EncounterSetupPlan Plan { get; }
    internal ICardFacts Facts { get; } = new Printed();

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId.Split('-')[0] switch
        {
            "hero" => CardKind.Hero,
            "main" => CardKind.MainScheme,
            "villain" => CardKind.EncounterVillain,
            "side" => CardKind.EncounterSideScheme,
            _ => CardKind.Minion,
        };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            attribute == "StartingThreat" && faceId.StartsWith("side-", StringComparison.Ordinal)
                ? int.Parse(faceId.AsSpan(5), System.Globalization.CultureInfo.InvariantCulture) + 1
                : attribute == "HP" ? 5 : fallback;
    }
}
