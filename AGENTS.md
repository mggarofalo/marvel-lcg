# AGENTS.md

Guidance for AI agents working in this repository.

## What this repo is

A **fork of [irefrixs/marvel-lcg](https://github.com/irefrixs/marvel-lcg)** — a Python implementation of the Marvel Champions LCG, playable in a browser.

It is now also the **reference engine** for a planned rewrite in C#, tracked in the Plane project `MARVEL`. That gives the code here a specific job: it is the source of truth for how the game currently behaves, and the thing a new engine will be validated against.

Two kinds of work happen here:

1. **Preparation tooling** — a self-play bot, a replay corpus harness, and a spec-validation harness. New code, ours.
2. **Targeted fixes** to make the above possible.

Anything else — refactors, cleanups, modernization of the upstream engine — is out of scope. See [docs/migration.md](docs/migration.md) for why.

## Fork hygiene

- The `upstream` remote is `irefrixs/marvel-lcg`. Every file we add is permanent diff against upstream, so keep additions **additive** and confined to new files where practical.
- Do not reformat, rename, or refactor upstream files without a reason tied to a Plane issue.
- `docs/install_guide.md`, `docs/card_scripting_guide.md`, `docs/engine_architecture.md`, `docs/debug_guide.md`, and `docs/editor_guide.md` are upstream-authored. Treat them as reference material — read them, do not rewrite them.

## Quick start

```bash
pip install -r requirements.txt   # currently FAILS - see below
python main.py                    # serves the web client on 127.0.0.1:2345
```

**Known blocker:** `requirements.txt` lists `PIL`, which is not an installable PyPI package (the real one, `Pillow`, is already on the next line). Install will fail as written. Tracked as `MARVEL-2`; fix that issue before relying on a clean-clone setup.

Configuration comes from `launch.json` merged with command-line flags — see `engine/config.py` and the Configuration section of [docs/engine_architecture.md](docs/engine_architecture.md). Command line beats `launch.json` beats defaults.

## Architecture

Three layers: `core/` (utilities) → `engine/` (platform: devices, controllers, web server, config) → `game/` (rules, cards, abilities). Card definitions live in `cards/pack/`, data in `data/`, the web client in `public/`.

Read [docs/engine_architecture.md](docs/engine_architecture.md) before making structural changes.

Four facts that matter more than the rest:

- **Input blocks.** `Controller.ChoiceOne` (`engine/controller/controller.py`) blocks a thread inside `self.input.GetInput(...)` waiting for a websocket or keypress. This is why the threading, task, and job machinery exists, and it is why driving the engine from code needs a new device type rather than a function call.
- **Replays are seed + input list.** A saved scene records the RNG seed and every player input. Replaying re-executes them. This is what makes undo, skip, and deterministic replay work.
- **Every replay step carries a state digest.** `World.CalculateCRC()` (`game/world/world_render.py:123`) produces a per-card state dict that `engine/controller/module/replay.py` compares on every replayed step, printing a key-by-key diff on mismatch. This is the project's oracle — treat it as a wire format.
- **The RNG is non-standard.** `engine/lib/mt19937.py` is MT19937, but `shuffle()` is not Fisher-Yates (it does `10 * len` random swaps) and `randint` truncates a float division. The quirks are part of the contract.

## Critical constraints

**Determinism is load-bearing.** The replay corpus is only an oracle if the engine is deterministic. Do not introduce into any gameplay path:

- wall-clock time or dates
- unseeded randomness, or any RNG other than the seeded `Random` instance
- iteration over unordered `set`/`dict` where the order can affect game state
- threading or async that touches game state

**The corpus is immutable once frozen.** Changing engine behavior after the corpus is generated invalidates it. If a change is genuinely required, that is a decision to raise, not to make silently.

## Security

Card scripts are **executed as Python**. `cards/database.py` calls `exec()` on custom card modules with no sandboxing. The AST denylist in `engine/security/command_validation.py` is wired only into the cheat console (`game/world/cheat/cheat_cmd_helper.py`), not into card loading — and a denylist over import names would not be sufficient there anyway.

Consequences for agents:

- Never load or execute a card script from an untrusted source, including as part of testing.
- Do not extend the `exec`-based loading path. Removing it is a goal of the migration, not something to build on.

## Workflow

### Plane

All work is tracked in Plane, project `MARVEL`. Every issue belongs to a module (phase). See [docs/plane.md](docs/plane.md).

### Branching

`master` is the long-lived branch (inherited from upstream). Cut a short-lived `<type>/marvel-<id>-<slug>` branch off `master`, open a PR, and squash-merge.

**Never close or merge a pull request you did not open in the current session.** If a PR looks like a blocker, report it and stop.

### Commits

Conventional Commits: `<type>(<scope>): <description>`.

## Testing

`python main.py -test` replays every file in the configured replay folders and asserts per-step digest equality (`game/test/test.py` → `TestRun`).

**`replays/` is currently empty and not tracked in git**, so the suite has nothing to run. Generating that corpus is the entire point of the `Corpus and Oracle` phase. Until it exists, there is no regression suite — weigh changes accordingly.

New tooling written in this repo needs its own tests. Follow the principles in the receipts repo's `docs/agentic-testing.md`: test behavior not implementation, no assertion-free tests, coverage is an observed outcome and never a target.
