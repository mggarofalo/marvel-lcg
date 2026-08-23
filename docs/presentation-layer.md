# Presentation layer: Godot instead of ASP.NET Core

MARVEL-159. A proposal, not yet a decision. The decision is made when that issue
closes, and this document is rewritten to record what was decided.

Two questions are open and are called out under [Costs and open questions](#costs-and-open-questions):
whether Godot's .NET export still cannot target the web, and whether card art is
shipped as scans or drawn procedurally.

## What this proposes

Build the C# client in Godot, and drop `Marvel.Server` and the TypeScript web
client from the MVP.

Three things follow from that, and each one costs almost nothing now and a great
deal later:

1. The fold's return signature gains a semantic event stream, so the client can
   animate what happened rather than diff two board snapshots.
2. The prompt becomes a list of affordances anchored to board objects, rather
   than a list of option strings.
3. A build gate proves the engine assemblies cannot reference Godot.

Everything else in `docs/migration.md` survives unchanged.

## What is already settled

This document does not reopen any of the following. They are recorded in
[migration.md](migration.md) and
[card-dsl.md](card-dsl.md).

| Decided | Where |
|---|---|
| C# as the target language | migration.md, "Target language" |
| The engine is a fold: `(state, input) -> (state, prompt or gameOver)` | migration.md, "Architecture" |
| Cards become data, not sandboxed scripts | migration.md, MARVEL-92 |
| The DSL node set, and where compiled code begins | card-dsl.md |
| One repo, `src/` and `py_src/` | MARVEL-3 |
| Reqnroll for Gherkin, one set of `.feature` files | MARVEL-22, MARVEL-41 |
| Digest v2 and the standard RNG as cross-engine contracts | MARVEL-44, MARVEL-38 |

The open question is only where the pixels come from. `migration.md` currently
answers it in one line: keep the existing TypeScript client and serve it from
ASP.NET Core. That answer was made when the goal was a working engine. If the
goal is a game, it is the wrong answer.

## Why Godot fits

### The fold is already a game loop

The Python engine blocks a thread inside `Controller.ChoiceOne`, and
`WorldRender.PresentInternal` serialises the whole board on every present. That
suits a browser, which diffs a document and repaints.

Godot wants the opposite: a pump that drains events and runs tweens. The fold
gives exactly that. Input pushes a value in, `_Process` drains what comes out.
Nothing has to bend.

### Cards as data make affordances derivable

This is the strongest argument, and it connects two decisions that were made
separately.

Today a card's ability handler is opaque Python. Nothing can know what the card
will ask you before it asks. With an effect tree, the engine can walk the tree
without running it. Three things become possible that a card game needs:

- Show what a card will target before the player commits to playing it.
- Grey out an unplayable card and say why it is unplayable.
- Render the card's rules text from its own data, in the player's language.

The web client never needed any of this. A game does. And it falls out of the
card DSL work that is already done.

### Determinism survives, if the wall is real

`AGENTS.md` lists four things that must never enter a gameplay path: wall-clock
time, unseeded randomness, unordered iteration, and threads touching game state.

Godot supplies three of the four as conveniences: `Time`, `RandomNumberGenerator`
and the frame `delta`. If `Marvel.Rules` cannot reference `GodotSharp`, none of
them can reach game state. The presentation layer can then use wall-clock time
and randomness freely, for particle jitter and animation timing, because nothing
it does can feed back.

This is the discipline the Caves of Qud port is usually credited with, though I
have not verified the details of that history and would not cite it. The lesson
holds on its own: keep engine types out of the engine.

### Godot removes work from the MVP

The current plan keeps the TypeScript client and needs ASP.NET Core to serve it.
A Godot client needs no HTTP surface at all. The MVP is single-player and local,
which `migration.md` already states.

## What Godot forces that the current plan lacks

### A semantic event stream

Full board snapshots are enough to draw a board. They are not enough to animate
one. An animation needs to know that card 01096 moved from hand to discard
because an ability's cost consumed it. A snapshot only shows that the discard
pile got taller.

Two rules matter here:

Derive the stream, do not maintain it. The interpreter should emit event records
as a byproduct of executing DSL nodes. A parallel hand-written path will drift
from the rules, and then the animations start lying about what happened.

Verify it against the corpus. For every recorded step, applying the emitted
events to the previous state must reproduce the next state. That turns a whole
class of animation bug into a CI failure, using a corpus that already exists.

### Affordances instead of option strings

The prompt today is a list of labels. `MARVEL-41` already says the C# prompt
"must carry the option set and enough context to tell a mid-resolution prompt
from a turn-level one". A game needs more than that: each option has to name the
board object the player clicks.

Proposed shape:

```
Affordance {
  Id            // what gets folded back in
  Kind          // play, attack, thwart, choose-target, pay-cost, ...
  AnchorId      // the card or player object the player interacts with
  Label         // the existing domain-level label, unchanged
  Legality      // legal, or a reason it is not
}
```

The label stays exactly as MARVEL-41 requires, so the spec suite is unaffected.
`AnchorId` and `Legality` are additions, and both are derivable from the ability
envelope that card-dsl.md already describes as declarative.

### One rule to protect the DSL

Keep animation and pacing metadata out of the card DSL.

A sequence of ten nodes might be one visual beat or ten. The temptation will be
to annotate the tree. Do not. Put presentation hints in a side table keyed by
card id and node path, or derive them from event kinds. Putting view concerns in
the rules DSL is the same failure card-dsl.md warns about with escape hatches,
in different clothes.

## Proposed layout

```
src/
  Marvel.Core          ids, seeded MT19937, digest v2, canonical JSON writer
  Marvel.Rules         the fold: state, zones, phases, timing, event bus. No cards.
  Marvel.Cards.Dsl     node types, polymorphic deserialiser, validator, text renderer
  Marvel.Cards.Interp  nodes to transitions; emits events as a byproduct
  Marvel.Content       card data, scenario setup format, the compiled first-party set
  Marvel.Sim           headless: bots, policies, corpus replay, spec host, CLI driver
  Marvel.View          engine-agnostic view model: affordances, event-to-beat mapping
  ------------------------------ the wall ------------------------------
  Marvel.Godot         Godot project. Scenes, tweens, audio, input. Thin.
  Marvel.Server        deferred. Only if multiplayer ever happens.
tests/
  Marvel.Vectors.Tests RNG and digest fixtures. The first C# code written.
  Marvel.Rules.Tests   xUnit
  Marvel.Specs         Reqnroll, running py_src/specs/ in place
  Marvel.Corpus.Tests  sharded corpus replay gate
```

`Marvel.View` above the wall is the part that makes the rest work. It holds what
a card looks like on the table, and which visual beat an event implies, using no
Godot types at all. That keeps it unit-testable and keeps the Godot layer thin
enough to replace.

### Dependency rules

| Layer | May reference | Must never reference |
|---|---|---|
| `Core` | the base class library | anything game-shaped |
| `Rules` | `Core` | card content, prompts as UI |
| `Cards.*` | `Core`, `Rules` | file or network I/O, Godot |
| `View` | all of the above | `GodotSharp`, wall-clock time |
| `Godot` | everything | game state, except through the fold |

Enforce the wall in the build, not by convention. A step that fails if any
assembly below `Marvel.Godot` references `GodotSharp` is about ten lines. It is
the difference between intending to keep the engine portable and proving it. It
belongs in `ci.yml` alongside the three fixture staleness gates.

## Testing and verification

Three oracles already exist. Two more come with this proposal.

1. Vector fixtures come first. `datasets/rng/vectors.json` and
   `datasets/digest/vectors.json` are cross-language acceptance fixtures that
   exist today. The first C# written should be the RNG and the digest reader,
   tested against those files, before any game logic. That is MARVEL-8.

2. Corpus replay in `Marvel.Sim`. Fold the recorded inputs, compare the digest
   at every step. Digest v2 prints a card-by-card, field-by-field diff on
   mismatch. This is the mechanism that makes the port converge.

3. Reqnroll against the same `.feature` files, run in place from `py_src/specs/`.
   MARVEL-41 already forbids forking them, and requires a step catalogue
   conformance test on the C# side. Nothing here changes that.

4. New: event stream soundness. Replaying the emitted events onto the previous
   state must reproduce the next state, across the whole corpus.

5. New: affordance completeness. Every input the corpus recorded at a given step
   must appear in the affordance list the engine offered at that step. If the bot
   could take an action the client cannot express, the corpus finds it with no
   new authoring. This is the cheapest high-value test in the plan.

## Performance and libraries

A card game is not performance-bound. Three places genuinely are:

The fold, run across a 10,000-game corpus. Use a flat array of cards indexed by
object id, not a dictionary graph. Object id allocation order is already part of
the cross-engine contract, so a flat array is the natural shape anyway. Keep LINQ
out of the fold's hot path.

Digest computation, at roughly 5.7 MB per 491 steps raw. Write with
`Utf8JsonWriter` into a pooled buffer. Use `System.IO.Hashing` for the manifest.
Do not use Newtonsoft.

Undo and replay. Re-fold from a snapshot plus inputs rather than using persistent
data structures. It is cheaper to implement, cheaper to reason about, and it is
how the corpus already works.

Libraries worth naming:

- `System.Text.Json` with source-generated contexts, which are fast and safe for
  ahead-of-time compilation. Godot's iOS export needs that.
- `[JsonDerivedType]` polymorphism for DSL nodes. Prefer typed deserialisation
  over JSON Schema validation: the node set is closed, so the type system is the
  schema, and an unknown node fails closed for free.
- `CommunityToolkit.HighPerformance` for spans and pooling.
- BenchmarkDotNet, on the fold and the digest only.

Do not use an entity component system. A few hundred entities with very rich
per-entity rules is the case it handles worst.

## Costs and open questions

Web export for C# in Godot. Godot's .NET web export has historically been
unsupported, and was still experimental in the Godot 4.3 era. I have not
confirmed its current state, and that check should happen before any commitment.
This is the one decision that could permanently foreclose a browser build, and
the project has a working browser client today.

Card art changes category. A localhost development tool that uses card scans is
one thing. A distributable game is another. There is a real mitigation available:
because cards are data with a text renderer, the client can draw cards
procedurally instead of shipping scans. That choice shapes the view layer, so it
should be made early rather than discovered late.

Phase 6 gets much bigger. "Reconnecting the existing web client" becomes
"building a game client". That is the actual goal, so the cost is worth paying,
but it should be re-scoped openly rather than absorbed.

Keep a non-Godot driver permanently. A console driver over the fold, living in
`Marvel.Sim`. The engine must be playable before Godot opens, and when Godot
breaks you need to know whether the rules still work.

## Plane issues that need to change

The `Client and Integration` module currently holds four issues, and all four are
Python web server residuals that `migration.md` already settled as non-work.
There is no client work in it at all. So this proposal fills an empty module
rather than disrupting a planned one.

### Amend

| Issue | State | What changes |
|---|---|---|
| MARVEL-3 — Spike: target repo and solution layout | Done | Reopen or supersede. Its layout has no wall, no `Marvel.View`, and puts `Marvel.Server` in the MVP. It also explicitly defers "whether the existing TS/HTML frontend is vendored in or referenced", which is exactly the question Godot answers. |
| MARVEL-41 — Validate the scenario format against Reqnroll | Backlog | No change to its acceptance. Its third bullet, that the prompt "must carry the option set and enough context", is strengthened by the affordance shape rather than altered. Worth a note so the two are not designed twice. |
| MARVEL-8 — Implement the standard RNG in C# | Backlog | No scope change. Raise priority to Urgent. It is unblocked and it is the first C# code that should exist under either presentation decision. |
| MARVEL-92 — Scope the card DSL | Done | Add a note recording the rule that presentation hints stay out of the DSL. The follow-up work belongs in a new issue, not here. |

### Re-scope or close

| Issue | State | What changes |
|---|---|---|
| MARVEL-145 — The server does not decide what each seat sees | Backlog | If `Marvel.Server` leaves the MVP, this has no target for the foreseeable future. `migration.md` already records the forward-looking requirement in prose. Close it as recorded-not-tracked, or move it to `Probably Won't Do`. |
| MARVEL-153 — The web server serves arbitrary files and a cheat console | Backlog | Same. `migration.md` already states the carry-forward constraint: do not port `/read_file` or the cheat console onto a served surface. Close as recorded. |
| MARVEL-146, MARVEL-152 | Cancelled | Already cancelled. No action. |

### New issues to create

All in the `Foundations` or `Engine Core` module. Layer label `engine` unless
noted.

1. Decide the presentation layer, and record it. Module `Foundations`, labels
   `docs` and `spike`, priority Urgent. Acceptance is this document landed as
   `docs/presentation-layer.md` with a pointer added from `migration.md` where it
   currently assumes ASP.NET Core. Supersedes the frontend half of MARVEL-3.

2. Extend the fold signature with a semantic event stream. Module `Engine Core`,
   priority High, blocked by the presentation decision. This must land before the
   interpreter exists. Retrofitting it after 3,457 card ports is not viable.

3. Model the prompt as anchored affordances. Module `Engine Core`, priority High.
   Coordinate explicitly with MARVEL-41 so the label contract is designed once.

4. Prove the engine assemblies cannot reference Godot. Module `Foundations`,
   labels `tooling` and `dx`, priority Medium. A `ci.yml` gate next to the three
   fixture staleness checks.

5. Verify the event stream against the corpus. Module `Corpus and Oracle`, label
   `testing`, priority Medium, blocked by issue 2 and by MARVEL-158.

6. Verify affordance completeness against the corpus. Module `Corpus and Oracle`,
   label `testing`, priority Medium, blocked by issue 3 and by MARVEL-158.

7. Decide how cards are drawn: scans or procedural rendering. Module
   `Client and Integration`, labels `frontend` and `spike`, priority Low. Names
   the licensing question rather than answering it.

8. Confirm Godot's .NET export targets. Module `Foundations`, label `spike`,
   priority High if a browser build matters at all, Low if it does not. Check web,
   Android and iOS status for C# before committing.

## Sequencing

Phases 1 to 3 are unaffected. They run against the Python engine and produce
artifacts that outlive both clients. MARVEL-158, generating and freezing the
corpus, stays the critical path and stays Urgent.

Inside phase 4, the fold's signature should be settled before the interpreter is
written:

```
(state, input) -> (state, Affordance[], GameEvent[])
```

Getting that right now is nearly free. Getting it right later is not.

Godot itself comes last, and it should not open until the engine is playable from
a console.
