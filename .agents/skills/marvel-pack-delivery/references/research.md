# Research readiness

Read `docs/plane.md`, `docs/rules-provenance.md` and the applicable dataset guides.
Use connected sources or the Plane CLI; fetch one bounded snapshot for an audit
and inspect relevant children, links and comments before calling context absent.
Capture issue IDs/descriptions, child IDs, and inspected comment/link/relation
results once; those are separate Plane endpoints. Check high-priority or ambiguous
records first, respect rate limits, and mark failed or omitted requests unverified.
Do not re-inventory an already reconciled product list merely because a new agent
started. Follow the requested depth and scope: a readiness audit is not deep
research. When the owner requests all remaining content, research every requested
pack in bounded batches, including distant waves; proximity determines order,
not whether a pack receives the requested research. Keep an explicit coverage
ledger so checkpointing cannot silently shrink the assignment.

## What an implementation packet needs

| Question | Useful evidence |
|---|---|
| What is included? | Pack identifiers, exact local card IDs/faces and playable quantities; starter/obligation/nemesis or scenario/campaign inventory as applicable. |
| Why is the behavior correct? | Local card text, RR IDs, relevant insert sections and ruling IDs; pinned source paths/hashes where required by provenance rules. |
| What is new? | Capability map separating supported mechanics from engine, setup, campaign, contract and UI prerequisites; point to actual code/tests, not assumed support. |
| What must precede it? | Plane dependency IDs, owning layer, affected shared paths and a small first implementation slice. |
| How will it be proved? | Distinguishing examples, public entry point, invalid/no-op boundaries, persistence implications and any genuinely new native interaction. |
| What is unresolved? | Exact question, conflicting evidence if any, consequence, next bounded investigation and which work can continue independently. |

Record source completeness as reconciled, partial or unverified, especially for
preview products or incomplete acquisition snapshots. Check a recorded dependency
against its current implementation/PR evidence before describing it as missing.
A pending admission prerequisite may leave research and engine slices unblocked.

Existing linked research is sufficient when an implementer can resolve those
questions without rediscovering the pack. Do not require the same prose in every
issue, exhaustive card-by-card test plans before coding, or advance design of all
engine internals. A generic checklist, official product URL or face count alone
does not establish implementation readiness.

For an ambiguous timing clause, inspect vendored rulings before asserting a rule.
Write the smallest examples that distinguish plausible interpretations, including
causal context and captured identities where relevant. Separate official facts,
explicit owner decisions, engine-owned choices and unresolved hypotheses. An
unresolved behavior must not be silently guessed; isolate it while other work
continues. Browse original sources when local evidence is missing or uncertain,
and follow offline provenance/admission requirements before using new data.

## Audit output

Return a compact matrix with one row per requested pack: Plane ID, name/type,
evidence inspected, readiness, concrete gap and next action. Use:

- **Ready for a bounded slice:** authority, scope and dependencies support the
  specified next implementation task; this is not a promise of whole-pack readiness.
- **Research needed:** inventory or authority exists, but a named question prevents
  an appropriate implementation handoff.
- **Dependency blocked:** the context is adequate but a named prerequisite is pending.
- **Implementation review:** an implementation exists; identify its PR/revision and
  remaining verification or acceptance rather than assigning duplicate research.
- **Unverified:** access, missing attachments/comments or the audit allowance prevented
  inspection. Do not convert uninspected material into a finding that it is absent.

Inspect linked material selectively and record audit limits. Prioritize concrete
context gaps for the next wave, then shared prerequisites affecting several packs.
Update existing Plane work when authorized; avoid duplicate generic research
issues. An audit itself does not authorize bulk edits or mark packs complete.
