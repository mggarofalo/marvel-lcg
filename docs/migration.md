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
| Cards with no imperative handler | 531 (15%) |
| Cards that suspend for player choice mid-resolution | 334 (10%) |
| Data files (scenarios, encounter sets, decks) | 390 JSON — portable as-is |
| Frontend | 38 TS + 16 HTML + 69 CSS — portable as-is |
| Content matrix | 108 scenarios, 63 starter decks, 148 encounter sets, 25 challenges |

The distribution is the important part: card scripts are small and mostly shaped alike, but the trigger vocabulary has a long tail, and the tail is where port bugs will hide.

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

Every replay step records a state digest — `World.CalculateCRC()` (`game/world/world_render.py:123`) — which `engine/controller/module/replay.py` compares against the recorded value on replay, printing a **key-by-key diff** on mismatch. Combined with a seeded RNG, this means: replay the same corpus through both engines and get told exactly which card diverged at which step.

That is the difference between a rewrite that converges and one that does not.

**The problem: `replays/` is empty and untracked.** The corpus does not exist, and there is no AI or auto-play in the engine — it is entirely built around a blocking human. So the corpus has to be generated by a bot written against the Python engine, which is why that work is sequenced first.

### RNG: replace it on both sides with one standard (MARVEL-25)

**Decided.** Both engines implement the *same* precisely-specified standard RNG, so the same seed produces the same output in both. Seed-based cross-engine replay works, and no randomness needs to be recorded in the corpus.

The starting position was a mess: `engine/lib/random.py` dispatches on `disable_numpy_random`, which **defaults to False**, so the production RNG is `numpy.random` (legacy global `RandomState`) and the hand-written `engine/lib/mt19937.py` is dead code. Neither is a good contract — the custom one shuffles by `10 * len` random swaps instead of Fisher-Yates and derives bounded integers by truncating a float division.

Rather than port either set of quirks into C#, replace both.

Why it works: numpy and the repo's custom generator **already agree on the first draw from seed 42** (both `0.37454`) — both *are* MT19937. They diverge only in the consumption layer, where numpy takes two 32-bit words per double and the repo takes one. (Observable: the repo's third value equals numpy's second.) Standardize that layer and they agree.

**Do it before corpus generation.** Changing the RNG changes game outcomes. There is no corpus yet and no local replays, so the cost is zero today and becomes a full regeneration afterwards.

Under-specifying any of these reintroduces divergence, so the spec must pin all of them:

- **Seeding routine** — MT19937 has both `init_genrand` and `init_by_array`; numpy chooses by input type
- **Draw-to-float** — one word over 2³², or two words for a 53-bit double? This is the exact observed divergence
- **Prefer integers over floats** — bounded integers straight from the raw 32-bit output via rejection sampling, which sidesteps cross-language float semantics and removes modulo bias
- **Fisher-Yates direction** — downward vs upward yields different permutations from the same stream
- **State save/restore** — `Random.Undo()` currently leans on `numpy.random.get_state()`; MARVEL-7 found it unsound in both current backends, so fix rather than reproduce

This is a deliberate **behavior change to upstream code**, not an additive one — the one place we knowingly break fork hygiene, because it is a precondition for the migration.

### Other portability hazards

- `Random.states` grows without bound — every `choice`/`shuffle` appends a numpy state snapshot and only `Undo()` pops. Long games leak memory, and undo semantics depend on call history.
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

### Repo layout: a separate repo for the C# engine (MARVEL-3)

**Recommended, pending confirmation** — creating the repository is the owner's call.

This repository is a fork with a live `upstream` remote at `irefrixs/marvel-lcg`, and it needs to stay cheap to pull from. A large C# tree living here would make every upstream merge painful, permanently, in exchange for co-location benefits that barely materialize — the two codebases share no build, no toolchain, and no source.

So:

- **`marvel-lcg` (this fork)** — the Python reference engine, plus preparation tooling: the bot, the corpus harness, the determinism harness, the spec harness. Stays pullable from upstream.
- **A new repo for the C# engine** — its own solution, CI, and history.
- **A corpus repo** — per the storage decision above, pinned by SHA from both sides.

Agents doing the port check out both. In practice that is a non-issue.

Suggested solution layout, following the conventions already in use in the receipts repo:

```
Directory.Build.props / Directory.Packages.props   central package management
src/Marvel.Engine        core rules; no I/O, no RNG state
src/Marvel.Cards         card DSL and card data
src/Marvel.Server        ASP.NET Core; serves the existing TS client
tests/Marvel.Engine.Tests    xUnit
tests/Marvel.Specs           Reqnroll
```

## Testing strategy

Three oracles, used for different things.

**1. The replay corpus** — integration-level ground truth. Mechanically generated, covers whole games.

**2. Behavioral specs authored from printed card text** — `data/cards.json` holds the text the game's designers wrote. This is authoritative in a way that the Python implementation is not.

The discipline that makes this trustworthy at scale: **a scenario is not trusted until it passes against the running Python engine.** A disagreement is triaged as either a spec bug or an engine bug; both are worth finding. That makes it differential spec extraction rather than inference — and it only works while the Python engine is still the reference, which makes it urgent.

**3. The puzzle system** — `game/puzzle/puzzle.py` `RunPuzzle` is already a state-setup DSL (`CreateHandCards`, `SetThreat`, `Damage`, `Confuse`, …). It is the Gherkin `Given` clause, already built. `When` is selecting an effect; `Then` is a state assertion. This is how per-card specs execute, and it is also how cards unreachable by self-play get covered.

Vary spec depth by card complexity rather than writing a fixed number per card. The 531 cards with no handler need very little; the 334 that suspend for player choice need the most.

## Sequencing

1. **Foundations** — guidance, get the Python engine running reproducibly, decide repo layout and corpus storage
2. **Corpus and Oracle** — headless bot, determinism audit, coverage-directed corpus, freeze it
3. **Spec Extraction** — card text dataset, puzzle harness, validation runner, rules-engine specs
4. **Engine Core** — the C# fold
5. **Card DSL and Port** — DSL against the hard cases, then the 3,457 ports
6. **Client and Integration**

Phases 1–3 all run against the Python engine and produce artifacts that outlive it. Nothing in 4–6 can be validated without them.
