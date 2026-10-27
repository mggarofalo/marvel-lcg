# Ronan public fresh-host continuation

The four fixed checkpoints use normal public Kree Fanatic selection and
unmodified starters: Rhino Standard solo seed 1 after decision 10 (Ronan entry),
Rhino Expert solo seed 2 after decision 18 (The Accused attached), and Rhino
Expert multiplayer seed 4 after decisions 22 and 31 (Ronan boost entry and
phase-start engagement). Each checkpoint asserts the actual face, zone and seat.

The tests reuse the existing serialized restart harness. Each creates a new
factory, host and game from copied strict save bytes and authority metadata,
compares pending work, complete RNG state, digest, revisions, views and history,
rejects stale input without state/save change, and compares exact subsequent
events. A second independent fresh host follows one accepted continuation;
both twins then run to the terminal outcome with the same legal answers.
No gameplay state or opening hand is arranged.

Four cases pass on the reviewed elimination-cleanup candidate `8a35a678`.
Log: `C:/Users/mggar/Source/evidence/ronan-fresh-host-restored.log`.
The initial test attempt compared a resolve response with a sync response after
the second restart, so three cases failed on transient EventFaces. The harness
already normalizes the restarted source to Sync; the second restart now uses
the uninterrupted twin as source, preserving the full comparison. No production
code or assertion was weakened. That initial failure is fixture evidence, not a
runtime defect or accepted run.

These checkpoints cover conventional modular interactions through the existing
client/session contracts. Existing native selector and contract suites remain
the UI regression lane; no new interaction or human content approval is needed.
Independent review and final combined certification remain required.

## Independent restart review

Reviewed `a4c69d7fd61b7c0013f225571057391a7653cf35` in the isolated
`C:/Users/mggar/Source/marvel-ronan-restart-review` worktree. The unchanged
four-case test assembly passed independently in 2.92 minutes; log:
`C:/Users/mggar/Source/evidence/ronan-fresh-host-independent-review.log`.

The factory delegates directly to current `DatasetGameFactory`; it neither
changes the ability book nor prepares game state. Each restart creates its own
factory, store, host, game and world. Strict save serialization and independently
copied authority metadata retain the original archive. Equality includes the
complete generator state, outstanding agenda, effects, prompt, serialized save,
revision, world and history; accepted continuations also compare ordered events.
Stale rejection preserves state and save, with the same answer subsequently
accepted as its compatible control. The second restart uses the original twin,
so both views are Sync snapshots without removing EventFaces from comparison.

The review strengthens the Accused checkpoint with its actual identity host:
printed `90003` says "Attach to your identity." Zone and seat alone would permit
an incorrect host. All four cases pass again with this assertion (2.38 minutes):
`C:/Users/mggar/Source/evidence/ronan-fresh-host-independent-strengthened.log`.
No production code, policy, replay fixture or existing assertion changed.

Verdict: accepted for these four public restart checkpoints, including exact
continuation to terminal outcomes. The facedown and elimination repairs remain
separately reviewed prerequisites. Combined delivery and platform certification
remain outstanding; this review does not claim complete modular branch or UI
coverage.
