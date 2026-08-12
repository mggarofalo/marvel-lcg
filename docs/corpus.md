# Corpus generation

How a working bot becomes a corpus. Implemented in `py_src/tools/corpus/`, see MARVEL-15.

```bash
cd py_src
python -m tools.corpus.generate --games 200 --out ./corpus/ --dry-run   # what it would play
python -m tools.corpus.generate --games 200 --out ./corpus/            # play it
python -m tools.corpus.generate --games 200 --out ./corpus/            # again: resumes
python main.py -verify_replays -verify_folders ./corpus/               # check the whole tree
```

## The problem is sampling, not running

The configuration space cannot be enumerated. 108 scenarios (52 of them expert), 63 starter
hero decks, 148 encounter sets, 25 challenges, one to four players. Scenarios against hero
*sets* alone is already 1.7 million combinations before any of the rest.

So the deliverable is a sampling strategy, and it lives in `plan.py`. A plan is a **pure
function of `(seed, sizes, inventory)`** — nothing in it starts an engine, reads a clock or
touches the network. That is what makes "regenerate the identical corpus" a checkable claim:
`--dry-run` prints the plan and its digest without playing anything, and two runs of the same
plan produce byte-identical scenes.

## Three phases

| phase | what it does | games per case |
|---|---|---|
| `scenario-coverage` | `floor` passes over every scenario, each a fresh shuffle | 1 |
| `hero-coverage` | finishes the hero floor when the sweep above was too small to seat everyone | 1 |
| `random` | uniform fill to the game budget | `--games-per-case` |

**The floor is a guarantee, not a target.** If covering every scenario and hero `floor` times
needs more games than `--games` asked for, the plan holds more and says so:

```
the plan holds 108 games rather than the 10 requested: covering every scenario and
hero 1 time(s) needs that many. Lower -floor or narrow the inventory.
```

A corpus that quietly dropped 98 scenarios to hit a game count is exactly the corpus this
tool exists to avoid, so the alternative was never silently truncating.

**Heroes are drawn least-used-first**, ties broken by the planner's RNG. A uniform draw over
63 heroes leaves the tail badly under-sampled at any realistic corpus size — the
coupon-collector problem — and the tail is where port bugs hide. Least-used-first means every
hero is played once before any is played twice. The tie-break matters: `sorted` is stable, so
shuffling before the sort is the only thing stopping `adam_warlock` appearing in every game.

**Player counts are cycled, not sampled.** Solo and four-player are different games rather
than the same game scaled — per-player icons, villain hit points and scheme thresholds all
move with the count — and four values sampled over a hundred draws is lumpy. Cycling makes
the distribution exact.

**Standard and expert are two scenarios, not one with a difficulty flag.** They are separate
encounter decks with different cards, so covering "every scenario" covers both. Every expert
file has a standard counterpart; four standard scenarios have no expert form
(`captain_america`, `captain_marvel`, `iron_man`, `spider_woman`).

## Why a case is a run of seeds

Engine start is about 0.5s against 0.2–2s for a game, so a process that plays one game spends
most of its life importing. A *case* is therefore one `(scenario, heroes)` pairing played over
consecutive seeds, and each case gets one process.

The coverage phases still use one game per case: their whole purpose is breadth, and batching
there would multiply the floor by the batch size. The random phase batches, because that is
where throughput matters and another seed on the same pairing is as good as any other game.

## Processes, not threads

The engine is not thread-safe — `Controller.ChoiceOne` blocks a thread inside `GetInput`, and
the job and task machinery exists around that — so every case is a fresh `main.py -bot`
process. Workers default to `cpu_count - 2`.

Each case writes into its own folder under the output directory, so a corpus is a tree:

```
corpus/
  00000-klaw-99/          [scene].json, bot-manifest-*.json, bot-coverage-*.json
  00001-rhino-100/        ...
  progress.jsonl
  corpus-manifest.json
```

`-verify_replays` walks the tree (`ReplayVerifier.ExpandTree`), so one command verifies a
whole corpus. A flat folder expands to itself, so pointing at `./replays/` is unchanged.

## Resuming

One line of JSON per finished case is appended to `progress.jsonl`, keyed by `Case.id` — built
from the scenario, heroes and seeds rather than from the plan index, so reordering a plan does
not invalidate a half-finished corpus. The file is line-buffered, so a killed run loses at most
the case in flight; a truncated last line is an expected state and costs exactly one case.

## What a case can do

- **finish** — its games were played and saved.
- **fail** — non-zero exit. The bot's own crash capture has already written the artefacts
  (`crashes/`, MARVEL-12); the run records it and carries on, because most of what self-play
  finds is a pre-existing engine bug and stopping the corpus on one is how a corpus never gets
  generated. `--fail-fast` stops instead.
- **time out** — killed at `--case-timeout` (default 900s). This is the wall-clock cap that
  `bot_max_steps` cannot give you: the step cap bounds a game's *decisions*, and a game that
  wedges without taking a decision would sit there forever.

Only a run that produced nothing at all exits non-zero. A corpus with holes is still a corpus,
and `corpus-manifest.json` says where the holes are.

## Throughput

Reported at the end of every run and recorded under `timing` in the manifest:

```
throughput 4588.7 games/hour on 4 worker(s)  (2743.0 per worker-hour)
```

Both numbers, because they answer different questions: wall-clock says how long a corpus of
size N will take on this machine, per-worker says what adding machines would buy. Measured on
small scenarios; four-hero expert games are several times slower, so plan against a run of the
shape you actually want.

`timing` is the **only** part of the manifest that is not reproducible, and it is fenced into
its own key for that reason. Nothing regenerating a corpus should read it.

## Reproducibility, and one known hole

Two independent generations of the same plan produce byte-identical scene files. That is the
acceptance criterion, and it holds — the RNG is a specified contract (`docs/rng-contract.md`)
and bot saves omit wall-clock and machine metadata (MARVEL-27).

**The per-run `bot-manifest-*.json` files are not byte-identical across output directories.**
Since MARVEL-34 they record the fully resolved config, which includes `bot_save_folder` — an
absolute path that differs between `./corpus/` and `./corpus2/`. The scenes are unaffected.
This matters to MARVEL-18, which has to decide what a corpus integrity manifest hashes; the
answer is probably "the scenes", but it should be an explicit decision rather than an accident.
