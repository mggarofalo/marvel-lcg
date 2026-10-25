# Expansion research readiness — 2026-10-25

Tracking: MARVEL-681. This audit covers all 64 canonical pack records,
MARVEL-559–622, across the existing 11-wave inventory. It assesses recorded
implementation context, not the current release status of products or the
correctness of mechanics that have not yet been implemented.

## Outcome

The backlog accounts for the requested products, but it is not a set of ready
implementation handoffs. Captain America and Wrecking Crew have implementation
and evidence in [PR #439](https://github.com/mggarofalo/marvel-lcg/pull/439).
The remaining records generally provide pack-specific risks, local authority
pointers and broad acceptance criteria; they still need a bounded research packet
before assigning their unfamiliar mechanics. Do not create 62 duplicate generic
research issues or require exhaustive research of every distant wave now.

- [Early packs, MARVEL-559–590](2026-10-25-early-packs.md): 32 individual rows,
  with evidence, missing context and a proposed next action.
- [Later packs, MARVEL-591–622](2026-10-25-later-packs.md): the other 32 rows,
  shared prerequisites and explicit source acquisition gaps.

Green Goblin and Ms. Marvel are the earliest untouched packets. Their proposed
first-slice research is recorded separately so an implementer can use concrete
authority and capability findings without reading the entire audit:

- [Green Goblin](green-goblin-first-slice.md)
- [Ms. Marvel](ms-marvel-first-slice.md)

Fear No Evil's issue explicitly records missing villain/encounter faces in the
snapshot. Elektra, Iron Fist and Shadowland lack pinned card/insert sources in
their records. These are acquisition gaps in the repository evidence, not claims
about current external availability. Later source acquisition must follow
`docs/rules-provenance.md` and generated dataset gates.

## Interpreting dependencies and audit limits

MARVEL-630 appears as a shared admission blocker. PR #439 includes additive-content
compatibility evidence; assess its latest revision and results before assigning
duplicate work. Research and independent engine slices can proceed while admission
verification is pending. Angel also has a specific recorded MARVEL-640 timing
question. A Plane relation alone is not proof that a capability remains absent.

Both reviewers inspected all assigned descriptions and child mappings. Comments,
links and relations were inspected to the extent described in their reports;
some queries were rate-limited. Uninspected or failed requests remain unverified.
Most named mechanics were not independently checked against every card/ruling or
the complete codebase in this inventory audit. The first-slice packets add that
depth only for their explicit scope.

The audit forward-tested the repository `marvel-pack-delivery` skill. Resulting
improvements distinguish implementation review from research readiness, identify
source completeness, reconcile dependency status with current PR evidence, and
capture separate Plane endpoints without interpreting failed requests as absence.

## Next delivery

Finish PR #439's native CI correction and independent review, then evaluate the
current CI/merge and owner-acceptance status separately. For each subsequent wave,
deepen its first-slice packets, implement shared prerequisites, and establish a
public setup/action/save/restart path before bulk card authoring. Keep its current
Plane record linked to evidence rather than growing repeated generic checklists.
