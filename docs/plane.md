# Plane

Guidance for AI agents working with the Plane workspace for this project.

## Workspace

- **Project:** Marvel (identifier: `MARVEL`)
- **Workspace slug:** `dev`
- **CLI:** `plane` — use `plane --help` and `plane <command> --help` to discover commands. Bugs or missing features go to https://github.com/mggarofalo/plane-cli
- **Output:** `-o json` (default) for parsing, `-o table` for reading

Always pass `-p MARVEL -w dev`.

## Modules (phases)

Work is organized into phases, tracked as Plane **modules**. **Every issue must belong to one.** `plane issue create` has no module flag — attach in a second call:

```bash
ID=$(plane issue create --name "..." --labels <uuid> --priority medium --id-only -p MARVEL -w dev)
plane module add-work-items --module-id "Corpus and Oracle" --issues "$ID" -p MARVEL -w dev
```

| Module | Purpose |
|---|---|
| **Foundations** | Repo guidance, agent instructions, architecture decisions, dev tooling |
| **Corpus and Oracle** | Headless bot, determinism audit, coverage-directed replay corpus. Produces the behavioral oracle everything else validates against. |
| **Spec Extraction** | Behavioral specs authored from printed card text, validated against the Python engine before being trusted |
| **Engine Core** | The C# rules engine |
| **Card DSL and Port** | Data-only card ability DSL, then the card ports |
| **Client and Integration** | Reconnecting the existing web client |
| **Maintenance Backlog** | Default bucket for small hardening and fixes that fit no phase |

If no phase fits, use **Maintenance Backlog**.

## Labels

Every issue needs at least one **layer** label. Add **type** labels as appropriate.

**Layer:** `legacy` (Python engine) · `engine` (C#) · `cards` · `tooling` · `docs` · `frontend`

**Type:** `Feature` · `Improvement` · `Bug` · `cleanup` · `security` · `testing` · `epic` · `spike` · `dx`

Issues labeled `epic` are parent containers — skip them and work their children.

### Label UUIDs

`--labels` does **not** resolve names in the current CLI build (see Gotchas). Use these:

| Label | UUID |
|---|---|
| legacy | `4ad2d49e-1766-4206-8ac0-5bd2aae8e467` |
| engine | `8ec4d146-f513-4bd7-a3e4-97118c207b63` |
| cards | `34eccffd-5aa8-421f-8e7a-b6af403ed06c` |
| tooling | `cdeae3f9-a93d-4e1a-aba1-9376794944d6` |
| docs | `8102510f-fd40-4dd3-a949-5fbaeb698802` |
| frontend | `311f76a1-a35a-4e93-a235-8ab52a7e73ef` |
| Feature | `e5b4ea38-8739-43fb-ad9a-835d6a4918ea` |
| Improvement | `e028d9cd-4fcf-41c5-8f38-c7146d88a134` |
| Bug | `ca1ffed9-594c-4dfe-94be-01fd1c030e7b` |
| cleanup | `22a65926-55e4-44c9-8679-056ebade2834` |
| security | `254aed72-a123-4a2e-995d-a44052f8f414` |
| testing | `8e326f9b-f9e0-46de-af36-189bb16c20d9` |
| epic | `d446a55d-2578-4904-b26a-c31682e7ad37` |
| spike | `5f84ae77-6e2d-434d-88b0-e030f2750ee7` |
| dx | `6b10b5b3-538f-4cc3-bdd0-6af99339b1a8` |

Re-discover with `plane label list -p MARVEL -w dev --fields id,name` if they drift.

## Priority

Priority reflects **execution readiness**, not importance:

| Priority | Meaning |
|---|---|
| Urgent | Ready to start now, nothing blocks it |
| High | One step away |
| Medium | Blocked by two or more |
| Low | Far future |

A critically important issue that cannot start yet is Medium, not Urgent. This keeps "what's next" answerable by sorting.

## What's next

1. List issues in Backlog/Todo, excluding Done/Cancelled
2. Skip `epic`-labeled issues — work their children
3. Skip issues with unresolved blockers
4. Pick the highest-priority unblocked issue

## Issue workflow

**Start:**
```bash
plane issue update --resource-id <uuid> --state "In Progress" -p MARVEL -w dev
```
Branch as `<type>/marvel-<id>-<slug>`.

**Finish:**
```bash
plane issue update --resource-id <uuid> --state Done -p MARVEL -w dev
```
Then check whether it unblocks downstream issues and raise their priority.

**Create:** always `-p MARVEL -w dev`, at least one layer label, priority by readiness, and a module attached in a follow-up call.

## Gotchas

Verified against this workspace on 2026-08-06:

- **`--labels` requires UUIDs.** Name resolution does not apply to it; passing `docs,dx` returns `"docs" is not a valid UUID`. Use the table above.
- **`--resource-id` is a required flag on `issue update` and `issue get`**, not a positional argument.
- **`state_detail.name` is not returned** by `issue get` — extract `state` (a UUID) and map it yourself, or use `-o json`.
- **`plane label ensure` does not exist** in this CLI build despite appearing in the skill reference; only `create`, `delete`, `get`, `list`, `update`. Creating a duplicate label will not error, so check first.
- **`--description` accepts markdown** and is converted to HTML server-side. Headings, lists, and code blocks all render.
- **Batch mode (`--batch`) reads JSONL from stdin** — one object per line, not a JSON array. Note that heredoc pipes may be blocked by shell guards in worktree-isolated sessions; individual calls are the reliable fallback.
