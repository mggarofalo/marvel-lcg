# Runtime invariants

What must be true of the world at every decision the engine takes, why each rule
is checkable, and — for the ones with an edge — what state looks like a violation
and is not.

Implemented by [`py_src/game/world/invariants.py`](../py_src/game/world/invariants.py),
driven by `engine/controller/module/invariants.py`, pinned by
`py_src/unit_test/test_invariants.py`. Tracked as MARVEL-11.

## Why this is not the digest

[The state digest](state-digest-v2.md) records *what* the state is, so two runs can
be compared. It has no opinion about whether the state is **legal**. A run that
corrupts itself the same way every time reproduces perfectly and the oracle says
nothing — the corruption is in both the recording and the replay.

These rules say what legal means, so a single run can be caught in the act. The two
answer different questions and neither replaces the other:

| | question | fails when |
|---|---|---|
| digest | did this game reproduce? | two runs disagree |
| invariants | is this state possible? | one run reaches a state the rules forbid |

## Running it

```bash
python main.py -bot                        # on: the bot device checks by default
python main.py -device bot                 # on: same rule, whether or not you used the shorthand
python main.py -bot -no_check_invariants   # off: corpus generation, which has already paid for it once
python main.py -check_invariants           # on for a web session, for debugging
```

The bot device turns it on unless something has already said otherwise; every other
device leaves it off, where the cost buys nothing.

**It is not free.** Measured on this engine, at 85 cards:

| | checker off | on | |
|---|---|---|---|
| one 20000-decision game | 22.6s | 42.2s | **1.86×** |
| five ordinary games | 3.7s | 3.9s | 1.04× |

Roughly **0.8 ms per decision**, which is ~40% of the wall time of a decision-heavy
game. Short games hide it because the engine spends about two seconds loading the card
database before it plays at all. That is the number to weigh when deciding whether a
corpus run pays for it.

`ConfigVariables` note, because the obvious implementation does not work: the flag is
forced from the device in `Engine.Initialize`, **not** by adding `-check_invariants` to
the `bot` arg group. Expanding a group calls `InitVariable` for each of its keys
immediately, stamping `set_from = "CommandLine"`; the real command line is applied after
that loop, and `SetValue` returns early when `set_from` already matches. Put the flag in
the group and `-no_check_invariants` is silently discarded — the switch cannot be turned
off. Any other flag added to a group inherits the same trap; the root cause is MARVEL-64.

## When a rule breaks

The checker **aborts the game**. In order:

1. **Dump** the scene to `py_src/invariants/invariant-<scene>-step<n>-<rule>.json`,
   saved `deterministic=True` for the same reason a bot save is (MARVEL-27) — a repro
   carrying a host fingerprint and a timestamp is not one that can be committed or
   handed to someone else.
2. **Log** every violation found at that step, with the step index and the repro path.
3. **Raise** `InvariantViolation`, which derives from `core.errors.EngineIntegrityError`
   so `Log.OnCrash` re-raises it past the broad handlers in `EffectInvoker`,
   `Message2.Send` and `Engine.EngineRun`. The game is dropped, never saved, and the
   run exits non-zero.

The check runs in `Controller.ChoiceOne`, immediately after the step's digest is
computed — the same moment the digest describes. So the repro is a repro by
construction: reload the dumped scene, replay it, and the recorded inputs run out at
exactly the step that failed.

Aborting rather than playing on is deliberate. Once a rule is broken every later
decision is taken on a state already known to be wrong, so the reports that follow
describe the wreckage rather than the crash.

Both halves of that are checked end to end rather than asserted:

```bash
python -m tools.invariants.probe_repro          # exit 0 = aborted, and the repro reproduced
python -m tools.invariants.probe_repro --step 25 --seed 99
```

The probe injects a rule that fires at a chosen step of an ordinary bot game and then
asks the engine two questions. **Is the abort swallowed?** — the violation is raised
from inside `ChoiceOne`, which runs underneath handlers that catch broadly so one bad
card cannot end the game, and the identical claim was wrong once before (MARVEL-32,
`tools/determinism/probe_fabricated_input.py`). **Is the dump a repro?** — it asserts
the file holds exactly steps `0..n-1` and that replaying it fires the same rule at step
*n*.

## The rules

### Where a card is

| rule | statement |
|---|---|
| `zone/duplicate` | a card occupies at most one `(area, list)` slot |
| `zone/absent` | …and at least one |
| `zone/unclaimed` | the slot it occupies belongs to the area it names in `card.area` |

`Deck2.Insert` writes `card.area` and then edits the two lists, so the three facts can
disagree. The digest cannot see the disagreement: `_BuildPositionIndex` keeps whichever
slot it walked last and `_Record` falls back to an `/absent` zone, so a duplicated card
is recorded in one place and reproduces from the recording perfectly.

Areas are collected reflectively — anything with `cards`, `removed_cards` and
`deck_type` found on the world, the scenario, each player, each card's components, or
named by a `card.area`. A hardcoded list of attribute names would silently stop covering
a deck someone adds later, which is the failure mode this module exists to prevent.
Reaching the per-card decks matters: an upgrade or status deck that no card claims as
its `area` is reachable *only* through `card.components`.

`removed_cards` is one place, not two — the same distinction `digest.SUFFIX_REMOVED`
draws for a detached attachment.

### Which cards exist

| rule | statement |
|---|---|
| `identity/unregistered` | every card sitting in an area is in `object_manager.card_dict` |
| `identity/host` | `area.bind_card` resolves to a registered card |

The digest is built from `card_dict`, so a card that reached an area without being
registered can change the outcome of a game and never appear in a single recorded step.
`digest._Record` writes `area.bind_card.object_id` straight onto the wire, so an
unregistered host means an unresolvable id in the recording.

### What the numbers may be

| rule | statement | edge |
|---|---|---|
| `counters/negative` | every counter total ≥ 0 | |
| `tokens/negative` | every token total ≥ 0 | this is the **threat** floor: `Scheme2.threat` is `GetTokens('threat')` |
| `health/max-negative` | `max_health` ≥ 0 | |
| `health/over-max` | in play, `health` ≤ `max_health` | infinite-health cards are exempt — `HasHealth.health` reports 1 while `max_health` reports 0 |

Read off `card.components` rather than `GetStateFields`, which covers only cards in
play, in a status area or in a boost area. A counter that went negative in a discard
pile is still a bug and still reproduces.

**There is no threat ceiling.** A scheme is not capped at its threshold; it advances
when it reaches one, and being over it for the moment before that resolves is legal.

**There is no lower bound on health, anywhere.** That is a calibration result, not an
omission — both halves were tried and both fired on ordinary play:

- *In play*: `CanHealth.UpdateHealth` writes a negative value, and
  `TakeDamageWithOverkillTarget` then asks the first player for a "Simultaneous Overkill"
  order while the unit still stands at `health <= 0`. That decision goes through
  `ChoiceOne`, so the checker is looking straight at it.
- *Out of play*: the negative simply stays. `Card.MoveToArea` resets *ready* but not
  health, and `Health.OnParentReset` runs only from `Reset(is_flip=False)` — so a minion
  defeated by 2 overkill sits in the encounter discard pile at −2 until something puts it
  back into play. Caught on the first multiplayer calibration game.

Neither reaches the wire: `digest._Fields` returns nothing for a card off the field.

### Exhausted and ready

| rule | statement |
|---|---|
| `ready/exhausted-out-of-play` | a card outside an `is_in_play` area is ready |

`Card.MoveToArea` calls `ResetReady()` on the way out of play exactly so a card cannot
carry an exhausted state into a deck and back out again. An exhausted card in a hand or
a discard pile means something moved it without going through that path — and the digest
records `is_exhaust` for status and boost areas, so the wrong value reproduces.

### Hand size — tried and removed

| rule | statement |
|---|---|
| ~~`hand/over-limit`~~ | ~~at `Phase.State.PlaceThreat`, each live player's counted hand ≤ `player.hand_size`~~ |

**Removed under MARVEL-76.** It joins the health floor and the threat ceiling above: a
rule that fired on ordinary play.

The reasoning it was built on was that hand size is a limit **at particular moments**,
not a continuous bound — a hero draws past it, plays down from it, and is only required
to be at or under it after the discard step in `PlayerPhase.EndPhase`. `PlaceThreat`,
villain phase step one, looked like the first named moment after that and before a
single encounter card had been dealt.

Two things were wrong with it, and one game of four-hero Ultron found both:

- **`PlaceThreat` is a span, not an instant.** Encounter cards are dealt, minions engage,
  and every forced effect and response they trigger resolves while the phase state is
  still `PlaceThreat`. The checker runs from `ChoiceOne`, so it is looking straight at
  those mid-resolution states.
- **Any card that draws outside the end phase legitimately breaks the bound.** Thor's
  printed *"Have at thee!" — Response: After you engage a minion, draw 2 cards* took a
  legal hand of 4 against a hand size of 4 up to 6, and it stays there until the next
  end phase discards it down. There is no decision point in a round where the bound
  reliably holds.

The property itself is real — `MayDiscardHandCardsAndDrawUpToMax` computes the excess
and passes it as the *minimum* to `AskDiscardFaces`. But it is a **post-condition of that
operation**, not an invariant of the world, so that is where it now lives, asserted
directly after the discard and the draw. `unit_test/test_hand_size.py` covers it, and
`unit_test/test_invariants.py` fails if the rule reappears here.

This is also why the opt-out reading is gone. The engine asks whether a card counts by
sending `CheckIfFaceCountHandSize` per card, which a read-only checker cannot do, so the
rule had to read the ability's trigger class off `face.ability.abilities` and approximate
the answer. In its new home the engine's own `GetCountHandSizeFaces()` is available and
the approximation is unnecessary.

### Step and phase counters

| rule | statement | edge |
|---|---|---|
| `replay/step-count` | `replay.current_step_id == len(replay.history_inputs)` | |
| `progress/step` | the step counter never falls by more than one between decisions | `PlayerAction.AskChooseAbility` pops the recorded step when a chosen turn option fails to resolve (`player_action.py:364`), then asks again |
| `progress/round` | `world.round_id` never decreases | |
| `progress/phase` | `world.phase_id` never decreases | |

The step counter is **not monotone**, which is why `progress/step` is a bound and not an
equality. Nothing bounds the rise either: the debug console pushes its own operations
from inside `ChoiceOne`'s retry loop, so several steps can land between two decisions
without anything being wrong.

`replay/step-count` is the rule with teeth. The counter and the history move together —
`InputModule.Push` increments both, `Pop` decrements both — and every saved scene pairs
step *n* with `history_inputs[n]`. Drift means a digest recorded against a step was taken
at a different moment, which surfaces much later as an unexplainable mismatch.

The cross-decision rules live in a `Progress` object reset by `ControllerManager.Setup`,
which runs for a new game, a load, a replay and an undo alike.

## Read-only is a hard constraint

Nothing in these rules may send a `Message`, allocate an `Effect`, or touch the RNG. A
checker that perturbs the game changes the thing it is measuring and breaks the
determinism the corpus rests on — see [determinism-audit.md](determinism-audit.md).

That rules out some inviting engine helpers. `Player.GetCountHandSizeFaces` sends
`CheckIfFaceCountHandSize` once per card, so no rule here may call it — the hand-size
rule worked around that by reading the ability list directly, and the awkwardness was an
early sign it did not belong here (MARVEL-76). When adding a rule, check that every
accessor it calls is a plain read; if the natural accessor is not, that is evidence the
property is a post-condition of an operation rather than an invariant of the world.

## Adding a rule

A rule that fires on legal play is worse than no rule: it aborts a game that was fine and
it teaches everyone to ignore the checker. Before adding one:

1. Write it, with the passing *and* the failing case, in `unit_test/test_invariants.py`.
   Self-play can only ever show that nothing fired — it cannot show that a rule would
   fire, and it cannot show the sentinels are the ones intended.
2. Calibrate. Run bot games across several scenario, hero-count and policy combinations
   with the checker on. Anything that fires is either a real engine bug — which gets its
   own Plane issue — or a sentinel you had not thought of.
3. Write the sentinel down here. A sentinel that lives only in someone's head gets
   removed by the next person who reads the code.
