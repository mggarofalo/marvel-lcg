# Behavioral spec harness

How a card's printed text becomes an executable claim about the engine, and how
that claim earns the right to be trusted.

## Why this exists

The replay corpus is a good oracle for one question: *did this game reproduce?*
It cannot answer *does Swinging Web Kick deal 8 damage?* — a CRC mismatch is a
hex diff, not a sentence about the game.

Behavioral specs answer that second question, and they are what the C# engine
will be held to. But a spec authored from printed card text is a **guess** until
something checks it. The Python engine is the only thing that can check it, and
only while it is still the reference. So:

> A scenario authored from printed card text is not trusted until it passes
> against the running Python engine. A disagreement is never dismissed — it is
> triaged as either a spec bug or an engine bug, and both are worth finding.

That is differential spec extraction, and it is what `tools/spec/` implements.

## The two pieces

| | What it does | Entry point |
|---|---|---|
| **Harness** | Runs one scenario, reports what happened | `python -m tools.spec.run_case` |
| **Validation runner** | Runs the suite, assigns verdicts, keeps the quarantine | `python -m tools.spec.validate` |

Run both from `py_src/`. Every data path in the engine is relative to it.

```bash
cd py_src
.venv/Scripts/python.exe -m tools.spec.run_case specs/scenarios/
.venv/Scripts/python.exe -m tools.spec.validate
.venv/Scripts/python.exe -m tools.spec.validate --trusted-only    # the CI gate
```

## How a scenario runs

**Given** builds a board out of `game/puzzle/puzzle.py` `RunPuzzle` commands. A
puzzle scene starts with no encounter deck and no player deck, so the board
contains exactly what the scenario asks for and nothing else.

**When** selects an effect through the headless bot device
(`engine/device/manager/bot/`). The scenario's action goes through
`Controller.ChoiceOne` — validation, CRC, `replay.Push` — the same path a
browser POST takes. There is no shortcut around it.

**Then** asserts over readable state: health, damage, threat, zone, counters,
tokens, statuses, hand size, round, phase.

After the last `When`, the harness stops at the first decision the engine offers
that can be declined. A declinable decision means nothing is pending and the
action has fully resolved, so that is where the board is snapshotted.

A run costs about 9 ms after a 0.2 s engine boot, with a fresh world each time.

## Writing a scenario

Scenarios are `.feature` files under `py_src/specs/scenarios/`. Gherkin, because
the trusted suite has to outlive the Python engine: Reqnroll (the maintained
SpecFlow successor) binds the same step text to C# with `[Given(@"...")]`, so
the file that validates against Python today is the file the C# engine is held
to later.

```gherkin
Feature: Spider-Man basic actions

  Background:
    Given the scenario "rhino"
    And the hero "spider_man"

  Scenario: A basic attack deals the hero's ATK and exhausts them
    Given "01001a" is in hero form
    When the player attacks "Rhino in VillainArea"
    Then "Rhino in VillainArea" has 12 health
    And "01001a" is exhausted
```

Supported: `Feature`, `Background`, `Scenario`, `Scenario Outline` with
`Examples`, `Given`/`When`/`Then`/`And`/`But`, `@tags`, `#` comments. Doc
strings and data tables are not supported; a step that needs a list takes a
comma-separated one.

**A step that matches nothing is a parse error naming the line.** A scenario
compiles completely or not at all, so a typo can never become a silently skipped
assertion.

### Naming a card

Anywhere a step takes a card, it takes a *card reference*:

| Written | Means |
|---|---|
| `"01094"` | by card id |
| `"Rhino"` | by printed name |
| `"Rhino in VillainArea"` | qualified by zone |
| `"01005 #2"` | the second copy, in object-id order |
| `"01005 #2 in hand"` | both |

Zones are `DeckType` member names (`HandsArea`, `VillainArea`, `DiscardPile`,
`PlayerDeck`, `MainSchemesArea`, `EngagedEnemiesArea`, …). Common aliases work
too: `hand`, `deck`, `discard`, `play`, `encounter deck`.

**A reference that matches two cards is an error, not a first match.** A
scenario that says `"01005"` with two copies in play has not decided what it is
testing, and the harness says so with both candidates listed. The same applies
to an action with two legal targets and a scenario that names neither: guessing
would make the result depend on engine ordering rather than on the card.

### Step vocabulary

The same sentence can mean different things in different clauses — `"X" has 5
threat` sets the board under **Given** and asserts it under **Then** — so the
Gherkin keyword decides which table is consulted. `And`/`But` continue the
clause above them, as Gherkin defines.

#### Given — configuration

| Step |
|---|
| `the scenario "rhino"` |
| `the hero "spider_man"` |
| `the heroes "spider_man", "she_hulk"` |
| `the seed is 7` |
| `the difficulty is expert` |

#### Given — the board

| Step | Effect |
|---|---|
| `the hand contains "01005", "01007"` | generates cards into hand |
| `the player deck contains …` | |
| `the player discard pile contains …` | |
| `the encounter deck contains …` | |
| `the encounter discard pile contains …` | |
| `the set aside deck contains …` | |
| `"01094" has 3 damage` | |
| `"01094" is healed 2` | |
| `"01097b" has 5 threat` | sets the total, not a delta |
| `"01094" has 2 "attack" counters` | |
| `"01094" has 1 "threat" tokens` | |
| `"01094" is stunned` / `is confused` / `is tough` | |
| `"01001a" is exhausted` / `is ready` | |
| `"01005" is discarded` | |
| `"01101" is in play` / `is revealed` | may bring the card into the game |
| `"01001a" is in hero form` / `is in alter-ego form` | |
| `the player draws 2 cards` | |

Given is **declarative**. `RunPuzzle.Stun` is a toggle, so saying "is stunned"
about an already-stunned card would un-stun it; the harness checks first and
only acts when the board disagrees. The same for form changes.

Only `is in play` and `is revealed` may bring a *new* card into the game, and
only from a bare card id. Every other verb must name a card already on the
board, so a misspelled name is an error rather than a silently conjured card.
For several copies, fill a zone (`the encounter deck contains "01101", "01101"`)
and address them with `#1` / `#2`.

#### When

| Step |
|---|
| `the player attacks "<card>"` |
| `the player thwarts "<card>"` |
| `the player defends against "<card>"` |
| `the player changes form` |
| `the player plays "<card>"` |
| `the player plays "<card>" targeting "<card>"` |
| `the player chooses "<option>"` |
| `the player chooses "<option>" on "<card>"` |
| `the player chooses "<option>" targeting "<card>", "<card>"` |
| `the player chooses "<option>" on "<card>" targeting "<card>"` |
| `the player passes` |

`<option>` is the effect name the client renders — `Attack`, `Thwart`,
`Change_Form`, `Play`, or a card's own ability name. `on "<card>"` disambiguates
when several options share a name, which is normal for `Play`.

#### Then

| Step |
|---|
| `"<card>" has N health` / `N damage` / `N threat` |
| `"<card>" has N "<name>" counters` / `tokens` |
| `"<card>" is in the "<zone>"` |
| `"<card>" is [not] in play` / `exhausted` / `ready` / `stunned` / `confused` / `tough` |
| `"<card>" has N "<property>"` — any value in the engine's render info, e.g. `is_completed` |
| `the player has N cards in hand` / `in the deck` / `in the discard pile` |
| `player 2 has N cards in hand` |
| `the player is [not] eliminated` |
| `the game is [not] over` |
| `the players won` / `the players lost` |
| `it is round N` |

## Verdicts

`validate.py` assigns one verdict per scenario, from what the run observed
rather than from judgement:

| Verdict | What it means | Where it goes |
|---|---|---|
| `PASS` | every Then held | the trusted suite |
| `FAIL-spec-wrong` | the scenario could not be executed as written — a Given named a card that is not in the game, a When was never offered, a Then asked about something the board does not have | quarantine + triage |
| `FAIL-engine-suspected` | the scenario ran cleanly and a Then disagreed anyway | quarantine + triage |
| `ERROR` | the engine raised, or logged a failure it swallowed | quarantine + triage |

The split matters. **`FAIL-spec-wrong` means the engine never offered what the
scenario describes**, so the likeliest explanation is that the author misread
the card. **`FAIL-engine-suspected` means the engine did something** — every
Given applied, every When matched an offered option — and it disagrees with
printed text. That one is worth reading carefully.

`ERROR` catches a case the engine would otherwise hide: exceptions raised while
broadcasting a message are caught, logged and play continues. A scenario that
"passed" over a swallowed traceback has proved nothing, so a logged failure
demotes the verdict.

## The quarantine

`specs/trusted.json` is the trusted suite. It is written **only** by
`validate.py`, **only** from `PASS`, and every entry is pinned to the SHA-256 of
the scenario source it was validated against.

There is no flag that adds an entry by hand. Editing a scenario changes its hash
and drops it out of the trusted suite on the next `--trusted-only` run, which
exits non-zero. That is deliberate: a suite you can talk your way into is not an
oracle.

Everything else lands in `specs/quarantine.json` with its verdict and reason.

`specs/scenarios/known_disagreements.feature` is wrong on purpose — one scenario
per failing verdict. It is the proof the gate works. If any of it starts
passing, the harness has stopped telling the truth.

## Triage

```bash
python -m tools.spec.validate --triage triage.json
```

One record per disagreement, with what an adjudicator needs to call it: the
scenario as authored, the decisions the policy saw and what it answered at each,
the failing assertion with expected and actual, the board at the halt, and the
engine's own play-by-play for the run.

## Counts over time

`specs/history.jsonl` gains one line per validation run: totals per verdict and
the disagreement rate. A rising rate means the authoring process is drifting —
scenarios are being written faster than they are being checked, or from a
misunderstanding that is spreading.

```bash
python -m tools.spec.validate --check-drift 0.05   # fail if the rate rose >5pp
```

The timestamp in that file is a report field. It is not a gameplay input, and
nothing in the file feeds back into a run — the engine's determinism rules are
about what can change a game's outcome.

## Notes for the C# engine

The step vocabulary is the contract. Each step in the tables above maps to one
Reqnroll binding:

```csharp
[Given(@"the hand contains (.*)")]
public void GivenTheHandContains(string cards) { ... }
```

`SpecCase` (`tools/spec/case.py`) is the intermediate representation and is
JSON-serialisable, so a scenario can be handed to a C# runner without a Gherkin
parser in the loop if that turns out to be easier.

## Two engine details worth knowing

**`RunPuzzle.FindOrCreateFace` never searches the field.** Its `FindCardOnField`
call is commented out (`game/puzzle/puzzle.py:43`), so a bare
`Puzzle.Damage("01094", 3)` against the villain in play silently creates a
*second* Rhino in the aside deck and damages that one. The harness resolves card
references itself (`tools/spec/resolve.py`) and hands `RunPuzzle` an
already-resolved `CardFace`, which takes that path out of play without changing
engine code.

**`PuzzleHelper.Exec` is not used.** It `exec`s each command and rebinds every
card in the world for every command, which measured about 3× the cost of a
direct call at ~450 cards and gets worse as the card count grows. Given steps
call `RunPuzzle` methods directly. That is also what keeps the harness off the
engine's `exec`-based path, which the migration is trying to remove rather than
build on.
