# AGENTS.md

Guidance for AI agents working in this repository.

## What this repo is

A digital implementation of the Marvel Champions LCG, mid-migration from Python to C#. Work is tracked in the Plane project `MARVEL` — see [docs/plane.md](docs/plane.md).

```
py_src/     Python reference engine (the game as it exists today) + preparation tooling
src/        C# engine (empty until the Engine Core phase)
datasets/   generated and vendored data both engines consume
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

`numpy` is **gone** as of MARVEL-38 — it was only ever there as the default RNG backend. Nothing imports it. Do not add it back without a reason that survives [docs/rng-contract.md](docs/rng-contract.md).

## Architecture

Three layers inside `py_src/`: `core/` (utilities) → `engine/` (devices, controllers, web server, config) → `game/` (rules, cards, abilities). Card definitions in `cards/pack/`, data in `data/`, web client in `public/`.

Read [docs/engine_architecture.md](docs/engine_architecture.md) before structural changes.

Four facts that matter more than the rest:

- **Input blocks.** `Controller.ChoiceOne` (`py_src/engine/controller/controller.py`) blocks a thread inside `self.input.GetInput(...)` waiting for a websocket or keypress. This is why the threading, task, and job machinery exists. Removing it is a goal of the C# design.
- **Replays are seed + input list.** A saved scene records the RNG seed and every player input; replaying re-executes them. This is what makes undo, skip, and deterministic replay work.
- **Every replay step carries a state digest.** `World.CalculateCRC()` (`py_src/game/world/world_render.py:123`) produces a per-card state dict that `engine/controller/module/replay.py` compares on every replayed step, printing a card-by-card diff on mismatch. **This is the project's oracle — treat it as a wire format.** It is specified in [docs/state-digest-contract.md](docs/state-digest-contract.md); read that before changing anything it touches.
- **The RNG is a specified contract.** One MT19937 stream, seeded once per game, written down in [docs/rng-contract.md](docs/rng-contract.md) and implemented by `engine/lib/mt19937.py`. There is no backend flag any more, and no floating point anywhere in it — bounded integers come off the raw 32-bit stream by masked rejection. **Changing any of it changes every game outcome**, so treat it as a wire format: `datasets/rng/vectors.json` is the cross-language fixture the C# port is accepted against, and `unit_test/test_rng.py` fails if you move the stream without regenerating it.

## Critical constraints

**Determinism is load-bearing.** The replay corpus is only an oracle if the engine is deterministic. Do not introduce into any gameplay path:

- wall-clock time or dates
- unseeded randomness, or any RNG other than the seeded `Random` instance
- iteration over unordered `set`/`dict` where the order can affect game state
- threading or async that touches game state

The engine has been audited against all four — see [docs/determinism-audit.md](docs/determinism-audit.md) for what was found, what the harness must pin, and which sets are already known harmless. **Check there before re-deriving anything.** Run `python -m tools.determinism.check_runs` after any change to a gameplay path.

**Do not author specs from `data/cards.json`.** The engine's card text has 36 cards corrupted by an encoding round-trip and 197 that differ materially from the printed card — `03025` is missing an entire rules line. Printed text lives in `datasets/cards/`, built from a vendored MarvelSDB snapshot. See [docs/card-dataset.md](docs/card-dataset.md).

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

Each run also writes `bot-manifest-<scenario>-<heroes>-<seed>-<games>.json` beside its scenes, recording the resolved input timeout, the policy, the fabricated-input count, and one entry per game. **The input timeout must be 0**: a non-zero one lets `DoGetInput` return an untouched `"{}"` that the replay records as a decline nobody made. Generation refuses to start or save if it is not, and the bot device raises `FabricatedInputError` rather than let one through — see MARVEL-32.

**Do not let an exception you raise for integrity get swallowed.** `EffectInvoker`, `Message2.Send`, the cost and target checkers, and `Engine.EngineRun` all catch broadly so one bad card cannot end the game, and all report through `Log.OnCrash` — which re-raises only when `Build.release` is false, and `build.py` hardcodes it true. If continuing would produce a *wrong artefact* rather than a wrong frame, derive from `core.errors.EngineIntegrityError`: `Log.OnCrash` re-raises that class regardless of the build.

Decisions come from a **policy** (`BotPolicy.Choose(decision) -> CommandDescriptor`) injected into `BotDeviceManager`. The two shipped policies are deliberately trivial — they prove the device works, they do not play well. A real policy subclasses `BotPolicy` and registers in `BotPolicyFactory`.

The device answers through `DeviceManager.WhenInput`, the same entry point the web server uses for a browser POST, so `Controller.ChoiceOne` runs its normal validation, CRC and `replay.Push` path. **Do not add a shortcut around `ChoiceOne`** — bot replays must be structurally indistinguishable from human ones or the corpus is worthless.

## Testing

From `py_src/`:

```bash
# fast tests: pure logic, no engine bootstrap beyond `import engine`
python -m unittest unit_test.test_bot unit_test.test_teamup_order \
                   unit_test.test_local_effect_order unit_test.test_scene_hash \
                   unit_test.test_bot_timeout unit_test.test_card_dataset \
                   unit_test.test_rng unit_test.test_package_tools \
                   unit_test.test_replay_crc
# spec harness and puzzle commands: boot the engine and play puzzle boards,
# still under a second
python -m unittest unit_test.test_spec_harness unit_test.test_spec_validate \
                   unit_test.test_puzzle
python -m tools.determinism.check_runs --runs 6  # digest reproduction across processes
python -m tools.determinism.check_scene_repro    # same seed -> same saved file
python -m tools.spec.validate --trusted-only     # every trusted behavioral spec
python main.py -bot -bot_verify                  # generate a game and replay-verify it
```

Name the modules explicitly. `unittest discover` picks up `unit_test/test_all.py`,
which is the replay suite described below and does not run.

**The suite does not touch the repository.** Running it leaves `git status` clean
and creates no commits. Packaging is a separate, deliberate step:

```bash
python -m tools.package.bump              # bump BUILD in build.py and commit it
python -m tools.package.bump --no-commit  # bump only
python -m tools.package.zip_cards         # write cards-<version>.zip (gitignored)
```

Both of these mutate the working tree, and `bump` commits — never wire them into
a test, a hook, or CI. They used to be `test_IncreaseVersion` and `test_zip_cards`
in `unit_test/test_task.py`, so every run of the suite bumped the version and
left a commit on whatever branch was checked out; with agents in parallel
worktrees, two suites editing the same `BUILD` line collide at merge. That is
MARVEL-55, and `unit_test/test_package_tools.py` guards against it coming back.
The card zip is known incomplete (MARVEL-56) and not byte-reproducible (MARVEL-57).

Regenerate the RNG vectors after touching anything in `engine/lib/mt19937.py` or the `Random` facade — `unit_test.test_rng` fails until you do:

```bash
python -m tools.rng.emit_vectors         # write datasets/rng/vectors.json
python -m tools.rng.emit_vectors --check  # non-zero if stale
```

Regenerate the card dataset after touching `data/cards.json`, the card scripts, or `datasets/marvelsdb/`:

```bash
python -m tools.cards.extract           # write datasets/cards/
python -m tools.cards.extract --check    # exit 1 if the checked-in copy is stale
```

The replay suite (`game/test/test.py` → `TestRun`) re-executes a scene's inputs and asserts per-step digest equality. **There is no working command-line flag for it** — `-test` only expands to `-device -no_editor …`, nothing sets the `InTesting` start state, and the process blocks in `WaitUntilGameStart()`. The `-test_all` and `-profile_folder` branches are unreachable because `build.py` hardcodes `Build.release = True`. Tracked as MARVEL-28. Until then, use `-bot_verify` or the `/T` debug command from the web client.

**`replays/` is empty and untracked**, so there is no regression suite yet. Building it is the entire point of the `Corpus and Oracle` phase — weigh changes accordingly.

New tooling needs its own tests: test behavior not implementation, no assertion-free tests, coverage is an observed outcome and never a target.

## Behavioral specs

The replay corpus answers "did this game reproduce". It cannot answer "does Swinging Web Kick deal 8 damage". Behavioral specs do, and they are what the C# engine will be held to. The format is decided in MARVEL-22 — read it before changing `tools/spec/`.

**A scenario is a transcript**: one `When` per engine decision, with `Then`s interleaved, in Gherkin `.feature` files under `py_src/specs/`. The engine is a fold `(state, input) -> (state, prompt)` and a scenario is a literal trace of it.

```gherkin
When I play "Nick Fury"
Then I am prompted to choose one
  | Draw 3 cards              |
  | Deal 4 damage to an enemy |

When I choose "Deal 4 damage to an enemy" targeting "Shocker"
Then "Shocker" has 4 damage
And I am not prompted again
```

The verbosity buys the two assertions a batched format cannot make: **which options the engine offered** (state-dependent behavior — Nick Fury is printed as a three-way choice but offers two when no scheme has threat) and **that the resolution ended**. And the harness **never answers a decision the transcript omits** — an unanswered mid-resolution choice is `FAIL-spec-wrong`, not a silent pick. Without that rule the other two are decoration.

```bash
python -m tools.spec.run_case specs/                    # run scenarios, see what happened
python -m tools.spec.validate                           # assign verdicts, update the manifests
python -m tools.spec.validate --trusted-only            # the gate: every trusted spec must pass
python -m tools.spec.validate --triage triage.json      # records for adjudicating disagreements
```

**A scenario is not trusted until it passes.** `specs/trusted.json` is written only by the validation runner, only from `PASS`, and each entry is pinned to the hash of its scenario source — edit a scenario and it drops out on the next run. There is no way to add one by hand. Everything else is quarantined with a verdict: `FAIL-spec-wrong` (the engine never offered what the scenario describes — probably a misread card), `FAIL-engine-suspected` (it ran cleanly and disagreed anyway), or `ERROR`. A disagreement is triaged, never dismissed; both kinds are worth finding.

`specs/steps.catalogue.json` is the closed step vocabulary; a test asserts the parser implements exactly it, so drift between the Python and C# runners fails a build. Scenarios name cards by printed name and are tagged `@card:<id>`; object ids never appear in a spec.

`specs/self-test/quarantine.feature` is wrong on purpose and must stay that way — it is the proof the gate works.

This only works while the Python engine still runs and is still the reference. Read [docs/spec-harness.md](docs/spec-harness.md) before authoring.

## Workflow

### Plane

All work is tracked in Plane, project `MARVEL`. Every issue belongs to a module (phase). See [docs/plane.md](docs/plane.md).

### Branching

`master` is the long-lived branch. Cut a short-lived `<type>/marvel-<id>-<slug>` branch off `master`, open a PR, and squash-merge.

**Never close or merge a pull request you did not open in the current session.** If a PR looks like a blocker, report it and stop.

### Commits

Conventional Commits: `<type>(<scope>): <description>`. Use `py` scope for `py_src/` changes and `engine` for `src/`.
