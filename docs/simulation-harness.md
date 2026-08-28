# Configurable whole-game simulation harness

MARVEL-266. `Marvel.Sim` is the permanent headless driver for the engine. It
runs named game setups under deterministic policies and writes records that can
be reviewed, aggregated and replayed.

The harness replaces a permanent Cartesian integration matrix. A researcher
chooses the scenario, heroes, modular sets, seeds and policy for each run. CI
keeps only a small semantic smoke set. Larger sweeps run when a question calls
for them.

This document specifies choices made by the engine project. The Rules Reference
does not define command-line flags, record formats, policy algorithms or seed
selection. Those choices are ours and are pinned here so one run can reproduce
another.

## Scope

The first complete harness supports these jobs:

- Run one scenario with one to four ordered heroes.
- Use the recommended modular sets, a chosen ordered list, or no modular sets.
- Run in standard or expert mode.
- Run explicit, consecutive or deterministically selected random seeds.
- Resolve each prompt with a named and versioned policy.
- Record setup, every decision, every event and enough state to replay.
- Replay a record and stop at the first divergence.
- Preserve a useful failure capsule when a game throws or exceeds its limit.
- Aggregate outcomes, lengths, actions, events and failures across games.

`Marvel.Sim` does not become a second rules engine. It does not alter state,
construct private commands or infer legal moves. It deals through
`Marvel.Content`, resolves through `Marvel.Rules.Play.Game`, and chooses only
from the current `Prompt`.

The harness is for automated research. It does not claim that a policy plays
well, that sampled seeds prove completeness, or that a win rate measures game
balance.

## Project boundary

The executable lives at `src/Marvel.Sim`. It targets the repository-wide
framework and follows the dependency wall in
[presentation-layer.md](presentation-layer.md#dependency-rules).

It may reference `Marvel.Core`, `Marvel.Rules`, `Marvel.Cards` and
`Marvel.Content`. Those projects must never reference it. Policies and record
writers belong in `Marvel.Sim`; reusable game rules do not.

The command uses the checked-in files under `datasets/`. It performs no network
requests. It never changes a dataset.

## Commands

The executable has three commands:

```text
Marvel.Sim run     Run one configured set of games.
Marvel.Sim replay  Re-run one recorded game and verify every step.
Marvel.Sim report  Rebuild an aggregate report from completed game records.
```

A representative run is:

```bash
dotnet run --project src/Marvel.Sim -- run \
  --scenario rhino \
  --difficulty expert \
  --hero spider_man \
  --hero she_hulk \
  --modular legions_of_hydra \
  --games 1000 \
  --seed-mode random \
  --selection-seed 266 \
  --policy acting@1 \
  --policy-seed 9001 \
  --policy-option decline_one_in=4 \
  --decision-limit 600 \
  --record compact \
  --output artifacts/sim/rhino-expert-two-player
```

Repeated `--hero` and `--modular` flags preserve their order. Hero order is
seat order. Modular-set order is the order passed to `Dealer.DealOrder`.

The same run can be stored as JSON configuration:

```json
{
  "schema": "marvel.sim.config/v1",
  "scenario": "rhino",
  "difficulty": "expert",
  "heroes": ["spider_man", "she_hulk"],
  "modular_sets": ["legions_of_hydra"],
  "games": 1000,
  "seeds": {"mode": "random", "selection_seed": 266},
  "policy": {
    "name": "acting",
    "version": 1,
    "seed": 9001,
    "parameters": {"decline_one_in": 4}
  },
  "decision_limit": 600,
  "record": "compact",
  "compression": "gzip"
}
```

`--config FILE` reads this shape. Gameplay flags cannot accompany `--config`.
Operational flags may accompany it: `--output`, `--jobs` and `--shard`. This
separation keeps the recorded game plan independent of the machine running it.

The run command requires `--output DIR`. The explicit path is the caller's
permission to write records there. The harness never chooses a repository path
and never writes beside a dataset or test. See [Output behavior](#output-behavior).

## Configuration rules

The command validates the full plan before dealing any game. A bad name or an
ambiguous combination is a configuration error, not a failed simulation.

The following rules apply:

- `scenario` is a standard campaign key from `SetupCatalog`, without the
  `_expert` suffix.
- `difficulty` is `standard` or `expert` and is required.
- Standard resolves the named campaign. Expert resolves `<scenario>_expert`.
- The resolved campaign must exist and its `Expert` flag must match the request.
- `heroes` contains one to four distinct `SetupCatalog` hero keys.
- Each modular set must exist, and the list cannot contain a duplicate.
- Omitting modular sets uses the campaign's recommended list.
- `--no-modulars` selects an empty list and cannot accompany `--modular`.
- `games` is positive and agrees with the selected seed mode.
- `decision_limit` is positive. It defaults to 600.
- `record` is `compact` or `full`. It defaults to `compact`.
- `compression` is `gzip` or `none`. It defaults to `gzip`.
- Every policy parameter must be declared by that policy version.

The scenario and difficulty spelling is an engine-project choice. The dataset
stores standard and expert setups as separate campaign records. The command
keeps that storage detail out of research configurations while still recording
the resolved campaign key.

Omitted and empty modular lists remain different. This follows the existing
`Dealer.EncounterSetNames` contract: `null` means the printed recommendation,
while an empty list means no modular set.

## Seed plans

One game seed names one engine random stream. `WorldSetup.Deal` seeds that
stream once, and setup and gameplay consume the same stream. The harness never
reseeds a world.

A run selects game seeds in exactly one of three ways:

| Mode | CLI | Result |
|---|---|---|
| Explicit | repeat `--seed N` | The seeds in argument order |
| Consecutive | `--seed-start N --games G` | `N` through `N + G - 1` |
| Random | `--seed-mode random --selection-seed N --games G` | The next `G` raw MT19937 words |

Explicit mode infers `games` from the number of `--seed` values. If `--games`
is also present, the numbers must agree. Consecutive mode rejects overflow
rather than wrapping from `uint.MaxValue` to zero.

Random selection uses a separate `MersenneTwister` seeded with
`selection_seed`. Each call to `NextUInt32` produces one game seed. Duplicate
values remain in the plan. Removing one would change stream consumption and
make the simple algorithm harder to reproduce.

The selection generator never enters a `World`. It chooses game inputs and
cannot affect a game's random stream. The algorithm follows
[rng-contract.md](rng-contract.md), including a legal game seed of zero.

No seed mode reads wall-clock time, process state, `System.Random` or operating
system entropy. A request for random seeds without `selection_seed` is a
configuration error.

## Policy streams

Policy randomness is separate from game randomness. A policy cannot consume
`World.Random`, even when it can inspect the full world. This keeps a policy
change from moving the encounter deck's random stream.

Every run names a policy as `<name>@<version>` and supplies one 32-bit master
seed. The run planner creates a separate MT19937 stream from that master seed.
It draws one raw word for each `(game index, seat)` in game-major, seat-major
order. That word seeds the policy instance for that seat.

The planner creates all game and policy seeds before applying a shard. A shard
therefore uses the same seeds as the corresponding games in an unsharded run.
Every game header records the final game seed and every seat policy seed, so a
replay does not need to derive them again.

Each policy declares one visibility level:

| Visibility | Policy input |
|---|---|
| `prompt_only` | The current prompt and public run metadata |
| `full_state` | The prompt and the complete `World` |

Visibility is recorded because a full-state research fuzzer can use hidden
information that a player cannot. Reports must not silently combine policies
with different names, versions, parameters or visibility levels.

`acting@1` is the first baseline policy. It is a deterministic legal-action
fuzzer, not a strategy bot. It uses only its prompt, sometimes declines a
cancellable prompt, selects one legal affordance, selects legal targets and
pays only with offered resource generators. Its `decline_one_in` parameter is a
positive integer and defaults to 4.

A policy version is immutable after records use it. A change that can choose a
different answer from the same prompt and stream creates a new version. A
refactor that preserves every answer may keep the version, with a test that
pins the claim.

## Setup and execution

Each game follows one production path:

1. Read `datasets/setup/setup.json` into `SetupCatalog`.
2. Read `datasets/cards/cards.json` into `CardCatalog`.
3. Load `datasets/abilities/abilities.json` into one card ability runner.
4. Resolve the standard or expert campaign and the modular-set choice.
5. Call `Dealer.DealOrder` with the ordered heroes and modular sets.
6. Call `Blueprints.From` without changing that order.
7. Call `WorldSetup.Deal` with the game seed, hero display names, expert flag
   and the ability runner.
8. Pass the same ability runner to `Game.Begin`.
9. While `Game.Pending` exists, ask the policy for `Prompt.Player` and call
   `Game.Resolve` once.
10. Stop when `Game.Pending` is absent, an exception escapes, or the decision
    limit is reached.

The same runner must serve setup and gameplay. A setup card resolved by one
interpreter and a revealed card resolved by another is not one game.

Setup events go into the header. `Game.Begin` produces the initial prompt but
no events. Each later event list comes directly from the corresponding
`Resolution`. The harness does not derive a parallel event stream from digest
differences.

The run is single-threaded within a game. `--jobs N` may run independent worlds
at the same time, but no thread or asynchronous task may touch one world from
another. Output ordering remains game-index order, regardless of completion
order.

`--shard I/N` runs game indices whose `index % N == I`. `I` starts at zero.
The manifest records the full plan and the selected shard.

## Multiplayer and implied actions

An ordered hero list creates one policy instance per seat. The harness dispatches
each prompt to `Prompt.Player`. It never assumes that `Game.Active`, the card's
owner or `Affordance.AnchorPlayer` is the player answering.

This distinction covers obligations such as “give this card to the
<character> player.” The setup order identifies every seat, while the engine
decides who receives and answers for the card. The record preserves the prompt
player, anchor player, events and state digest. A review can therefore see both
the chosen seat and the resulting card placement.

Cross-player Actions use implied requests. During another player's turn, an
eligible Action is directly available to the player who may perform it. The
engine does not create a synthetic `Ask`, `Request` or `Accept` decision. The
permission that another player could ask is checked when the engine builds the
affordance.

The harness adds no handshake of its own. It treats that direct affordance like
any other action and dispatches the prompt by `Prompt.Player`. The record keeps
`AnchorPlayer`, which may differ from the active player. This makes implied
cross-player use visible without inventing an interaction the engine does not
need.

An implied request changes neither ownership nor payment. Targets and resource
generators must still come from the selected affordance. A policy cannot spend
cards from another seat unless the engine offered those generators.

## Durable decisions

`Affordance.Id` is a session handle. A record must never persist it. The stable
selector is this pair:

```text
(anchor_id, verb)
```

This is the persistence rule already settled in
[affordances.md](affordances.md#id-is-a-handle-not-a-name). A selected decision
stores that pair, the ordered targets and the resource generator card ids.

A decline stores no selector:

```json
{"kind":"decline"}
```

A taken affordance stores:

```json
{
  "kind": "take",
  "selector": {"anchor_id": 9, "verb": "Play"},
  "targets": [49],
  "resources": [12, 17]
}
```

Target order is significant and remains unchanged. Resource order has no rules
meaning, so the writer sorts distinct resource ids in ascending order for one
canonical spelling.

Before taking an affordance, the runner requires exactly one legal option with
the selected pair. Zero matches are `selector_not_found`. More than one match
is `selector_ambiguous`. Both fail the game before `Game.Resolve` runs. The
label and list position are diagnostic fields, never fallback identity.

Recorded prompts also omit every `Affordance.Id`. Their affordances keep verb,
anchor, anchor player, label, legality, target request and cost options. A
`ResourceSource.Effect` is currently a card object id and remains in the record
as part of the payment menu.

## Game record stream

Each game is one UTF-8 JSON Lines stream. Gzip compression changes only the
container. Decompressing a `.jsonl.gz` file produces the same bytes as a
`.jsonl` file from the same run.

Every JSON value is one physical line with no insignificant whitespace. Object
keys use the order shown by the source-generated writer. Arrays retain domain
order. The stream contains one header, zero or more steps, and one result or
failure record.

The schema id is `marvel.sim.game/v1`. A reader rejects an unknown major
version. New optional fields may be added within version 1 only when an older
reader can ignore them without changing a decision or verdict.

### Header record

The header identifies the executable contract, setup and random inputs. After
successful setup it also contains the full initial digest because every replay
begins there. If setup throws, `initial` is null and the following failure
record names the setup error.

The example below is formatted for reading. A file stores it on one line and
contains the complete digest and events.

```json
{
  "schema": "marvel.sim.game/v1",
  "type": "header",
  "game_index": 0,
  "engine": {
    "commit": "5336236a93606b6e3b26c36d4b266cdea94eb2bf",
    "digest_version": 2
  },
  "setup": {
    "scenario": "rhino",
    "campaign": "rhino_expert",
    "difficulty": "expert",
    "heroes": ["spider_man", "she_hulk"],
    "seat_names": ["Spider-Man", "She-Hulk"],
    "modular_sets": ["legions_of_hydra"],
    "used_recommended_modulars": false
  },
  "game_seed": 1608637542,
  "policy": {
    "name": "acting",
    "version": 1,
    "visibility": "prompt_only",
    "master_seed": 9001,
    "seat_seeds": [123, 456],
    "parameters": {"decline_one_in": 4}
  },
  "limits": {"decisions": 600},
  "record": "compact",
  "initial": {
    "digest": "{\"v\":2,\"cards\":[]}",
    "fingerprint": "b06ec3cd3c0b6b4ece7b78f2bd99f14f8f8307339a0fd161f93e24d8abd2a31a",
    "setup_events": []
  }
}
```

The digest and fingerprint values above only show field shape. A real header
contains `World.Digest().Canonical()` and its actual `Fingerprint()`.

The commit identifies the built source when available. A packaged build may
use its assembly informational version instead. The field is provenance, not a
replay input. Replay reports a difference but does not reject solely because
the build identity changed.

### Step record

One step records the prompt before the decision and state after resolution:

```json
{
  "schema": "marvel.sim.game/v1",
  "type": "step",
  "index": 0,
  "prompt": {
    "player": 0,
    "asking": "TurnOption",
    "when": "Untimed",
    "trigger": "WhenPlayerInTurn",
    "label": "Spider-Man takes a turn",
    "cancellable": true,
    "affordances": [
      {
        "verb": "Play",
        "anchor_id": 9,
        "anchor_player": 0,
        "label": "Play Swinging Web Kick",
        "targets": {"legal": [49], "min": 1, "max": 1},
        "costs": [],
        "illegal": null
      }
    ]
  },
  "decision": {
    "kind": "take",
    "selector": {"anchor_id": 9, "verb": "Play"},
    "targets": [49],
    "resources": [12, 17]
  },
  "events": [],
  "after": {
    "fingerprint": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
  }
}
```

The prompt is the stable wire projection defined in
[affordances.md](affordances.md). It keeps the offered order for research and
for divergence reports. Its affordances omit session ids.

Compact records store a full digest in the header and terminal record, with a
SHA-256 fingerprint after each step. Full records also store the canonical
digest in every step's `after` object. The digest remains a string so byte
equality stays observable.

Events use the production serialization from
[event-stream.md](event-stream.md#the-vocabulary). They remain in execution
order. An empty event array is meaningful and is always present.

### Terminal record

A game that reaches an engine outcome ends with:

```json
{
  "schema": "marvel.sim.game/v1",
  "type": "result",
  "status": "finished",
  "outcome": "PlayersWin",
  "rounds": 8,
  "decisions": 143,
  "metrics": {
    "taken": 91,
    "declined": 52,
    "verbs": {"Action": 11, "Attack": 18, "Play": 22},
    "events": {"CardsMoved": 74, "FieldSet": 81},
    "distinct_anchor_cards": 27
  },
  "terminal": {
    "digest": "{\"v\":2,\"cards\":[]}",
    "fingerprint": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
  }
}
```

`outcome` uses the engine enum spelling: `PlayersWin`, `VillainWins` or
`PlayersLose`. `Unfinished` is never a finished result.

Metrics are observations derived from records. They never affect policy
choices or replay. `verbs` counts taken decisions by verb. `events` counts
event records by kind. `distinct_anchor_cards` counts distinct card object ids
selected during the game.

## Replay and divergence

Replay starts from the header rather than loading its digest as mutable state.
It reads the current datasets, deals the game again, and resolves every recorded
decision from the beginning. This follows the engine's snapshot-plus-input
model and checks setup as well as gameplay.

For each game, replay performs these checks in order:

1. Resolve and validate the recorded setup names and mode.
2. Deal with the recorded game seed and seat order.
3. Compare the complete initial digest and setup events.
4. Compare the stable current prompt with the recorded prompt.
5. Resolve a taken decision by exactly one `(anchor_id, verb)` pair.
6. Validate targets with `TargetRequest.Allows`.
7. Validate each resource against the selected cost's offered generators.
8. Call `Game.Resolve` once.
9. Compare the event list in order.
10. Compare the state fingerprint and any recorded full digest.
11. Compare the terminal status, outcome, round and full digest.

Replay stops at the first mismatch. The report names the game and step, then
shows the narrowest useful difference. A state mismatch uses the structural
digest diff rather than only printing two hashes.

Replay never falls back to `Affordance.Id`, label, list index or a best match.
A missing or ambiguous stable selector is itself the divergence.

The command is:

```bash
dotnet run --project src/Marvel.Sim -- replay \
  --input artifacts/sim/rhino-expert-two-player/games/000000-1608637542.jsonl.gz
```

It writes diagnostics to standard error and changes no file unless an explicit
diagnostic output path is supplied.

## Failure capsules

A run failure still produces a complete stream unless record output itself has
failed. Its last record has `type: "failure"` and one of these categories:

| Category | Meaning |
|---|---|
| `decision_limit` | The game still had a prompt at the configured limit |
| `policy_error` | The policy returned an illegal or ambiguous decision |
| `rules_not_implemented` | The engine reached a named unimplemented rule |
| `engine_exception` | Another exception escaped setup or resolve |
| `record_error` | A record could not be serialized or committed |

The failure record contains:

- The game and step index.
- The exact replay command.
- The exception type and message, when present.
- The last known good full digest and fingerprint, or null when setup failed.
- The digest immediately after the failure, when the world still exists.
- The open prompt and attempted durable decision.
- The most recent 20 step records and their events.
- The setup, game seed, policy version, parameters and seat policy seeds.

The last known good digest is captured after setup and before each
`Game.Resolve`. The post-failure digest matters because the engine mutates in
place and an exception may expose partial mutation. The two values tell a
reviewer whether failure was fail-closed.

The capsule omits a stack trace from its portable JSON. Paths, generated method
names and line numbers vary by build and operating system. The type, message,
inputs and state are enough to reproduce under a debugger.

A decision-limit failure records the still-open prompt and no attempted
decision. It is a research result, not an engine outcome. Reports keep it apart
from all three game outcomes.

## Aggregate reports

The run command writes `report.json` and a concise `report.md` after all selected
games finish. The report command rebuilds both from game streams and produces
the same aggregate values.

Each report contains:

- The fully resolved configuration and shard.
- Requested, selected, finished and failed game counts.
- Counts for `PlayersWin`, `VillainWins` and `PlayersLose`.
- Counts by failure category.
- Decision and round minimum, median, 95th percentile and maximum.
- Taken and declined decision totals.
- Taken decisions by verb and prompt question.
- Events by event kind.
- Distinct printed card faces used as selected anchors.
- Every failed seed with its replay command.

The median and 95th percentile use nearest-rank over values sorted ascending.
The report states the policy name, version, parameters and visibility beside
every group. It does not combine incompatible policy groups into one rate.

`report.json` is the machine-readable authority. `report.md` is a rendering of
the same values. Neither includes a timestamp, elapsed wall time or throughput
inside its deterministic result section. A runner may print timing to standard
error, but timing never enters a game record or comparison.

## Output behavior

The output directory must not exist. An existing path is a configuration error.
This avoids an implicit overwrite and prevents two runs from becoming one
dataset by accident.

One run creates this layout:

```text
<output>/
  manifest.json
  report.json
  report.md
  games/
    000000-1608637542.jsonl.gz
    000001-3421126067.jsonl.gz
  failures/
    000019-787846414.json
```

The six-digit prefix is the index in the full run plan, not completion order or
shard-local order. A failure has both its game stream and a convenience copy of
the failure capsule under `failures/`.

Each game writes to a temporary file inside the requested output directory and
renames it only after the terminal record is flushed. The manifest lists game
files in game-index order with their SHA-256 hashes and statuses. It is written
last. A missing manifest therefore means the run did not finish committing its
artifacts.

Gzip output fixes the header modification time to zero, stores no source file
name and uses one pinned compression level. Map keys that do not have a schema
order are sorted by ordinal string order. These choices make successful game
streams byte-identical for the same configuration and binaries. Exception
messages remain diagnostics and are not a cross-build byte contract.

The harness creates no file when validation fails. It never writes to
`datasets/`, `src/`, `tests/` or `docs/` unless the caller explicitly names one
of those paths as the output directory. Tests use temporary directories and
leave `git status` unchanged.

## Exit codes

The commands use these exit codes:

| Code | Meaning |
|---|---|
| 0 | Every selected run finished, or every replay matched |
| 1 | At least one run failed or stopped at its decision limit |
| 2 | Configuration, input schema or output-path error |
| 3 | Replay divergence |

A batch continues after an individual game fails, unless record output itself
is unavailable. This preserves the other seeds and allows one report to show a
failure cluster. `record_error` stops the batch because later results could not
be trusted to persist.

## CI boundary

CI does not run the hero-by-scenario-by-player-count product. It checks the
harness contract with small, named tests:

- Configuration validation and the three seed-plan vectors.
- Policy seed derivation, including shard equivalence.
- JSONL and gzip round trips.
- One solo game that generates and replays a compact record.
- One two-player game that generates and replays a full record.
- A two-player obligation case that places the card with the matching identity
  player.
- An implied cross-player Action with no request or acceptance decision.
- Stable-selector failure for zero and multiple matches.
- A forced failure that proves the last-good and post-failure digests are kept.
- Report rebuilding from one success and one failure.
- A test that starts and ends with a clean repository status.

These are semantic examples, not samples of the full product. Large sweeps run
manually or in a separate scheduled job and publish ordinary artifacts. Their
records are not vendored under `datasets/` and are not a build dependency.

## Staged delivery

Implementation should land in four reviewable stages:

1. Add `Marvel.Sim`, configuration validation, seed planning and policy
   version contracts. Pin them with small unit vectors.
2. Run one game, write compact and full records, and replay stable decisions.
3. Add multiplayer dispatch, implied Actions, obligation coverage and failure
   capsules.
4. Add batching, deterministic jobs and shards, compression and aggregate
   reports.

Each stage must keep the solution green on Windows and Linux. A stage that
cannot replay what it writes is incomplete.

## Acceptance

MARVEL-266 is complete when the following statements are executable tests:

- One command can name any supported scenario, one to four ordered heroes,
  modular sets, difficulty, a game count and deterministic seeds.
- The same configuration and binaries produce the same ordered records.
- Game, seed-selection and policy random streams cannot consume one another.
- Every record carries replayable setup, a stable affordance selector, ordered
  targets, payment generators, events and state checks.
- Replay detects setup, prompt, event, digest and outcome divergence at the
  first differing step.
- A failed game preserves enough state and input to reproduce and inspect it.
- Multiplayer records show the real answering seat and card placement.
- Cross-player Actions require no synthetic request or acceptance decision.
- CI proves the harness with bounded semantic games and never runs the full
  Cartesian matrix.
- Running the test suite leaves the repository unchanged.
