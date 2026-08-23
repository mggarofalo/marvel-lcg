# Engine conventions

Load-bearing conventions inside `py_src/` that are not obvious from the code, and
the reasoning behind them. Split out of AGENTS.md, which routes here rather than
restating it.

Everything here is about the **Python reference engine**. Where a convention is
meant to survive into the C# engine, it says so.

## Configuration precedence

`engine/config.py` resolves every flag from four sources, strongest first:

1. the command line
2. an arg group it expanded (`-bot`, `-test`)
3. `launch.json`
4. the declared default

It is **precedence, not arrival order**. `-bot -no_check_invariants` and
`-no_check_invariants -bot` mean the same thing, because
`ConfigVariables.InitVariable` re-derives the winner every time it runs rather
than remembering who wrote first.

It did not always. A group expansion wrote straight into `instance_command` and
stamped `set_from = "CommandLine"` before the real command line was read, and
`SetValue` returned early when the source matched — so a flag inside a group
could not be overridden at all, of any type. That was MARVEL-64.
`unit_test/test_config.py` pins the rule, including that `-bot` and `-test`
still expand to exactly what they used to.

Two further rules about groups:

- **A group takes no arguments.** A value written after one is ignored, because
  there is nothing to attach it to.
- **A group must never name a valued variable with no value.** `ConfigVariables`
  reads a valueless flag as a bool, so a bare `-device` inside a group set the
  device *name* to the string `"True"` (MARVEL-28). It then fell through to the
  interactive keyboard device and blocked in `WaitUntilGameStart()` forever.
  `unit_test/test_verify_replays.py` fails if any group does that again.

## Integrity errors must not be swallowed

`EffectInvoker`, `Message2.Send`, the cost and target checkers, and
`Engine.EngineRun` all catch broadly so one bad card cannot end the game, and
all report through `Log.OnCrash` — which re-raises only when `Build.release` is
false, and `build.py` hardcodes it true.

**If continuing would produce a *wrong artefact* rather than a wrong frame,
derive from `core.errors.EngineIntegrityError`.** `Log.OnCrash` re-raises that
class regardless of the build.

`unit_test/test_integrity_errors.py` enforces the rule rather than restating it.
It walks `engine/` and `game/` and fails on any `except` an integrity error
could stop at: a broad clause that swallows, or an `except EngineIntegrityError`
that never re-raises. **Clause order counts** — `except Exception` written above
`except EngineIntegrityError` catches it first and makes the guard dead code, so
the broad clause is still reported.

Everything that legitimately absorbs is listed in `REVIEWED_ABSORBERS` with the
reason. **Adding an entry is a decision:** if an integrity error can reach the
`try`, put `except EngineIntegrityError: raise` ahead of the broad clause
instead. The scan goes quiet either way and only one of the two is correct.

**Threads were the case the convention alone missed.** `Job.run_job` and
`Task.run` wrap their work in `except Exception`, and a worker thread cannot
raise at its caller, so an integrity error there was logged and dropped
(MARVEL-54). Both now hold it and re-raise on whoever waits —
`JobManager.WaitForAllJobsToComplete` / `WaitForAnyJobToComplete` /
`Job.WaitFinished`, and `TaskManager.WaitTasksFinished` / `Task.WaitFinished`.
It is deliberately not cleared once raised: every waiter gets it. Ordinary
exceptions stay absorbed. `python -m tools.determinism.probe_threaded_integrity`
checks both on the real `WaitConnect` job path in a real game.

### `Log.HasError` is a correctness signal

It is how `-bot_verify` decides a replay diverged (`TestRun.Run` returns True
unconditionally and reports a failed case by *logging* it) and how the spec
harness catches a case that passed over a swallowed exception. It reads the
counts `LogHelper.StatLog` writes, so anything that stops those being written
silently turns both gates into always-pass.

That is exactly what MARVEL-65 was, in two places at once: `StatLog` skipped
recording on a release build, and `PrintLog` skipped it for a hidden category.
Recording now happens before every display filter. **Never gate it on a
presentation concern** — whether a line is printed and whether an error is
detectable are different questions.

## `Build.release` is hardcoded true and stays that way

It is read at around sixty sites and decides far more than log formatting:
`Log.OnCrash` re-raises only when it is false, the editor only initialises when
it is false, and `Ver.ui_version_str` gains the `r`/`d` suffix every API route's
`app_version` cookie is checked against.

The headless bot's crash capture exists *because* a release build absorbs —
flipping the flag would turn every absorbed card bug into a run-ending exception
and change what a corpus run means. What MARVEL-28 removed was the line above
it, `release = "RELEASE" in os.environ`, which read as an override and was dead:
the hardcode overwrote it unconditionally. Nothing in the verification path
reads the flag either way.

## Crash capture

Because the broad handlers swallow, most of what self-play trips would otherwise
be a traceback on stdout and nothing else.
`engine/device/manager/bot/crash.py` turns each one into an artefact in
`crashes/` (gitignored, `-bot_crash_folder`). The run installs
`Log.crash_observer` for the duration and takes it back down afterwards
(MARVEL-12).

| File | What it is |
|---|---|
| `bot-crash-<class>-<signature>.json` | the scene: seed plus every input up to the failure, an ordinary replay |
| `bot-crash-<class>-<signature>.crash.json` | the sidecar: class, traceback, step, state digest, seed, and the exact command that regenerates the game |
| `bot-crashes-<scenario>-<heroes>-<seed>-<games>.json` | the run report: distinct signatures with occurrence counts and a minimal repro for each |

- **One bug is one file.** Failures group by a signature over the exception type
  and the frames it travelled through, so ten thousand recurrences produce one
  scene. The exception *message* is deliberately not part of it — it carries
  card names and would split one bug per game.
- **The minimal repro is the shortest one.** Whichever occurrence reached the
  failure in the fewest steps replaces the stored scene.
- **Five classes**, in resolution order: `fabricated-input`
  (`FabricatedInputError`, plus the runner's refusals for a non-zero resolved
  timeout or a counted fabricated input), `invariant-violation` (the MARVEL-11
  checker, replay verification disagreeing, a scene that would not save),
  `engine-assert`, `timeout-stall` (`BotStuck`, `NoProgressError`,
  `bot_max_steps`, restart exhaustion), `unhandled-exception`.
- **Only `fabricated-input` gets no scene.** There a decision in the recorded
  list was returned by a timed-out wait rather than made by the policy, so
  writing it would put a replay of a decision nobody made on disk; the seed
  reproduces it instead (MARVEL-32). Every other class keeps its scene,
  including a checker violation — its inputs were all genuinely made by the
  policy and only the state computed from them is wrong.
  `SCENE_WITHHELD_CLASSES` covered every `EngineIntegrityError` until MARVEL-66,
  which produced two artefacts that disagreed about whether a repro existed.
- Artefacts read no clock and no host: traceback paths are relative.
- **A captured crash does not fail the run.** Most of what self-play finds is a
  pre-existing bug in this engine, to be logged rather than to block corpus
  generation. `-bot_fail_on_crash` gates on them; `-no_bot_capture_crashes`
  turns collection off.

## Headless runs build no `WorldDescriptor`

`WorldRender.PresentInternal` serialises the whole board on every present, and
one thing reads it — the browser sync in `GameServerSync.handle_post`.
`DeviceManager.IsRenderNeeded()` answers whether anything will; `BotDeviceManager`
and the determinism harness's `NullDeviceManager` say no, everything else
inherits `True`.

Measured at 2.6x end-to-end on twelve games (7.41s → 2.87s), and 61% of profiled
runtime, because a present happens on *every message* rather than only at a
decision — 2317 presents against 192 digests in one six-game run (MARVEL-29).

Only the descriptor is conditional. The prompt, round id, render id, game log
and `WaitSync` all still happen, because the rest of the engine reads them and a
headless run's recorded steps have to be **identical** to a rendered one's rather
than merely similar. `unit_test/test_render_skip.py` pins that split.

One thing is *not* identical: `object_manager.index_dict["check_message"]`.
Building a descriptor asks the game questions, and those allocate
`check_message` objects, so a rendered run ends with a higher count than a
headless one on the same seed and inputs. Nothing persists it — saves and
per-step digests are unaffected — but `tools/determinism/headless.py` folds the
whole index into its aggregate run digest, so harness run digests changed once
when this landed (MARVEL-75).

## Bot saves and run manifests

Bot saves are **deterministic saves**: `sign`, `time`, and `playtime` are
omitted, so the same seed writes a byte-identical file on any machine and no
host fingerprint reaches the repo. `-no_bot_deterministic_save` restores the
human save format; human-facing saves are unaffected either way (MARVEL-27).

Each run writes `bot-manifest-<scenario>-<heroes>-<seed>-<games>.json` beside
its scenes: the resolved input timeout, the policy, the fabricated-input count,
`crashes`, and one entry per game.

**The input timeout must be 0.** A non-zero one lets `DoGetInput` return an
untouched `"{}"` that the replay records as a decline nobody made. Generation
refuses to start or save if it is not, and the bot device raises
`FabricatedInputError` rather than let one through (MARVEL-32).

**The manifest also carries the fully resolved config and the commit that
produced it**, because the engine is deterministic *for a given configuration*
and not across configurations — the audit measured 158 against 183 forced
effects under different flags, with per-card digests unchanged.
`engine/config_record.py` snapshots every registered `ConfigVariables` entry
with the source that decided it, and `-verify_replays` compares each manifest it
finds against the running process and **fails the run on drift**
(`-verify_allow_config_drift` waives it).

Read that module's docstring before touching the comparison: what is compared is
a **denylist**, so a new gameplay flag is compared by default, and the
exclusions each stand for a reason. Two were calibration results rather than
guesses — `check_invariants` is forced on by the resolved device and so can
never agree between a generator and a verifier, and a variable only one side
*registered* is not drift at all, because a config variable exists only once the
module declaring it has been imported. `tools/replay/probe_verify.py` pins that
the gate rejects real drift and accepts the honest path (MARVEL-34).

## Bot policies

Decisions come from a **policy** (`BotPolicy.Choose(decision) -> CommandDescriptor`)
injected into `BotDeviceManager`. The two shipped policies are deliberately
trivial — they prove the device works, they do not play well. A real policy
subclasses `BotPolicy` and registers in `BotPolicyFactory`.

`BotCommand` is the shared plumbing under every policy, and it answers with the
**minimum legal** command: the fewest targets, the cheapest payment. That is
neutral except where the size of the payment *is* the effect. For resources, a
printed X (`Variable`) or "spend up to N" (`UpTo`) is paid maximally, up to the
ceiling, in engine order; `-no_bot_pay_variable_cost` restores the old planner
(MARVEL-135). Card and counter costs carry the equivalent `pay_size_is_effect`
option marker and are also maximised; `-no_bot_pay_variable_card_cost` restores
their old minimum (MARVEL-138). Ordinary effect targets are still
minimum-selected.

**An unmoved digest does not clear a change like this**: the wide matrix may
never reach the affected cost, so bound it with constructed cases.

The device answers through `DeviceManager.WhenInput`, the same entry point the
web server uses for a browser POST, so `Controller.ChoiceOne` runs its normal
validation, CRC and `replay.Push` path. **Do not add a shortcut around
`ChoiceOne`** — bot replays must be structurally indistinguishable from human
ones or the corpus is worthless.

## Fixture staleness

All three `--check` gates (`tools.rng.emit_vectors`, `tools.digest.emit_vectors`,
`tools.cards.extract`) mean the same thing by "stale", and it is written down
once in `py_src/tools/fixtures.py`: the checked-in file must be **byte for byte**
what the generator would write. Not the same parsed JSON — key order, the
one-card-per-line layout, number formatting and Unicode escaping are all part of
what a C# implementer reads, so they are all part of the comparison.
`unit_test/test_fixture_staleness.py` mutation-tests the comparison and asserts
the three gates still share it.

That makes **line endings part of the comparison**, so `.gitattributes` pinning
`eol=lf` is load-bearing rather than tidy — git's `core.autocrlf` defaults to
true on Windows and a clone made before MARVEL-67 has a working tree full of
CRLF. A CRLF checkout fails all three gates, deliberately, with a message that
says so: the repair is to re-normalise the checkout, not to regenerate anything.

The three used to disagree, and nobody could say how. All three read the file in
text mode, so Python's universal-newline translation quietly hid CRLF from every
one of them; what actually failed on Windows was `tools.cards.extract`, for an
unrelated reason. Its output header carries a SHA-256 of each engine file it was
built from, `tools/cards/engine.py:Sha256` hashed raw bytes, and on a CRLF
checkout those bytes differ. **That hash is newline-normalised now**
(MARVEL-73): it names the file's content, not the checkout, and it is no longer
the value `sha256sum` prints on a CRLF tree.

## Packaging is never a test

```bash
python -m tools.package.bump              # bump BUILD in build.py and commit it
python -m tools.package.bump --no-commit  # bump only
python -m tools.package.zip_cards         # write cards-<version>.zip (gitignored)
```

Both mutate the working tree, and `bump` commits — **never wire them into a
test, a hook, or CI.** They used to be `test_IncreaseVersion` and `test_zip_cards`
in `unit_test/test_task.py`, so every run of the suite bumped the version and
left a commit on whatever branch was checked out; with agents in parallel
worktrees, two suites editing the same `BUILD` line collide at merge. That is
MARVEL-55, and `unit_test/test_package_tools.py` guards against it coming back.

The card zip covers every folder under `cards/pack/` holding a card script —
derived by walking the tree, not from a list (MARVEL-56) — and is
byte-reproducible across checkouts (MARVEL-57). Arcnames are flat, so
`ZipCards` refuses a duplicate basename rather than silently overwriting.

## Replay verification outcomes

`-verify_replays` re-executes a scene's inputs and asserts per-step digest
equality (`game/test/test.py` → `TestRun`). Three outcomes, in
`game/test/verify.py`:

- **pass** — every recorded step reproduced its digest.
- **fail** — a step diverged, or the replay raised.
- **incomplete** — the recording ran out while the game was still going. Every
  step it held reproduced, but the file describes less than a game, so it fails
  the run unless `-verify_allow_incomplete`.

Three things are load-bearing:

- **A corpus folder is not all scenes.** `bot-manifest-*.json` and
  `bot-coverage-*.json` sit beside the scenes they describe, and every field of
  `Scene` has a default — so loading a manifest as a scene *succeeds* and yields
  an empty game rather than an error. `ReplayVerifier.IsSceneDocument` filters
  before the load, not through it.
- **The device must never block.** `-verify_replays` forces `-device verify`,
  whose only job is to end the game when the recording runs out instead of
  waiting for a decision.
- **Verifying nothing is a failure.** An empty folder exits non-zero, because
  "the gate found no divergence" and "the gate never ran" must not look the same
  to CI.

## Server-side visibility filtering

> **`py_src` is never served** — see
> [migration.md](migration.md#deployment-py_src-is-never-served). The residuals
> named below are not Python work items. This is a cooperative game and secrecy
> between players is mostly not the point; what carries forward is that **the
> server decides what each seat sees**, rather than trusting the client's
> assertion of `p` / `hot_seat` / `watch`. A permissive policy is legitimate —
> it just has to be a policy.

`ToDescriptor.World` builds one `WorldDescriptor` per render for the whole table
— a `CardDescriptor` for every card in the game, each carrying `card_id`, `name`
and `info` next to a `visible_for_players` list. Until MARVEL-62 all of it went
to whichever client asked and `visible_for_players` was an instruction the
browser was trusted to follow, so a `curl` with a valid `app_version` cookie
read the encounter deck in order, every other player's hand, and the identity of
every face-down card in play.

`engine/device/web/server/world_visibility.py` now strips the face off every
card the requesting players may not see, before `handle_get_world` puts it on
the wire.

- **The walk is driven by the shape of the descriptor, not a list of zone
  names.** A zone added to `WorldDescriptor` is filtered the day it is added, and
  `unit_test/test_world_visibility.py` fails if a field is added to
  `CardDescriptor` without a decision about whether it may leave the server.
- **Redaction, not deletion.** A client still draws a face-down deck of the right
  height, so a hidden card keeps its object id, its zone, its exhausted state and
  its printed back. Hidden cards within a zone come back sorted by object id,
  because object ids are stable for a whole game and the real order would
  otherwise say which card is on top. That the ids are real at all is the
  residual — MARVEL-146.
- **It is filtering, not authentication, and it covers the cards only.** `p`,
  `hot_seat` and `watch` are asserted by the requesting client and nothing checks
  them (MARVEL-145). The descriptor's free text — `prompt`, `prompt_last_text`,
  `event_name` — is composed by the message senders and goes out unfiltered, and
  some of those strings name cards moving into a hand (MARVEL-152). `/read_file`
  plus the `/debug` console still reach a saved scene, seed and all (MARVEL-153).
- A consequence: the browser's `?show_all_cards` and the debug console's
  `cheat_show_all_cards` can no longer reveal what the server did not send. The
  scene rule `show_all_cards` (`game/world/world_rule.py`) still does, for player
  0, which is the path puzzles and tests use.

## Things that look broken but aren't

- Most API routes require an `app_version` cookie matching `Ver.ui_version_str`
  (`<version>r` release, `<version>d` debug). A **cookieless request always fails
  the version check** and is served `public/clean_cache.html` — see
  `IsVersionMatch` in `py_src/engine/network/web_server.py:88`. To call the API
  directly:
  `curl -s --cookie "app_version=0.5.9.201r" http://127.0.0.1:2345/list_scenarios`
- `assets/` and `replays/` are absent from a clean clone and the engine runs
  anyway — card images come from the `image_servers` in `launch.json`, and
  missing ones are generated as placeholders by `engine/lib/image_creator.py`.
- The web client's TypeScript compiles to **gitignored** JavaScript, so a clean
  clone has no compiled client. See `py_src/public/js/tsconfig.json`. The API
  works without it; the browser UI does not.
- `numpy` is **gone** as of MARVEL-38 — it was only ever the default RNG backend.
  Nothing imports it. Do not add it back without a reason that survives
  [rng-contract.md](rng-contract.md).

## Cross-OS digest agreement

`check_runs` proves each platform reproduces *itself* across fresh processes.
That is a weaker claim than it looks: an ordering hazard can resolve one way in
every process on one machine and the other way on the other machine, and no
single process ever sees both. `tools/determinism/cross_os.py` closes that gap in
two halves, because CI cannot do it in one — each leg of the `cross-os` matrix
runs `emit` and uploads its trace, then `cross-os-compare` downloads both and
diffs them per step.

What is compared is pinned in `COMPARED_FIELDS` and guarded by
`unit_test/test_cross_os.py`: the run digest, the step count, `persisted_index`,
`game_over`, and `error`. The platform block — OS, release, machine, Python
version — is recorded for the report and deliberately never compared. Narrowing
that set is the one edit that would make this gate pass on a real divergence, so
the test pins the set itself.

**`persisted_index`, not the whole `object_index`** (MARVEL-75).
`ObjectManager.index_dict` counts every allocated object; only three of those
counters — `card`, `effect`, `message` — have ids that reach something anyone
keeps. That is measured, not chosen: `m`, `e` and `c` are the only id prefixes
anywhere in a saved scene, and `card` is the only one on the v2 digest wire
(`owner` there is a seat index, not the `player` counter). Folding the rest in
made the harness assert that the engine allocated the same number of *internal
query objects*, which moves whenever anything asks the engine a question — twice
in one day, both benign, an investigation each time.
`headless.PERSISTED_ID_CATEGORIES` is the set and `unit_test/test_run_digest.py`
pins both directions.

A divergence here means **the corpus is only valid on the platform that produced
it**, which constrains the whole C# validation strategy. File it and stop; do not
work around it. The audit names the identity-hash orderings (team-up units,
forced-effect resolution) as the likeliest to differ under another allocator.

The harness can drive the engine two ways. `decline_everything` answers every
prompt with the empty command; `PolicyDriver` answers from a real `BotPolicy`, so
the game plays cards, runs roughly twice as long, and opens response windows a
decline-only run never reaches. Which driver a measurement used is load-bearing:
a decline-only run reaches no forced-ability tie-break at all, so digest evidence
from one says nothing about MARVEL-39 or MARVEL-40 (MARVEL-69).
