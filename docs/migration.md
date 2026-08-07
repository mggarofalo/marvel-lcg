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

### RNG: two backends, and the default is numpy

`engine/lib/random.py` dispatches on `disable_numpy_random`, which **defaults to False**. The production RNG is therefore `numpy.random` (legacy global `RandomState`), not the hand-written `engine/lib/mt19937.py`. The custom generator is dead code in the default configuration.

This materially affects the port, and which backend the corpus is generated with is an open decision:

- **numpy backend** — matches current production behavior, and `Random.Undo()` works (it snapshots and restores numpy state). Porting requires reproducing numpy's legacy `RandomState` bit-exactly: `seed`, `shuffle`, and `choice` including `replace=False`. Feasible — numpy freezes legacy stream behavior by policy under NEP 19 — but a meaningful chunk of work.
- **custom MT19937 backend** — far easier to port (about 100 lines of well-understood code), but it is not what the game actually runs, and `Random.Undo()` is a **no-op** in that mode, so any corpus exercising undo would diverge.

Do not assume the custom MT19937 is the contract. Tracked as its own decision issue.

### Other portability hazards

- If the custom backend is chosen: `shuffle()` is not Fisher-Yates (it performs `10 * len` random swaps) and `randint` truncates a float division. Both would need porting bit-exactly, quirks included.
- `Random.states` grows without bound — every `choice`/`shuffle` appends a numpy state snapshot and only `Undo()` pops. Long games leak memory, and undo semantics depend on call history.
- Card `object_id` allocation order determines the digest's dict keys, so allocation order is part of the cross-engine contract.

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
