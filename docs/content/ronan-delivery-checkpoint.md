# Ronan: complete Kree Fanatic delivery candidate

Parent: MARVEL-567. This is the five-face, seven-card print-and-play modular set,
not the Galaxy's Most Wanted Ronan scenario. The pinned official PDF and exact
inventory are in [source authority](research/remaining/ronan-source-pin.md).
Quantities are 90001/90002/90003 once each and 90004/90005 twice each.
All five actual authored rows have independent review. The integrated book has
723 rows, SHA-256
`4b1032c2d6b512708ab58549fe49da010337dad7b045b803af950e179596a3c9`.

## Integrated proof

- [Public admission](ronan-public-admission.md), independently reviewed in
  [admission review](ronan-public-admission-review.md), covers complete-pack
  rejection controls, exact recipe quantities, both transports and all 80
  supported scenario/mode/player-count setups.
- [Four fixed public games](ronan-public-games.md) complete with unchanged
  starters/seeds and exact accepted-decision replay. The original failed seed-3
  archive is preserved; it is not substituted with an easier game.
- [Fresh-host continuation](ronan-public-restarts.md) has four independently
  verified natural entry, attachment and engagement checkpoints. Two independent
  serialized restarts preserve authority/save bytes, full RNG state, pending
  work, views and history; stale input is rejected and terminal events match.
- Enemy-attack defeat receipts and activation-local defeat outcomes have separate
  implementation/review evidence. The actual card reviews are under
  `research/remaining/ronan-90001-*`, `ronan-90002-authored.md`,
  `ronan-two-face-independent-review.md` and `ronan-90004-*`.
- [Facedown-window review](facedown-window-independent-review.md) closes the
  actual Highway Robbery/Great Responsibility failure. Facedown attached cards
  cannot offer printed abilities; explicit legal event origins remain supported.
- [Elimination review](elimination-owned-independent-review.md) verifies owned
  cards outside the eliminated player's area, ordered minion transfer followed
  by owned-card disposal, projections and canonical transcript changes.

Replay contract v28 is the unpublished aggregate boundary; v27 journals are
explicitly rejected. Existing frozen Core/RoRS routes still require exact replay.
[Played campaign reconciliation](played-campaign-v28-reconciliation.md) proves
the corrected elimination moves cause earlier rejection of incompatible old
campaigns, without changing their source files or losing the original later
divergence controls. No protocol 42, save 7, RNG or digest-v3 format change occurs.

## Verification and remaining gate

The integrated unit run at `e1dd181d` passed 9,894 tests, including 8,083 Content
tests, before the ownership-wide elimination repair. At `bb1ec14e`, integration
passed 36 Release, 129 architecture and 992 Godot tests (seven native opt-ins
skipped), plus 699 Server tests; three Server diagnostic expectations failed.
Those exact failures are repaired with causal evidence, and all 11 affected
played-campaign tests pass on the integrated repair. The full acceptance lane
passes **197/197** at `87769c34`.

A remaining synthetic owner-shuffle fixture is reconciled with normal elimination
removing the original card, then an explicitly introduced late synthetic source
tests absent-owner routing. Its six focused cases pass. This changes no runtime.

Logs are `C:/Users/mggar/Source/evidence/ronan-integrated-*.log` and
`ronan-owner-shuffle-reconciliation.log`. These are revision-specific results,
not a claim that the final combined CI has passed. Final CI must certify the
candidate before merge and parent closure. Existing native selector/interaction
and package gates cover the reused interaction classes; no blanket human content
approval is required.

Hulk PR #458 is merged at `f256e30b` after all eleven jobs passed on `0edf7c9a`.
The Ronan candidate contains that exact tree and records the tree-identical
squash ancestry in `75dd7175`; the audit is `ronan-hulk-squash-audit.txt`.
Managed CI receives the same 90-minute bound as platform gates: the certified
Hulk managed job took 58m31s against its 60-minute bound, and Ronan adds eight
complete-game/restart cases. Test selection and correctness assertions are unchanged.


## Final fixture reconciliation

The post-cleanup unit attempt at `f5a03322` passed all five non-Content projects
(1,815 tests). Content reported three failing cases: two eliminated-owner Clea
fixtures and the standard/expert transcript assertion containing two stale Iron
Man elimination pins. The run was stopped after those findings amid machine
memory pressure; it is not a completed passing unit run. Its full log is
`C:/Users/mggar/Source/evidence/ronan-final-unit.log`.

The Clea fixtures now first assert that normal elimination removes the original
foreign-controlled owned card, then introduce explicitly synthetic late copies
to isolate absent-owner routing. Every original shuffle, RNG, interrupt and
controller assertion remains. For each Iron Man transcript, independent full
canonical comparison found exactly 40 changed RemovedArea indices, with all
other fields identical; both old/new hashes match the pins. The root independently
rechecked those snapshots before integrating `ac697031` as `eba0cbb7`.

All three affected complete test classes pass **20/20** on the integrated fixture
repair (`ronan-final-fixture-reconciliation.log`). No runtime change was needed.
Final CI remains responsible for the complete combined unit, integration,
acceptance, native and package gates before merge.
## Setup overflow repair — MARVEL-864

CI run `40495273740` at `93a996e5` exposed the same native failure on Windows
and Linux: `shell-finish-100-motion` could no longer see Start game after the
additional Kree Fanatic choice wrapped onto another row. The Windows package
and Linux native shard 1 passed on that attempt; it is not final certification.

Source `4e80832e` (integrated as `a216bb14`) reserves a fixed setup footer while
keeping the complete choice list scrollable. The Start button retains its readable
typography and hides in join mode and after entering play. Independent review
`63e450e0` (integrated as `0f06f741`) passes the original full native case and a
pointer-driven lifecycle at 100% and 150%, including join/create, Kree selection,
entry into play, scale, motion and viewport resize. Both agents inspected captures;
no production change was needed during review. This restores the existing setup
interaction and requires no separate human content acceptance.

Source focused evidence passes 16 Godot, 129 architecture and three release
manifest tests, native manifest checks and the four-seat scenario setup case.
Normal hooks pass on source and review, including 66 architecture checks. The
source captures before behavior-preserving helper extraction are under
`C:/Users/mggar/Source/evidence/ronan-footer-final-render-100` and `-150`.
Independent final driver evidence is under `ronan-footer-independent-100-refactored`
and `ronan-footer-independent-150-final`; the original-case rerun is under
`ronan-footer-independent-shell`. New final CI must certify the integrated head.