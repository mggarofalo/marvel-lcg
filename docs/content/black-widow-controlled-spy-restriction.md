# Controlled Spy character play restriction (MARVEL-770 / B5)

The canonical printed text for Spycraft (`08018`) and Espionage (`08033`)
requires: “Play only if you control a Spy character.” The catalog exposes this
prerequisite independently from a trait required on the acting identity. The
engine checks it before offering a price and again before committing play.

Authority is `datasets/cards/cards.json`, `rr:character`, and
`rr:ownership-and-control.8`. Identities in either form and allies are characters;
“you control” refers only to cards currently in play under that player's control.
The implementation uses `EffectiveCards.Kind`, `CardControl.ControllerOf`, and
`Traits.Has`, including current grants, losses, and temporary profiles. Physical
ownership, another player's Spy, an out-of-play Spy, and a Spy support do not
establish this prerequisite.

The vendored MarvelCDB FAQ records both faces as queried without entries. The
Hall of Heroes rulings mentioning these cards concern their interrupt timing,
reveal cancellation, and surge; none changes this printed play prerequisite.
No ability row, setup recipe, product boundary, or public admission changes here.

Focused evidence is in `ControlledCharacterPlayRestrictionTests`, alongside the
existing `IdentityTraitPlayRestrictionTests` and `RuleCitationTests`: 24 cases
pass in Release. Test-local empty ability rows isolate play legality; they do not
implement either card's interrupt or establish playable Black Widow content.
The same-cost Aunt May control verifies that otherwise eligible cards can be
priced in the negative fixtures.

The current-control predicate was replaced with `true` in a compiled mutant.
The focused ownership theory fails for a physically owned Spy controlled by
another seat and for another seat's own Spy. Sources were restored, rebuilt
with `dotnet build ... --no-incremental`, and the same 24 cases passed. Repeated
price queries and rejected stale play preserve the card digest, full RNG state
and consumption, continuous effects, pending agenda frames, and pending prompt.
Subsequent paid play matches an unprobed control's events and complete snapshot.

Local logs are outside the repository under
`C:/Users/mggar/AppData/Local/Temp/bw-spy-*.log`. These are focused Windows engine
checks. Independent review and combined integration gates remain lead-owned;
this packet does not claim a full managed, native, or cross-platform run.
