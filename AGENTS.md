# AGENTS.md

Guidance for AI agents working in this repository.

## What this repo is

A digital implementation of the Marvel Champions LCG, mid-migration from Python to C#. Work is tracked in the Plane project `MARVEL` — see [docs/plane.md](docs/plane.md).

```
py_src/     Python reference engine (the game as it exists today) + preparation tooling
src/        C# engine (empty until the Engine Core phase)
docs/       project documentation, decisions, and audits
```

`py_src/` began as a fork of [irefrixs/marvel-lcg](https://github.com/irefrixs/marvel-lcg). **We no longer track upstream**, so there is no fork hygiene to preserve — refactor it freely where a Plane issue justifies it.

Its job now is to be the **behavioral source of truth**: the definition of how the game currently behaves, and the thing the C# engine is validated against. Read [docs/migration.md](docs/migration.md) for why the migration is happening and what has been decided.

## Run everything from `py_src/`

**This is the single easiest thing to get wrong.** All Python paths are relative to the working directory — `launch.json` points at `./data/`, `./replays/`, `./assets/`, and `engine/config.py` resolves config files the same way. Run from the repo root and the engine will not find its data.

```bash
cd py_src
uv venv --python 3.13
uv pip install -r requirements.lock     # pinned resolution
.venv/Scripts/python.exe main.py        # serves the web client on 127.0.0.1:2345
```

Verify it came up:

```bash
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:2345/main   # expect 200
```

Tooling under `py_src/tools/` must be invoked as a module so the package resolves:

```bash
.venv/Scripts/python.exe -m tools.determinism.check_runs --runs 6
```

### Things that look broken but aren't

- Most API routes require an `app_version` cookie matching `Ver.ui_version_str` (`<version>r` release, `<version>d` debug). A **cookieless request always fails the version check** and is served `public/clean_cache.html` — see `IsVersionMatch` in `py_src/engine/network/web_server.py:88`. To call the API directly: `curl -s --cookie "app_version=0.5.9.201r" http://127.0.0.1:2345/list_scenarios`
- `assets/` and `replays/` are absent from a clean clone and the engine runs anyway — card images come from the `image_servers` in `launch.json`, and missing ones are generated as placeholders by `engine/lib/image_creator.py`.
- The web client's TypeScript compiles to **gitignored** JavaScript, so a clean clone has no compiled client. See `py_src/public/js/tsconfig.json`. The API works without it; the browser UI does not.

### Dependencies

Python is pinned to 3.13 (`py_src/.python-version`), managed with [uv](https://docs.astral.sh/uv/). `requirements.txt` is the direct list; `requirements.lock` is the pinned resolution (`uv pip compile requirements.txt -o requirements.lock`). Install from the lock.

`numpy` is currently required — it is the default RNG backend, and it was missing from the original dependency list. It goes away when MARVEL-38 lands.

## Architecture

Three layers inside `py_src/`: `core/` (utilities) → `engine/` (devices, controllers, web server, config) → `game/` (rules, cards, abilities). Card definitions in `cards/pack/`, data in `data/`, web client in `public/`.

Read [docs/engine_architecture.md](docs/engine_architecture.md) before structural changes.

Four facts that matter more than the rest:

- **Input blocks.** `Controller.ChoiceOne` (`py_src/engine/controller/controller.py`) blocks a thread inside `self.input.GetInput(...)` waiting for a websocket or keypress. This is why the threading, task, and job machinery exists. Removing it is a goal of the C# design.
- **Replays are seed + input list.** A saved scene records the RNG seed and every player input; replaying re-executes them. This is what makes undo, skip, and deterministic replay work.
- **Every replay step carries a state digest.** `World.CalculateCRC()` (`py_src/game/world/world_render.py:123`) produces a per-card state dict that `engine/controller/module/replay.py` compares on every replayed step, printing a card-by-card diff on mismatch. **This is the project's oracle — treat it as a wire format.** It is specified in [docs/state-digest-contract.md](docs/state-digest-contract.md); read that before changing anything it touches.
- **The RNG is being replaced.** Today `engine/lib/random.py` dispatches on `disable_numpy_random`, which defaults to False, so `numpy.random` is the production RNG and the hand-written `engine/lib/mt19937.py` is dead code. Both are being replaced by one precisely-specified standard implementation shared with C# (MARVEL-38). Until then, anything touching determinism must state which backend it assumes.

## Critical constraints

**Determinism is load-bearing.** The replay corpus is only an oracle if the engine is deterministic. Do not introduce into any gameplay path:

- wall-clock time or dates
- unseeded randomness, or any RNG other than the seeded `Random` instance
- iteration over unordered `set`/`dict` where the order can affect game state
- threading or async that touches game state

The engine has been audited against all four — see [docs/determinism-audit.md](docs/determinism-audit.md) for what was found, what the harness must pin, and which sets are already known harmless. **Check there before re-deriving anything.** Run `python -m tools.determinism.check_runs` after any change to a gameplay path.

**The corpus is immutable once frozen.** Changing engine behavior after generation invalidates it. That is a decision to raise, not to make silently. It is also why the RNG replacement must land *before* corpus generation.

## Security

Card scripts are **executed as Python**. `py_src/cards/database.py` calls `exec()` on custom card modules with no sandboxing. The AST denylist in `engine/security/command_validation.py` is wired only into the cheat console, not into card loading — and a denylist over import names would not be sufficient anyway.

- Never load or execute a card script from an untrusted source, including in tests.
- Do not extend the `exec`-based loading path. Removing it is a goal of the migration, not something to build on.

## Headless bot

Plays games with no client attached — no websocket, no HTTP server, no keyboard. Lives in `py_src/engine/device/manager/bot/`. Run from `py_src/`:

```bash
python main.py -device bot                              # one game, seed 1, saved to replays/
python main.py -bot -bot_games 50 -bot_seed 1000        # 50 games, seeds 1000..1049
python main.py -bot -bot_verify                         # replay each saved scene, check the digest
python main.py -bot -bot_scenario klaw -bot_heroes she_hulk captain_marvel
```

`-bot` is shorthand for `-device bot` plus quieter logging. Exit code 0 when every game finished and saved, 1 otherwise.

Bot saves are **deterministic saves**: `sign`, `time`, and `playtime` are omitted, so the same seed writes a byte-identical file on any machine and no host fingerprint reaches the repo. `-no_bot_deterministic_save` restores the human save format. Human-facing saves are unaffected either way — see MARVEL-27.

Decisions come from a **policy** (`BotPolicy.Choose(decision) -> CommandDescriptor`) injected into `BotDeviceManager`. The two shipped policies are deliberately trivial — they prove the device works, they do not play well. A real policy subclasses `BotPolicy` and registers in `BotPolicyFactory`.

The device answers through `DeviceManager.WhenInput`, the same entry point the web server uses for a browser POST, so `Controller.ChoiceOne` runs its normal validation, CRC and `replay.Push` path. **Do not add a shortcut around `ChoiceOne`** — bot replays must be structurally indistinguishable from human ones or the corpus is worthless.

## Testing

From `py_src/`:

```bash
# fast tests: pure logic, no engine bootstrap beyond `import engine`
python -m unittest unit_test.test_bot unit_test.test_teamup_order \
                   unit_test.test_local_effect_order unit_test.test_scene_hash
python -m tools.determinism.check_runs --runs 6  # digest reproduction across processes
python -m tools.determinism.check_scene_repro    # same seed -> same saved file
python main.py -bot -bot_verify                  # generate a game and replay-verify it
```

Name the modules explicitly. `unittest discover` picks up `unit_test/test_all.py`,
which is the replay suite described below and does not run.

The replay suite (`game/test/test.py` → `TestRun`) re-executes a scene's inputs and asserts per-step digest equality. **There is no working command-line flag for it** — `-test` only expands to `-device -no_editor …`, nothing sets the `InTesting` start state, and the process blocks in `WaitUntilGameStart()`. The `-test_all` and `-profile_folder` branches are unreachable because `build.py` hardcodes `Build.release = True`. Tracked as MARVEL-28. Until then, use `-bot_verify` or the `/T` debug command from the web client.

**`replays/` is empty and untracked**, so there is no regression suite yet. Building it is the entire point of the `Corpus and Oracle` phase — weigh changes accordingly.

New tooling needs its own tests: test behavior not implementation, no assertion-free tests, coverage is an observed outcome and never a target.

## Workflow

### Plane

All work is tracked in Plane, project `MARVEL`. Every issue belongs to a module (phase). See [docs/plane.md](docs/plane.md).

### Branching

`master` is the long-lived branch. Cut a short-lived `<type>/marvel-<id>-<slug>` branch off `master`, open a PR, and squash-merge.

**Never close or merge a pull request you did not open in the current session.** If a PR looks like a blocker, report it and stop.

### Commits

Conventional Commits: `<type>(<scope>): <description>`. Use `py` scope for `py_src/` changes and `engine` for `src/`.
