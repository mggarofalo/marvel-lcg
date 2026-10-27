# Kang Dominion storage candidate — MARVEL-868

Prepared on base `c5d8b1c7092b3b9411ca95b6187d7b72dd0851b5` for independent
review. This is a nonpublic engine prerequisite, not complete pack acceptance.
The delivery lead records the exact implementation commit in MARVEL-868/568.

Printed authority is `datasets/cards/cards.json`: 11009b–11012b store one
set-aside Dominion facedown beneath 4A when the local stage completes; removal
of the local stage and matching II waits until phase end. 11013a reveals III
before revealing each stored Dominion. Current RR `rr:reveal.step.1`–`.4`
governs ordinary encounter revelation. The local grouping and whole-group join
follow `datasets/rules-packs/mc11/game-areas.md`; the clarification forbids
transferring attachments/statuses to III. The vendored FAQ/rulings search adds
no storage or reveal-order exception. Dominion's damage prohibition and credited
defeat reward are separate printed behavior, outside this storage prerequisite.

The test-local completion Forced Response uses `captureKangLocalCompletion`.
Its actual completion occurrence marks the stage complete before the response;
the scenario policy prevents generic advancement or loss. One physical Dominion
is selected by ascending object id, an engine choice among identical set-aside
copies. Storage retains physical ownership, host identity and facedown state.
The only out-of-play host permission admits scenario-owned 11023 from the
set-aside area into concealed storage beneath scenario-owned, never-entered 4A.
Other hosts, other cards, other hosted zone kinds and a departed/reentered 4A
retain refusal. Normal in-play host validation remains active.

The local scheme and II retain their captured incarnations until phase end,
then leave before the existing destination/unique-copy choices. The existing
phase owner survives the join and central transition. III's full reveal finishes
before each stored physical Dominion reveals through normal encounter windows;
4B waits for all children. Storage insertion order is the engine's deterministic
choice. Captured physical incarnations prevent a departed/reentered stored copy
from being revealed by a stale continuation. No digest, RNG, session or protocol
format changes are made. Existing zero-stored-Dominion tests remain relevant.

Windows Release evidence is under `C:/Users/mggar/Source/evidence/`:

- `kang-dominion-first-focused.log`: initial compiled 24-case slice and controls.
- `kang-dominion-expanded-content.log`: expanded Kang regression, 109 passing.
- `kang-dominion-privacy.log`: actual-card concealment for cooperative, both
  restricted seats and no-seat scope; physical ids, faces and storage movement/
  attachment subjects stay hidden until authorized revelation.
- `kang-dominion-incarnation.log`: nine passing focused cases, including actual
  two-Dominion suspended reveals and stale-copy/old-host controls.
- `kang-dominion-mutant-{host-permission,storage,phase-removal,reveal,stored-incarnation,host-incarnation}.log`:
  each distinguishing compiled mutant fails. The storage movement mutant uses a
  runtime impossible-id guard; an initial unreachable-code variant was discarded.
- `kang-dominion-restored-build.log`: restored nonincremental Release build.
- `kang-dominion-restored-content.log`: final affected content regression,
  including duplicate completion refusal and ordinary 4B completion loss.
- `kang-dominion-restored-view.log`: final complete View suite.
- `kang-dominion-commit.log`: initial normal hook attempt, rejected for complexity.
- `kang-dominion-decomposition-complexity.log`: unchanged-threshold focused
  complexity check after cohesive extraction of validation, storage permission,
  local villain matching, ordered reveal suspension and reveal-order assertions.
- `kang-dominion-decomposition-build.log`: nonincremental Release Content build
  after extraction, with no warnings or errors.
- `kang-dominion-decomposition-content.log`: post-extraction affected Content
  regression, filtered to Kang, MainScheme, HostedInvocation and UpgradeHostCost.
- `kang-dominion-decomposition-view.log`: post-extraction complete View suite,
  440 passing.
- `kang-dominion-decomposition-commit.log`: final normal hooks and commit result.

The decomposition preserves the candidate's conditions, assertions, physical ids
and order. The six compiled distinguishing mutants above were killed before
this extraction; mutation runs were not repeated after extraction.

The two-Dominion test reaches real local completions, suspends each ordinary
reveal on a synthetic choice, and proves III/Dominion/4B order and physical
identity. Its synthetic departure/reentry branch isolates stale incarnation
refusal. The View test prepares actual physical storage directly to isolate the
information boundary; it does not claim a public journey. The overlay leaves
Dominion's damage prohibition and defeat reward unauthored, and does not claim
those behaviors or a final victory with an active Dominion.

Remaining scope: all-player local elimination completion, solo/three/four-seat
composition, simultaneous local departures and ordering with competing delayed
phase-end work remain outside the bounded controller. No production ability rows,
whole-product admission, public save/replay journey, native interaction, Linux or
full managed suite is claimed. Existing final III victory is a separate accepted
prerequisite. Independent review is required before this packet is accepted.
