# Determinism audit of the Python engine

Tracked as `MARVEL-7`. Audited against commit `0f992b5` on 2026-08-06,
CPython 3.13.14 on Windows 11.

## Why this exists

The migration to C# is validated by replaying a corpus through both engines and
comparing `World.CalculateCRC()` at every step. That only works if the Python
engine is deterministic. If it is not, replays will not reproduce, and the
oracle is worthless. This audit runs before the corpus is generated rather than
after, because a corpus recorded against a nondeterministic engine cannot be
salvaged.

## Verdict

**The engine can be made deterministic, and on the evidence gathered it already
is for the games that could be driven.** Nothing found requires architectural
change. The two hazards that could genuinely corrupt a corpus are a wall-clock
timeout on player input, which is switched off by a configuration value, and
one set-ordering site in the effect pipeline, which is a one-line sort away from
being safe.

Concretely: seven scenario-and-hero combinations were played headlessly, 100
times each for the two smoke cases and 20 times each for the wider matrix, in
fresh processes. Every run reproduced every per-step digest byte for byte.

What is *not* established: no corpus exists yet, so nothing was replayed through
the engine's own replay path; no Linux host was available, so the cross-OS half
of the acceptance criterion is untested; and the driver used declines every
optional ability, so branches only a real bot would reach are unexercised. Those
gaps are listed at the end with the commands that close them.

## What was checked, and how

Two kinds of work.

**Static tracing.** Every suspected source was followed from where it is
introduced to whether it can reach either game state or the digest. The
distinction matters: a `set` that is only ever tested for membership cannot
change anything, and reporting it would waste the reader's time. Findings below
are limited to sites where the nondeterministic thing actually escapes.

**Empirical probes.** Three, all in `tools/determinism/`. One establishes which
container orderings CPython reproduces across processes. One checks the seeded
RNG. One boots the engine with no human and diffs the per-step digests across
runs. Results are quoted inline; every number can be regenerated with the
commands in "The harness".

## Findings

### F1 — Wall-clock timeout on player input can change a decision (High)

`engine/device/manager/base.py:82-119`

`DoGetInput` waits on a condition with `self.timer.max_timeout` as the timeout.
When it expires, the player is dropped from `asking_players` and the function
returns `ask_options[player_id].input_json`, which is still its initial `"{}"`.
`Controller.ChoiceOne` parses that as effect id `0` — decline — and records it
as the step's input. So a slow client silently becomes "the player passed".

`max_timeout` comes from `NewGameDescriptor.timeout` by way of
`GameSession.GameSetup` (`game/game_run/game_session.py:64-70`). It defaults to
`0`, which the code treats as "no timeout", so the hazard is dormant in the
default configuration. It is a real path into game state, not a theoretical one,
and it is the only wall-clock value anywhere in the engine that can.

**Recommendation.** The corpus generator must force `timeout = 0` and refuse to
generate from any scene whose timeout is non-zero. Record the value in the
corpus metadata. In the C# engine, a timeout is a property of the transport, not
of the fold, and should not be able to synthesise an input.

### F2 — On-card effect ordering comes from a `set` of `CardFace` (High)

`game/message/message.py:42` — `self.related_faces: Set['CardFace'] = set()`
`game/event/manager.py:644-655` — `find_local_effects()` iterates it

`Message2.related_faces` is a `set`. `CardFace` defines `__lt__` but no
`__hash__` or `__eq__`, so it is hashed by identity — that is, by memory
address. `find_local_effects()` walks that set and appends each face's matching
local effects to a list, so the list order is address order.

That list then reaches two places that do not sort it:

- `game/event/manager.py:822-829` builds `forced_effects` as the local effects
  followed by the global ones, and hands it to `ProcessForcedEffect`
  (`game/event/manager.py:331-397`). That function takes `forced_effects[0]`,
  and where several forced abilities are on one card it resolves them in list
  order. Where they are on different cards it asks the first player to break the
  tie — but the *order the options are offered in* is still set order.
- `game/event/manager.py:763-771` runs local effects for `NoSendResolve`
  messages in list order.

The optional path is already safe: `game/event/manager.py:301` does
`sorted(filtered_effects, key=lambda e: e.object_id)` before offering anything
to a player. The forced path simply never got the same treatment. The comment at
`game/event/manager.py:337-338` — "The order of `forced_effects` is by the id,
which means when this card be created" — describes an invariant the code does
not actually hold.

This bites whenever one message relates two or more faces that each carry a
matching forced ability. `AddRelatedFace` is called with every moved card, every
attacker and defender, and so on, so multi-element sets are routine.

**Empirically, it did not fire.** Deliberately shifting the allocator before
boot changed identity-hash ordering inside the engine process (verified by
probing a fresh set in the same process: `2,9,1,8,0,7,6,5,3,4` became
`4,2,3,6,9,0,7,1,8,5`) and left every per-step digest identical. So the hazard is
latent in these games, not live. It is still a hazard: nothing in the code
prevents it, and "we did not hit it in seven scenarios" is not a guarantee.

**Recommendation.** Sort in `find_local_effects` by `effect.object_id`, matching
what the optional path already does. One line. It changes resolution order in
cases where the order was previously arbitrary, so it needs its own issue and a
corpus regenerated afterwards — do not land it silently.

**Status: fixed (MARVEL-31).** The closure is now
`EventManager.FindLocalEffects`, a static method that sorts by
`Effect.object_id`, so both consumers — the forced path and the `NoSendResolve`
path — see creation order. Covered by `unit_test/test_local_effect_order.py`.

*Digest impact, measured.* All seven wide-matrix digests are unchanged
(`97fa1611b360d813`, `9fafd7bbe3691fea`, `0d9b285879f79e09`, `625cb6235b2da284`,
`335b13994eb44ac3`, `0f56cf5fc6bc92dc`, `9e97e7d8310a7fd5`). On its own that is
ambiguous, so `tools/determinism/probe_local_effect_order.py` counts what the
sort actually saw:

```
total: 81 batches, 5 with two or more effects, 0 reordered by the sort
```

So the sort was reached and agreed with the address order everywhere it could
have disagreed — the digests are unchanged because the orders matched, not
because the code path went unexercised. Coverage is still thin: five
multi-effect batches, largest of size two. The decline-only driver is the limit
here, not the fix.

*Rules check.* The Rules Reference says "if two or more forced abilities would
initiate at the same moment, the first player determines the order in which the
abilities initiate, regardless of who controls the cards bearing those
abilities." `ProcessForcedEffect` already implements that via
`first_player.AskChooseFace`, so list order decides only which order the options
are *offered* in — not who decides. Two deviations found while confirming this
are filed separately: `MARVEL-39` (the tie-break resolves the wrong effect when
a delay ability is in the batch) and `MARVEL-40` (abilities on one card skip the
first-player choice entirely).

### F3 — `GetTeamUpUnits` returns a list built from a `set` (High)

`game/card/face/attribute/has_teamup.py:24-36`

```python
faces: Set['Ally|Identity'] = set()
...
return list(faces)
```

This is the legal-target list for cards with the TeamUp keyword. It is wired in
at `game/selector/selector_target_helper.py:15,297` and used by at least
thirteen printed cards (Ant-Man/Wasp, Colossus/Shadowcat, Cable/Deadpool, and so
on). `game/selector/selector_target.py:77` pins the TeamUp target range at
`(2,2)`, so both entries are always selected.

The order then reaches the recorded wire format:
`engine/controller/controller.py:319-322` writes
`[x.GetReplayText() for x in select_effect.targets]` into the step's command.
Two runs of identical play can therefore produce corpus files whose target lists
are permuted. Replay tolerates this — targets are re-resolved by card object id
— but a cross-engine diff would not, and byte-comparison of two independently
generated corpora would show spurious differences.

**Recommendation.** `return sorted(faces)`. `CardFace.__lt__` already orders by
card object id, so this is safe and free.

**Status: fixed (MARVEL-30).** `GetTeamUpUnits` now returns
`sorted(faces, key=lambda face: face.card.object_id)` — the explicit key rather
than `__lt__`, so the ordering contract is readable at the call site. It is the
same pattern the optional-effect path applies to `Effect.object_id`, over a
different id namespace. `Card.object_id` comes from one monotonic per-category
counter in `game/object/manager.py` and the set only ever holds faces of
distinct cards, so the key is a total order with no ties. Covered by
`unit_test/test_teamup_order.py`. Both smoke-matrix digests are unchanged
(`97fa1611b360d813`, `9fafd7bbe3691fea`), which is expected: neither audited
scenario played a team-up card.

### F4 — Change Form abilities are registered in set order (Medium)

`game/player/element/player_setup.py:70-76`

```python
identities: Set['AlterEgo|Hero'] = set([])
...
for identity in sorted(identities):
    add_change_form_effect(identity, list(identities))
```

The outer loop sorts. The list passed in does not. That list becomes `faces` at
line 38, and the loop over it at line 44 registers one ability per entry, so it
determines the order in which `Effect` object ids are allocated.

Line 41-42 filters the list down to same-card faces when there are more than two
identities, which neuters the problem for most heroes — but only for heroes
whose card id does not start with `HACK_HERO_ID = '3100'`. Heroes with more than
two identity faces are the exposure.

**Recommendation.** `sorted(identities)` in both places. Trivial and safe.

### F5 — Configuration flags shift object id allocation (Medium)

`game/event/manager.py:139-146`, `game/statistics/game_statistics.py:255-256`

`RegisterPlayRule` registers the statistics and achievement abilities only when
`Engine.statistics.CanRegisterAbility()` is true, which reduces to
`pause_internal != True`. That is set from the `statistics` config flag, the
`pause_test_statistics` flag, whether the scene is a puzzle, and whether the
cheat console is active.

Measured on `rhino / spider_man / seed 12345`:

| configuration | `forced_effect` ids allocated | card digests |
|---|---|---|
| default | 158 | identical |
| `-no_statistics` | 158 | identical |
| `-no_pause_test_statistics` | 183 | identical |

The good news is in the third column: **the card-state digest is unaffected**.
`CalculateCRC` is keyed on card object ids (`game/world/world_render.py:123-132`)
and cards are allocated from a separate counter, so effect-id drift does not
reach it. Recorded commands do carry effect ids, but
`CommandDescriptor.FindNewEffectIdInternal` re-resolves them from the card id
and the effect's display name, which is why replay survives.

**Recommendation.** Not a defect to fix, but a pin to record. Store the full
resolved config alongside the corpus and replay under the same one. Treat any
change to these flags as invalidating the corpus.

### F6 — Saved replay files are not byte-reproducible and embed a machine fingerprint (Medium)

`game/scene/scene.py:101-134`, `engine/user/user_info.py:60-68`

`PrepareSave` writes three values that vary with the machine and the moment:

- `sign` — `UserInfo.fingerprint`, an md5 of hostname, processor, language and
  time zone
- `time` — local wall-clock, `"%Y-%m-%d %H-%M"`
- `playtime` — elapsed seconds

Two machines that play identically produce different files. This does not touch
per-step digests, but it does affect the corpus: files cannot be byte-compared,
regeneration always shows a diff, and committing them writes host-identifying
data into the repository.

**Recommendation.** The corpus generator should normalise or strip these three
fields before the file is frozen. Doing it in the generator rather than in
`Scene` keeps the change out of upstream code.

**Status: fixed (MARVEL-27).** Answered on both sides, and the
"keep it out of upstream code" caveat above is obsolete — we no longer track
upstream.

*What a hash may depend on.* `Scene.HashablePayload(data)` takes a scene as
loaded from disk and returns canonical JSON of everything except
`PROVENANCE_KEYS` (`sign`, `time`, `playtime`, `path`, `clients`, `report`, all
defined in `game/scene/scene.py`) and the file checksum, with keys sorted so
the result does not depend on the order `PrepareSave` emitted them in.
MARVEL-17 and MARVEL-18 hash this, not the file.

*What a generated file contains.* `Scene.Save(..., deterministic=True)` does not
write `AMBIENT_KEYS` at all — not even values carried in from a scene loaded
off a human save. `BotRunner` passes it, controlled by `bot_deterministic_save`
(default on), so nothing the bot writes carries a host fingerprint into the
repository. Every human-facing save path leaves the argument at its `False`
default, so that behaviour is untouched.

*Measured.* `tools/determinism/check_scene_repro.py` plays the same seed twice
in fresh processes:

```
deterministic save on   payloads identical, files byte-identical, no ambient keys
deterministic save OFF  payloads identical, sign/time/playtime written
```

The payload digest is the same value in both modes, so how a scene was saved
does not change what it hashes to. The control deliberately does *not* assert
that two human-style saves differ: `playtime` is written to one decimal place
and a game finishing in under a second can round the same way twice. What it
asserts is that the ambient metadata comes back, which is what proves the two
modes are not the same code path.

`python main.py -bot -bot_verify` still passes against a
deterministically-saved scene, which settles "confirm nothing in replay
verification depends on them".

### F7 — Test case ordering depends on directory listing order (Low)

`game/test/test.py:15-33`

`GetTestCases` collects files via `FileManager.ListDir` (`os.listdir`) and sorts
them only by a version tuple parsed out of the filename. Python's sort is
stable, so files sharing a version keep filesystem order — which differs between
Windows and Linux, and can differ after files are rewritten.

Per-case digests are unaffected, since each case is independent. What changes is
the order failures are reported in, and the order of any state that survives
between cases inside one process.

**Recommendation.** Sort by path as the final tiebreak in the harness, or accept
it and note that test *ordering* is not part of the contract.

### F8 — Web server threads mutate game state (Low)

`engine/device/web/server/server_new_game.py:13,19,23,48`

`Restart`, `NewGame`, `LoadReplay` and `LoadScene` are called from the aiohttp
thread while the game thread may be inside a step. This is a control-plane race
in interactive play, not a per-step hazard, and it disappears entirely in a
headless run.

**Recommendation.** The corpus generator must not run the web device. The
harness in `tools/determinism/` already uses a null device manager.

### F9 — `Random.Undo()` is unsound in both backends (Low)

`engine/lib/random.py:52-87`

Under the custom backend `Undo()` does nothing at all. Under numpy it pops
`Random.states`, but the pushes are conditional:
`RandomChoice2` returns early without pushing when `x == 1` (it delegates to
`RandomChoice`, which pushes once) and when `len(input_list) == x` (no push at
all). So pushes and pops are not paired, and `Random.states` grows without bound
across a session.

The only caller is `game/world/cheat/cheat_cmd_helper.py:390`, so this is dead
in normal play.

**Recommendation.** Leave it. Note it so nobody wires undo into corpus
generation and assumes RNG state rewinds correctly.

### F10 — The default RNG backend is an undeclared dependency (Low, but blocking)

`engine/lib/random.py:5`, `requirements.txt`

`disable_numpy_random` defaults to `False`, so `numpy.random` is the production
RNG and `engine/lib/mt19937.py` is dead code unless the flag is set. `numpy` is
not in `requirements.txt` — a clean install of what is listed (`packaging`,
`PIL`, `Pillow`, `requests`, `aiohttp`, `typing_extensions`, `colorama`) has no
numpy, and none of those pull it in transitively. The engine would raise
`ImportError` at the first shuffle.

`requirements.txt` also still lists `PIL`, which is not an installable package
(`MARVEL-2`).

This matters to determinism because *which backend runs* decides the entire RNG
stream, and right now that is decided by whether numpy happens to be installed.
Measured stream digests over 200 rounds of choice / choice-without-replacement /
shuffle at seed 20260806:

```
numpy   68ac1a69e720d7e3b4ee43c1391a98a5f8c4b6cc1d17cbff01f171bcaa92c231
custom  a2d622f1bb851b865a723ad7530034e38f75b52eb820f3651d7bc747912525da
```

Both are stable across processes. They do not agree, and they never will —
`mt19937.shuffle` performs `10 * len` random swaps rather than Fisher-Yates, and
`randint` truncates a float division.

**Recommendation.** Declare the dependency explicitly and pin the flag
explicitly, so the backend is a decision rather than an accident. Which backend
the C# port targets is `MARVEL-25`; this audit's input to that decision is that
both are reproducible, so the choice can be made on portability grounds alone.
Reproducing numpy's legacy `RandomState` bit-exactly in C# is substantially more
work than reproducing the 120-line `mt19937.py`.

## Checked and cleared

Recording these so nobody re-derives them.

**Threading does not touch game state.** `JobManager.Simultaneous`
(`engine/job/manager.py:74-77`) — the only job API called from gameplay
(`game/world/world.py:326,331,385`, `game/event/manager.py:327`) — is a plain
sequential loop despite the name. The genuinely parallel paths are
`ControllerManager.WaitConnect` / `WaitSync`
(`engine/controller/manager.py:113-128`), `WebDevice.IsInputReady`
(`engine/device/web/web_device.py:45`) and the websocket send loop
(`engine/device/web/server/server_socket.py:88`). All four are transport: they
wait for or push to clients. None mutates game state and none consumes the RNG.
`Controller.ChoiceOne` runs on the game thread.

**The digest is order-insensitive where it matters.** `CalculateCRC`
(`game/world/world_render.py:123-132`) iterates `object_manager.card_dict`,
which is a `dict` populated in `AddObject` order — insertion-ordered and
therefore reproducible. Per-card values come from `CardFace.crc_value`
(`game/card/face/card_face.py:182-183`), which *sums* a dict's values, so the
dict's own order is irrelevant.

**Sets that cannot escape.** Each of these is membership-test, `len`, or set
algebra only, and was read in full:

- `Card.Visible.visible_players` — `game/card/card.py:31`
- `retaliated_unit` — `game/effect/effect_invoke.py:44`
- `from_areas` / `into_areas` / `related_decks` —
  `game/message/sender/sender_card.py:857-859` (indexed only when length is 1,
  which is asserted; `IsDeckTypeRelated` returns on any match)
- `traits` in `CountTraitNum` — `game/operate/faces.py:497`
- `Ability.func_names` — `game/ability/ability.py:34`. Joined into a string at
  `game/ability/ability.py:1071`, but that is `Ability.GetName`, which reaches
  logs only. `Effect.GetDisplayName` (`game/effect/effect.py:425-478`) does not
  call it, so the replay wire format is unaffected.
- `get_titles` — `game/card/face/model/face_name.py:103-110`
- `powers_set` — `game/card/face/base/friend.py:14`
- `check_faces` — `game/world/cheat/cheat_cmd_helper.py:310-323`
- `undo_effect_card_cache` — `engine/controller/module/undo.py:31-75`. A set of
  ints (hash-stable), used only for membership filtering that preserves the
  caller's order.
- `Types.RemoveDuplicates` — `core/utility/types.py:32-35`. Set used as a seen
  marker; output order is the input's.
- `world.cheat.test_cards` — sorted before use at `game/scene/scene.py:84`.

**`engine/lib/sorted_set.py` is not evidence of a past gameplay bug.** Its only
user is `engine/profile/coverage.py:13`, tracking which card scripts ran. It
never touches game state. Reasonable guess: it was written to make a coverage
report stable, not to fix a replay.

**Card scripts are clean.** All 3,859 files under `cards/` were swept. Five
`set` constructions, all membership-only. No `datetime`, no `time.time`, no
stdlib `random`, no `uuid`, no `os.urandom`, no `id()`, no `hash()`, no
`os.listdir`, no `glob`, no threading, no async. All 23 `Rand.*` call sites take
ordered engine collections, never set-derived lists.

**Wall-clock time is otherwise confined.** `core/lib/time.py` is used by the
logger, the profiler, the input/sync timers (F1), session playtime, and scene
save metadata (F6). No other path.

**Card database loading is deterministic.** `cards/database.py` is driven
entirely by JSON document order over an explicit, config-supplied file list.
No directory enumeration, so no filesystem ordering.

**The seed round-trips.** `GameSession.GameSetup`
(`game/game_run/game_session.py:75-78`) draws a seed from unseeded stdlib
`random` only when the scene has none, and writes it back into the scene before
use. Loading a scene restores it.

## Required environment pinning

The harness applies all of these; see `tools/determinism/pinned_env.py`.

| Pin | Why |
|---|---|
| `PYTHONHASHSEED=0` | Sets of strings iterate in hash order, which is per-process by default. Measured: 8 distinct orderings across 8 processes unset, 1 when pinned. No digest was observed to move because of it — 30 unpinned runs reproduced — but this is defence in depth and it costs nothing. |
| `PYTHONIOENCODING=utf-8` | Not optional. The engine logs card names containing `U+26A0`; on a cp1252 console the logger raises and takes a different path. Observed during this audit. |
| `PYTHONDONTWRITEBYTECODE=1` | Keeps runs from differing in filesystem side effects. Hygiene, not correctness. |
| `timeout = 0` on every generated game | F1. A non-zero timeout can fabricate an input. |
| No web device during generation | F8. Keeps the aiohttp thread out of game state. |
| Record the resolved config with the corpus | F5. `statistics`, `pause_test_statistics`, `disable_numpy_random`, and the scene rules all shift id allocation. |

### What pinning `PYTHONHASHSEED` does not fix

Sets of engine objects are hashed by address, not by content. Pinning the hash
seed makes them *look* stable, because it removes one source of allocation
variance — but the stability is incidental. Varying the allocation history under
a pinned seed changes the ordering:

```
PYTHONHASHSEED=0, set of 10 objects
  perturb=0      o2,o9,o7,o3,o4,o0,o8,o1,o6,o5
  perturb=7      o2,o9,o7,o3,o4,o0,o8,o1,o6,o5
  perturb=64     o8,o1,o5,o6,o2,o9,o7,o4,o3,o0
  perturb=5000   o8,o1,o5,o6,o2,o9,o7,o4,o3,o0
```

So F2, F3 and F4 must be fixed at the source. There is no environment variable
that makes them safe.

## The harness

`tools/determinism/` — additive, imports nothing into engine code paths.

| File | What it does | Runnable today |
|---|---|---|
| `pinned_env.py` | The single definition of the pinned environment | n/a |
| `headless.py` | Boots the engine with a null device manager and returns a per-step digest trace. Answers every prompt with "decline" | yes |
| `check_runs.py` | Runs the same game N times in fresh processes and diffs the traces. The MARVEL-7 acceptance test | yes |
| `check_corpus.py` | Replays the corpus N times via `main.py -test` and checks every recorded digest | needs the corpus |
| `probe_hash_order.py` | Establishes which container orderings CPython reproduces | yes |
| `probe_rng.py` | Checks both RNG backends for cross-process stability and prints their golden digests | yes |

`headless.py` drives the engine through `DeviceManager.DoGetInput`.
`InputDevice.GetInput` is `@final`, but it delegates, so a `DeviceManager`
subclass is the supported seam — no engine code was modified. When the real bot
lands (`MARVEL-5`), replace the `decide` callback; everything else stands.

```bash
uv venv --python 3.13
uv pip install packaging Pillow requests aiohttp typing_extensions colorama numpy

.venv/Scripts/python.exe -m tools.determinism.probe_hash_order
.venv/Scripts/python.exe -m tools.determinism.probe_rng
.venv/Scripts/python.exe -m tools.determinism.check_runs --runs 100 --matrix smoke
.venv/Scripts/python.exe -m tools.determinism.check_runs --runs 20 --matrix wide
.venv/Scripts/python.exe -m tools.determinism.check_corpus --runs 100   # once a corpus exists
```

## Results

All on Windows 11, CPython 3.13.14, environment pinned unless stated.

```
100 run(s) per case, 2 case(s), environment pinned
PASS rhino / spider_man / seed 12345            (7 steps,   digest 97fa1611b360d813)
PASS klaw / captain_marvel+she_hulk / seed 999  (400 steps, digest 9fafd7bbe3691fea)
all cases reproduced byte-identically

20 run(s) per case, 7 case(s), environment pinned
PASS rhino / spider_man                              (7 steps)
PASS klaw / captain_marvel+she_hulk                  (400 steps)
PASS ultron / iron_man+thor+black_panther+cap        (405 steps)
PASS the_wrecking_crew / ms_marvel+hawkeye           (403 steps)
PASS crossbones / black_widow+doctor_strange+hulk    (404 steps)
PASS mutagen_formula / cyclops+jubilee               (400 steps)
PASS thanos / gamora+drax                            (403 steps)
all cases reproduced byte-identically

30 run(s) per case, 2 case(s), environment NOT pinned
both cases still reproduced byte-identically
```

400 engine processes in total, and a little over 100,000 compared per-step
digests. Not one differed.

## What still has to be run

Listed so the remaining work is explicit rather than implied.

1. **Replay the corpus.** `replays/` is empty and untracked. Once `MARVEL-5`
   produces a corpus, `check_corpus.py --runs 100` closes the "same seed plus
   same inputs" half of the acceptance criterion against the engine's own
   replay path rather than a proxy.
2. **Run on Linux.** No Linux host was available. Both halves of the acceptance
   criterion say "both OSes". The identity-hash findings (F2, F3, F4) are the
   ones most likely to differ, since allocator behaviour differs.
3. **Drive with the real bot.** The decline-only driver never plays a card, so
   it never reaches the ability-cost machinery, most response windows, or any
   card that requires a target. Re-run `check_runs.py` with the bot's `decide`
   once it exists.
4. **Exercise F2 and F3 deliberately.** None of the seven scenarios put two
   forced local abilities on one message, and none of the decks contain a TeamUp
   card. Build a puzzle (`game/puzzle/`) for each and confirm the digest is
   stable — or, better, fix them first and confirm the puzzle then passes under
   allocator perturbation.

## Candidate follow-up issues

| Proposed | Severity | Summary |
|---|---|---|
| Sort local effects before forced resolution | High | F2. One-line sort in `find_local_effects`; changes resolution order in previously arbitrary cases, so it needs a corpus regeneration and its own review. |
| Sort `GetTeamUpUnits` output | High | F3. `return sorted(faces)`. Removes set order from recorded target lists. |
| Force `timeout = 0` in the corpus generator | High | F1. Generator-side guard plus a refusal on non-zero timeout scenes. |
| Sort identity list in `register_change_form` | Medium | F4. `sorted(identities)` in both places. |
| Normalise `sign` / `time` / `playtime` when freezing the corpus | Medium | F6. Generator-side; also stops a machine fingerprint entering the repo. |
| Record the resolved config alongside the corpus | Medium | F5. Config drift changes id allocation; make it visible rather than silent. |
| Declare `numpy` and pin `disable_numpy_random` explicitly | Low | F10. Feeds `MARVEL-25`. Also finishes `MARVEL-2` by removing `PIL`. |
| Run the determinism harness on Linux in CI | Low | Closes acceptance item 2 above and keeps it closed. |
