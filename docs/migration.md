# Migration to C#

Context for the planned rewrite. Tracked in the Plane project `MARVEL` — see [plane.md](plane.md).

This document records **why** the migration is happening and **what was decided**, so agents do not re-litigate settled questions or re-derive measurements.

## Why migrate

Two reasons, in order of weight.

**1. Card scripts execute arbitrary Python.** `cards/database.py` calls `exec()` on card modules. The AST denylist in `engine/security/command_validation.py` guards only the cheat console, not card loading — and a denylist over import names would not hold anyway. Any downloaded card pack is arbitrary code execution. The upstream README acknowledges this; there is no fix that preserves the current design.

**2. The architecture is built around blocking on player input.** `Controller.ChoiceOne` blocks a thread inside `GetInput`. Everything downstream — `engine/task/`, `engine/job/`, condition variables, sync/notify — exists to support that. It makes the engine hard to test, hard to drive programmatically, and hard to reason about.

## Measurements

Taken 2026-08-06. Re-measure rather than trusting these if the tree has moved substantially.

| | |
|---|---|
| Engine + game + core | 58,905 LOC Python |
| Card scripts | 3,457 files, ~102k LOC |
| Card script size | median 27 lines, p90 41, p99 57, max 187 |
| Distinct `AbilityFactory` methods | 310 |
| Trigger-method coverage | top 10 = 57%, top 50 = 83%, top 150 = 95% |
| Cards with no imperative handler | 531 scripts (15%) |
| Cards that suspend for player choice mid-resolution | 440 scripts (13%) |
| Data files (scenarios, encounter sets, decks) | 390 JSON — portable as-is |
| Frontend | 38 TS + 16 HTML + 69 CSS — portable as-is |
| Content matrix | 108 scenarios, 63 starter decks, 148 encounter sets, 25 challenges |

The distribution is the important part: card scripts are small and mostly shaped alike, but the trigger vocabulary has a long tail, and the tail is where port bugs will hide.

The last two rows are now recomputed on every run of `python -m tools.cards.extract`, each stated next to the rule that produces it — see [card-dataset.md](card-dataset.md#stratification). The 531 reproduced exactly. The player-choice figure was originally recorded here as 334 from a measurement whose rule was never written down; no principled rule reproduces it, so 440 supersedes it.

## Decisions

### Target language: C#

Chosen over TypeScript and Rust. Determinism control is better than TypeScript's, `yield return` iterators model suspend-for-player-input directly, and **Reqnroll** (the maintained SpecFlow successor) is the strongest Gherkin tooling available — which matters given the testing strategy below. Costs accepted: slower compile loop than TS, and no shared language with the existing web client.

The existing TS/HTML/CSS client is kept and served from ASP.NET Core.

### Architecture: the engine is a fold

```
(state, input) -> (state, prompt | gameOver)
```

No blocking, no threads in gameplay paths. "Ask the player" is a `yield`, not an I/O wait.

This falls out of the change rather than being extra work: replay becomes a fold over the recorded input list, undo becomes re-folding a prefix, tests lose all timing flake, and driving the engine from an agent or a bot becomes a function call instead of a websocket client.

### Cards become data, not sandboxed scripts

The goal is not a safer sandbox. It is removing the concept of card-supplied code.

A card ability becomes a serializable tree of typed nodes (`seq`, `ifThen`, `forEach`, `dealDamage`, `askPlayer`, `placeCounter`, …) that the engine interprets. Third-party cards then cannot execute anything, because there is nothing to execute — they are data that can be schema-validated, diffed, hashed, and rendered as English.

The trust boundary is **provenance, not expressiveness**:

- A small number of first-party scenario scripts stay as compiled engine code. Some content genuinely does not fit a data DSL — `cards/pack/twc/07001a.py` (Breakout) builds four encounter decks and registers abilities dynamically at setup; `cards/pack/deadpool/44057.py` (Tic-Tac-Toe) computes win-lines over a counter grid.
- Everything a user can author or download is data only.

Forcing the hard tail into the DSL would warp it into a general-purpose language, which reintroduces the original problem. Do not do this.

**Design the DSL against the hardest ~30 cards first, not the common ones.** The common cases fall out for free; the tail does not. Projects like this routinely hit 90% quickly, then bolt escape hatches onto the DSL until it is a scripting language again.

## The oracle

The single highest-leverage asset, and the reason preparation comes before any C# code.

Every replay step records a state digest — `World.CalculateCRC()` (`game/world/world_render.py:123`) — which `engine/controller/module/replay.py` compares against the recorded value on replay, printing a **card-by-card diff** on mismatch. Combined with a seeded RNG, this means: replay the same corpus through both engines and get told exactly which card diverged at which step.

The digest is specified in full in [state-digest-contract.md](state-digest-contract.md), including what it does *not* capture — each card's value is a plain sum of its state fields, so the diff names the card but not the field, and most cards are absent from it entirely. That specification is the input to any decision about replacing the digest before the corpus is frozen.

That is the difference between a rewrite that converges and one that does not.

**The problem: `replays/` is empty and untracked.** The corpus does not exist, and there is no AI or auto-play in the engine — it is entirely built around a blocking human. So the corpus has to be generated by a bot written against the Python engine, which is why that work is sequenced first.

### RNG: replace it on both sides with one standard (MARVEL-25, MARVEL-38)

**Decided, and landed on the Python side.** Both engines implement the *same* precisely-specified standard RNG, so the same seed produces the same output in both. Seed-based cross-engine replay works, and no randomness needs to be recorded in the corpus. The specification is [docs/rng-contract.md](rng-contract.md); MARVEL-8 implements it in C#.

The starting position was a mess: `engine/lib/random.py` dispatched on `disable_numpy_random`, which **defaulted to False**, so the production RNG was `numpy.random` (legacy global `RandomState`) and the hand-written `engine/lib/mt19937.py` was dead code. Neither was a good contract — the custom one shuffled by `10 * len` random swaps instead of Fisher-Yates and derived bounded integers by truncating a float division.

Rather than port either set of quirks into C#, both were replaced.

Why it works: numpy and the repo's custom generator **already agreed on the first draw from seed 42** (both `0.37454`) — both *are* MT19937. They diverged only in the consumption layer, where numpy takes two 32-bit words per double and the repo took one. (Observable: the repo's third value equalled numpy's second.) Standardising that layer makes them agree. The raw word stream is unchanged from what the engine produced before, which `unit_test/test_rng.py` pins against both numpy's values and MT19937's own published ones.

**It had to happen before corpus generation.** Changing the RNG changes game outcomes. There was no corpus and no tracked replays, so the cost was zero; afterwards it would have been a full regeneration.

What the contract pins, each of which would reintroduce divergence if left open:

- **Seeding routine** — always `init_genrand` with one 32-bit word, never `init_by_array`
- **No floats at all** — bounded integers come straight off the raw 32-bit output by masked rejection, which sidesteps cross-language float semantics and removes modulo bias. The draw-to-float question that caused the original divergence simply stops existing.
- **Fisher-Yates direction** — downward for `Shuffle`, upward for the partial shuffle behind `ChooseWithoutReplacement`
- **Exact consumption** — including that a bound of 1 still consumes a word, and that selecting every element consumes none
- **State save/restore** — 624 words plus an index. `Random.Undo()` is bounded and errors past the end, rather than the previous unbounded list that silently did nothing under one backend

This is a genuine behavior change to the reference engine, not an additive one. That is fine — we no longer track upstream, so `py_src/` can be changed wherever a Plane issue justifies it. The divergences are listed in section 9 of the contract.

### Other portability hazards

- ~~`Random.states` grows without bound~~ — fixed by MARVEL-38. Snapshots now live in a bounded ring (`UNDO_DEPTH`), so a long game no longer grows one per draw, and undo no longer depends on total call history.
- Card `object_id` allocation order determines the digest's dict keys, so allocation order is part of the cross-engine contract.
- Two collections whose iteration order can reach recorded replays: `GetTeamUpUnits` returns `list(set(...))`, and forced-effect resolution order derives from an identity-hashed set. **`PYTHONHASHSEED=0` does not fix these** — it hides them. They must be fixed at the source.

### Corpus storage: gzipped, in a separate pinned repo (MARVEL-4)

**Decided, from measurement rather than estimate.** 13 bot-generated games: mean 11.0 KB, range 5.2–20.8 KB, 7–45 steps — roughly **0.5 KB per step** raw. Gzip at level 9 compresses the set **8.2x** (146 KB → 17.8 KB), and `engine/lib/json.py` already supports gzip.

Extrapolating to deeper games from a heuristic policy (a few hundred steps each), a 10,000-game corpus lands near 1.5 GB raw, under 200 MB compressed.

The decisive property is that **the corpus is immutable and write-once**. Git's problem is repeated churn on large files, not one-time storage — so a dedicated repo holding compressed, never-modified shards is fine at this scale and avoids both LFS friction and object-storage infrastructure.

- Store gzipped
- Separate repo, pinned from here by commit SHA
- Hash manifest checked into *this* repo, so integrity is verifiable without fetching the corpus
- Shard by scenario so CI can fetch a subset instead of the whole thing

Re-measure once the heuristic policy exists; these games came from a random policy that loses in ~20 steps, so per-game size is a lower bound.

### Repo layout: one repo, `src/` and `py_src/` (MARVEL-3)

**Decided.** A single repository holding both engines:

```
py_src/     Python reference engine + preparation tooling
src/        C# engine
docs/       shared documentation, decisions, audits
```

The earlier recommendation was a separate repo, on the grounds that this was a fork needing to stay cheap to pull from upstream. **That constraint is gone — we no longer track upstream.** With it removed, the decisive factor is that agents work poorly across two repositories: cross-referencing the Python reference while porting is constant, and every split introduces coordination the work doesn't need.

Consequences:

- `py_src/` can be refactored freely where a Plane issue justifies it. There is no upstream diff to preserve.
- **All Python commands run from `py_src/`.** Paths in `launch.json` and `engine/config.py` are relative to the working directory.
- The corpus still lives in its own repo, pinned by SHA — that is about immutable bulk data, not about code organization, so the MARVEL-4 decision is unaffected.

Planned C# layout, following the conventions already in use in the receipts repo:

```
src/Directory.Build.props / Directory.Packages.props   central package management
src/Marvel.Engine        core rules; no I/O, no RNG state
src/Marvel.Cards         card DSL and card data
src/Marvel.Server        ASP.NET Core; serves the web client
src/tests/Marvel.Engine.Tests    xUnit
src/tests/Marvel.Specs           Reqnroll
```

The web client currently lives at `py_src/public/` because the Python server serves it over relative paths. Whether it moves to a shared top-level location is deferred to the Client and Integration phase.

## Testing strategy

Three oracles, used for different things.

**1. The replay corpus** — integration-level ground truth. Mechanically generated, covers whole games.

**2. Behavioral specs authored from printed card text** — the text the game's designers wrote, which is authoritative in a way that the Python implementation is not. It comes from the vendored MarvelSDB snapshot, not from `data/cards.json`: the engine's copy has 36 cards corrupted by an encoding round-trip and 197 that say something materially different from the printed card. See [card-dataset.md](card-dataset.md).

The discipline that makes this trustworthy at scale: **a scenario is not trusted until it passes against the running Python engine.** A disagreement is triaged as either a spec bug or an engine bug; both are worth finding. That makes it differential spec extraction rather than inference — and it only works while the Python engine is still the reference, which makes it urgent.

**3. The puzzle system** — `game/puzzle/puzzle.py` `RunPuzzle` is already a state-setup DSL (`CreateHandCards`, `SetThreat`, `Damage`, `Confuse`, …). It is the Gherkin `Given` clause, already built. `When` is selecting an effect; `Then` is a state assertion. This is how per-card specs execute, and it is also how cards unreachable by self-play get covered.

Vary spec depth by card complexity rather than writing a fixed number per card. The 531 scripts with no handler need very little; the 440 that suspend for player choice need the most. `datasets/cards/summary.json` carries both, recomputed.

## Sequencing

1. **Foundations** — guidance, get the Python engine running reproducibly, decide repo layout and corpus storage
2. **Corpus and Oracle** — headless bot, determinism audit, coverage-directed corpus, freeze it
3. **Spec Extraction** — card text dataset, puzzle harness, validation runner, rules-engine specs
4. **Engine Core** — the C# fold
5. **Card DSL and Port** — DSL against the hard cases, then the 3,457 ports
6. **Client and Integration**

Phases 1–3 all run against the Python engine and produce artifacts that outlive it. Nothing in 4–6 can be validated without them.
