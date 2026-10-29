# Kang Temporal: Time Portal defeat prerequisite

Research note for MARVEL-568. It records whether the engine can already express Time Portal's replacement before the four Temporal rows (11030 Ancient Warrior, 11031 Chitauri Soldier, 11032 Tyrannosaurus Rex, 11033 Time Portal) are authored. It does not admit Kang or any Temporal card.

## Authority

- `datasets/cards/cards.json` face `11033`: "Forced Interrupt: When this scheme is defeated, shuffle it into the encounter deck instead of discarding it."
- `rr:defeat.1`: "If an ally, minion, or side scheme is defeated, it is discarded."
- `rr:replacement-effect`: "A replacement effect replaces a specified effect with a different effect."
- `rr:leaves-play.2`: "Return each token and status card on that card to the supply."
- `ruling:22d159cabcc98d45`: with Red Skull's side-scheme deck, "shuffle Time Portal into the encounter deck instead of placing it in the side scheme discard pile."

## Question

Does the existing `shuffleInto` effect, run as a Forced Interrupt on `WhenCardDefeated`, replace the discard, or does defeat finalization discard the card afterwards anyway?

## Trace

The defeated-card window suspends the scheme's defeat, so finalization is scheduled as a `DefeatCompletionOperation` that carries the scheme's incarnation. `DefeatProcedure.FinalizeScheme` moves the card only if it still has that incarnation and is still in play. A card the interrupt has already shuffled into the encounter deck fails both checks, so the discard does not happen.

## Probe

`tests/Marvel.Content.Tests/Play/TimePortalDefeatPrerequisiteTests.cs` uses a test-local 11033 row (`shuffleInto {cards: this, deck: encounterDeck}`) and the ability-less side scheme 01109 as a control, both on the ordinary encounter topology and on Red Skull's. It places threat and a counter on the scheme, defeats it, and finishes the agenda.

All four cases pass. The portal ends in the encounter deck, never in a discard pile, with exactly one `Shuffle_Into` move and no tokens left on it. The control goes to the matching discard pile. Removing the incarnation and in-play guard from `DefeatProcedure.FinalizeScheme` fails both portal cases, so the probe exercises the guard rather than passing vacuously.

## Result

No engine prerequisite blocks the Temporal rows. The four rows can be authored with full-lifecycle tests through the existing `shuffleInto` effect, and the existing rows stay unchanged.
