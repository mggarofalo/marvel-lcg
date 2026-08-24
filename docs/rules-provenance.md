# Rules provenance

How a rule gets into the engine, how it is proved, and how it is **patched** when
the published rules move.

This document exists because the migration plan had a hole. The oracle chain was:

```
printed card text → spec → validated against py_src → trusted.json → C# must match
```

and it has a blind spot AGENTS.md already names: a spec authored from ambiguous
printed words is checked against a Python engine implementing *the same reading
of the same words*, so it enters `trusted.json` having confirmed only that the
engine agrees with itself. MARVEL-143 patched that for cards, by harvesting
official rulings. Nothing patched it for **rules** — the Rules Reference is the
authority for phases, timing, keywords and damage, and it is not in this
repository at all.

The consequence is not hypothetical. Every rules spec under `py_src/specs/rules/`
currently asserts what the Python engine does, cross-checked against nothing.

## Three authorities

Everything the engine must be correct about comes from one of three published
sources. Each is vendored as a snapshot, each is diffable, and each can move.

| Authority | Dataset | Grain | Moves when |
|---|---|---|---|
| **Rules Reference** (FFG/Asmodee) | `datasets/rules-reference/` | rule entry | a new RR version is published |
| **Printed card text** (MarvelSDB) | `datasets/marvelsdb/` → `datasets/cards/` | card code | a new pack ships |
| **Official rulings** (MarvelCDB FAQ) | `datasets/marvelcdb-faq/` | ruling per card | rulings are added or revised |

All three now exist. The Rules Reference was the missing one until MARVEL-154; see [`datasets/rules-reference/UPSTREAM.md`](../datasets/rules-reference/UPSTREAM.md).

All three obey the existing rule: **nothing under `datasets/` may require the
network to regenerate.** Each is *vendored* — harvested once, pinned, read
offline.

## The Rules Reference as a citation index

Built in MARVEL-154. Implementing it changed one thing about this design, and
the change is worth recording: **the thing a spec pins to and the thing an
agent reads are not the same artefact.** Trying to make one serve both makes it
bad at each — a one-sentence fragment is right for a diff and useless for
adjudicating a rules question, and the full normative text of 261 entries is
not something to inline into a pin.

So the harvest emits both, from one parse, covered by the same hashes:

| | For | Grain |
|---|---|---|
| `index.json` | machines — spec pinning, `tools.rules.diff` | every citable unit |
| `entries/*.md` | agents and humans | one linked document per entry |
| `icons.json` | both | glyph legend, derived from the document |

Citations are three-tier, because the grain a spec argues from is not the
entry: `rr:cost` is the entry, `rr:cost.3` its third clause, `rr:cost.3.1` a
qualification of that clause. Each clause is anchored in the markdown, so a
citation resolves to a place in a document someone can actually read.

The index carries, per record:

```json
{
  "version": "1.5",
  "entries": [
    {
      "id": "damage.3",
      "path": ["Damage", "Excess damage"],
      "fragment": "Excess damage is not applied to another target.",
      "hash": "sha256:…"
    }
  ]
}
```

- **`id` is stable across versions.** It is the citation key specs bind to, and
  it must survive a reflow of the document. Derive it from the section path and
  the rule's position within it, not from a page number.
- **`fragment` is one sentence**, enough to make the citation legible in a diff
  and in a spec review. The full PDF stays local and gitignored; this index is
  what is committed.
- **`hash` covers the entry's normative text**, which is what makes the patch
  loop below computable.

The index is a vendored snapshot with its own `UPSTREAM.md` recording the RR
version and harvest date, on exactly the same footing as
[`datasets/marvelsdb/`](../datasets/marvelsdb/) and
[`datasets/marvelcdb-faq/`](../datasets/marvelcdb-faq/UPSTREAM.md).

## Provenance pinning

**This is the mechanism the whole document exists for.**

Today `specs/trusted.json` pins each trusted scenario to the hash of its own
source — edit the scenario and it drops out on the next validation run. That is
the right idea applied to one input out of four.

Extend the pin to **every authority the scenario derives from**:

| A scenario tagged | is pinned to |
|---|---|
| (always) | the hash of its own `.feature` source |
| `@card:01001a` | the printed-text hash of `01001a` in `datasets/cards/` |
| `@rr:damage.3` | the entry hash of `damage.3` in the RR index |
| `@ruling:01001a` | the hash of that card's rulings |

A trusted scenario is trusted **against a stated set of inputs**. When any of
them moves, the scenario leaves `trusted.json` automatically and returns to
triage. Nothing is trusted across a change to the thing it was authored from.

That single property is what makes all three patch paths below identical, and it
is why the answer to "a new rulebook came out" is a command rather than an
audit.

## The patch loop

The same four steps, whichever authority moved.

**1. Refresh the snapshot. It is a diff, not a sync.**

```bash
python -m tools.rules.harvest --check     # is the pinned RR still current?
python -m tools.cards.extract --check     # is the card dataset stale?
```

A refresh is a reviewable act. `datasets/marvelsdb/UPSTREAM.md` is already
explicit that refreshing changes printed text and therefore every spec authored
from it — that is a feature of this design, not a hazard, because the next step
tells you exactly which ones.

**2. Compute the affected set.**

```bash
python -m tools.rules.diff --from 1.5 --to 1.6
```

Entries added, removed, and changed. This is the whole of what moved.

**3. Compute the blast radius.**

```bash
python -m tools.rules.impact --changed impact.json
```

Which trusted scenarios cite an affected entry; which cards those scenarios
cover; which engine rules those scenarios are the only proof of. **The patch
scope is computed, not guessed** — that is the deliverable of this whole design.

**4. Re-adjudicate, then re-validate.**

The affected scenarios have already dropped out of `trusted.json` by pinning.
Each is re-read against the new rule text and either re-passes unchanged,
is edited, or becomes a `FAIL-engine-suspected` — a rules change the engine does
not yet implement. That last bucket is the patch list.

```bash
python -m tools.spec.validate --trusted-only    # the gate, unchanged
```

## Finding the rules errors you suspect are there

A citation index makes the question systematic instead of incidental. Once every
rules spec carries a citation:

```bash
python -m tools.rules.coverage              # the summary
python -m tools.rules.coverage --uncited --sort   # what to author next
python -m tools.rules.coverage --suspect    # cited rules whose spec fails
```

Built in MARVEL-154. It answers three things that could not be asked before it:

- **Which RR entries have no citing spec?** Unverified rules. This is the honest
  measure of how much of the rulebook the engine is actually proved against, and
  it will start out low.
- **Which specs cite no RR entry?** Assertions grounded in nothing but the
  Python engine's own behavior — the blind spot, made countable.
- **Which cited entries have a spec that fails?** Suspected engine rules errors,
  with a citation attached, which is the difference between a bug report and an
  argument.

Your expectation is that there are some errors and that they are not severe.
This is how you find out, rather than discovering them one at a time through
adversarial review of unrelated PRs.

### The baseline, measured

The first run, before any authoring:

```
  entries                  0 / 215   cited (0.0%)
  citable records          0 / 1071  cited (0.0%)
  ungrounded rules scenarios      88   assert the engine, cite nothing
```

**None of the rulebook was proved against anything.** That is not a surprise —
it is the blind spot this document opens by naming, now with a number on it.
The 88 are every scenario under `specs/rules/`: each passes, and each confirms
only that the engine agrees with itself.

`specs/rules/resource-icons.feature` was cited as the worked example, taking it
to 5 / 215. The remaining nine files are the obvious next work, and
`--uncited --sort` ranks what is left by clause count — `rr:keywords` (29),
`rr:attack-enemy-activation` (25), `rr:defend-defense` (24), `rr:cost` (20),
`rr:target` (20).

Citing one file also produced the first thing the loop caught. The header of
`resource-icons.feature` said resource icons are printed in a card's *top-left*
corner. `rr:resource.1` says bottom-left, and the Rules Reference is right.
Trivial in itself, and precisely the class of quiet error that a spec suite
grounded in nothing but the engine cannot surface.

## New expansions, new keywords

The same loop, entered from the card dataset instead of the RR.

A new pack lands in the MarvelSDB snapshot. `tools.cards.extract` regenerates
`datasets/cards/`. Provenance pinning drops any scenario whose cited card text
moved. Two new questions get answered by existing tooling:

- **New cards** appear in `tools.spec.coverage` as uncovered — they join the
  campaign at the tier their text earns.
- **New keywords and mechanics** are the case that matters. A keyword the engine
  has never seen has no RR entry cited by any spec and no implementation. It
  surfaces as an RR-coverage gap the moment the RR index is refreshed alongside
  the pack, which is the earliest anything could tell you.

MARVEL-144 — the pinned snapshot is a month behind, missing `jj` (43 cards) and
`luke_cage` (38) — is the first live exercise of this path, and should be worked
as the pilot rather than as a routine refresh.

## What this does not do

- **It does not make the RR machine-readable in a semantic sense.** Entries are
  citable and hashable, not executable. Nobody generates engine code from them.
- **It does not remove judgment.** A changed rule still has to be read by someone
  who then decides what the engine should do. What it removes is *searching* for
  what a change touched.
- **It does not replace the corpus.** Digest-compared replay answers "did these
  two engines do the same thing". Citations answer "is that thing correct". Both
  are needed and neither substitutes for the other.

## Sequencing

The C# milestone is **contracts first, then breadth**: MARVEL-8 (RNG) and
MARVEL-41 (Reqnroll spec runner) make both cross-language fixtures pass before
engine core is ported broadly.

Rules provenance sequences **beside** that, not after it, for one reason: the
citation tags land in spec sources, and every spec authored before the tags
exist is a spec that has to be revisited to add one. The cost of this work grows
with the size of the spec suite, and the suite is being written now.
