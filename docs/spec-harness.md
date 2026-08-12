# Behavioral spec harness

How a card's printed text becomes an executable claim about the engine, and how
that claim earns the right to be trusted.

The format decision this implements is [MARVEL-22](https://plane.wallingford.me);
read it before changing anything here.

## Why this exists

The replay corpus is a good oracle for one question: *did this game reproduce?*
It cannot answer *does Swinging Web Kick deal 8 damage?* — a CRC mismatch is a
hex diff, not a sentence about the game.

Behavioral specs answer the second question, and they are what the C# engine
will be held to. But a spec authored from printed card text is a **guess** until
something checks it. The Python engine is the only thing that can check it, and
only while it is still the reference. So:

> A scenario authored from printed card text is not trusted until it passes
> against the running Python engine. A disagreement is never dismissed — it is
> triaged as either a spec bug or an engine bug, and both are worth finding.

That is differential spec extraction, and it is what `tools/spec/` implements.

## A scenario is a transcript

The engine is a fold `(state, input) -> (state, prompt)`. A scenario is a
literal trace of that fold: **one `When` per decision**, with `Then`s
interleaved wherever the board is worth checking.

```gherkin
Feature: Nick Fury

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01084
  Scenario: damage is dealt to the chosen enemy, not the first one
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And "Shocker" is in play

    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |

    When I choose "Deal 4 damage to an enemy" targeting "Shocker"
    Then "Shocker" has 4 damage
    And "Rhino" has 0 damage
    And I am not prompted again
```

This is more verbose than "set up, act, assert", and the verbosity buys two
things nothing else can.

**`Then I am prompted to choose one` pins the question, not just the answer.**
The option set is behavior. Nick Fury is printed as a three-way choice, but the
engine offers *two* when no scheme has threat — "remove 2 threat" has no legal
target and is filtered out. A format that cannot say which options appeared
cannot catch that changing.

**`Then I am not prompted again` pins that the resolution is over.** `event_name`
discriminates a mid-resolution ask from the turn menu coming back around, so
"the card finished without asking me anything else" is checkable.

A format that batches actions and asserts once at the end encodes the number of
prompts *implicitly*. It passes against an engine that asks a different set of
questions and lands on the same final state. With 334 interaction-heavy cards to
author, that is the mid-project rewrite MARVEL-22 exists to prevent.

**The harness never answers a decision the transcript omits.** A mid-resolution
choice with no beat to answer it is `FAIL-spec-wrong`, not a silent pick. That
rule is the format's whole value; without it the two assertions above are
decoration.

Gherkin permits `When` after `Then`. One-`When`-per-scenario is a user-story BDD
convention, not a language or Reqnroll constraint, and it is the wrong
convention for an interactive rules engine.

## Layout

```
specs/cards/<pack>/<card_id>-<slug>.feature   one file per card
specs/rules/<topic>.feature                   rulebook behavior
specs/self-test/quarantine.feature            deliberately wrong, proves the gate
specs/steps.catalogue.json                    the closed vocabulary
specs/trusted.json                            generated: scenarios that passed
specs/quarantine.json                         generated: everything else
specs/history.jsonl                           generated: counts per run
```

Tag scenarios `@card:01084` so verdicts and coverage numbers join against the
card-text dataset by card id.

**Granularity: one scenario per decision path.** Not one per card, not one per
ability. `Scenario Outline` only where branches are genuinely symmetric. This is
also the unit the validation runner hands a single verdict to. Nick Fury needs
three; budget roughly 3–5 per interaction-heavy card.

## Running it

| | What it does | Entry point |
|---|---|---|
| **Harness** | Runs scenarios, reports what happened | `python -m tools.spec.run_case` |
| **Validation runner** | Assigns verdicts, keeps the quarantine | `python -m tools.spec.validate` |

Run both from `py_src/`. Every data path in the engine is relative to it.

```bash
cd py_src
.venv/Scripts/python.exe -m tools.spec.run_case specs/
.venv/Scripts/python.exe -m tools.spec.validate
.venv/Scripts/python.exe -m tools.spec.validate --trusted-only    # the CI gate
```

A run costs about 25 ms after a 0.2 s engine boot, with a fresh world each time.

## How a scenario runs

**Given** builds a board out of `game/puzzle/puzzle.py` `RunPuzzle` commands,
applied between `GameSetup()` and `GameLoop()`. A puzzle scene starts with no
encounter deck and no player deck, so the board contains exactly what the
scenario asks for and nothing else. Given is a block: it comes before the
transcript and cannot appear in the middle of one.

**The transcript** is played by a `BotPolicy`
(`engine/device/manager/bot/`). MARVEL-20 did not need a new engine seam — a
spec runner *is* a policy. Decisions go back through `DeviceManager.WhenInput`,
so `Controller.ChoiceOne` runs its normal validation, CRC and `replay.Push`
path. Choice, target and payment are one `CommandDescriptor`, so
`When I choose "X" targeting "Y"` is a single decision.

**Assertions are evaluated inside the policy**, at the decision that follows the
action. `decision.world` is the board right after the previous decision
resolved, and it is the only place an intermediate `Then` is observable — once
the engine unwinds, those states are gone.

## Naming cards

Scenarios name cards by **printed name**; the runner resolves. Object ids never
appear in a spec: `CommandDescriptor` is object-id based and `ChoiceOne` remaps
ids through `FindNewEffectId`, so an id in a scenario would be meaningless
across runs.

| Written | Means |
|---|---|
| `"Rhino"` | by printed name |
| `"01094"` | by card id, when the name is ambiguous |
| `"me"` / `"I"` | the identity, whichever form it is in |
| `"the main scheme"` | the main scheme in play |
| `"Rhino in VillainArea"` | qualified by zone |
| `"01005 #2"` | the second copy the scenario created |

Printed names collide across packs — five cards are called "Nick Fury" — so the
runner prefers ids from the set the scenario is playing, and honours `@card:`
tags. When that is still ambiguous it says so and asks for an id.

**A name that matches several cards, only one of which is on the board, means
the one on the board.** "Rhino" in a scenario about the fight is the Rhino in
play, not the stage-2 card in the villain deck. Two Rhinos actually in play is a
real ambiguity and is an error listing both.

Ambiguity is never resolved by guessing — including target selection. An effect
with two legal targets and a scenario that names neither is refused. A single
legal target is auto-selected by the engine itself and produces no prompt, so
naming it would be noise.

### Two copies of the same card

`#N` counts the matching copies **in the order the scenario created them** — the
order the `Given` lists them. Create both, then address them:

```gherkin
Given the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
And "Hydra Mercenary #1" is in play
And "Hydra Mercenary #2" is in play

When I attack "Hydra Mercenary #2"
Then "Hydra Mercenary #2" has 2 damage
And "Hydra Mercenary #1" has 0 damage
```

Creation order rather than position in a zone, because **position moves under a
shuffle and creation order does not**: measured on a four-card player deck,
`Deck2.Shuffle` moved two copies from positions [0, 2] to [2, 1] while their
object ids stayed [5, 7]. Shuffles are RNG-driven and the two engines do not
share an RNG yet (MARVEL-38), so a position-based ordinal would name a different
card in C# than in Python. Creation order also survives a card changing zones,
which is what lets `#1` and `#2` stay meaningful after both minions enter play.

Two rules keep that honest:

- **An ordinal may not index cards the scenario did not create.** The two cards
  named "Rhino" in a Rhino setup were both allocated by the engine, so `"Rhino
  #2"` would mean whichever the allocator reached second. It is an error telling
  you to name a zone. (A redundant ordinal on an already-narrowed ref, like
  `"Rhino #1 in VillainArea"`, is fine — it has nothing to choose between.)

- **A creating step may not act on the same card twice.** Given is declarative,
  so a second `"Hydra Mercenary" is in play` resolves to the card the first
  created; the scenario would read as two minions and run as one. Create both
  and use ordinals instead. This covers `is revealed` as well as `is in play`,
  and matters more there: `CardFace.Reveal` has no idempotency check, so a
  repeat re-runs the reveal pipeline and double-fires reveal triggers rather
  than quietly doing nothing.

## Option labels

The engine's option names are identifiers built from the label string in the
Python card script — `Deal_4_damage_to_an_enemy` — not from printed text. Both
sides are normalised (`_` → space, collapse whitespace, casefold) before
comparison, so **a scenario never asserts a raw engine identifier**. The C#
engine must expose the same domain-level labels; that is MARVEL-41.

## The step vocabulary

`specs/steps.catalogue.json` is the contract. `unit_test/test_spec_validate.py`
asserts `tools/spec/gherkin.py` implements exactly it — a form added on one side
without the other fails the build, so step-definition drift cannot rot silently.

The same strings bind to Reqnroll in C#:

```csharp
[Given(@"my hand is (.*)")]
public void GivenMyHandIs(string cards) { ... }
```

Read the catalogue for the current list. The shape:

- **Given** — `the scenario is`, `the hero is`, `I am in hero form`,
  `my hand is`, `my deck is`, `"<card>" is in play`,
  `the main scheme has <n> threat`, damage/threat/counter/status setters
- **When** — `I play`, `I choose`, `I attack`, `I thwart`, `I change form`,
  `I pass`, each optionally `targeting "<card>"`
- **Then** — `I am prompted to choose one` + table, `I am not prompted again`,
  `I cannot attack "<card>"`, card state (`has <n> damage`,
  `is in the "<zone>"`, `is [not] stunned`), my state
  (`I have <n> cards in hand`), game state (`the game is over`,
  `it is round <n>`, `it is the villain phase`, `it is the "<phase>" phase`)

A step that matches nothing is a parse error naming the line. A scenario
compiles completely or not at all.

### Asserting what the engine will not let you do

`I cannot attack "Rhino"` is the third assertion a transcript can make, and the
only one about something *not* being possible.

It exists because a restriction can be invisible to the other two. Guard is
"while this minion is engaged with you, you cannot attack the villain", and the
engine enforces it by emptying the `Attack` option's legal targets — the option
set is unchanged, so `I am prompted to choose one` sees nothing, and no card's
state changed, so no `Then` sees anything either. Stun works the same way: a
stunned hero is still offered `Attack`, with no legal target.

The step passes two ways, because "I cannot attack Rhino" is true either way:

| | |
|---|---|
| the action is not offered at all | an alter-ego has no `Attack` |
| the action is offered but will not take this card | Guard, stun |

**A card the scenario cannot resolve fails rather than passing.** "You cannot
attack a card that is not in this game" is true and worthless, and it is the one
way this step could pass while establishing nothing — so a misspelled name is
`FAIL-spec-wrong`, not a proven restriction.

Always write the control next to it. `specs/rules/status.feature` pairs "a
stunned hero cannot attack" with "a stunned hero can still thwart", because
without the second an engine that had forgotten how to do anything at all would
satisfy the first. `specs/self-test/quarantine.feature` carries the proof that
the step can fail.

### Phases come at two grains

The rulebook has three phases and the engine walks twelve `Phase.State`s, so
both are assertable and they mean different things:

| Written | Means |
|---|---|
| `it is the villain phase` | the rulebook's phase — `player`, `villain` or `end` |
| `it is the "Enemy Activation" phase` | one engine state, by name |

Reach for the rulebook grain by default. Reach for the quoted one to pin a
*transition*: "the villain phase" cannot tell threat placement from enemy
activation, and the order of those two is exactly the sort of thing a port gets
wrong. The mapping between them is `PHASE_GROUPS` in `tools/spec/state.py`, and
a `Phase.State` nobody classified raises rather than answering "no" — a
scenario must not fail for a reason that has nothing to do with the scenario.

## A scene has no decks, so a round cannot finish without them

The board holds exactly what the scenario asks for, which is the point — but the
end of a turn draws up to hand size and the villain phase deals an encounter
card. A scenario that ends a turn without stocking both does not walk the
phases, it ends the game: the hero is eliminated for an empty deck in round 1,
or the run stops with "There were no cards in either the encounter deck or the
encounter discard pile". Both are the real rule applied to an artificial board.
Any scenario that reaches the villain phase stocks `my deck is` and `the
encounter deck is` first; `specs/rules/phase-structure.feature` shows the shape.

## Decks are written top-first

`the encounter deck is "A", "B", "C"` puts **A** on top: the first card named is
the next one dealt, revealed or looked at.

The top is the only end the game has a name for. Effects say "look at the top
card of the encounter deck", "put this card on top of your deck", "reveal the
top card"; nothing addresses the bottom, and a deck is shuffled before play
anyway, so the bottom is not a position a scenario has any reason to describe.

This matters most during a villain activation, which takes two cards off the
top: the boost card first, then the encounter card that is dealt and revealed.
In a three-card list the first boosts, the second is revealed, and the third is
what a surge reaches.

Applies to `my deck is`, `my discard pile is`, `my set aside deck is`, `the
encounter deck is` and `the encounter discard pile is`. **Not** `my hand is` — a
hand has no top, and its order decides nothing but which copy is `#1`.

Two things follow that are worth keeping straight:

**Order does not survive a shuffle.** These stack a scene; they do not pin the
deck for the rest of the game. An encounter deck that runs out is reshuffled
from its discard pile, and after that the order is the RNG's. A scenario that
plays past a reshuffle must not depend on what comes next.

**`#N` is unaffected.** It still counts the copies in the order the scenario
*wrote* them, which is also the order they were created. The engine's list runs
the other way — `Deck.GetTop` is `cards[-1]` — so the harness creates the cards
in written order and restacks them afterwards (`StackTopFirst`). Reversing the
list handed to `RunPuzzle` instead would fix the draw order and silently
redefine `#1` as the last card written, which is why it does not do that.

It read bottom-first until MARVEL-82.

## Verdicts

| Verdict | What it means | Where it goes |
|---|---|---|
| `PASS` | every assertion held | the trusted suite |
| `FAIL-spec-wrong` | the scenario could not be executed as written — a Given named a card that is not in the game, a When was never offered, the transcript ended mid-resolution, a Then asked about something the board does not have | quarantine + triage |
| `FAIL-engine-suspected` | the transcript ran cleanly and an assertion disagreed anyway | quarantine + triage |
| `ERROR` | the engine raised, or logged a failure it swallowed | quarantine + triage |

The split matters. **`FAIL-spec-wrong` means the engine never offered what the
scenario describes**, so the likeliest explanation is that the author misread the
card. **`FAIL-engine-suspected` means the engine did something** and it
disagrees with printed text. That one is worth reading carefully.

`ERROR` catches a case the engine would otherwise hide: exceptions raised while
broadcasting a message are caught, logged and play continues. A scenario that
"passed" over a swallowed traceback has proved nothing, so a logged failure
demotes the verdict.

## The quarantine

`specs/trusted.json` is the trusted suite. It is written **only** by
`validate.py`, **only** from `PASS`, and every entry is pinned to the SHA-256 of
its scenario source.

There is no flag that adds an entry by hand. Editing a scenario changes its hash
and drops it out of the trusted suite on the next `--trusted-only` run, which
exits non-zero. That is deliberate: a suite you can talk your way into is not an
oracle.

`specs/self-test/quarantine.feature` is wrong on purpose — one scenario per
failing verdict, including a transcript that omits a mid-resolution choice. It
is the proof the gate works. If any of it starts passing, the harness has
stopped telling the truth.

## Triage

```bash
python -m tools.spec.validate --triage triage.json
```

One record per disagreement: the scenario as authored, the decisions the policy
saw and what it answered at each, the failing assertion with expected and
actual, the board at the halt, and the engine's own play-by-play.

## Counts over time

`specs/history.jsonl` gains one line per validation run: totals per verdict and
the disagreement rate. A rising rate means the authoring process is drifting.

```bash
python -m tools.spec.validate --check-drift 0.05   # fail if the rate rose >5pp
```

The timestamp there is a report field, not a gameplay input — nothing in the
file feeds back into a run.

## Two engine details worth knowing

**`RunPuzzle.FindOrCreateFace` never searches the field.** Its `FindCardOnField`
call is commented out (`game/puzzle/puzzle.py:43`), so a bare
`Puzzle.Damage("01094", 3)` against the villain in play silently creates a
*second* Rhino in the aside deck and damages that one. The harness resolves card
references itself (`tools/spec/resolve.py`) and hands `RunPuzzle` an
already-resolved `CardFace`. Tracked as MARVEL-51.

**`PuzzleHelper.Exec` is not used.** It `exec`s each command and rebinds every
card in the world for every command, which measured about 3× the cost of a
direct call at ~450 cards and worsens as the card count grows. It is also the
wrong interface, because it forces setup through strings and `c<object_id>`
references. Called directly, `RunPuzzle` takes real `CardFace` objects and the
object-id problem disappears.
