---
name: marvel-pack-delivery
description: Research, implement, review and integrate Marvel Champions hero, scenario and campaign packs in this repository. Use for pack or wave delivery and Plane readiness audits; use the native testing skill for Godot interaction details.
---

# Deliver a playable pack

Start with the owner's requested product and the current Plane record. Read the
repository AGENTS router and applicable scoped guidance. This skill supplies
pack-specific decisions; it does not replace those rules or authorize new work.
Paths below are relative to the repository root unless linked otherwise.

## Choose the next work

- For research or a Plane audit, read [research readiness](references/research.md).
- For implementation or review, read [implementation and proof](references/implementation.md).
- For native interaction work, use [marvel-godot-testing](../marvel-godot-testing/SKILL.md).

Use one integrated wave candidate and one current delivery record. Reuse the
canonical Plane pack/wave record; put substantial evidence under `docs/content/`
and link it from that record rather than creating a competing status tracker. Keep an exact
revision, inventory, dependency state, evidence locations and next executable
action there. Link Plane children rather than copying every implementation log.
Distinguish inventoried, researched, implemented, verified, merged and accepted.
A pack title in the backlog establishes none of the later states.

## Establish complete playable scope

Account for every playable face and its physical quantities where setup or deck
construction uses them. Include identity forms, starter, obligation, nemesis,
modular sets, scenario modes and campaign cards/rules as applicable. Reused cards
still require coverage, even when their implementation already exists.
Do not make divider, token or marketing packaging reconciliation a gate unless
the requested playable outcome depends on it or the owner asks for it.

Printed card authority is `datasets/cards/`; executable interpretations belong
in `datasets/abilities/`. SVGs and other presentation assets are not executable
card coverage. Use the authority and dataset rules in AGENTS rather than copying
online text into authored behavior or treating a legacy implementation as authority.

## Sequence the wave

Identify unfamiliar engine, setup, campaign and interaction capabilities before
bulk authoring. Group dependent cards behind a shared prerequisite. Plan a public
setup → characteristic action → save/restart slice as soon as complete admission
is possible; use nonpublic engine tests first. A research-only world can bypass
the very production setup path that needs verification.

When agent work is authorized, follow the global software-factory bounded-agent
guidance and `.parallel-sensitive`. Give fresh workers one reviewable outcome,
owned paths, dependency revisions, raw authority links, evidence and a checkpoint.
Serialize shared ability-book and engine timing edits. Use separate implementation
and review assignments for new engine capabilities. A worker checkpoint returns
control to the lead to integrate, redirect or deliberately renew; it does not end
the wave. Do not run a permanent agent per pack or delegate the entire backlog.

## Close a wave

Integrate reviewed packets before broad verification. Reconcile the complete
inventory against public admission and actual gameplay. Record evidence against
the revision tested, including failures and untested scope; do not combine older
passing runs into a claim of a final green candidate.

Reuse existing CI/platform/package gates. Add native evidence for new interaction
behavior, not automatically for every conventional card. Keep owner exploration
and requirements refinement separate from agent regression work. Prepare the
reviewable PR/build before any genuinely missing approval; never mark acceptance
complete from test counts or a scripted journey.
