# Kang local elimination repair

Base: independent review commit `ddb5199f9d90e25be7cedf833a292129422693ea`,
reviewing source `d557ecc10649d93c57ca0ba77cf3932a3ef685da` against accepted
base `c91cd4f26ecb0e317a9dba51703da4b6e76b6e85`. The repair revision is the
commit containing this record. The original implementation and independent
review records remain unchanged.

The two findings concern engine-owned retained execution invariants, with the
same authority as the reviewed packet: actual card faces 11009b–11012b,
`rr:player-elimination.step.5`, `rr:player-elimination.4`, and MC11 game-area
reunion rules. These synthetic topology and suspension probes express engine
boundaries; printed rules do not prescribe the capture representation.

Elimination preflight reads the outstanding Kang join or departure-order
operation and verifies its exact outstanding phase owner. A retained batch owns
both source captures and possible destination groups. Its unsupported elimination
boundary therefore applies even after local 3B removal and transfer out of
`Pending`. Rejection precedes elimination's capture, seat, card and topology
mutations. This makes no rollback promise for damage placed before elimination.

Before elimination mutates, original pending captures for the player's source
must still match that source's complete group. A successful elimination removes
only its player from the original captured list; it cannot import arbitrary
members from current topology. Existing malformed eliminated-seat and arbitrary
topology guards remain intact.

The departing-source suspended-join regression is unchanged. The second review
regression retains its uncontrolled prior join and later phase-boundary rejection
control, and now also requires rejection at `Elimination.Eliminate`, unchanged
digest/topology/events/RNG/pending snapshot, identical pending capture, and a live
seat. Its original form remains in the review commit; the earlier rejection is
required to establish elimination atomicity. Additional controls cover suspended
ordering sources and destination, plus two valid eliminations from an actual
previously joined group followed by successful departure.

Verification: Windows, Release, serial execution, node reuse disabled and `-m:1`.
Fresh nonincremental Content and Rules builds passed with zero warnings/errors.
All 163 Kang tests and all 65 Rules tests selected by `Elimination|Places` passed.
The existing suite covers survivor flow, local completion/Dominion storage,
phase-end removal/reunion, final survivor advancement and all-player loss.

Four compiling mutants were killed by the complete local-elimination test class:

- Disable join-operation ownership check: two suspended-join failures.
- Disable departure-order ownership check: ordering rejection failure.
- Invert the pending preflight source filter: stale-group pre-mutation rejection failure.
- Retain the eliminated member instead of the survivors: two capture shrink failures.

Each mutant had a successful fresh compile before its failing test run. Sources
were restored, rebuilt nonincrementally, and all 163 Kang tests passed again.
Normal commit-hook results are included in the handoff for this revision.

Raw evidence is outside the repository under `C:/Users/mggar/Source/`:
`kang-repair-build.log`, `kang-repair-content.log`, `kang-repair-rules-build.log`,
`kang-repair-rules.log`, `kang-repair-mutant-*-{build,test}.log`,
`kang-repair-restored-build.log`, `kang-repair-restored-content.log`, and
`kang-repair-commit.log`. Exact mutant replacements and execution sequence are
in `kang-repair-mutations.ps1` there.

Remaining: independent repair closure, complete Kang setup/admission/public
replay, broader integrated/platform gates and native interaction evidence.
This bounded nonpublic repair does not establish Kang pack readiness.
