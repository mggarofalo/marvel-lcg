# Card coverage

**This is the metric that decides whether the corpus is worth anything.**

The replay corpus is the behavioral oracle the C# port is validated against. A
corpus that never exercises forty percent of the cards cannot validate forty
percent of the port, and without measurement that is discovered at the end. Card
coverage is the measurement, and it is produced automatically at the end of every
corpus run.

Written for MARVEL-13.

## What "exercised" means

Three levels, because "the card was in the deck" and "the card did something" are
different claims and only the second validates anything:

| Level | Claim | Recorded by |
|---|---|---|
| **present** | the card existed in the game at all | walking `world.object_manager.card_dict` at game end |
| **entered play** | it reached an in-play zone | `ModelOnEvent.ApplyAfterEnterPlay`, past the guard that confirms it arrived |
| **resolved** | one of its abilities triggered **and resolved** | `EffectInvoker.InvokeOperation`, after the operation returns |

A card sitting in a deck all game is *present* and nothing else. A boost card
flipped and discarded is present, and if its ability never fired it is not
resolved. The report keeps the three apart on purpose.

## Why ability-level, not card-level

The engine builds abilities through `AbilityFactory` — 346 static methods, of
which **303 are actually named by a card script**. The distribution is
long-tailed: a few dozen carry most of the uses and the tail runs to three
hundred. **The tail is where port bugs will hide, so the tail is what has to be
measured.** Counting cards alone would report a scenario as well covered while
every unusual trigger in it went unfired.

So the primary number is *which `AbilityFactory` methods fired*, and the primary
output is the ranked list of the ones that did not.

### How an ability knows what built it

`CardCoverage.Instrument()` replaces every public static method on
`AbilityFactory` with a wrapper that stamps the returned `Ability` with the
method's name (`Ability.factory`). A factory that calls another factory
overwrites the inner stamp, so an ability ends up carrying the **outermost**
method — the one the card script actually named.

That matters because the denominator is static: `tools/cards/scripts.py` reads
`AbilityFactory.<name>` off each card script's syntax tree into
`datasets/cards/cards.json`. Runtime and dataset therefore use the same
namespace, and one can be subtracted from the other.
`unit_test/test_card_coverage_play.py` asserts that nothing fires at runtime
which the dataset has never heard of — so a renamed factory method fails a test
instead of quietly shrinking the denominator.

**Instrumentation has to be installed before the first card is built.**
`CardsDB.ability_cache` builds a card's abilities once per process and hands the
same objects to every later copy; instrument after that and those cards carry no
attribution for the rest of the run. `BotRunner.Run` installs it before its game
loop.

## Running it

Coverage is **on by default for the bot** and off everywhere else. Every run
writes `bot-coverage-<scenario>-<heroes>-<seed>-<games>.json` beside its scenes,
next to the manifest.

```bash
cd py_src
python main.py -bot -bot_games 50 -bot_seed 1000     # writes the report automatically
python main.py -bot -no_bot_coverage                 # off
```

One run answers "what did these games reach". The corpus question is the union
across every run, which is what the merge tool is for:

```bash
python -m tools.coverage.report replays/                      # merge every artefact in a folder
python -m tools.coverage.report replays/bot-coverage-*.json   # or name them
python -m tools.coverage.report replays/ --out coverage.json --top 40
```

Ranking is recomputed from the merged observations, never combined from the
per-run rankings: a trigger unreached in every run individually may still be
reached by the corpus.

## The report

```jsonc
{
  "coverage_version": 1,
  "generator": "bot",
  "engine_version": "0.5.9.205",
  "universe":  { "available": true, "source": "...", "cards": 3781, "factories": 303 },
  "games":     [ /* one record per game: seed, scenario, heroes, player_count,
                    expert, challenges, rules, modes, outcome, cards
                    (present / entered_play / resolved), stages, factories,
                    triggers, ability_types */ ],
  "totals":    { "games": 50,
                 "cards": { "present": 61, "entered_play": 18, "resolved": 44,
                            "resolved_in_universe": 40, "universe": 3781 },
                 "factories": { "fired": 36, "universe": 303 } },
  "counts":    { /* the same, summed per card id / factory / trigger / ability type */ },
  "reached":   { "scenarios": {}, "heroes": {}, "player_counts": {},
                 "challenges": {}, "difficulty": {}, "stages": {} },
  "never_fired_factories":  [ { "factory": "WhenCardBecomeBoost", "cards": 354 } ],
  "never_exercised_cards":  [ { "card_id": "21139a", "name": "Odin", "pack": "hlk",
                                "score": 5, "unfired": [ /* ... */ ] } ]
}
```

### The two ranked lists

These are the direct input to coverage-directed generation.

- **`never_fired_factories`** — descending by how many card scripts register the
  method. A trigger 354 cards depend on is worth reaching before one a single
  card uses.
- **`never_exercised_cards`** — descending by how many never-fired factories that
  card would newly light up, then ascending by card id so the order is stable.
  A greedy set-cover weight, not a proof of optimality: reaching the top card
  does not guarantee the next one is still second. **Recompute after each
  generation round.**

### The universe, and what is outside it

The denominator is *cards the engine has a script for* — 3781 of 4344. A card
with no script has no card-specific behavior to validate; a vanilla minion, a
plain resource, a main scheme with only printed stats. Counting them as unreached
would put a permanent floor under the miss rate.

They still show up in the observations, because they still resolve abilities that
the engine's own face classes give them. So do the two `rule_*` pseudo-cards.
That is why `totals.cards` reports both `resolved` (what was observed) and
`resolved_in_universe` (the intersection, which is what the ratio uses).

### Missing dataset

If `datasets/cards/cards.json` is absent, the run is **not** failed — the
observations are the expensive half and are still written. What is lost is the
ability to name what was missed, and `universe.available` is `false` with a
reason, rather than empty ranked lists that would read as "nothing was missed".

## Determinism

Coverage is an observation. It writes no game state, reads no clock, touches no
RNG, and sorts every collection before serialising, so:

- a game played with coverage on saves a **byte-identical** scene to the same
  game with it off;
- the same seeds produce a byte-identical coverage report across processes;
- `-bot_verify` replays each saved scene through the engine again, and the
  recording window is closed before that happens, so verification cannot
  double-count.

All three are checked — the first two by hand against `main.py -bot`, the third
by `unit_test/test_card_coverage.py::test_end_game_closes_the_window`, and the
underlying engine determinism by `python -m tools.determinism.check_runs`.

Coverage from a game that **failed** is discarded. A discarded game is not in the
corpus, so crediting the corpus with what it reached would claim coverage no
saved scene can reproduce.

## Where the code is

| File | Job |
|---|---|
| `py_src/engine/profile/card_coverage.py` | the recorder: instrumentation, the three hooks, the per-game record |
| `py_src/engine/profile/coverage_report.py` | the universe, aggregation and the two ranked lists. Pure stdlib |
| `py_src/tools/coverage/report.py` | command line: merge run artefacts, re-rank, print |
| `py_src/unit_test/test_card_coverage.py` | the rules, against stand-ins |
| `py_src/unit_test/test_card_coverage_play.py` | a real game, and the dataset-drift tripwire |

Not to be confused with `py_src/engine/profile/coverage.py`, which counts
executions of card-script source locations, is disabled in a release build, and
measures something else.
