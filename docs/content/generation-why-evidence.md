# Generation Why? authored face evidence

Base: `2b39822e`, including the independently reviewed global-support counting
prerequisite (`d7a4d551` integration and `2b39822e` review). This packet appends
actual face `05026` to the unmodified full ability book: 570 distinct authored
faces, including 28 of 34 Ms. Marvel faces. Public admission remains closed.
Melee integration is separate and will change the combined inventory.

## Authority and representation

Joined `datasets/cards/cards.json` prints a side scheme with starting threat
`2*`, hazard 1, boost 3, and: "Discard the top card of each player's deck for
each ally and Persona support in play."

Vendored `ruling:e8654dd34b58be12` (June 2023, Alex Werner) fixes that combined
count at initiation; a subsequently entering White Fox does not increase it.
The reviewed global `allies` and Persona-filtered `supports` queries feed a
single `discardTop`, `player: each` instruction. Its existing synchronous
operation evaluates the numeric expression before traversing player decks.
No new engine operation or timing window is introduced. This packet does not
claim an interleaved White Fox response test; the prerequisite evidence explains
the current scheduling contract.

`rr:player-deck.3` stops discarding from a newly shuffled player deck;
`rr:player-deck.1` immediately deals that player a facedown encounter card.
Printed starting threat and icons remain card facts rather than authored grants.

## Behavioral evidence

`GenerationWhyCardTests` uses the actual full book and schedules
`RevealEncounterCard` through `Sequence`, including entry and completed reveal.
Four cases establish:

- Either revealing player discards four cards from both decks when two allies
  and two Persona supports are in play. Ordinary supports and hand Personas do
  not contribute. Assertions identify the exact discarded physical cards.
- The scheme stays in play with four threat and one hazard. A subsequent real
  encounter-dealing stage gives the non-first seat the extra hazard card when
  that seat holds the first-player token. Printed boost remains three.
- A two-card deck resets once and receives one encounter card, while the other
  player discards all four. The reset deck retains both original physical cards.
- With only ordinary supports in play and allies/Personas in hand, neither deck
  changes or consumes randomness, while scheme entry and its hazard still work.

The prerequisite's broader synthetic cases retain empty/exact exhaustion,
eliminated recipients, effective-profile and projected membership, query purity,
and numeric quota freezing coverage; these are not duplicated as played games.

## Validation

The Release focused run and restored run each pass 51 tests: the four actual-face
cases, ability-data and lowering tests, and `GlobalPersonaCountTests`.
Logs outside the repository:
`C:/Users/mggar/Source/generation-why-{focused,restored}.log`.

A data mutant changes only 05026's Persona support selector from `supports` to
`allies`; the same compiled test binary reads the modified book. Three actual
face cases fail at expected four versus actual two discarded cards, and the
zero-count control passes. Restoration occurs in `finally`, and the original
51-case filter then passes. This mutates data rather than compiled sources, so
there is no stale compiled mutant to rebuild. Evidence:
`C:/Users/mggar/Source/generation-why-mutant.log`.

A byte comparison against `2b39822e` proves all 571,712 bytes before the final
array closing suffix are preserved. Parsed rows before the appended row also
match exactly; no duplicate ids are introduced. `git diff --check` passes.
The explicit authored-face inventory admits only the new face. No generated
dataset, setup recipe, engine, presentation or public catalog files change.

Full content regression and normal commit-hook results are recorded in
`C:/Users/mggar/Source/generation-why-{content,hooks}.log`. This packet supplies
engine/content evidence for independent review; no native or public Ms. Marvel
journey is claimed.

## Independent actual-face review

Reviewed exact candidate `4086334599a08e29e53fb780786e0fa379167a7f`,
including the Core correction `600e18b3a253b88a8fd61100b43b0b3c0d52a8a2`.
Raw joined card facts and vendored ruling `ruling:e8654dd34b58be12` agree
with the row. Inspection of `AbilityCardDiscardExecution.DiscardTop` confirms
that the numeric count is evaluated before the player-deck loop; there is no
intervening player response window inside that synchronous instruction. The
existing prerequisite tests distinguish frozen amounts, empty/exact exhaustion,
effective support membership and projections; these remain synthetic contracts.
The actual full-reveal tests independently retain exact physical discards,
both revealing seats, non-Persona and out-of-play exclusions, zero-count RNG
purity, short-deck reset, starting threat and subsequent hazard dealing.

Independent Release focused and restored runs pass 108 cases, covering actual
05026, global-count contracts, both-direction Get Behind Me! tests, ability data
and lowering, and the unchanged pinned Core card-face transcripts. Logs are
`C:/Users/mggar/Source/generation-core-independent-{focused,restored}.log`.

Two separate actual-row data mutants are rejected by the actual-face tests:
changing `PERSONA` to `LOCATION` fails the zero-count control (expected zero,
actual two discards); changing the recipient from `each` to `you` fails three
cases because the other player's deck is untouched. Logs are
`generation-core-independent-generation-{trait,recipient}.log` in that external
directory. Each mutant uses the compiled candidate tests, changes only data,
and restores the exact original bytes in `finally`; no compiled mutant exists.

Verdict: accepted for this authored face and its existing engine contracts.
This is engine/content acceptance; complete Ms. Marvel public admission and a
public/native whole-pack journey remain separate work.
