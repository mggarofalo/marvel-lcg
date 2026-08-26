# Presentation layer: Godot instead of ASP.NET Core

MARVEL-159. **Decided on 24 August 2026.** The client is built in Godot; the
TypeScript client and the ASP.NET Core web host are dropped from the MVP. What
follows was written as a proposal and is kept in that voice where the reasoning
still reads better as argument than as decree.

Two things are settled that were open when this was drafted, and one question
remains:

- **Runtime floor: `net8.0`.** Forced by Godot, and it constrains every assembly
  in the solution. See [The runtime floor](#the-runtime-floor).
- **Web export: gone, and it costs less than assumed.** See
  [Export targets](#export-targets).
- **Card rendering: procedural.** Decided the same day, and *not* on licensing
  grounds. See [How cards are drawn](#how-cards-are-drawn).

Release targets are settled: macOS and Windows clients, with the server also
runnable as a Linux container. Web is explicitly not wanted, which removes the
only material risk the Godot decision carried.

The export-target question is answered. MARVEL-166 confirmed against Godot
4.7.2-stable that C# cannot be exported to the web, and will not be soon. What
that costs is smaller than this document first assumed — see
[Export targets](#export-targets).

## What was decided

Build the C# client in Godot for macOS and Windows, and drop the TypeScript web
client. Keep `Marvel.Server`, but not in the shape `migration.md` assumed: it is
the engine host, bundled inside the client for local play and separately runnable
as a Linux container.

Three things follow from that, and each one costs almost nothing now and a great
deal later:

1. The engine's return signature gains a semantic event stream, so the client can
   animate what happened rather than diff two board snapshots.
2. The prompt becomes a list of affordances anchored to board objects, rather
   than a list of option strings.
3. A build gate proves the engine assemblies cannot reference Godot, and
   cannot drift above its runtime floor.

Everything else in `docs/migration.md` survives unchanged.

## What is already settled

This document does not reopen any of the following. They are recorded in
[migration.md](migration.md) and
[card-dsl.md](card-dsl.md).

| Decided | Where |
|---|---|
| C# as the target language | migration.md, "Target language" |
| The engine is a resolve: `(state, input) -> (state, prompt or gameOver)` | migration.md, "Architecture" |
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

### The engine is already a game loop

The Python engine blocks a thread inside `Controller.ChoiceOne`, and
`WorldRender.PresentInternal` serialises the whole board on every present. That
suits a browser, which diffs a document and repaints.

Godot wants the opposite: a pump that drains events and runs tweens. The engine
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

**Settled by MARVEL-160 — see [event-stream.md](event-stream.md).** The
vocabulary is nine records, measured against the corpus rather than designed:
201,870 recorded transitions fall into twelve shapes, and a reducer over those
records reproduces 100% of them. What follows is the argument that led there.


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

**Settled by MARVEL-161 — see [affordances.md](affordances.md).** The five-field
sketch below turned out to be a third of the answer: measured over 1,997 prompts
and 6,351 options, legal targets are informative on 86.5% and cost on 53.5%, and
neither is in the sketch. What follows is the reasoning that led there.


The prompt today is a list of labels. `MARVEL-41` already says the C# prompt
"must carry the option set and enough context to tell a mid-resolution prompt
from a turn-level one". A game needs more than that: each option has to name the
board object the player clicks.

Proposed shape:

```
Affordance {
  Id            // what gets resolved back in
  Kind          // play, attack, thwart, choose-target, pay-cost, ...
  AnchorId      // the card or player object the player interacts with
  Label         // the existing domain-level label, unchanged
  Legality      // legal, or a reason it is not
}
```

The label stays exactly as MARVEL-41 requires, so the spec suite is unaffected.
`AnchorId` and `Legality` are additions, and both are derivable from the ability
envelope that card-dsl.md already describes as declarative.

`Marvel.Cards` is one project rather than the two below it, and that is a
deliberate deviation with an expiry: the split is between a *reader* and a
*runner*, and what would justify it — a validator and a text renderer — does not
exist. The namespaces are already `Dsl` and `Run`, so splitting them is moving
files. See [card-dsl.md](card-dsl.md), "What is implemented".

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
  Marvel.Rules         the engine: state, zones, phases, timing, events. No cards.
  Marvel.Cards         node types, deserialiser, interpreter  [exists, as one project]
  Marvel.Cards.Dsl     ... to split out when there is a validator and a text renderer
  Marvel.Cards.Interp  ... to split out with it
  Marvel.Content       card data, scenario setup format, the compiled first-party set
  Marvel.Sim           headless: bots, policies, corpus replay, spec host, CLI driver
  Marvel.View          engine-agnostic view model: affordances, event-to-beat mapping
  ------------------------------ the wall ------------------------------
  Marvel.Godot         Godot project. Scenes, tweens, audio, input. Thin.
  Marvel.Server        engine host. Embedded in the client, or a Linux container.
tests/
  Marvel.Vectors.Tests RNG and digest fixtures. The first C# code written.
  Marvel.Rules.Tests   xUnit
  Marvel.Specs         Reqnroll, running specs/ in place
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
| `Godot` | everything | game state, except through the engine |

Enforce the wall in the build, not by convention. **Done in MARVEL-162** — the
gate is two `<Error>` targets in `Directory.Build.targets`, so it fails a local
build and not only CI.

It reads `@(ReferencePath)` *after* `ResolveReferences`, which is the list the
compiler will actually be handed: every ProjectReference, every PackageReference
and their transitive closures, plus any bare `<Reference>` pointing at a .dll on
disk. Reading the `.csproj` files instead would catch a project that names Godot
and miss a package three levels down that depends on it — and it is the second
one that will happen.

A second target pins the runtime floor. `TargetFramework` has to equal
`$(MarvelTargetFramework)`, because the failure to guard against is not subtle
reasoning about framework compatibility; it is someone creating a project next
year, taking whatever TFM the SDK offers, and finding out when Godot tries to
reference it.

Both deny by default. `Marvel.Godot` opts out with
`<MarvelMayReferenceGodot>true</MarvelMayReferenceGodot>`, which is a deliberate
edit to a `.csproj` rather than something that happens by adding a package.

**And both were watched firing.** `tools/godot-wall.sh` builds four throwaway
projects under `tests/godot-wall/` that are supposed to fail, and checks that
they fail with the right error code. One of them — `Marvel.WallProbe` — names
Godot nowhere and reaches it through an intermediate project, which is the case
a `.csproj` scan would wave through. Another proves the opt-out still works,
without which `Marvel.Godot` could not build at all. It runs offline: the
`GodotSharp` they reference is a one-class stub. An `<Error>` condition nobody
has watched evaluate to true is a claim about a build, not a property of one.

## Testing and verification

Three oracles already exist. Two more come with this proposal.

1. Vector fixtures come first. `datasets/rng/vectors.json` and
   `datasets/digest/vectors.json` are cross-language acceptance fixtures that
   exist today. The first C# written should be the RNG and the digest reader,
   tested against those files, before any game logic. That is MARVEL-8.

2. Corpus replay in `Marvel.Sim`. Resolve the recorded inputs, compare the digest
   at every step. Digest v2 prints a card-by-card, field-by-field diff on
   mismatch. This is the mechanism that makes the port converge.

3. Reqnroll against the same `.feature` files, run in place from `specs/`.
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

The engine, run across a 10,000-game corpus. Use a flat array of cards indexed by
object id, not a dictionary graph. Object id allocation order is already part of
the cross-engine contract, so a flat array is the natural shape anyway. Keep LINQ
out of the engine's hot path.

Digest computation, at roughly 5.7 MB per 491 steps raw. Write with
`Utf8JsonWriter` into a pooled buffer. Use `System.IO.Hashing` for the manifest.
Do not use Newtonsoft.

Undo and replay. Re-resolve from a snapshot plus inputs rather than using persistent
data structures. It is cheaper to implement, cheaper to reason about, and it is
how the corpus already works.

Libraries worth naming:

- `System.Text.Json` with source-generated contexts, which are fast and safe for
  ahead-of-time compilation. Godot's iOS export needs that.
- `[JsonDerivedType]` polymorphism for DSL nodes. Prefer typed deserialisation
  over JSON Schema validation: the node set is closed, so the type system is the
  schema, and an unknown node fails closed for free.
- `CommunityToolkit.HighPerformance` for spans and pooling.
- BenchmarkDotNet, on the engine and the digest only.

Do not use an entity component system. A few hundred entities with very rich
per-entity rules is the case it handles worst.

## Export targets

Answered by MARVEL-166, researched on 23 August 2026 against Godot 4.7.2-stable,
released 18 August 2026.

| Target | C# support |
|---|---|
| Windows, Linux, macOS | Fully supported |
| Android | Supported since 4.2, experimental |
| iOS | Supported since 4.2, experimental. Simulator templates are x64 only, and export needs a macOS host. |
| Web | Not supported |

The Godot 4.7 documentation is unambiguous: "Projects written in C# using Godot 4
currently cannot be exported to the web." The 4.8 development docs say the same.

Web is also not close, which matters because the May 2025 prototype demo makes it
look imminent. The enabling pull request is open, still marked draft, and carries
the milestone `4.x`, which means unscheduled. Its only commit is dated April 2025.
The tracking issue has been open since January 2023. Demand is not the constraint:
the pull request has 443 reactions. Plan as though C# web export does not exist.

### Losing web costs the client, not the engine

This document originally called web export the one decision that could
permanently foreclose a browser build. That was too strong, and the reason is the
wall.

`Marvel.Core`, `Marvel.Rules`, `Marvel.Cards.*` and `Marvel.View` are plain .NET
class libraries that never reference `GodotSharp`. Godot's limitation applies to
the Godot export pipeline, not to those assemblies. A browser client can be a
Blazor WebAssembly application over the same engine, which is a supported
Microsoft path.

So the browser option survives as a different client over one engine, rather than
being lost. That raises the build gate from hygiene to a load-bearing part of the
argument, and it is why MARVEL-162 should land early rather than late.

### One engine-level consequence

iOS forbids just-in-time compilation, so iOS builds compile ahead of time. Blazor
WebAssembly trimming wants the same discipline. Two different future targets, one
requirement: source-generated `System.Text.Json` contexts, and no runtime
reflection in the engine. Honour it from the first line of `Marvel.Core` rather
than retrofitting it.

## The runtime floor

Measured on 24 August 2026, and the one part of this decision that reaches code
already written.

`GodotSharp` 4.7.0 ships **`net8.0`**. A `net8.0` project cannot reference a
`net10.0` library — that is a NuGet error, not a warning — so every assembly
below the wall has to sit at or below Godot's floor. `Directory.Build.props`
now says `net8.0` for the whole solution.

Targeting .NET 10 *from* Godot is not an available alternative. It is not the
default, and [godotengine/godot#112701](https://github.com/godotengine/godot/issues/112701)
reports the managed host failing to probe shared-framework assemblies under
`net10.0`, with the reporter's workaround being a hand-written
`AssemblyLoadContext` resolver. .NET 10 support is
[still under discussion](https://github.com/godotengine/godot-proposals/discussions/13076)
for a later release. Plan on `net8.0`.

**What it cost, taken now:** one API call. `Marvel.Core` used
`Convert.ToHexStringLower`, which arrived in .NET 9; it is now
`Convert.ToHexString(...).ToLowerInvariant()`. Nothing else in the assembly
needed anything above .NET 8. Taken later — after the engine, the interpreter and
the card set — the same change is a solution-wide audit.

No multi-targeting. One TFM for the solution is the whole point; a project that
builds for two runtimes has to be *tested* on two runtimes, and the digest's
JSON escaping is runtime behaviour.

That last point has a consequence for CI. `dotnet test` on a machine with only
the .NET 10 runtime silently rolls forward, so the tests would pass without ever
touching .NET 8. `ci.yml` installs the 8.0 runtime alongside the pinned SDK so
the tests run on the runtime the client will actually host. Locally,
`DOTNET_ROLL_FORWARD=Major dotnet test` works and is not the same check.

### Why the digest survived the floor unchanged

Worth recording, because the pattern generalises.

The state digest is compared byte for byte across the two engines, so its JSON
escaping is part of the contract. The C# side originally hand-wrote an escaper to
match Python's `json.dumps` exactly. That was the wrong instinct twice over: a
hand-written encoder is a maintenance liability, and it would have to be
re-verified against every runtime change — which is exactly the kind of thing a
TFM floor makes you think about.

The replacement is `Utf8JsonWriter` with
`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, plus two regex passes that
reconcile the two writers' remaining differences. The platform writer keeps
responsibility for everything structural; the normaliser only adjusts spelling.
Full reasoning in
[state-digest-v2.md](state-digest-v2.md#reconciling-two-json-writers).

The general form: when two runtimes have to agree, let each use its native tool
and reconcile the output mechanically — rather than reimplementing one runtime's
behaviour inside the other, or constraining the data until the disagreement
becomes unreachable. The second of those was the first answer here, and it
worked, but it made the digest's *contents* hostage to a JSON quirk.

## Server topology

Decided 23 August 2026. `migration.md` treats `Marvel.Server` as a later phase for
multiplayer, deferred and deliberately not architected. That is not the shape it
takes.

The server is the engine host, and it runs in two places from the same assembly:

- Bundled inside the Godot client, in-process, for local single-player.
- Standalone in a Linux container, for hosted play.

Two consequences, and the first is the one that decides whether this works.

### The client speaks one interface, whatever the transport

If the client calls the engine directly when bundled and over a wire when hosted,
those are two code paths and they will diverge. The bugs will only appear in the
hosted case, which is the case that is harder to debug.

So the client always speaks the same interface. Only the transport changes:
in-process for the bundled case, a socket for the container. The engine makes this
straightforward, because `(state, input) -> (state, affordances, events)` says
nothing about where the function runs.

### Affordances and events have to be wire types

This is a new constraint on MARVEL-160 and MARVEL-161, and it is cheap now and
expensive later. Both the affordance list and the event stream cross a network in
the hosted case, so both need stable, versioned, serialisable representations.
They cannot hold object references into engine state.

The digest is the counter-example that proves the rule: it records hidden state
truthfully and must never reach a client. With a real server that stops being a
convention and becomes something the wire format enforces.

### Visibility becomes a real requirement again

`migration.md` records the forward-looking rule in prose: the server decides what
each seat sees, and the client's assertion is an input to that decision rather
than the decision itself. That was filed away as untracked because nothing was
being served.

Something is being served now. The rule needs an owner on the C# side. This is a
cooperative game, so a permissive policy is legitimate — it still has to be
chosen rather than arrived at by nobody checking.

## How cards are drawn

MARVEL-165, decided 24 August 2026: **procedural**, and the reason is not the one
this document expected.

The draft framed this as a licensing problem — scans are fine for a localhost
tool and not for a distributable game. That pressure turned out not to apply.
The audience is the author and possibly a few friends, so the distribution
question that would have forced procedural rendering does not arise. The choice
is made on merit instead.

The merit is **a card can be drawn in its current state rather than its printed
state.** A scan shows what was printed: base cost, base attack, the traits the
card shipped with. A procedurally drawn card shows what the card *is right now* —
cost reduced by an ally, attack buffed for the phase, a keyword granted this
turn, a trait added by an upgrade. Scan-based clients invariably end up bolting
badges and overlays on top to say the same thing, and the overlays and the rules
drift apart. Here the renderer reads the same state the digest does.

### Two things, not one

"Procedural" settles the frame, not the picture. Splitting them is what makes
this tractable:

| part | source |
|---|---|
| frame, name, cost, stats, traits, rules text, keywords | drawn from card data, live |
| the illustration in the art box | still an image file |

So procedural rendering narrows the image question to the art box; it does not
remove it. Images are the client's responsibility for now, and move to
`Marvel.Server` when that exists. A user-supplied art pack is explicitly not a
requirement yet.

### Why this is cheaper than it sounds

`data/cards.json` already carries everything the renderer needs, for all 4,344
cards: `name`, `subname`, `type`, `faction`, `traits`, the full `stats` block,
marked-up `text` and `text_plain`, `flavor` and `errata` — see
[card-dataset.md](card-dataset.md#cardsjson). The renderer's inputs exist and are
already regenerated and hash-pinned on every `tools.cards.extract` run.

What does not exist is layout, a frame per card type and faction, and the icon
glyphs.

### Consequences for the view layer

- `Marvel.View` owns card layout, and layout is its largest single piece of work.
  This is the decision the doc warned should be made "early rather than
  discovered late," and it is why it is recorded before that assembly exists.
- The renderer consumes **current** state, so it reads from the same place the
  affordance list does (MARVEL-161) rather than from static card data alone.
- Localisation is **not** a requirement now. Procedural rendering happens to make
  it a string table rather than a second art set, which is a free option kept
  open rather than a goal.

## Costs and open questions

Card art: **decided, see [How cards are drawn](#how-cards-are-drawn).**

Phase 6 gets much bigger. "Reconnecting the existing web client" becomes
"building a game client". That is the actual goal, so the cost is worth paying,
but it should be re-scoped openly rather than absorbed.

Keep a non-Godot driver permanently. A console driver over the engine, living in
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
| MARVEL-145 — The server does not decide what each seat sees | Backlog | The Python half is genuinely dead: `py_src` is never served. But the requirement is live again, because `Marvel.Server` runs as a container. Close this Python issue, and re-file the requirement against the C# server. |
| MARVEL-153 — The web server serves arbitrary files and a cheat console | Backlog | Same. `migration.md` already states the carry-forward constraint: do not port `/read_file` or the cheat console onto a served surface. Close as recorded. |
| MARVEL-146, MARVEL-152 | Cancelled | Already cancelled. No action. |

### New issues to create

All in the `Foundations` or `Engine Core` module. Layer label `engine` unless
noted.

1. Decide the presentation layer, and record it. Module `Foundations`, labels
   `docs` and `spike`, priority Urgent. Acceptance is this document landed as
   `docs/presentation-layer.md` with a pointer added from `migration.md` where it
   currently assumes ASP.NET Core. Supersedes the frontend half of MARVEL-3.

2. Extend the engine signature with a semantic event stream. Module `Engine Core`,
   priority High, blocked by the presentation decision. This must land before the
   interpreter exists. Retrofitting it after 3,457 card ports is not viable.

3. Model the prompt as anchored affordances. Module `Engine Core`, priority High.
   Coordinate explicitly with MARVEL-41 so the label contract is designed once.

4. Prove the engine assemblies cannot reference Godot. Module `Foundations`,
   labels `tooling` and `dx`, priority Medium. A `ci.yml` gate next to the
   fixture staleness checks. **Filed as MARVEL-162; done.** It landed as an
   MSBuild gate rather than a CI-only one, so it fails a local build too, and it
   grew a second target for the runtime floor and a proof that both fire.

5. Verify the event stream against the corpus. Module `Corpus and Oracle`, label
   `testing`, priority Medium, blocked by issue 2 and by MARVEL-158. **Filed as
   MARVEL-163; done.** 100% including position, and it found two defects: an
   area needs an identity, and a landing index describes the area the step
   leaves. See [event-stream.md](event-stream.md#verified-against-engine-state).

6. Verify affordance completeness against the corpus. Module `Corpus and Oracle`,
   label `testing`, priority Medium, blocked by issue 3 and by MARVEL-158.
   **Filed as MARVEL-164; done.** See
   [affordances.md](affordances.md#verified-against-the-corpus).

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

Inside phase 4, the engine's signature should be settled before the interpreter is
written:

```
(state, input) -> (state, Affordance[], GameEvent[])
```

Getting that right now is nearly free. Getting it right later is not.

Godot itself comes last, and it should not open until the engine is playable from
a console.
