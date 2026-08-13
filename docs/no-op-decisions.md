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

## The loose form: a loop that moves without going anywhere (MARVEL-99)

The guard above counts *consecutive identical* digests, which only sees a cycle
of period 1. A 321-case corpus run found the other shape. 21 cases failed and
**18 were `timeout-stall`**; 14 of those spent the entire 20,000-step
`bot_max_steps` budget and yielded nothing, at ~5.6% of throughput.

### What the loop is

Six of the fourteen were reproduced decision by decision (klaw 887,
master_mold_expert 287, rhino 411, sabretooth 275, spiral 107,
thanos_expert 62). Every one ends in the same unit:

```
19996 p0 WhenPlayerChooseAbility Response FORCED  cmd=(1, [66, 56])  digest A
19997 p0 WhenPlayerChooseAbility Response FORCED  cmd=(1, [66, 56])  digest B
19998 p0 WhenPlayerChooseAbility Response FORCED  cmd=(1, [66, 56])  digest A
19999 p0 WhenPlayerChooseAbility Response FORCED  cmd=(1, [66, 56])  digest B
      options: [Select_2_cards_to_swap, Cancel]
```

- **Unit**: one `WhenPlayerChooseAbility` offering exactly two options,
  `Select_2_cards_to_swap` and `Cancel`. The policy answers `Cancel`
  and the loop ends; it answers *swap* and the loop repeats.
- **Period**: **2**. Swap the two cards, swap them back. The digest alternates
  between exactly two values and never reaches a third.
- **Why `NoProgressError` does not fire**: a swap is a real state change, so
  every decision changes the digest and the consecutive-identical counter resets
  on every step. The board is genuinely different each time; it is only the *pair*
  of boards that repeats.

The loop is entered in round one or two — step 10 (spiral 107), 37 (sabretooth),
40 (master_mold), 44 (klaw), 75 (rhino), 153 (thanos) — and never left. Between
19,847 and 19,990 of each game's 20,000 decisions are the same prompt.

The `while True` it rides is `PlayerAction.SwapTheseCards`, the "put them back in
any order" mechanic: re-offer the swap until the player declines. Two cards call
it — Falcon's **Up, Up, and Away** (53005) and the SHIELD upgrade **Intelligence**
(50051) — and every one of the fourteen cases has falcon or nick_fury among its
heroes. That answers the question the scenario list raises: Klaw, Rhino, Thanos,
Spiral and Master Mold have nothing in common, and they did not need to.

### The fourteen are one cause, and so are the other four

They are also the *same* cause as the four `enchantress` stalls, which look
different only because they were caught rather than missed:

| Signature | Cases | Where it ended |
|---|---|---|
| `4083f456` | 14 | `bot_max_steps`, ~20,000 steps |
| `1b95a1b2` | 3 | `NoProgressError`, steps 65-162 |
| `ecd950fa` | 1 | `NoProgressError`, step 91 |

The two `enchantress` signatures are **one bug reported twice**. Their tracebacks
are identical except for one frame: `game/event/manager.py` line **1075** against
line **1076** — two adjacent calls to `ProcessOptionalEffect`. The crash signature
hashes the frames it travelled through, so a two-line difference in one function
splits one bug across two artefacts. Worth knowing before reading any signature
count as a bug count.

Traced, the enchantress loop is:

```
33 p1 WhenPlayerInTurn Normal FORCED n=9  cmd=(86, [1])  digest X
34 p1 WhenPlayerInTurn Normal FORCED n=9  cmd=(86, [1])  digest X
   options: [Ask, Attack, Thwart, Play, Play, Play, Play, Forced_Action, Alter-Ego_Action]
```

Same shape, period 1 instead of 2: a **forced** decision with **nine** legal
answers, and the policy picks the first one — `Ask`, the inert alter-ego action —
on every pass. It is forced because a `Forced_Action` is pending, so the turn
cannot be ended. The digest does not move, so `NoProgressGuard` catches it at 32.

### The cause

`FirstLegalPolicy` (and `NoOpAwarePolicy`) switched `RepeatGuard` off whenever
`decision.can_cancel` was False:

```python
repeats = self.guard.Update(decision)
if not decision.can_cancel:
    repeats = 0            # "a forced decision has one legal answer"
```

The reasoning is written down as *"End Turn recurs every turn with one legal
answer"* — and the code tests the wrong half of it. **"The engine will not accept
a cancel" is not "there is only one answer."** `PlayerAction.MayChooseOneAbility`
appends an explicit `Cancel` *ability* to the option list and then asks forced, so
the bot sees `can_cancel == False` alongside two selectable options, one of which
is the way out. With the guard off, `index` stayed 0 for ever.

Both stall shapes are that one line. Nothing about the scenarios, the cards or the
board was needed to explain either.

### Why not the other two fixes

- **The game.** `SwapTheseCards`'s unbounded `while True` is what the loop rides,
  and bounding it would end these fourteen. It would not end the four
  `enchantress` stalls, which involve no such loop — and the loop is not wrong:
  a human rearranges cards until satisfied and then presses Cancel. The engine
  offers a legal way out on every pass. It is the bot that never takes it.
- **The guard's window alone.** Generalising `NoProgressError` catches the loose
  form, but catching is not fixing: the case still yields no scene. It turns a
  20,000-step nothing into a 256-step nothing, which is worth having and is not
  the repair.

The policy is where a decision that is legal, available and ignored gets ignored,
so that is where it is fixed. See `RepeatOffset` in `bot/policies.py`.

### The backstop, generalised

`NoProgressGuard` now runs a second counter: **decisions since the last board
state the game had never been in before**. A cycle of any period stops producing
novel states once it closes, and the consecutive-identical run is the special case
where the period is 1. Like the original, it needs to know nothing about which
ability was offered.

Measured over **902 completed scenes and 105,633 decisions** from the corpus run
— every case that finished — the longest legitimate run with no novel state is
**10**:

| Decisions with no novel state | Occurrences |
|---|---|
| 0 | 41059 |
| 1 | 18142 |
| 2 | 7413 |
| 3 | 1366 |
| 4 | 227 |
| 5 | 50 |
| 6 | 5 |
| 7 | 1 |
| 10 | 1 |
| 11+ | 0 |

The longest is `project_wideawake` / valkyrie+spider_man / **seed 364**, which
reaches step 107 having not seen a new board state for 10 consecutive decisions,
and then plays on to a real result — 141 steps, `Players Lost`. That is the case
this limit has to survive: a legitimate game that repeats state for a while and
still completes. At 256 it clears it by a factor of 25.
`bot_stall_limit` defaults to **256**, twenty-five times the measured maximum and
still two orders of magnitude below the wall it replaces. The margin is wider than
the tight limit's eight-fold one because a revisited state is a weaker signal than
a frozen one.

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
