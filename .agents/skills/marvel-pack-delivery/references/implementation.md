# Implementation and proof

Use the AGENTS routing table for `card-dsl`, timing, setup, rules citations,
behavioral specs, persistence and presentation guidance. Read only the concerns
the packet changes. Keep game legality/state transitions in engine-owned code;
extend visibility-safe contracts when the UI lacks necessary meaning.

## Before bulk card authoring

Before turning a ruling-based failing probe into an engine prerequisite, compare
the ruling's `rrg_scope` and date with the current vendored Rules Reference.
Read the current keyword/timing clause, including its changed definition; a
historical official answer is not automatically the current behavior. If they
conflict, inspect the official update explanation and the provenance graph before
dispatching implementation. Record which authority governs and why. For example,
RR 1.8 overkill requires actual defeat; pre-1.8 Loki and simultaneous-overkill
answers must not be used to restore the superseded behavior. An assertion based
on the older rule is a test finding, not evidence of an engine defect.

Map each new mechanic to its engine prerequisite and test a characteristic card.
Keep card DSL changes separate from engine mechanisms where practical. Confirm
cost assessment, choices and mutations happen in the correct order: a rejected
answer must preserve the prompt, state, RNG and pending effects, including on retry.
Challenge legal no-ops, ties, replacements, nested activations, departed targets
and multiplayer ownership only where the mechanic makes them relevant.

Check fixture card kinds, printed stats and resource encodings against the
generated catalog before diagnosing a failure. Scenario setup and initial draws
can change a prepared deck: assert the meaningful starting state after setup.
For admission regressions, include an otherwise valid same-cost control so a
missing offer cannot pass because the fixture was already ineligible. A mutant
only proves the check when it compiles, fails for the intended behavior, and the
restored revision passes that same check.

Restoring a compiled mutant with a timestamp-preserving copy can leave its binary
cached. Force a non-incremental rebuild of restored sources before claiming the
restored run passes. Serialize mutation, restoration, verification and hooks in
each worktree; preserve evidence outside the repository. A returned process ID is
not completion: harvest its exit before changing mutant sources, restoring them,
or starting another build/test/hook in that worktree. Independent worktrees do not
require a global build lock. Apphost/output-lock warnings or overlapping builds
invalidate a mutation verdict until an isolated compiled rerun distinguishes it;
do not classify an apparent survivor as equivalent from contaminated evidence.

Use the full encounter procedure when proving entry, surge and cleanup together;
a direct placement or When Revealed call may deliberately omit later lifecycle
steps. When extending a shared player/encounter operation, also run an existing
playerless scenario setup. Evaluate resolving-player context only on paths whose
printed instruction requires it.

After authoring rows, run the ability-data tests as well as characteristic card
tests. Reconcile explicit authored-face and starting-counter inventories with the
printed facts, and keep unknown-card negative controls genuinely unauthored.
Do not permit an entire incomplete pack merely to satisfy a face-inventory gate.

When an actual row replaces a test-local placeholder, inspect fixtures using that
card ID. Prefer the actual row when its behavior belongs in the test; otherwise
explicitly replace it inside the documented synthetic fixture. Keep duplicate-row
rejection intact. Passing tests on a smaller book do not prove fixture integration.

A deferred child can change the current agenda frame while an answer resolves.
Prove that answering completes the exact original frame, including attachment and
ordering choices. Direct-runner fixtures must drain the real continuation before
asserting final outcomes; do not weaken outcomes to accommodate pending work.

When adding a suspension point to an existing effect, compare its complete causal
events with the uninterrupted path, not only final state or same-candidate replay.
Check delayed placement, owned synchronous defeat/cleanup, simultaneous recipients
and later instructions in the same ability where reachable. Preserve the existing
forced-ability association policy across resumption without borrowing a nested
interrupt's or an ancestor's identity. Same-candidate replay can reproduce the
same missing metadata twice and still pass.

Use explicit UTF-8 when editing dataset and authority text. Inspect unrelated
non-ASCII diff changes; changing both a test input and its expected value can hide
encoding damage from the test itself.

For full activation probes, assert that the activation completed and no unintended
game-ending condition abandoned its responses. Supply legal encounter/player deck
state for the path under test. A missing attack-end response after encounter-deck
exhaustion ended the game is a fixture finding, not evidence of a receipt defect.

Capture causal facts when an effect needs them later. Current active villain,
current defender or current board membership may no longer describe the attack
that caused a continuation. Distinguish declared defense, completed attacks,
damage prevented/dealt, defeat and replacement; do not substitute one receipt for
another. Expected behavior must come from the card/rule/ruling, not these examples.

Use the selected ability body and reachable branch when deciding whether an event
has a qualifying effect. An inactive branch or another ability on the same card
does not establish eligibility. Preflight must consider payment-dependent branches
before committing costs; an empty payment receipt is not evidence of the eventual
branch. Compare the preview with execution after modifiers and printed caps.

When a bespoke engine effect implements a printed interrupt or response, preserve
its timing tier and ordering alongside data-authored abilities. A priority hook
must not silently resolve it before another simultaneous forced ability. For
example, Norman Osborn's would-attack replacement and Webbed Up require the
first player's ordering choice (`rr:forced.5`); status cancellation still has
priority (`rr:status-cards.2`). Prove both orders and suppression of the remaining
ability after replacement, using an already admitted conflicting card where possible.

Distinguish references to "you", "your hero" and "your identity" in both forms;
an enemy attack primitive may legitimately support alter-ego targets when a card
explicitly allows them. For player-specific ordering or selection, exercise a
non-first player so a hardcoded first-player prompt cannot pass unnoticed.

## Independent review

Give the reviewer raw authority, exact diff and evidence, without a desired verdict.
Ask which plausible wrong implementation still passes. New decisions should earn
distinguishing behavior tests and mutants under repository policy. Review cost
atomicity and identity/ownership across suspended choices, not just happy-path text.
Retain genuine historical fixtures; do not rewrite them to make current replay pass.
Trace the first differing prompt/event against the exact factory and ability book
used by the fixture before revising its expected result. Frozen content may retain
an old card interpretation even when the current book is corrected. A newly
exposed divergence needs explicit rejection, unchanged archive/no-publication
proof and a compatible nonempty control; it is not permission to retrofit the
archive or relax event comparison. Separately prove the corrected current card.

For shared-rule changes, run whole affected test classes where practical rather
than a handpicked method list that can miss an older supported interaction. When
a failure is called pre-existing, compare with the genuine pre-feature revision;
an intermediate commit already containing the feature does not establish that.
Preserve the old valid control and fix a demonstrated regression instead of
relabeling it an unsupported boundary.

A cost preview must account for the facts that earlier supported cost steps can
change before later legality checks: quantities, active sources, ownership and
area membership where relevant. Reuse engine-owned evaluation and payment order.
Use a failing interaction and a valid same-cost control to bound the repair;
do not build a second general interpreter or silently publish an invalid offer.
An unimplemented reached interaction must raise explicitly. Distinguish rejected
input, which must preserve its supported decision state, from an explicitly
documented unsupported execution boundary after committed costs; do not invent
rollback guarantees that the engine contract does not provide.

## Public integration

Exercise the production factory/session path, not only direct world construction.
Before a pack is complete, use nonpublic engine tests for its first capability.
Use a test-only production-path catalog only if an actual supported injection
seam exists; name it in the handoff rather than assuming one. Otherwise defer
the public journey until complete admission. Do not weaken the admission gate or
build a new bypass solely to claim an early vertical slice. A first scenario or
starter working does not authorize advertising an incomplete product.
Verify starter/scenario recipes, modes, player counts and campaign setup through
public contracts. Test whole-pack admission with a removed required face and a
meaningfully incomplete or wrong recipe. Preserve legal custom deck construction
and prove adjacent unsupported content stays excluded.

For shared setup changes, compare ordinary supported scenarios as well as the new
pack. Research-game coverage does not establish public catalog/setup correctness.
Every new setup record also enters the behavioral authority universe. Add its
reviewed adjudication, regenerate/check the behavior catalog and update the
enumeration assertions. Expansion records remain `outside-core`; implementing
them does not give them executable Core transcript coverage. Run those authority
checks as well as the setup tests before handing off the packet. Include the global
`RiseOfRedSkullSetupDataTests` recipe inventory even when the new pack is unrelated
to Rise of Red Skull; its declared counts cover every research recipe. Preserve
separate accepted-product counts until the product admission gate actually changes.
Also update `ExpansionSetupAuthorityTests`: its independent source inventory and
product reasons must include new research recipes without claiming Core transcripts.
Include `BehaviorAuthorityTests` source and obligation totals, and both-transport
public deck catalog inventories. Audit unsupported-content fixtures that name the
new pack: replace their example with a catalogued, still-unsupported face while
preserving every rejection assertion. Supported identity faces have zero permitted
player-deck quantity; unsupported faces remain absent from the quantity map.
Whole-game replays supplement focused rules evidence; record whether they exercise
victory, defeat, promotion and campaign continuation rather than inferring it.
Apply the canonical pack's outcome requirements: scenario packs need their stated
victory/defeat matrix; hero packs need representative complete games and focused
characteristic, nemesis, obligation and extra-card evidence. Do not invent a
hero-specific victory gate or spend repeated tactical-policy packets pursuing one
unless a stated requirement or concrete unproved behavior needs that outcome.

## Select verification by consequence

- Run focused behavior and architecture checks while editing; normal hooks remain
  enabled. Integrate the reviewed packet before the required broader lanes.
- Use `docs/testing.md` for managed and native evidence selection. Avoid concurrent
  heavy local suites that compete with deadline-sensitive startup/socket checks.
  Investigate failures before increasing deadlines or changing scheduling.
- Native editor scripts load Debug assemblies. Build the current sources before
  using `-NoBuild`; use actual viewport input and inspect visible outcomes.
  Headless interaction success does not establish rendered appearance.
- Reuse platform and packaging CI for their existing obligations. Add separate
  package journeys only for a concrete uncovered package-specific behavior.
- For replay/save changes, use genuine compatible predecessor fixtures and genuine
  incompatible negative controls. Preserve hashes, history/proofs and backups;
  distinguish semantic JSON equality from byte-for-byte archive equality.

Maintain one evidence record with revision, command/profile, result and artifacts.
Label partial runs and earlier revisions. CI is evidence only after its jobs pass;
the owner can refine or reject a fully tested candidate during exploration.
