using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.State;

/// <summary>Lifecycle work performed while the opening board is dealt.</summary>
public sealed class SetupLifecycleTests
{
    [Rule("rr:when-revealed-abilities.1")]
    [Fact]
    public void EncounterCardsEnteringDuringSetupResolveAfterScenarioSetup()
    {
        // An encounter card entering during setup resolves its When Revealed
        // ability during the dedicated setup step. The first deferred ability
        // puts a second card into play; it joins the end of the same queue.
        var facts = new Facts();
        var abilities = new RecordingAbilities(facts);
        WorldSetup.Deal(
            facts,
            [
                new CardBlueprint("alterego,hero", SetupSlot.Identity, 0),
                new CardBlueprint("schemeA,schemeB", SetupSlot.MainScheme, -1),
                new CardBlueprint("villain", SetupSlot.Villain, -1),
                new CardBlueprint("setupSide", SetupSlot.SetAside, -1),
                new CardBlueprint("laterMinion", SetupSlot.SetAside, -1),
            ],
            ["p0"],
            seed: 13,
            abilities);

        Assert.Equal(
            ["schemeB", "villain", "setupSide", "laterMinion"],
            abilities.Revealed);
    }

    private sealed class RecordingAbilities(Facts facts) : NoCardAbilities
    {
        public List<string> Revealed { get; } = [];

        public override IReadOnlyList<GameEvent> WhenRevealed(
            World world, Card card, int player)
        {
            Revealed.Add(card.FaceId);
            if (card.FaceId != "setupSide")
            {
                return [];
            }

            var later = world.Cards.Single(each => each.FaceId == "laterMinion");
            var events = new List<GameEvent>();
            Reveal.Resolve(world, facts, later, player, events);
            return events;
        }
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "alterego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "schemeA" or "schemeB" => CardKind.MainScheme,
            "villain" => CardKind.EncounterVillain,
            "setupSide" => CardKind.EncounterSideScheme,
            "laterMinion" => CardKind.Minion,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            faceId == "setupSide" && attribute == "Setup" ? 1 : fallback;
    }
}
