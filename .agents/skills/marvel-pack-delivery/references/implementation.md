# Implementation and proof

Use the AGENTS routing table for `card-dsl`, timing, setup, rules citations,
behavioral specs, persistence and presentation guidance. Read only the concerns
the packet changes. Keep game legality/state transitions in engine-owned code;
extend visibility-safe contracts when the UI lacks necessary meaning.

## Before bulk card authoring

Map each new mechanic to its engine prerequisite and test a characteristic card.
Keep card DSL changes separate from engine mechanisms where practical. Confirm
cost assessment, choices and mutations happen in the correct order: a rejected
answer must preserve the prompt, state, RNG and pending effects, including on retry.
Challenge legal no-ops, ties, replacements, nested activations, departed targets
and multiplayer ownership only where the mechanic makes them relevant.

Capture causal facts when an effect needs them later. Current active villain,
current defender or current board membership may no longer describe the attack
that caused a continuation. Distinguish declared defense, completed attacks,
damage prevented/dealt, defeat and replacement; do not substitute one receipt for
another. Expected behavior must come from the card/rule/ruling, not these examples.

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
Whole-game replays supplement focused rules evidence; record whether they exercise
victory, defeat, promotion and campaign continuation rather than inferring it.

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
