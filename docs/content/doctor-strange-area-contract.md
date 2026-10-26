# Typed Invocation pile contract — MARVEL-817

Candidate `feat/invocation-area-contract`, based on `8506121c`. This is the
public area-meaning portion of MARVEL-817, not complete prompt/native acceptance.

`WorldDescriptor.InvocationDecks` carries detached owner/deck/discard area IDs
from each engine-initialized `Seat.InvocationDeck`. The descriptors contain no
card IDs or order. They remain public during mulligan, when the existing area
filter still conceals every Invocation face. After mulligan, discard and recycle,
the same filter grants the top and public discard only. Both owner seats and all
four visibility policies are covered. Generic additional areas gain no Invocation
meaning merely from their zone spelling.

Board presentation labels those typed piles `INVOCATION DECK` and
`INVOCATION DISCARD`, retaining seat ownership and existing generic area layout.
It does not parse card names or text to classify them. This does not establish
that the resulting Godot layout, payment, grouped choices or lifecycle explanation
is usable; the remaining MARVEL-817 native and prompt work remains required.

Protocol 42 adds `world.invocation_decks` and rejects protocol 41 at the existing
host boundary. Publication validates unique owners, unique correctly owned piles,
zone roles and absence of a host. Invalid/dangling/ambiguous references raise a
codec error before client publication. Both embedded and socket tests verify the
same fields and independent source/consumer collection storage. The sole engine
read allowance is the typed deck's public area identities; no hidden card access
is added. Replay v27, save schema 7, RNG and digest formats remain unchanged.
Current runtime identity checks and release-script expectations name protocol 42;
historical fixture bytes and recorded evidence are unchanged.

## Verification

Logs under `C:/Users/mggar/Source/evidence/invocation-area-contract-*`:

- `view-final.log`: full View, 371 passed.
- `server-final.log`: 41 affected transport, snapshot and recovery checks passed.
- `old-peers.log`: eight version-rejection cases passed, including protocol 41.
- `desktop-identity.log`: seven managed desktop identity/root checks passed.
- `release.log`: all 36 release policy checks passed.
- `architecture-restored.log`: 29 boundary/layout/test-quality checks passed.
- `owner-mutant.log`: compiled removal of area ownership validation kills the
  wrong-owner case; ten other contract cases remain green.
- `piles-mutant.log`: compiled deck/discard swap kills both owner-seat projection
  cases. Restored nonincremental builds are recorded in `restored-build.log` and
  `view-restored-build.log`; subsequent restored View and Server runs pass.

During test authoring the digest check first compared record identity instead of
canonical content. It now compares `Canonical()` plus RNG word consumption; this
was a test defect, not a product mutation. An earlier attempted swap-mutant run
did not compile and is not counted as mutation evidence. The final logged swap
run compiled and failed on the exact swapped area IDs. Initial architecture
failures were corrected with the precise new engine type/member allowances and
removal of an unused client type allowance; no broad dependency gate was weakened.

Normal hooks and independent review are required before integration. No new
public setup recipe, production card row, native artifact or whole-pack
certification is supplied by this packet.

## Independent contract review — 2026-10-26

Reviewed source `f3542f7a46be231a53a682ca40b70b704cc2fa90` against
`8506121c` in `review/invocation-area-contract`. Verdict: accepted for the
partial typed public area contract. No production correctness or privacy defect
was found in this scope. Two stale current-version statements in
`docs/release-policy.md` are corrected to protocol 42; historical protocol 41
change notes and archived artifacts remain intact.

`InvocationAreaContractReviewTests` adds seven independent synthetic cases.
They distinguish duplicate owners with otherwise valid distinct piles, ambiguous
seat ownership, exact empty typed piles among generic additional piles for two
seats, and isolation of prior snapshots and board labels from subsequent source
and consumer mutation. The wire shape contains only owner/deck/discard; three
negative cases reject added card IDs, order and public-top metadata. Exported
collections own detached storage; these tests do not claim consumers cannot
mutate their own detached copies.

Independent logs under `C:/Users/mggar/Source/evidence/invocation-area-review/`:

- `server-baseline.log` and `server-restored.log`: 18 passed, comprising the
  complete new review class and existing embedded/socket Invocation transport class.
- `owner-uniqueness-mutant.log`: a compiled mutation accepting an already-seen
  owner fails precisely the new distinct-pair case; the other 17 cases pass.
  Production source is byte-restored in a `finally` block, followed by the
  nonincremental build in `restored-build.log` and the green restored run.
- `view.log`: full View, 371 passed, including the engine-backed lifecycle and
  all four viewer scopes for each owner seat.
- `host-identity.log`: complete setup/compatibility and build-identity classes,
  27 passed, including all eight old-version rejections through protocol 41.
- `release.log`: full release policy, 36 passed.
- `architecture.log`: complete presentation source/assembly, source-layout and
  test-quality classes, 29 passed.
- `commit.log`: normal pre-commit hooks and commit outcome.

The first review-test build hit analyzer CA1861 on an inline expected array; the
assertion now compares the exact joined property names. This was a test-authoring
defect and is not a product finding or a mutation result.

This review does not complete MARVEL-817. Grouped-choice composition, costs,
source/context and lifecycle explanations, native interaction evidence and any
required owner acceptance remain separate work. No engine behavior, ability book,
public admission recipe, Godot UI or historical fixture is changed by this review.
No full Server suite, whole-content certification, native run or second-platform
execution was performed here; those claims are outside this bounded contract review.
