# Actual Bring the Hammer Down authoring

This bounded packet authors actual 90004 on base `c87159ea3f2615e5fc25d14362eb29477d8f919a`. The production book has 723 rows. All 722 prior raw JSON objects remain byte-exact; the book is UTF-8 without BOM and LF-only. Candidate book SHA-256: `4b1032c2d6b512708ab58549fe49da010337dad7b045b803af950e179596a3c9`.

## Authority and composition

Joined `datasets/cards/cards.json` 90004 instructs Ronan the Accuser to activate against his engaged player, or gains surge if he is absent. Its boost condition deals the first player one facedown encounter card if this activation defeats a character. Printed boost value is three. The vendored MarvelCDB FAQ queried 90004 but contains no card-specific ruling. Physical quantities and publisher authority are pinned in [the source record](ronan-source-pin.md).

The reveal uses existing `eachPlayer` frames with `playerOrder`, a title-filtered `cardsIn yourPlayArea` selection, and the engaged player's own form. Only the frame containing the live Ronan schedules an attack or scheme. Explicit player order avoids an irrelevant ordering decision. The absent branch gains surge for the original revealing player. These DSL spellings and frame composition are engine choices.

The boost registers `afterActivation`, then tests accepted `activationDefeatedCharacters` with `atLeast` one and deals exactly one card. It reads the receipt, never remaining health, raw attack damage or current card zones. RR 1.8 `rr:boost-boost-icon.3` excludes boost-ability damage; `rr:activation.7` completes an initiating effect after the activation fully resolves. The independent [activation outcome review](activation-defeat-outcome-review.md) establishes the queued activation boundary (`rr:activation.8`), continuation through delayed choices, identity elimination and a two-defeat receipt. Those synthetic prerequisite tests are separate evidence, not actual 90004 coverage. The older [composition diagnostic](ronan-hammer-composition-probe.md) remains a historical synthetic probe.

## Actual-book behavior

`RonanHammerActualCardTests` loads the production book through `RonanRemainingFixture`, with no replacement or synthetic 90004 row. Seven cases prove:

- Revealing for player 1 activates Ronan engaged with player 0 in hero and alter-ego form; another engaged minion does not activate. Cleanup completes.
- Ronan in the discard pile is absent: the revealing non-first player receives exactly the known top encounter card facedown through the full reveal procedure.
- Equal five-damage attacks distinguish a defeated ally from a surviving ally. Only the lethal attack deals one facedown card, to first player 0 rather than defender/controller 1; draining again does not repeat the deal.
- An actual Concussive Blast boost defeats the ally during a scheme that also uses actual 90004. Boost-only defeat does not qualify and no encounter card is dealt.
- One overkill attack defeats the ally and its identity but deals only one card to the surviving first player. A following nonlethal activation with another actual 90004 does not borrow the preceding receipt.

The 24 exact production-row-count methods now require 723; the authored-face inventory includes 90004. The existing public-admission negative assertion still passes: Kree Fanatic remains excluded without its separate setup/admission packet.

## Verification

Windows worktree: `C:/Users/mggar/Source/marvel-ronan-hammer-actual`. Logs are outside the repository under `C:/Users/mggar/Source/evidence/ronan-hammer-actual-`.

- `focused.log`: 33/33 passed, selecting all seven actual cases, the 24 exact count methods and two citation checks. The precise count filter is `C:/Users/mggar/Source/evidence/ronan-hammer-count-filter.txt`.
- `mutant.log`: valid production-row mutant changed both literal one-counts in 90004 to two (threshold and dealt count); two of seven actual cases failed, distinguishing missed single defeat and excess encounter-card quantity. It parsed and executed normally.
- Exact original book bytes were restored. `restored-build.log`: forced non-incremental Release build passed. `restored.log`: same 33/33 selection passed.
- `data.log`: 193/193 passed, selecting the complete `Marvel.Content.Tests.Cards` namespace, actual 90004, accepted `ActivationDefeatOutcomeTests` and rule citation tests.
- `git diff --check` passed before commit; normal commit hooks are enabled.

Initial compile failures were fixture-only: missing timing namespace and an incorrect boost helper name. The first executable reveal checks found an unwanted mandatory ordering prompt from default `eachPlayer`; declaring existing `playerOrder` resolved it before all passing evidence above.

No engine, setup, public admission, replay or session contract is changed. No full managed lane, public gameplay/save journey, native UI, Linux verification or whole-pack acceptance is claimed. Independent review of this actual row remains separate.
