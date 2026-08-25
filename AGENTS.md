# AGENTS.md

Guidance for AI agents working in this repository. **This file is a router.** It
holds the rules you can break without noticing; everything else is a pointer.

## What this repo is

A digital implementation of the Marvel Champions LCG, mid-migration from Python
to C#. Work is tracked in the Plane project `MARVEL` — see [docs/plane.md](docs/plane.md).

```
py_src/     Python reference engine (the game as it exists today) + tooling
src/        C# engine — `Marvel.Core`, `Marvel.Rules`, `Marvel.Content`
tests/      C# tests, plus `godot-wall/` (projects that must fail to build)
tools/      repo-level scripts that are not Python-engine tooling
datasets/   generated and vendored data both engines consume
docs/       project documentation, decisions, and audits
```

`py_src/` began as a fork of [irefrixs/marvel-lcg](https://github.com/irefrixs/marvel-lcg).
**We no longer track upstream**, so there is no fork hygiene to preserve.

Its job now is to be the **behavioral oracle**: the definition of how the game
currently behaves, and the thing the C# engine is validated against. It is not
the product. See [docs/migration.md](docs/migration.md).

**`py_src/` is frozen except where it blocks the oracle.** A defect there earns
work only if it changes behavior the C# engine must reproduce, corrupts a
corpus, or blocks a spec from being authored. Everything else is noise — see
[Scope discipline](#scope-discipline).

**`py_src` is never served.** It is a development tool and an oracle, never
deployed, never bound beyond localhost. **Its network surface is not a work
item** — do not file issues against the Python web server's exposure. The C# MVP
is single-player and local; `src/Marvel.Server` is a later phase and is not being
architected now. What carries forward is one note for whoever designs the C# wire
format — *the server decides what each seat sees, rather than trusting the
client's assertion* — recorded in
[migration.md](docs/migration.md#deployment-py_src-is-never-served). This is a
cooperative game: a permissive policy is fine, but it has to be chosen.

## Run everything from `py_src/`

**This is the single easiest thing to get wrong.** All Python paths are relative
to the working directory — `launch.json` points at `./data/`, `./replays/`,
`./assets/`, and `engine/config.py` resolves config files the same way. Run from
the repo root and the engine will not find its data.

```bash
cd py_src
uv venv --python 3.13
uv pip install -r requirements.lock     # pinned resolution; python 3.13, see .python-version
.venv/Scripts/python.exe main.py        # serves the web client on 127.0.0.1:2345
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:2345/main   # expect 200
```

Tooling under `py_src/tools/` must be invoked as a module:
`python -m tools.determinism.check_runs --runs 6`.

Surprises that are not bugs — the `app_version` cookie, the missing `assets/`,
the gitignored compiled client — are in
[docs/engine-conventions.md](docs/engine-conventions.md#things-that-look-broken-but-arent).

## Non-negotiables

These are the rules that cost a day when broken. Nothing else in this file is
as important.

1. **Determinism is load-bearing.** The replay corpus is only an oracle if the
   engine is deterministic. Never introduce into a gameplay path: wall-clock
   time or dates; unseeded randomness or any RNG other than the seeded `Random`;
   iteration over unordered `set`/`dict` where order can affect game state;
   threading or async that touches game state. Run
   `python -m tools.determinism.check_runs` after any gameplay change.

2. **The RNG and the state digest are wire formats.** One MT19937 stream, seeded
   once per game, no floating point. `World.CalculateDigest()` serialises every
   card and is compared on every replayed step. `datasets/rng/vectors.json` and
   `datasets/digest/vectors.json` are the cross-language fixtures the C# port is
   accepted against. Changing either changes every game outcome.

3. **Card scripts are executed as Python.** `py_src/cards/database.py` calls
   `exec()` with no sandboxing; the AST denylist in
   `engine/security/command_validation.py` guards only the cheat console. Never
   load a card script from an untrusted source, including in tests. Do not
   extend the `exec`-based path — removing it is the point of the migration.

4. **Do not author specs from `data/cards.json`.** 36 cards are corrupted by an
   encoding round-trip and 197 differ materially from the printed card (`03025`
   is missing an entire rules line). Printed text lives in `datasets/cards/`.

5. **Nothing under `datasets/` may require the network to regenerate.** Every
   dataset is either *generated* (rebuildable offline and byte-identically,
   guarded by a `--check` gate in CI) or *vendored* (copied once from a pinned
   upstream, read as-is). There is no third kind. This makes
   [`marvelcdb`](https://github.com/mggarofalo/marvelcdb-cli) an **acquisition
   and research tool, never a build or engine dependency** —
   `py_src/tools/cards/harvest_faq.py` is the only module that invokes it and
   nothing imports it.

6. **The corpus is immutable once frozen.** Changing engine behavior after
   generation invalidates it. That is a decision to raise, not to make silently.

7. **An integrity error must not be swallowed.** If continuing would produce a
   *wrong artefact* rather than a wrong frame, derive from
   `core.errors.EngineIntegrityError` — `Log.OnCrash` re-raises that class
   regardless of the build.
   [Full rule and the thread case.](docs/engine-conventions.md#integrity-errors-must-not-be-swallowed)

8. **The test suite never touches the repository.** Running it leaves
   `git status` clean and creates no commits. Packaging (`tools.package.bump`,
   `tools.package.zip_cards`) mutates the tree and must never be wired into a
   test, a hook, or CI.

9. **Never close or merge a pull request you did not open in this session.** If
   a PR looks like a blocker, report it and stop.

## Before you touch X, read Y

| Touching | Read first |
|---|---|
| anything in a gameplay path | [determinism-audit.md](docs/determinism-audit.md) |
| `engine/lib/mt19937.py`, the `Random` facade | [rng-contract.md](docs/rng-contract.md) |
| `game/world/digest.py`, `CardFace.GetStateFields`, zone flags, card id allocation | [state-digest-v2.md](docs/state-digest-v2.md) |
| structural changes to `core/` → `engine/` → `game/` | [engine_architecture.md](docs/engine_architecture.md) |
| `game/world/invariants.py`, or adding an invariant rule | [invariants.md](docs/invariants.md) |
| corpus generation, sampling, freezing | [corpus.md](docs/corpus.md) |
| `tools/coverage/`, renaming an `AbilityFactory` method | [card-coverage.md](docs/card-coverage.md) |
| `bot/policies.py`, `bot/progress.py`, the stall guards | [no-op-decisions.md](docs/no-op-decisions.md) |
| authoring or running behavioral specs | [spec-harness.md](docs/spec-harness.md) |
| the spec campaign, sharding, depth tiers | [spec-campaign.md](docs/spec-campaign.md) |
| refreshing a vendored snapshot, a new RR version, a new pack | [rules-provenance.md](docs/rules-provenance.md) |
| citing a rule from a C# test, `[Rule]`, what nothing is held to | [rules-citations.md](docs/rules-citations.md) |
| `datasets/cards/`, `tools/cards/extract` | [card-dataset.md](docs/card-dataset.md) |
| `datasets/setup/`, scenario or starter-deck data, setup order | [setup-dataset.md](docs/setup-dataset.md) |
| `datasets/digest/prompts.json`, `Marvel.Rules.Fold`, what a prompt offers | [prompt-dataset.md](docs/prompt-dataset.md) |
| play areas, game areas, anything resolving by *where a card is* | [places.md](docs/places.md) |
| `Marvel.Rules.Fold`, the villain phase, what a revealed card does | [villain-phase.md](docs/villain-phase.md) |
| the card ability DSL | [card-dsl.md](docs/card-dsl.md) |
| the client, the fold's return signature, `Marvel.Server` | [presentation-layer.md](docs/presentation-layer.md) |
| adding a C# project, or changing a `TargetFramework` | [presentation-layer.md](docs/presentation-layer.md#dependency-rules) |
| `engine/config.py`, arg groups, crash capture, packaging, visibility filtering, `Build.release` | [engine-conventions.md](docs/engine-conventions.md) |
| a scene saved before `0.5.9.205` | [state-digest-contract.md](docs/state-digest-contract.md) |
| Plane issues, modules, labels, priority | [plane.md](docs/plane.md) |
| why any of this is happening | [migration.md](docs/migration.md) |

## Commands

```bash
# --- self-play -------------------------------------------------------------
python main.py -device bot                       # one game, seed 1, saved to replays/
python main.py -bot -bot_games 50 -bot_seed 1000 # 50 games
python main.py -bot -bot_verify                  # replay each saved scene, check the digest
python main.py -bot -no_check_invariants         # invariants off (corpus generation)

# --- corpus ----------------------------------------------------------------
python -m tools.corpus.generate --games 200 --out ./corpus/
python -m tools.corpus.generate --dry-run        # print the plan and its digest
python -m tools.corpus.freeze                    # content-address the scenes
python -m tools.coverage.report replays/         # what the run actually exercised
python -m tools.coverage.reach --corpus ./corpus/ # gate: a played card must be in the allowlist

# --- replay verification ---------------------------------------------------
python -m tools.events.verify ~/Source/marvel-lcg-corpus --per-shard 1
python -m tools.affordances.verify ~/Source/marvel-lcg-corpus --per-shard 1
python main.py -verify_replays                            # every folder in replay_folders
python main.py -verify_replays -verify_folders ./corpus/
python -m tools.determinism.check_corpus --runs 100

# --- specs -----------------------------------------------------------------
python -m tools.spec.run_case specs/             # run scenarios, see what happened
python -m tools.spec.validate                    # assign verdicts, update the manifests
python -m tools.spec.validate --trusted-only     # the gate
python -m tools.spec.coverage --rulings          # cards with an official ruling
python -m tools.cards.rulings <card_id>          # print the ruling

# --- fixtures (regenerate-or-fail; --check is what CI runs) ----------------
python -m tools.rng.emit_vectors                 # after touching the RNG
python -m tools.digest.emit_vectors              # after touching anything the digest reads
python -m tools.cards.extract                    # after touching data/cards.json or datasets/marvelsdb/
python -m tools.setup.emit_setup                 # after touching data/scenarios/, data/encounter_sets/, deck/starter/
python -m tools.digest.emit_prompts              # after touching anything that changes what a prompt offers

# --- the C# side (run from the repository root, not py_src/) ---------------
dotnet build                                     # the wall gates every project
dotnet test
bash tools/godot-wall.sh                         # prove both gates still fire

# --- determinism probes ----------------------------------------------------
python -m tools.determinism.check_runs --runs 6
python -m tools.determinism.check_runs --runs 4 --matrix wide --policy first
python -m tools.determinism.cross_os emit --out trace.json --label $(uname -s)
python -m tools.determinism.cross_os compare a.json b.json
```

**The unit test tiers live in [`.github/workflows/ci.yml`](.github/workflows/ci.yml)
— that file is the source of truth, not this one.** Copy the two `python -m
unittest` invocations from it. Do not use `unittest discover`: it picks up
`unit_test/test_all.py`, an old harness that mutates `Build.release`, appends to
`sys.argv` and writes result files into the working directory.

New tooling needs its own tests: test behavior not implementation, no
assertion-free tests, coverage is an observed outcome and never a target.

## Behavioral specs, in one paragraph

The replay corpus answers "did this game reproduce". It cannot answer "does
Swinging Web Kick deal 8 damage". Specs do, and they are what the C# engine is
held to. **A scenario is a transcript** — one `When` per engine decision, `Then`s
interleaved, Gherkin under `py_src/specs/`. The harness **never answers a
decision the transcript omits**; an unanswered mid-resolution choice is
`FAIL-spec-wrong`, not a silent pick. **A scenario is not trusted until it
passes**: `specs/trusted.json` is written only by the validation runner, only
from `PASS`, each entry pinned to the hash of its source. There is no way to add
one by hand. `specs/self-test/quarantine.feature` is wrong on purpose and must
stay that way.

**Check for a ruling before asserting timing.** A spec authored from ambiguous
printed words is checked against a Python engine implementing the same reading of
the same words, so it enters `trusted.json` having confirmed only that the engine
agrees with itself. `--rulings` flags the cards where an official MarvelCDB
ruling exists. Details in [spec-harness.md](docs/spec-harness.md).

The general form of that problem — and the patch loop for when a rulebook, a
ruling or a pack changes — is [rules-provenance.md](docs/rules-provenance.md).
A trusted scenario is trusted *against a stated set of inputs*; when one moves,
it drops out and returns to triage.

## Scope discipline

This project has a documented tendency to generate tangential work: an
adversarial review of every PR against a 59k-LOC legacy engine always finds
something real, and every finding used to become an issue. Before filing:

- **Does this change behavior the C# engine must reproduce?** If no, it is not
  worth an issue. A crash in a path no scenario reaches is not oracle behavior.
- **Is `py_src` the right place to fix it?** If the C# engine will not inherit
  the defect, record it and move on.
- **Is this the third generation of the same thread?** Fixing the tooling that
  tests the fixes to the engine is a signal to stop and re-anchor on
  `docs/migration.md`.

Two numbers say whether the project is moving: **cards with a trusted spec**
(`python -m tools.spec.coverage`) and **C# lines in `src/`**. Issue throughput
says nothing.

## Workflow

- **Plane** — all work is tracked in project `MARVEL`; every issue belongs to a
  module (phase). See [docs/plane.md](docs/plane.md).
- **Branching** — `master` is long-lived. Cut `<type>/marvel-<id>-<slug>` off
  `master`, open a PR, squash-merge.
- **Commits** — Conventional Commits. Scope `py` for `py_src/`, `engine` for `src/`.
- **Parallel agents** — read [`.parallel-sensitive`](.parallel-sensitive) before
  dispatching concurrent work. Two issues whose scopes both touch a path listed
  there are serialized, not parallelized.

### CI

| Workflow | Runs | What |
|---|---|---|
| [`ci.yml`](.github/workflows/ci.yml) | every push to `master`, every PR | both unit tiers, five fixture staleness checks, trusted specs, one generated-and-verified game, `git status` clean, the C# build and test suite, and the Godot wall |
| [`determinism.yml`](.github/workflows/determinism.yml) | nightly 06:00 UTC, or manually | `check_runs` across fresh processes, cross-OS digest comparison, replay and invariant probes |

Both pin Python from `py_src/.python-version`, install from `requirements.lock`,
and set `PYTHONIOENCODING=utf-8`.

**Everything in `ci.yml` is verified green on Windows and Linux.** Keep it that
way: a gate that has never passed on one OS belongs in `determinism.yml` behind
an explicit `runs-on`. A red `master` must mean something broke.

`replays/` is empty and untracked, so **there is no regression suite yet**.
Building it is the entire point of the `Corpus and Oracle` phase — weigh changes
accordingly.
