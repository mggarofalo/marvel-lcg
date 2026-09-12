using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Behavior.Run;

internal static class GivenTranscriptVocabulary
{
    internal static IReadOnlyList<TranscriptBinding> Bindings() =>
    [
        Bind("core-scene", TranscriptStepKind.Given,
            "a canonical Core scene is dealt", DealScene),
        Bind("stack-player-deck", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s player deck contains only these next cards", StackPlayerDeck),
        Bind("stack-player-deck-empty-discard", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s player deck contains only these next cards with all other deck cards in hand",
            StackPlayerDeckWithEmptyDiscard),
        Bind("stack-player-deck-leave", TranscriptStepKind.Given,
            @"these cards are next on seat (?<seat>\d+)'s player deck",
            StackPlayerDeckLeavingRemainder),
        Bind("set-player-hand", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s hand contains exactly these cards", SetPlayerHand),
        Bind("set-empty-player-hand", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s hand is empty", SetEmptyPlayerHand),
        Bind("set-card-readiness", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<state>ready|exhausted)",
            SetCardReadiness),
        Bind("set-card-damage", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) damage",
            SetCardDamage),
        Bind("set-card-counters", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) (?<type>[a-z-]+) counters?",
            SetCardCounters),
        Bind("set-acceleration-tokens", TranscriptStepKind.Given,
            @"the main scheme has (?<count>\d+) acceleration tokens?",
            SetAccelerationTokens),
        Bind("set-identity-face", TranscriptStepKind.Given,
            @"seat (?<seat>\d+) shows identity face (?<face>\d+[a-z]?)",
            SetIdentityFace),
        Bind("set-villain-stage", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is the faceup villain",
            SetVillainStage),
        Bind("place-ally", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is an ally controlled by seat (?<seat>\d+)",
            PlaceAlly),
        Bind("place-support", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is a support controlled by seat (?<seat>\d+)",
            PlaceSupport),
        Bind("engage-minion", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is a minion engaged with seat (?<seat>\d+)",
            EngageMinion),
        Bind("engage-facedown-drone", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is a facedown Drone minion engaged with seat (?<seat>\d+)",
            EngageFacedownDrone),
        Bind("clear-facedown-drones", TranscriptStepKind.Given,
            @"seat (?<seat>\d+) has no facedown Drone minions",
            ClearFacedownDrones),
        Bind("place-side-scheme", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is a side scheme in play",
            PlaceSideScheme),
        Bind("place-obligation", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is an obligation in seat (?<seat>\d+)'s play area",
            PlaceObligation),
        Bind("place-player-discard", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) starts in seat (?<seat>\d+)'s discard pile",
            PlacePlayerDiscard),
        Bind("attach-identity-upgrade", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is an upgrade attached to seat (?<seat>\d+)'s identity",
            AttachIdentityUpgrade),
        Bind("attach-card", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is attached to card (?<host>\d+[a-z]?) copy (?<hostCopy>\d+)",
            AttachCard),
        Bind("give-card-status", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has a (?<status>stunned|confused|tough) status card",
            GiveCardStatus),
        Bind("stack-encounter-deck-discard", TranscriptStepKind.Given,
            "the encounter deck contains only these next cards with all other deck cards in the encounter discard pile",
            StackEncounterDeckWithDiscard),
        Bind("stack-encounter-deck-dealt", TranscriptStepKind.Given,
            @"the encounter deck contains only these next cards with all other deck cards dealt facedown to seat (?<seat>\d+)",
            StackEncounterDeckWithDealtCards),
        Bind("stack-encounter-deck-leave", TranscriptStepKind.Given,
            "these cards are next on the encounter deck", StackEncounterDeckLeavingRemainder),
    ];
}
