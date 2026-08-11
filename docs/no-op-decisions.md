# No-op decisions

Some abilities are offered to a player, recorded as a replay step, and legally
resolve without changing anything. A policy that always answers the same way
rides one of them forever while the step counter climbs — the failure MARVEL-37
was opened for.

This document is the inventory the issue asked for, the cross-check against the
web client's exclusion list, and the reasoning behind detecting the problem
structurally rather than by naming the abilities.

## How the inventory was measured

A saved scene records, per step, the decision that was asked and the state digest
at the moment it was asked. **A decision that changed nothing is one whose digest
equals the next step's.** No instrumentation is needed and it works on any corpus:

```bash
python -m tools.noop.scan replays/
```

The definition is operational, not semantic. It answers *"did this decision
advance the game"* — the question `NoProgressGuard` acts on — and does **not**
claim an ability can never change anything. A targeting sub-choice legitimately
changes nothing until the effect it belongs to resolves. Read the counts as "how
often this shape resolved to nothing".

The measurement below is 21 scenes across five scenarios at one, two, three and
four heroes, under the `first` policy: 2966 decision transitions, of which
**1031 (34.8%) left the digest unchanged**.

## The inventory

### Declines — 320 of 1031

A decision answered with no effect at all. Legitimately changes nothing, and the
single largest class.

| Count | Event |
|---|---|
| 187 | `WhenPlayerInTurn` |
| 132 | `WhenPlayerLikeInTurn` |
| 1 | `AfterEffectResolved` |
| 1 | `AfterUnitChangeForm` |

`WhenPlayerInTurn` is the main-turn prompt: declining ends the turn, which is
progress in the game's terms even though the board is untouched at the instant
the next decision is asked.

### An effect was chosen and nothing changed — 711 of 1031

| Count | Ability | Card | Name |
|---|---|---|---|
| 109 | `Ask` | 05001b | Kamala Khan |
| 87 | `Ask` | 08001b | Natasha Romanoff |
| 82 | `Ask` | 04001b | Clint Barton |
| 72 | `Ask` | 01010b | Carol Danvers |
| 59 | `Ask` | 01019b | Jennifer Walters |
| 51 | `Ask` | 09001b | Stephen Strange |
| 49 | `Ask` | 10001b | Bruce Banner |
| 30 | `Ask` | 01029b | Tony Stark |
| 29 | `Ask` | 01040b | T'Challa |
| 27 | `Ask` | 06001b | Odinson |
| 26 | `Ask` | 03001b | Steve Rogers |
| 8 | `Futurist` | 01029b | Tony Stark |
| 8 | `I_Can_Do_This_All_Day!` | 03001a | Captain America |
| 5 each | `Choose` | 01001b, 01010b, 01019b, 04001b, 05001b … | (alter-egos) |
| 4 | `Minion_Activates_Order` | 01010a | Captain Marvel |
| 3 | `Take_Damage` | 05001a | Ms. Marvel |

**`Ask` is 621 of the 711**, on every alter-ego in the corpus. It is the
multiplayer action that offers a teammate a chance to act and does nothing at all
if they decline — the exact ability MARVEL-37 names, confirmed to be the dominant
case rather than merely an example.

The others each have a reason of their own:

- **`Choose`** on an alter-ego is the prompt that precedes an ability, so it is
  the targeting-sub-choice case above rather than an inert ability.
- **`Minion_Activates_Order`** and **`Take_Damage`** are ordering choices: when
  the order picked happens to be the order that was already going to happen, the
  digest at the next decision is unchanged.
- **`Futurist`** and **`I_Can_Do_This_All_Day!`** are card abilities that can
  legally resolve to nothing.

## Why this inventory is not complete, and cannot be

The corpus above resolved **385 of 3781 card ids (10.2%)** and fired **98 of 303
`AbilityFactory` methods (32.3%)**. Two thirds of the ability surface has never
been exercised, so any ability list drawn from it is a lower bound. Generating
more games raises the number and never proves it final.

That is the argument for the design below: an enumeration is useful for
understanding the failure and useless as a guard.

## Cross-check: the web client's exclusion list

`public/js/marvel/auto_activate.ts`:

```ts
if (["Flip to alter-ego form", "Ask", "Change form", "Change Form", 'Defense'].includes(name)) {
    return false;
}
```

MARVEL-37 records this as "a starting point, not a proven-complete set", with the
implication that it and the bot's list should converge. **They should not, because
they are answering different questions.**

`isHasAutoActivate` vetoes *auto-clicking* when any of those options is on offer.
Flipping form, changing form and declaring a defense are the opposite of no-ops —
they are consequential and depend on player judgement, and auto-activating them
would take a real decision away from a human. The list is about **consequence**,
not inertness.

Only `Ask` appears on both lists, and only that entry appears for the same
underlying reason: it is offered constantly and usually does nothing.

The measured evidence agrees. Across the corpus a form change is not a no-op —
`AfterUnitChangeForm` appears once, as a decline — because flipping a hero changes
the digest by definition.

**Conclusion:** the client list is not an under-specified version of the bot's
inventory and should not be used as a seed for it. The one shared entry is a
coincidence of `Ask` being both inconsequential and ubiquitous.

## The detector

Two guards exist, and they work on different things.

`RepeatGuard` (`engine/device/manager/bot/policies.py`) works on **the question**.
When the same decision signature recurs inside a sliding window, `FirstLegalPolicy`
moves further down the option list. It is disabled for forced decisions, because
"End Turn" legitimately recurs every turn with one legal answer. A sliding window
rather than "same as last time" because the loops are two-player *cycles*, not
repeats.

`NoProgressGuard` (`engine/device/manager/bot/progress.py`) works on **the answer**.
The digest is the engine's own account of what changed, so a run of decisions that
all leave it identical has by definition made no progress — whatever ability was
offered, whether or not it is in the table above, and whether or not anyone has
enumerated it. It raises `NoProgressError`, which derives from
`EngineIntegrityError` so the broad handlers between the bot and the runner cannot
absorb it.

This is the answer to the issue's third question. The heuristic over questions
stays, because it lets a run *continue* past a no-op instead of dying at one. The
structural check is the backstop, because a heuristic that silently masks a real
infinite loop is worse than a crash during corpus generation.

### Choosing the limit

Measured over 44 scenes and 4759 decisions, the longest run of consecutive
decisions leaving the digest unchanged was **4**, with no tail:

| Run length | Occurrences |
|---|---|
| 1 | 751 |
| 2 | 321 |
| 3 | 58 |
| 4 | 8 |
| 5+ | 0 |

`bot_no_progress_limit` defaults to **32** — eight times the observed maximum, so
real play is not expected to approach it. It is deliberately loose: a false
positive aborts a generation run, while the failure it catches is unbounded and
will reach any threshold.

Measured effect, on a two-hero game with `-bot_repeat_window 0` (the naive policy
the issue describes): without the guard the game runs to the 3000-step cap and is
reported as a warning with no scene saved; with it, the run stops at **step 37**
and names the cycling decision.

### What it does not do

It does not fire for a human. A person may sit on an alter-ego "Ask" as long as
they like — the guard lives in `BotDeviceManager`, not in the engine, because only
an automated policy looping forever is an error.

It does not count rejected answers. `SupplyInput` runs again for the same step when
the engine refuses an answer, and a refused answer has not yet had its chance to
change anything; counting retries would fire the guard on a policy being corrected
rather than one that is spinning.

## Feeding MARVEL-10 and MARVEL-14

A policy that plays well needs to know that `Ask` is nearly always inert in the
position the bot finds it. The table above is the input, `tools/noop/scan.py`
regenerates it against a larger corpus, and `NoProgressGuard` is what makes it safe
to develop a new policy at all: a policy that stalls now fails in tens of steps
with the cycle printed, instead of thousands of steps later with a warning.
