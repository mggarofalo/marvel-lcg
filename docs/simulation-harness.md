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
  --decision-limit 600 \
  --output artifacts/sim/rhino-expert-two-player.jsonl
```

Repeated `--hero` and `--modular` flags preserve their order. Hero order is
seat order. Modular-set order is the order passed to `Dealer.DealOrder`.

When `--output` is omitted, JSONL goes to standard output and the human summary
goes to standard error. Naming an output file is explicit permission to write
that file. See [Output behavior](#output-behavior).

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
- `--policy`, when present, is `acting` or `acting@1`.

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

Every run supplies one 32-bit policy seed. Game `i` uses that value plus `i` as
its game-local policy master seed. A separate MT19937 stream draws one raw word
for each seat in seat order. Those words seed the seat policies and are written
to the start record. A one-game reproduction command can therefore use the
recorded game-local master seed directly. None of these streams enters a
`World`.

Each policy declares one visibility level:

| Visibility | Policy input |
|---|---|
| `prompt_only` | The current prompt and public run metadata |
| `full_state` | The prompt and the complete `World` |

Visibility is recorded because a full-state research fuzzer can use hidden
information that a player cannot. Reports must not silently combine policies
with different names, versions, parameters or visibility levels.

`acting@1` is the first baseline policy. It is a deterministic legal-action
fuzzer, not a strategy bot. It plays payable cards and Actions, changes to hero
form, prefers attacks or urgent thwarting, selects legal targets and pays only
with offered resource generators. Random choices use the answering seat's
policy stream.

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

Setup events go into each game's start record. `Game.Begin` produces the initial prompt but
no events. Each later event list comes directly from the corresponding
`Resolution`. The harness does not derive a parallel event stream from digest
differences.

The initial implementation is sequential. Independent-game parallelism may be
added later, but no thread or asynchronous task may ever touch a world owned by
another game. Record order remains game-index order.

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
selector is:

```text
(anchor_id, anchor_player, verb, label, occurrence)
```

`anchor_player` distinguishes which player actually takes an implied shared
action; `label` distinguishes several actions on one card. `occurrence` is the
zero-based position among exact four-field matches because repeated choice
nodes can otherwise be identical. This authored prompt order is part of the
engine wire format; the rules do not define persistent command identifiers. A
selected decision also stores ordered targets and resource generator ids.

A decline stores no selector:

```json
{"kind":"decline"}
```

A taken affordance stores:

```json
{
  "kind": "take",
  "selector": {
    "anchor_id": 9,
    "anchor_player": 0,
    "verb": "Play",
    "label": "Play Swinging Web Kick",
    "occurrence": 0
  },
  "targets": [49],
  "resources": [12, 17]
}
```

Target order is significant and remains unchanged. Resource order has no
tabletop meaning, but it determines the order of generated events, so the
record retains the policy's order to make exact replay possible.

Before taking an affordance, the runner requires exactly one legal option with
the selected selector occurrence. A missing occurrence fails before
`Game.Resolve` runs. The session handle is never a fallback identity.

Recorded prompts also omit every `Affordance.Id`. Their affordances keep verb,
anchor, anchor player, label, legality, target request and cost options. A
`ResourceSource.Effect` is currently a card object id and remains in the record
as part of the payment menu.

## Game record stream

One run is one UTF-8 JSON Lines stream. Every JSON value is one physical line.
Arrays retain domain order. The stream starts with one run header, then contains
one `start`, zero or more `step` records, and one `result` or `failure` record
per game. One final `summary` record makes the aggregate machine-readable.

The numeric `schema` is `1`. Replay rejects any other value.

### Header record

The run header records the configuration shared by every game:

```json
{
  "type": "header",
  "schema": 1,
  "scenario": "rhino",
  "difficulty": "expert",
  "heroes": ["spider_man", "she_hulk"],
  "modular_sets": ["legions_of_hydra"],
  "policy": "acting",
  "policy_version": 1,
  "policy_visibility": "full_state",
  "policy_seed": 9001,
  "decision_limit": 600,
  "seed_mode": "random",
  "selection_seed": 266,
  "seeds": [342154546, 3107468503]
}
```

`modular_sets` is null when printed recommendations were requested and an empty
array when no modular set was requested.

Each successfully dealt game follows with a start record containing its game
index, game seed, game-local policy master seed, ordered seat policy seeds,
complete setup events, and full canonical initial digest. Setup uses the same
fresh `AbilityRunner` that gameplay receives.

### Step record

One step records the prompt before the decision and state after resolution:

```json
{
  "type": "step",
  "game": 0,
  "step": 0,
  "prompt": {
    "player": 0,
    "asking": "TurnOption",
    "when": "Untimed",
    "trigger": "WhenPlayerInTurn",
    "label": "Spider-Man takes a turn",
    "cancellable": true
  },
  "decision": {
    "decline": false,
    "anchor_id": 9,
    "anchor_player": 0,
    "verb": "Play",
    "label": "Play Swinging Web Kick",
    "occurrence": 0
  },
  "targets": [49],
  "resources": [12, 17],
  "events": [],
  "digest": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
}
```

The record never persists `Affordance.Id`. See [Durable decisions](#durable-decisions).
It stores a SHA-256 fingerprint after each step, while start and terminal
records store full canonical digests.

Events use the production serialization from
[event-stream.md](event-stream.md#the-vocabulary). They remain in execution
order. An empty event array is meaningful and is always present.

### Terminal record

A game that reaches an engine outcome ends with a result record:

```json
{
  "type": "result",
  "game": 0,
  "seed": 1608637542,
  "outcome": "PlayersWin",
  "round": 8,
  "decisions": 143,
  "metrics": {
    "cards_played": 22,
    "player_attacks": 18,
    "payments": 20,
    "resource_abilities_used": 3
  },
  "terminal_digest": "{\"v\":2,\"cards\":[]}"
}
```

`outcome` uses the engine enum spelling: `PlayersWin`, `VillainWins` or
`PlayersLose`. `Unfinished` is never a finished result.

Metrics are observations and never affect policy choices or replay.

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
5. Resolve a taken decision by exactly one
   `(anchor_id, anchor_player, verb, label, occurrence)` selector.
6. Validate targets with `TargetRequest.Allows`.
7. Validate each resource against the selected cost's offered generators.
8. Call `Game.Resolve` once.
9. Compare the event list in order.
10. Compare the state fingerprint and any recorded full digest.
11. Compare the terminal status, outcome, round and full digest.

Replay stops at the first mismatch. The report names the game and step, then
shows the narrowest useful difference. A state mismatch uses the structural
digest diff rather than only printing two hashes.

Replay never falls back to `Affordance.Id` or a best match. A missing recorded
occurrence is itself the divergence.

The command is:

```bash
dotnet run --project src/Marvel.Sim -- replay artifacts/sim/rhino-expert-two-player.jsonl
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

The run appends one `summary` JSON value after all selected games finish and
prints the same values as one concise human sentence. `Marvel.Sim report`
renders that summary again from a saved stream.

The summary contains:

- Selected and failed game counts.
- Counts for `PlayersWin`, `VillainWins` and `PlayersLose`.
- Total decisions and rounds.
- Total cards played, player attacks, paid costs and resource abilities used.
- Failure signatures grouped by exception type and message.

Neither form includes a timestamp, elapsed wall time or throughput. The JSONL
summary is the machine-readable authority.

## Output behavior

With no `--output`, JSONL is written to standard output. With `--output FILE`,
the command creates or replaces that explicit file and prints the human summary
to standard output. Its parent directory is created when needed. The user chose
the path, so the write is authorized and unsurprising.

The harness creates no file when validation fails. It never writes to
`datasets/`, `src/`, `tests/` or `docs/` unless the caller explicitly names one
of those paths as the output file. Tests use temporary directories and
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
- Per-seat policy seed derivation.
- JSONL record and report round trips.
- One solo game that generates and replays a record.
- One two-player game that generates and replays a record.
- A two-player obligation case that places the card with the matching identity
  player.
- An implied cross-player Action with no request or acceptance decision.
- Stable-selector failure for zero and multiple matches.
- A forced failure that proves the last-good and post-failure digests are kept.
- Machine and human summary rebuilding.

These are semantic examples, not samples of the full product. Large sweeps run
manually or in a separate scheduled job and publish ordinary artifacts. Their
records are not vendored under `datasets/` and are not a build dependency.

## Staged delivery

The first implementation lands in three reviewable stages:

1. Add `Marvel.Sim`, configuration validation, seed planning and policy
   version contracts. Pin them with small unit vectors.
2. Run games, write records, and replay stable decisions, events and digests.
3. Add multiplayer dispatch, implied Actions, obligation coverage and failure
   capsules plus aggregate summaries.

Deterministic sharding and gzip are compatible future extensions. They do not
belong in the initial contract and are not required to run large sequential
research batches.

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
