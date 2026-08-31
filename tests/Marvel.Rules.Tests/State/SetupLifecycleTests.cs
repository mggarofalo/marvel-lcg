using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.State;

/// <summary>Lifecycle work performed while the opening board is dealt.</summary>
public sealed class SetupLifecycleTests
{
    [Fact]
    public void EvidenceRefusesBeforeAWorldIsDealt()
    {
        // `pack:mc50:gathering-evidence`: evidence is stored in hidden
        // envelopes, is not added to a deck, and supplies setup information in
        // later scenarios. Treating one as an encounter card would deal a
        // plausible board that the campaign rules do not describe.
        var stopped = Assert.Throws<RulesNotImplementedException>(() =>
            DealUnsupported("evidence", SetupSlot.Encounter));

        Assert.Contains("evidence", stopped.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("campaign evidence envelopes", stopped.Message, StringComparison.Ordinal);
    }

    [Rule("rr:player-side-scheme")]
    [Fact]
    public void PlayerSideSchemeRefusesBeforeAWorldIsDealt()
    {
        // A player side scheme is played from a player's hand and placed next
        // to the main scheme. Setup must not shuffle one into a deck whose play
        // path has no implementation and then return a game that can strand it.
        var stopped = Assert.Throws<RulesNotImplementedException>(() =>
            DealUnsupported("playerSideScheme", SetupSlot.PlayerDeck));

        Assert.Contains("PlayerSideScheme", stopped.Message, StringComparison.Ordinal);
        Assert.Contains("playing and resolving", stopped.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ChallengeRefusesBeforeAWorldIsDealt()
    {
        // A Challenge card modifies setup and play rather than merely sitting
        // in RemovedArea. The engine has no general challenge interpreter, so
        // the safe boundary is before setup consumes RNG or returns a board.
        var stopped = Assert.Throws<RulesNotImplementedException>(() =>
            DealUnsupported("challenge", SetupSlot.Challenge));

        Assert.Contains("Challenge", stopped.Message, StringComparison.Ordinal);
        Assert.Contains("setup and rules modifiers", stopped.Message, StringComparison.Ordinal);
    }

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

    private static World DealUnsupported(string faceId, SetupSlot slot) =>
        WorldSetup.Deal(
            new Facts(),
            [new CardBlueprint(faceId, slot, slot == SetupSlot.PlayerDeck ? 0 : -1)],
            slot == SetupSlot.PlayerDeck ? ["p0"] : [],
            seed: 13);

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
            "evidence" => CardKind.Evidence,
            "playerSideScheme" => CardKind.PlayerSideScheme,
            "challenge" => CardKind.Challenge,
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
