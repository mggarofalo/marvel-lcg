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

**The transcript states the choice and the targets; the payment follows from the
hand.** There is no step that says how much was spent — the runner delegates to
`BotCommand.BuildPayment`, which walks the hand in engine order. For a fixed
cost that is unambiguous and several files lean on it. For a cost whose *size*
is the effect — a printed X, or "spend up to N" — it means **the hand is the
payment**: everything on offer is spent, up to the ceiling, so a hand of the
card plus two fillers is X = 2. That is what makes `14006`, `22010`, `58018` and
`26022` specifiable at all (MARVEL-135), and it is why their scenarios set the
hand exactly and differ from each other by one card. MARVEL-136 asks for a step
that states an amount outright; until it lands, say it with the hand and say so
in the header.

## Naming cards

Scenarios name cards by **printed name**; the runner resolves. Object ids never
appear in a spec: `CommandDescriptor` is object-id based and `ChoiceOne` remaps
ids through `FindNewEffectId`, so an id in a scenario would be meaningless
across runs.

| Written | Means |
|---|---|
| `"Rhino"` | by printed name |
| `"01094"` | by card id, when the name is ambiguous |
| `"me"` / `"I"` | seat 1's identity, whichever form it is in |
| `"the main scheme"` | the main scheme in play |
| `"Rhino in VillainArea"` | qualified by zone |
| `"01005 #2"` | the second copy the scenario created |
| `"Drone Minion"` | by the face the card is presenting right now |

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

### A card can answer to two names at once

A ref matches the card's **printed** faces *and* the face it is presenting right
now. Those are the same thing for almost every card. They come apart when the
engine puts one card's face onto another card's object: `Card.SetAsCard` without
`remove_legacy` replaces `card.face` and leaves `printed_faces` alone.

The facedown DRONE is the case that matters. `Enemies.PutYouDeckTopCardAsFacedownMinion`
takes the top card of a player's deck and stands it up as a minion the game
displays as **"Drone Minion"**, while the card underneath keeps being an Aunt
May. Both names are live, and a scenario has reason to use either — the printed
one says *which card left the deck*, the drone one says *what is now engaged
with the hero*:

```gherkin
Then "Aunt May" is in the "EngagedEnemiesArea"
And "Drone Minion" is in the "EngagedEnemiesArea"
And "Drone Minion" has 1 health
```

Until MARVEL-102 only the printed faces were indexed, so the harness refused a
name it was itself printing: the validator's error message for the minion
activation prompt lists "Drone Minion" among the legal targets, because that
message reads `card.face`. Same shape as MARVEL-94 — the harness printed a name
it would not accept.

Drones are not a corner. 01134 Ultron, 01144 Android Efficiency and 01138b
Assault on NORAD all make them, and the whole Ultron encounter set is built
around them.

Two things follow.

**A drone needs Ultron Drones in play.** A DRONE has no printed statistics of
its own — the Ultron Drones permanent is what gives it 1 hit point — so on a
puzzle board without it the drone enters play with 0 hit points and is defeated
in the same breath. There is then nothing to name, and the scenario reads as
though the card did nothing.

**Two drones on one board are ambiguous, and the ordinal resolves it.**
`"Drone Minion #1"` is the first card *the scenario created* that is presenting a
drone face — not the first one to become one. `#N` still means creation order and
nothing else. What is new is that the set it indexes **grows while the scene
runs**, because a card starts answering to a second name partway through; a
printed name's match set is fixed once the `Given` block has run. So an ordinal
over a current-face name is a claim about the board at that beat, and
`"Drone Minion #1"` and `"Aunt May #1"` may well be different cards. Where the
scenario has the underlying identity to hand, naming it is the more stable
spelling.

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

Because the label comes from the script and not the card, **you cannot derive it
by reading the printed text** — Sonic Boom's cost branch is
`Spend_[[energy]][[mental]][[physical]]`, and Crisis Interdiction's follow-up is
built as `ForChoiceAbility("")` and comes out named `Play`. Probe for it (below)
rather than guessing.

## The authoring loop

Write the scenario, run it, read the failure, fix. The validator's messages are
built to be instructive rather than merely diagnostic, so the loop converges
fast — but only if you start it before you have written four scenarios on a
guess.

**Probe first.** A scenario whose only assertion is a deliberately wrong
one-row table makes the engine print what it actually offered:

```gherkin
  Scenario: probe
    Given I am in alter-ego form
    When I choose "Futurist"
    Then I am prompted to choose one
      | probe |
```

```
the engine offered Futurist on Tony Stark (01029b) in HeroArea
    (missing 'probe'; unexpected 'futurist')
```

The same trick reads back zone names — assert `is in the "nonsense"` and the
failure names the real one (`HandsArea`, `PlayerDeck`, `DiscardPile`,
`EncounterDiscardPile`, `EncounterDeck`, `RemovedArea`, `ObligationsArea`).
Note `DiscardsArea` is *not* the player discard pile.

The validator also volunteers the step form when a decision needs a target:

```
Futurist has 3 legal targets (Repulsor Blast, Mark V Armor, Pepper Potts);
say which with 'targeting "<card>"'
```

**Three traps that cost real time**, each hit independently while authoring the
first core-set batch:

- **An option table must be complete, including options the card did not
  create.** A card just added to hand shows up as a `Play` option on the next
  menu, and omitting it fails the scenario for a reason unrelated to the card.
- **A lone remaining option is still asked.** The engine only skips the prompt
  when there is neither an option nor a target left to pick, so a one-row table
  and a `When` that answers it are usually still required.
- **`Given "<card>" is in play` runs the card's enter-play response** during
  setup and can strand the transcript mid-resolution. `Given "<card>" is
  revealed` likewise runs the whole reveal pipeline, which is why obligation
  scenarios legitimately open with a `Then I am prompted to choose one` and no
  preceding `When`.

## Options and targets are different things

**`I am prompted to choose one` asserts the option set, not the target set.**
When a card offers one action over several cards — `AskChooseFace`,
`AskDiscardFaces`, any `ForChoiceAbility` with multiple legal targets — the
prompt is a *single row* and the cards are its targets. Tony Stark's "look at
the top 3 cards of your deck" arrives as one option named `Futurist` with three
targets, so a prompt table says nothing at all about the three cards.

Two steps cover that half (MARVEL-94):

```gherkin
    Then the legal targets for "Futurist" are
      | Repulsor Blast |
      | Mark V Armor   |
      | Pepper Potts   |
    And I cannot choose "Futurist" targeting "Backflip"
```

`I cannot choose "<option>" targeting "<card>"` is `I cannot attack` generalised
— same assertion, same machinery, any option — and it is what a printed
restriction like Crisis Interdiction's "remove 2 threat from a **different**
scheme" needs. `the legal targets for "<option>" are` is the positive form, and
naming an option the engine is not offering fails as *unresolvable* rather than
passing vacuously.

Prefer both to the older workaround of building a board where the illegal
candidate is the only one and asserting it survived. That reads like full
coverage and is not.

A third step says **how many** of those targets may be taken (MARVEL-120):

```gherkin
    Then the target maximum for "Play" is 3
```

`the legal targets for` pins which cards are candidates and a `When` naming
three of them pins that three is reachable, so a printed "up to 3" was bounded
from below only. Naming a fourth is refused with `Play takes 1..3 target(s)` —
the engine being right, with no passing spelling for it.

It is an **equality**, and worded as one on purpose. "takes at most 3 targets"
is the obvious spelling and promises an inequality: it would be satisfied by an
engine with no maximum at all whenever the board offers fewer than three
candidates. A step whose text means something looser than its check is what a
second runner reimplements the other way.

It reads `target_num_range[1]`, which is the **effective** ceiling:
`Selector.GetTargetRange` clamps the printed maximum to the number of legal
targets on the board. Two consequences, both worth knowing before reaching for
it. The claim only bites on a board offering *more* candidates than the ceiling
— with three cards to choose from, an engine that had lost the maximum entirely
still answers 3, and the failure message says so when the number it found is the
candidate count. And for a selector with no printed maximum (`range=(1, "All")`,
which is what "each X you control" compiles to) the number is the board's rather
than the card's, which is a claim worth making in its own right:
`specs/rules/target-counts.feature` states both shapes side by side.

Three things the vocabulary still cannot say. Two are from the first core-set
batch: **"gains surge" is invisible from a `Given`-time reveal** — the surged
card stops in `DealtEncounterCardsDeck`, so surge needs a real villain phase with
the encounter deck stacked; and `"<card>" is in the "<zone>"` reports a zone
*type*, so in a multiplayer scenario it cannot say *whose* area a card reached.
The third is **"this cost cannot be paid from this hand"** (MARVEL-120). The
engine does not filter an unaffordable ability off the menu — Vision's
`Spend an [energy] resource` is offered with a hand of mental cards — and a
`When` that tries it is refused with "Action is offered but cannot be paid for",
which is `FAIL-spec-wrong`. Right behaviour, no passing spelling, so the
negative half of a coloured cost has to be carried by a resource-icon assertion
instead.

## A card's resource icons

`Then "<card>" has <n> "<icon>" resource icons` reads what a player card prints
in its corner — `physical`, `mental`, `energy` or `wild` (MARVEL-120).

It is the same kind of claim as `"<card>" has <n> health`: a printed attribute
the engine reads at play time. `RES` was the only one of those with no step, and
it is the only thing distinguishing four shipped ids — **Wakanda Forever!** is
01043a/b/c/d, one printed text and one script across all four, differing in the
icon alone. `Coverage.Equivalents()` rightly declines to credit one's scenarios
to the others over it, because a cost naming specific icons makes two printings
non-interchangeable at payment time, so the tool counted four cards of work
against a vocabulary that could express one claim.

Two things it deliberately does not do. It counts icons **printed**, not costs
payable — a wild icon pays a physical cost and this still answers 0 physical;
what an icon *buys* is observable the ordinary way, by playing something and
seeing what the engine took. And it reads `printed_resource_internal` rather than
the `printed_resource` property: that property is a
`Message.WhenCountingResourcesOnCards` query, and constructing a message
registers an object in the world, so reading it would make a snapshot mutate the
game it is snapshotting. Two cards in the corpus answer that query and nothing in
this vocabulary can reach either yet.

A card that cannot carry icons at all — anything that is not a `ClassCard` —
answers "not a player card" rather than zero, so a scenario asking a villain for
its icons is `FAIL-spec-wrong` rather than a vacuous pass.

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
  `my hand is`, `my deck is`, `player <n>'s deck is`, `"<card>" is in play`,
  `the main scheme has <n> threat`, damage/threat/counter/status setters, and
  `my deck at setup is` / `the encounter deck at setup is` for the one case
  where the deck has to exist before `GameSetup()` runs
- **When** — `I play`, `I choose`, `I attack`, `I thwart`, `I change form`,
  `I pass`, each optionally `targeting "<card>"`
- **Then** — `I am prompted to choose one` + table, `I am not prompted again`,
  `I cannot attack "<card>"`, `the target maximum for "<option>" is <n>`, card state
  (`has <n> damage`, `has <n> "<icon>" resource icons`,
  `is in the "<zone>"`, `is [not] stunned`), my state
  (`I have <n> cards in hand`), another player's state
  (`player <n> has <m> cards in their deck`), game state (`the game is over`,
  `it is round <n>`, `it is the villain phase`, `it is the "<phase>" phase`)

### "I" means player 1, everywhere

`the heroes are "<a>", "<b>"` builds a two-player game, and until MARVEL-101 the
second player was write-only: every deck-stocking step was first person, so the
second player's deck could not be given a known top card, and only
`player <n> has <m> cards in hand` was per-player on the `Then` side. A card that
says **"each player"** — 242 of them carry the phrase, 197 of which the engine
implements — could be set up but not asserted.

Two steps close it, and they are the general form of steps that already existed:

```gherkin
Given player 2's deck is "Pepper Potts", "Energy"
Then  player 2 has 2 cards in their deck
```

**The first-person steps are sugar for player 1.** `my deck is` compiles to the
same step, with the seat left at its default — one meaning, one code path, and
no way for two spellings of one thing to drift apart. Adding per-player forms
*alongside* the first-person ones was the alternative and it is worse on both
counts: two implementations of every zone step for the C# runner to reproduce,
and a catalogue growing a near-duplicate of each.

Sugar does change what an existing step means, so it is worth being precise
about what changed. `world.players` is **rotated by one at the end of every
round** to pass the first player token, and loses a player outright on
elimination, so `world.GetFirstPlayer()` names the token holder rather than a
seat; `const_seat_order_players` does not move. The first-person `Given` steps
used to route through `RunPuzzle`, which stocks `GetFirstPlayer()` — while the
first-person `Then` steps have always read seat 1. **The two halves already
disagreed**, and coincided only because a `Given` block cannot follow a `When`
and so never runs after a rotation. Making both mean seat 1 removes that
disagreement rather than adding a third reading, and it is observationally inert
for every scenario that can be written today.

Naming a seat the game does not have is an error that says how many there are.

**`"me"` as a card ref is seat 1 as well** (MARVEL-104). `"me"`, `"I"` and the
other spellings in `resolve.SELF_NAMES` name a *card*, and they read the same
seat list through the same `harness.SeatOf` call the zone steps use. MARVEL-101
did not reach this one, so it went on reading `GetFirstPlayer()` — evaluated
live, at the beat, rather than during setup. That is the one place the token
reading is reachable: a `Given` block cannot follow a `When`, but a `When` beat
can sit anywhere in a transcript.

The shortest case that shows it is two heroes walking into round 2. Both alter
egos pass, the villain schemes at each, the round ends and the token moves to
seat 2 — so round 2 opens with the *second* hero. Once that hero has passed and
the transcript is back on seat 1's turn:

```gherkin
When I choose "Change Form" on "me"
Then I am in hero form
```

Under the token reading those two lines are about different heroes: `"me"` is
seat 2, no offered option is bound to her card, and the `When` is refused while
the engine's own message lists `Change_Form on Peter Parker` among what it
offered — the harness declining a card it is itself printing, the MARVEL-94
shape again. Under seat 1 they are about one hero and the transcript means what
it reads as.

Choosing the seat over the token is what makes the two halves of a transcript
agree; a scenario that genuinely needs "whoever holds the first player token"
has no spelling today, and should get its own rather than borrowing this one.
Note that `"<card>" is in the "<zone>"` still reports a zone *type*, so it cannot
say whose area a card reached; in a two-player scenario, tell the two drones
apart by the printed identity of the card each was made from.

**The `Then` subject is seat 1 as well** (MARVEL-107) — the `I` in
`I am in hero form` and `I have 3 damage`, which is a third place the word is
resolved and was the last one not to name a seat. It picked the first
`is_identity` card out of `StateView.cards`, and that tuple is in **object-id
order**: `resolve.AllCards` sorts by object id so its results are stable, which
is what that function needs and is not what a seat is.

It named seat 1 anyway, on every board this engine can build. Identity cards are
the first things allocated during setup, one per seat in seat order, so the
lowest-id identity is seat 1's and the three readings coincided. That is a port
hazard rather than a Python bug, and the kind [migration.md](migration.md) says
the two engines have to be *made* to agree on rather than assumed to: an engine
that numbered identities by pack, by hero name, or alter-ego before hero would
move `I am in hero form` to another player while `I have 3 cards in hand` and
`"me"` stayed on seat 1. The scenario would pass in Python, fail in C#, and read
as an engine disagreement. It is the same thing MARVEL-42 already refuses to let
a scenario lean on for `#N`.

So the three readings are now one rule, and it is a **seat**:

| Written | Resolved by |
|---|---|
| `I have <n> cards in hand`, `my deck is` | `harness.SeatOf(world, 0)` |
| `"me"` as a card ref | `harness.SeatOf(world, 0)` |
| `I` as a `Then` subject | `StateView.players[0].identity_object_id` |

The `Then` side holds a `StateView` rather than a world, so it cannot call
`SeatOf`; what it reads instead is the same seat order, because
`StateView.players` is captured from `world.const_seat_order_players`. The one
thing missing from the view was **which card each seat's identity is**, and
`PlayerState.identity_object_id` now carries it. Giving every `CardState` an
owner or a seat was the alternative and is bigger for no gain: only identities
need the link, and a seat is already a thing the view has. Both forms are faces
of one card object, so the id is the same in either form and the link survives a
change of form mid-transcript.

**Seats are read by position, not by `player_id`.** `StateView.Player(n)` used to
scan for a `PlayerState` whose `player_id` matched, which is a fourth reading of
the same idea and was true only by coincidence — `World.__init__` passes its loop
index as `player_id` while it fills `const_seat_order_players`, so the number and
the position agree today. Position is the seat by construction, so that is what
is read; `unit_test/test_spec_harness.py` records the engine's numbering as an
observation rather than depending on it. `player 0` compiles to seat -1 and is
refused, which a plain index would have answered with the *last* seat.

Two first-person steps are **not** about a seat and are unchanged.
`I am prompted to choose one` and `I cannot attack "<card>"` read the options
the engine is offering at the decision in front of the transcript, so their "I"
is whoever the engine is asking. That is the right reading for them — a
transcript answers decisions in the order they are put — but it means a
two-player scenario can have `I am prompted` about seat 2 and `I am in hero
form` about seat 1 in adjacent lines. Say which hero you mean by name when that
matters.

Nothing about this is observable in a scenario you can write today, and it is
pinned as a **property test rather than a reproduction**, deliberately: object
ids ascend from 1, identities are allocated first, and a card a `Given` creates
later gets a higher id, so no board reachable from a spec makes the two orders
disagree. What the tests do instead is capture a real two-player board and
relabel it the way another allocator would number it — each seat keeps its own
hero, only the numbers move — and require that `I` still means seat 1.

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

### A deck step adds to the deck, it does not replace it

`my deck is` and `the encounter deck is` **accumulate**. A `Background` that
stocks one card and a scenario that stocks two leaves a deck of three, not two,
and two `the encounter deck is` lists interleave — which silently changes which
card a villain activation takes as its boost card.

```gherkin
  Background:
    Given my deck is "Backflip"

  Scenario: …
    Given my deck is "Backflip", "Backflip"
    Then I have 3 cards in my deck        # not 2
```

Nothing about that is wrong — a deck is stocked by naming what goes into it, and
the steps are the same steps wherever they appear. It is a trap only because a
`Background` reads like a default that a scenario overrides, and it does not.

**So stock a deck in one place.** If any scenario in a file needs its own deck,
every scenario in that file should stock its own and the `Background` should
stock none. `01134-ultron.feature` and `01138b-assault-on-norad.feature` do
exactly that, with a comment saying why: both depend on *which* card is the
boost card, and a `Background` card sitting on top of it would have moved the
answer without moving the scenario.

### A deck that has to exist *before* setup

`Given` is applied after `GameSetup()` returns. That is the right order for
almost everything — a board is built by putting cards on it, and the engine has
to have finished dealing before there is a board to put them on. It is the
wrong order for exactly one thing: a **setup ability**, which fires *during*
setup and reads a deck no `Given` has stocked yet.

```gherkin
Given my deck at setup is "Vibranium Suit", "Vibranium", "Vibranium"
And the encounter deck at setup is "Defense Network", "Armored Guard", "Armored Guard"
```

These are not `Given` steps that run earlier; they are part of the **scene** the
engine sets up from, alongside `the scenario is` and `the seed is`. That is why
they read as configuration rather than as board-building.

**49 cards need them.** The engine sends `Message.WhenCardSetup` from two places
inside `GameSetup()` — `world.py` step 12 for every main scheme and villain, and
step 16 for every identity — and 49 cards hang an ability off one of those that
*searches* a zone a puzzle scene leaves empty: 37 main schemes, 5 alter egos, 4
challenge cards, 2 Civil War leaders and 1 support. Three are in the core set,
and all three had the gap written into a spec file header as prose because there
was no way to say it in a scenario: **01040b** T'Challa's Foresight, **01116a**
Underground Distribution's Defense Network search, **01137a** The Crimson Cowl
putting Ultron Drones into play.

The other 41 setup abilities are fine without these steps, and the difference is
worth knowing: `SetupPutIntoPlay`, `SetupWithSetAside` and
`BeginGameWithSetAside` take literal card ids and `CardFactory.GenerateCards`
them, so Wolverine's Claws and Vision's mass form upgrade arrive on a puzzle
board with nothing stocked. Only the ones that go through
`SearchInternal.FindCards` need a deck to find.

Two things this spelling costs, both of them consequences of it being a real
deck rather than a stack the scenario placed:

- **Order is not preserved.** `player_setup.SelectIdentity` shuffles the player
  deck at setup step 6, and the encounter deck is shuffled the same way. These
  are a *set* of cards, not a stack — which is what a real game does, and why
  the step serves abilities that **search**. Stack the draw order with the
  ordinary `my deck is` in the same scenario when a beat needs it; the two
  accumulate into one deck.
- **The cards cannot be named by ordinal.** They are allocated during setup,
  before `MarkEngineBaseline`, so they are the engine's cards and not the
  scenario's — the same rule that refuses `"Rhino #2"`. Name them by printed
  name or id.

There used to be a third: *"give a searching hero at least two cards."*
**Ignore it if you see it repeated anywhere** — it was never a property of this
step. `SelectorEnd.DoShuffle` asserted its source deck was non-empty, so a hero
whose setup ability searched its own deck and shuffled afterwards raised when
the search emptied it, and `Log.OnCrash` swallowed the raise on a release build:
the card was left stranded in the processing area and the game carried on.
That is MARVEL-131 and it is fixed — a one-card setup deck is now an ordinary
board, pinned by `specs/cards/core/01040b-tchalla.feature`.

**Why the existing steps were not simply moved.** Making `my deck is` and `the
encounter deck is` mean "this is the deck", full stop, is the obvious fix and it
was measured before it was rejected: routing both verbs into the scene turns
**102 of the 411 then-passing scenarios red and fixes none of them**. Cards the
scene creates break every `#N` ordinal over them; the setup shuffle destroys the
top-first order; and a card with the printed Setup keyword sitting in the deck
enters play at step 11 and moves the board under the transcript. Both orders are
real, so a scenario has to be able to pick.

## Expert is a scenario, not a flag

`data/scenarios/` holds 108 files, 52 of them `<name>_expert`, and **an expert
file is a different encounter deck with different villain stages** — not the
standard file with a switch thrown. The Break-In! prints it plainly: "Rhino (I)
and Rhino (II). (Rhino (II) and Rhino (III) instead for expert mode.)"

So the way to reach expert content is to name the expert scenario:

```gherkin
Given the scenario is "rhino_expert"
```

That board opens with **01095** in `VillainArea` at 15 hit points and **01096**
in `VillainDeck`, and defeating stage II advances stage III into play with its
printed 16 hit points and its Toughness keyword. Nothing else is needed. Of the
**46 villain ids that appear only in an expert scenario file**, 9 stand in the
villain area at setup and the other 37 are reached by defeating the stage before
them.

`Given the difficulty is expert` is a different thing and is easy to misread as
this one. It flips `campaign.expert`, which is what `Worlds.IsExpert` and the
`expert_mode_only` abilities on 25 card scripts read — on the **standard**
villain deck. That is a board no real game produces, and it will not put a
stage III anywhere: on the standard Rhino scene 01096 is in no zone at all, and
`Given "01096" is in play` leaves it in the encounter discard pile. Reach for
`the scenario is "<name>_expert"` unless you specifically mean "the expert rule
text, on the standard board".

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
