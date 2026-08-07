# The state-digest (CRC) cross-engine contract

Tracked as `MARVEL-9`. Specified against commit `ee130e9` on 2026-08-06, engine
build `0.5.9.201`, CPython 3.13 on Windows 11. Every claim below was either read
from source at the cited line or measured against a live game; measured claims
say so.

## Why this exists

`World.CalculateCRC()` is the oracle. Every recorded replay step carries its
value, and on replay `engine/controller/module/replay.py` recomputes it and
compares. When the C# engine diverges, this is the thing that says *which card,
at which step*. That makes it a wire format, and wire formats have to be written
down.

Nothing wrote it down. `docs/migration.md` and `AGENTS.md` both point at it and
call it a "key-by-key diff", which is half right and misleading in the half that
is wrong: the keys are card ids, not card fields, and each card's value is a sum
that loses the fields entirely.

This document is what a C# implementer needs in order to reproduce the digest
byte for byte without reading the Python. Where the current implementation is
ambiguous or wrong, it says so rather than inventing an answer — findings `D1`
through `D12` at the end.

## What the digest actually is

**It is not a hash.** It is a dictionary from card `object_id` to one small
integer, serialised as a Python dict literal with spaces stripped. That integer
is either a negative sentinel meaning "this card is somewhere I only track
coarsely", or the **plain arithmetic sum** of the state fields on the card's
current face.

A real one, from `rhino / spider_man / seed 12345`, step 5 of 7:

```
{1:27,9:-2,37:-2,40:-4,42:-2,44:-3,45:-2,46:-2,47:-2,48:14,49:23,53:2,54:-4,56:-3,68:-4,72:-3,81:0}
```

Eighty-two cards existed at that moment. Seventeen appear.

| id | card | where | value | how the value is built |
|---|---|---|---|---|
| 1 | Peter Parker (`01001b`) | hero area | 27 | `ally_limit 3 + hand_size 6 + health 10 + k_first_player_token 1 + recover 3 + restricted_limit 2 + traits 1 + with_player 1` |
| 9, 37, 42, 45, 46, 47 | six cards | hand | −2 | sentinel |
| 44, 40 | Strength, First Aid | player deck top / bottom | −3 / −4 | sentinel |
| 48 | The Break-In! (`01097b`) | main scheme | 14 | `escalation_threat 1 + k_threat 5 + printed_stage 1 + target_threat 7` |
| 49 | Rhino (`01094`) | villain area | 23 | `attack 5 + health 14 + printed_stage 1 + scheme 1 + traits 2` |
| 53 | Charge (`01099`) | attached to Rhino | 2 | `boost_const 2` |
| 56, 68 | Hydra Mercenary, Advance | encounter discard top / bottom | −3 / −4 | sentinel |
| 54, 72 | two encounter cards | encounter deck bottom / top | −4 / −3 | sentinel |
| 81 | Tough | Rhino's status area | 0 | empty — **its presence is the information** |

The other sixty-five cards — thirty-two mid-player-deck, twenty-five
mid-encounter-deck, five set aside, one mid-discard, the villain deck, and the
rules pseudo-card — are absent from the dict entirely.

Read that table twice before porting anything. It is the whole contract in one
picture: coarse position for most cards, a lossy integer sum for cards in play,
and silence for everything else.

## Wire format

`py_src/game/world/world_render.py:123-132`

```python
def CalculateCRC(self) -> List[str]:
    world = self.world
    infosets: List[Dict[int, int]] = [{}, {}, {}]
    for id in world.object_manager.card_dict:
        if id != 0:
            card = world.object_manager.card_dict[id]
            info = card.GetCRC(recalculate=True)
            if info != -1:
                infosets[0] |= {id: info}
    return [str(x).replace(' ', '') for x in infosets]
```

**Serialisation.** `str()` of a `Dict[int, int]`, then every space removed. So
`{1: 27, 9: -2}` becomes `{1:27,9:-2}`. An empty dict is the two characters `{}`.
Keys carry no quotes; negative values carry a leading `-`. Comparison is **string
equality** (`replay.py:86-89`), so a port must reproduce this exact text, not an
equivalent structure.

**Key order is insertion order, which is ascending id.** `card_dict` is a plain
`dict` filled by `ObjectManager.AddObject` (`py_src/game/object/manager.py:54-73`)
from a counter that only increments, so insertion order and numeric order are the
same. Do not sort in the port and assume you have matched it — sort *because*
ascending order is the contract, and note that the two coincide only as long as
ids are allocated monotonically.

**Recording.** `Controller.ChoiceOne` calls `CalculateCRC()` once, before the
input is solicited (`py_src/engine/controller/controller.py:54`), and writes
**slot 0 only** into the step (`controller.py:336-341`). So the recorded digest
describes the state the player was looking at when they chose, not the state
after.

**Comparison.** `InputModule.GetReplayOperation`
(`py_src/engine/controller/module/replay.py:73-175`):

- An empty recorded `crc` logs `Miss CRC` and passes. There is no version gate on
  the digest itself; `Versions.check_sum = 0.5.7.156` (`engine/lib/version.py:81`)
  marks when the field was added but is only consulted for file checksums.
- Otherwise the recorded string is compared against all three slots. Any match
  passes.
- Puzzle scenes skip the check entirely, and `Scene.PrepareSave` strips `crc`
  from puzzle inputs before saving (`py_src/game/scene/scene.py:113-117`).
- One caller passes `check_crc=False` (`py_src/game/event/manager.py:253`). That
  is the fast-undo lookahead peeking at the next recorded input; it does not
  advance a step, so it is not a hole in the oracle.

**On mismatch** the module builds a table over the union of both key sets and
prints one row per differing card id: the recorded value, the current value, and
their signed difference (`replay.py:101-145`). Negative values print as `Hand`,
`Top`, `Btm`. This is the diff that makes the oracle useful — but it is per
*card*, not per field. It tells you card 49 moved from 23 to 21. It cannot tell
you whether that was two damage or one lost trait and one lost attack.

## Which cards appear, and as what

`py_src/game/card/card.py:173-186`

```python
def GetCRC(self, *, recalculate: bool=False) -> int:
    if self.IsInHand():
        crc = -2
    elif self.IsInDeck() and self.area.GetAll()[-1] == self.face:
        crc = -3
    elif self.IsInDeck() and self.area.GetAll()[0] == self.face:
        crc = -4
    elif self.IsOnField() or self.area.flags.is_status_area:
        if recalculate:
            self.face.GetRenderInfo() # Calc crc here
        crc = self.face.crc_value
    else:
        crc = -1
    return crc
```

Five outcomes, evaluated in order. `-1` means "omit this card from the dict".

The predicates are all area flags, defined once in
`py_src/game/deck/deck_type.py:3-182` and attached to each `Deck2` at
construction:

| predicate | flag | areas with it |
|---|---|---|
| `IsInHand()` | `is_in_hand` | `HandsArea` only |
| `IsInDeck()` | `is_deck` | `PlayerDeck`, `DiscardPile`, `AdditionalDeck`, `AdditionalDiscardPile`, `EncounterDeck`, `EncounterDiscardPile` |
| `IsOnField()` | `is_in_play` | `HeroArea`, `AlliesArea`, `SupportsArea`, `UpgradesArea`, `EngagedEnemiesArea`, `VillainArea`, `MainSchemesArea`, `SideSchemesArea`, `EnvironmentArea`, `EvidenceArea`, `RuleArea`, `ObligationsArea` |
| — | `is_status_area` | `StatusArea` (one per card, holds Tough / Stunned / Confused) |

Three consequences a port must get right:

**`-3` and `-4` mean "top and bottom of a pile", not "of a draw deck".** Discard
piles carry `is_deck`, so the top and bottom of every discard pile are in the
digest too. Measured above: cards 56 and 68 are the encounter discard pile's top
and bottom.

**Top is `cards[-1]`, bottom is `cards[0]`.** `Deck2.GetAll` returns the backing
list in that order (`py_src/game/deck/deck.py:369-378`), matching `GetTop` and
`GetBottom` at `deck.py:380-390`. A single-card pile hits the top test first and
is `-3`, never `-4`.

**Set-aside is not a deck.** `AsideDeck` has `is_set_aside` and `is_removed` but
*not* `is_deck` (`deck_type.py:91-95`), so set-aside cards are always omitted.
`AdditionalDeck` does carry `is_deck`. Do not treat "set aside" as one category.

Everything else — the middle of any pile, the villain deck, the removed area, the
victory display, the resources area, the processing and revealing areas, the
dealt-encounter queue, and the boosting area — is `-1` and absent. **The digest
therefore says nothing at all about deck order.** A shuffle that leaves the top
and bottom cards in place is invisible to it.

## The per-card value

Two steps: build a dict of named fields, then throw the names away and sum.

### Building the field dict

`py_src/game/card/face/card_face.py:312-326`

```python
def GetRenderInfo(self) -> Dict[str, int]:
    info = self.GetInfoDict()
    self.crc = {
        'is_exhaust': int(not self.card.state.is_ready),
        'traits': self.GetTraitsTotalCount(),
    } | info
    self.crc = {k: v for k, v in self.crc.items()
                if v != 0 and k != 'curr_ally_limit' and k != 'curr_restricted_limit'}
    ...
```

Note the two filters and what they are worth:

- Dropping zero-valued entries **does not change the sum**. It only shortens the
  dict that the debug UI renders. It is not part of the semantic contract.
- Dropping `curr_ally_limit` and `curr_restricted_limit` **does** change the sum.
  These are the only two fields deliberately excluded, and they are excluded by
  name from a base-class filter — an implementer will not find that by reading
  the class that produces them.

`self.crc` is assigned before the face-up and in-play checks that follow, so it
is populated for every face `GetRenderInfo` is called on, including face-down
ones. See `D8`.

### Summing

`py_src/game/card/face/card_face.py:181-187`

```python
@property
def crc_value(self) -> int:
    return sum(self.crc.values())
```

That is the whole digest value for one card. **Field names never reach the wire.**
Two cards with `attack 5, health 14` and `attack 14, health 5` produce the same
number, and so does one with `traits 19`.

### Every field that can contribute

Three sources merge into the dict. Values are always coerced to `int`: a string
becomes 1 if non-empty else 0, a list becomes its length, `None` becomes 0, a
bool becomes 0 or 1 (`has_attribute.py:54-66`).

**Unconditional**, added by `GetRenderInfo`:

| key | source | meaning |
|---|---|---|
| `is_exhaust` | `card.state.is_ready` | 1 when exhausted |
| `traits` | `GetTraitsTotalCount()` (`face/model/trait.py:106-110`) | total number of *sources* granting a trait, summed over current traits — not the number of distinct traits |

**From `CardFace.GetInfoDict`** (`card_face.py:328-336`), on every face:

| key | meaning |
|---|---|
| `treat_as_if_blank` | 1 when the face is treated as blank |
| `consider_as` | 1 when the face is considered as anything at all — collapses *what* it is considered as |
| `with_player` | owning player's `player_id + 1`, only when the area's owner is a player |

**From `HasAttribute.GetInfoDict`** (`attribute/has_attribute.py:54-66`), which
walks the keys each face registered with `RegisterInfoDict` and reads them with
`getattr`. All thirty-six, with the file that registers each:

| key | file | live or constant |
|---|---|---|
| `attack` | `attribute/can_attack.py:54` | live — `max(0, GetKeyword('ATK'))` |
| `thwart` | `attribute/can_thwart.py:29` | live — `max(0, GetKeyword('THW'))` |
| `defense` | `attribute/can_defense.py:20` | live — `max(0, GetKeyword('DEF'))` |
| `scheme` | `attribute/can_scheme.py:26` | live — `max(0, GetKeyword('SCH'))` |
| `hand_size` | `attribute/has_hand_size.py:11` | live — `max(0, GetKeyword('HS'))` |
| `recover` | `attribute/can_recover.py:19` | live — `GetKeyword('REC')`, **not clamped** |
| `toughness` `guard` `patrol` `villainous` `hazard` `crisis` `acceleration_icon` `amplify` `assault` `incite` `quickstrike` `retaliate` `surge` `steady` `stalwart` `peril` `restricted` `temporary` `vulnerable` `boost_const` | one `has_*.py` / `can_*.py` each under `attribute/` | live — bare `GetKeyword(...)`, **not clamped** |
| `permanent` | `attribute/has_permanent.py:11` | live — `GetKeyword('Permanent') > 0`, so 0 or 1 |
| `health` | `attribute/can_health.py:42` | live — current hit points, **can be negative** |
| `is_infinite_health` | `attribute/can_health.py:43` | constant per face |
| `teamwork` | `attribute/can_teamwork.py:16` | live — a list, contributes its length |
| `printed_stage` | `attribute/has_stage.py:29` | constant per face |
| `target_threat` `escalation_threat` `is_completed` | `card_type/scheme_main.py:23-25` | live |
| `attack_consequential_damage` `thwart_consequential_damage` | `card_type/ally.py:19-20` | constant per face |

**From the remaining `GetInfoDict` overrides**, which add keys directly:

| key pattern | file | meaning |
|---|---|---|
| `ally_limit`, `restricted_limit` | `card_type/identity.py:34-42` | live limits |
| `curr_ally_limit`, `curr_restricted_limit` | same | **filtered out before summing** |
| `engaged_with` | `card_type/minion.py:19-30` | engaged player's `player_id + 1`, 0 when not in play |
| `k_<token name>` | `attribute/can_place_token.py:80-87` | one key per token type on the card — measured: `k_threat` on a main scheme, `k_first_player_token` on an identity |
| `c_<counter name>` | `attribute/can_place_counter.py:103-110` | one key per counter type |
| `c_<counter name>_printed` | `attribute/has_uses.py:64-71` | the printed uses count, when the card has uses |
| `f_<form name>` | `attribute/has_form.py:12-17` | 1 for the face's form |
| `victory` | `attribute/has_victory.py:13-17` | printed victory points, constant per face |

Token and counter key *names* come from game data, so the key set is open-ended.
It does not matter for the sum, but it does mean a port cannot enumerate the
fields from a fixed schema.

`HasAttribute.SetPlayerNum` reverses `info_dict` as an explicit "Hack"
(`has_attribute.py:23-25`). That changes the order keys are inserted in and
nothing else — the sum is order-independent, so it is harmless. Do not port it.

## The `object_id` allocation contract

The dict keys are card object ids, so **the order cards are created in is part of
the wire format**. Get it wrong and every key shifts.

`py_src/game/object/object.py:8-14` — every `Object` takes its id at construction
from `world.object_manager`. `py_src/game/object/manager.py:22-73` — one counter
per category, cards starting at `-1` and pre-incremented, so **the first card
created has id 0**.

Rules a port must reproduce:

1. **One id per card, not per face.** A double-sided card is one `Card` holding
   several `CardFace` objects and consumes one id. Only the current face
   contributes to the digest.
2. **Linked cards are allocated before their parent.**
   `CardFactory.GenerateCard` (`py_src/game/card/factory.py:29-59`) builds each
   face, and inside that loop `create_linked_faces` recursively generates the
   linked cards into the aside deck — all before `Card(...)` runs at line 50. So
   a card's linked companions have *lower* ids than it does.
3. **Ids are never reused and never removed.** `Card.Destroy`
   (`py_src/game/card/card.py:752-757`) unhooks the card from its area but leaves
   it in `card_dict` forever. See `D10`.
4. **The counter is per `World`.** A new game or a replay from the start
   allocates the same ids again, which is why undo — which re-executes from the
   recorded input list — reproduces them.
5. **Card ids are insulated from config drift.** `docs/determinism-audit.md` F5
   measured `forced_effect` id allocation moving from 158 to 183 across
   configurations with the card digests unchanged, because effects and cards draw
   from separate counters. Effect ids do reach recorded commands, but
   `CommandDescriptor.FindNewEffectIdInternal`
   (`py_src/game/scene/replay/operation.py:24-51`) re-resolves them from the card
   id plus the effect's display name.
6. **Id 0 is skipped by the digest.** In practice that is the `rule_a,rule_b`
   pseudo-card created first by `EventManager.RegisterPlayRule`
   (`py_src/game/event/manager.py:137`) into `world.area_insert`, which is a
   `RemovedArea` (`py_src/game/world/world.py:66`) and would score `-1` anyway.
   Confirmed by measurement. See `D9`.

## The list of three

`CalculateCRC` returns three strings, and `GetReplayOperation` accepts a match
against any of them (`replay.py:86-89`), with a special case selecting slot 1 for
scenes at version `0.5.9.4` (`replay.py:96-99`).

**Slots 1 and 2 are always the empty dict.** Nothing in the current code ever
writes to `infosets[1]` or `infosets[2]`; they are constructed empty at
`world_render.py:125` and serialised untouched at line 132. Measured across all
seven steps of `rhino / spider_man / seed 12345`: slot 1 and slot 2 were `"{}"`
every time.

So the three-way comparison is really "does the recorded value match the one real
digest, or is it the literal string `{}`", and the `0.5.9.4` branch diffs the
recorded dict against an empty one — producing a table that reports every card as
having disappeared. This is vestigial. A port should implement one digest. See
`D1`.

## Findings

Ordered roughly by how much they matter to a cross-engine port.

### D1 — Two of the three slots are dead, and one branch depends on them

`py_src/game/world/world_render.py:125,132`, `py_src/engine/controller/module/replay.py:86-99`

Covered above. Two concrete hazards beyond the wasted structure: a recorded step
whose `crc` is literally `{}` passes against slot 1 no matter what the engine
computes, and the `0.5.9.4` path produces a diff table against nothing.

**For the port.** Implement a single digest string. If replay compatibility with
`0.5.9.4` scenes is needed, that needs its own investigation — the current code
does not provide it, it only appears to.

### D2 — The per-card value is a sum, so it collides by construction

`py_src/game/card/face/card_face.py:182-183`

`sum(self.crc.values())` over several dozen small integers. Any change that adds
*n* to one field and subtracts *n* from another is invisible. Concretely: a
minion gains +1 ATK and loses a trait; an ally takes 1 damage and gains 1
toughness; threat goes up 2 on a scheme whose target threat drops 2.

This is not a theoretical objection to a hash — it is a plain sum of quantities
that routinely move in opposite directions during a single ability resolution.
And because the diff table prints only the net delta, a collision does not merely
hide a divergence, it hides it *silently*.

**For the port.** Reproduce the sum exactly for corpus compatibility. Separately,
treat the digest as a coarse tripwire rather than proof of equality, and expect
to need a richer per-field digest before trusting convergence. See "What a v2
should carry".

### D3 — Negative field values collide with the sentinel space

`py_src/game/card/face/attribute/can_health.py:53`, and every unclamped
`GetKeyword` accessor listed in the field table

`health` can go negative — `UpdateHealth` comments "We use `Set` to make health
can be a negative number" (`can_health.py:530`) — and twenty-one other fields
return a bare `GetKeyword(...)` with no clamp. The mismatch printer already
concedes this: `replay.py:120-122` has the comment "This happens when a unit has
negative health" over a branch that stops trying to name the value.

A card in play whose fields sum to −2, −3 or −4 is indistinguishable from a card
in hand or at a pile boundary. Nothing prevents it.

**For the port.** Do not build the C# implementation around "negative means
position". Reproduce the numbers, but treat the sentinel encoding as an accident
of the current format, and flag it as the first thing a v2 should fix.

### D4 — The mismatch verdict is inverted outside test mode

`py_src/engine/controller/module/replay.py:169-172`

```python
if all(x for x in diff_ids if x in CRC_IGNORE_IDS.value):
    return replay_input, True   # accepted
else:
    return replay_input, False  # rejected
```

The generator filters `diff_ids` down to those *in* the ignore list.
`crc_ignore_ids` defaults to `[]` (`replay.py:10`), so the filtered sequence is
empty, and `all(<empty>)` is `True`. **A digest mismatch is accepted by default.**
The intended reading — "accept only if every differing id is ignorable" — needs
the membership test outside the comprehension.

The oracle is not currently broken by this, because the paths that matter never
reach the faulty line: when `Test.IsInTesting()` is true the function has already
returned a rejection at `replay.py:167`, and `-bot_verify` sets
`Test.is_in_test = True` before replaying
(`py_src/engine/device/manager/bot/runner.py:148`). Live play and any non-test
replay path do fall through to it.

**Follow-up issue.** Small fix, but it changes behaviour on a path the corpus
work will eventually use.

### D5 — Boost cards are excluded, despite a branch that exists to include them

`py_src/game/card/card.py:180`, `py_src/game/card/face/card_face.py:321-326`

`GetRenderInfo` has an explicit `elif self.IsInPlay() or self.card.area.flags.is_boost_area`
branch. But `BoostingArea` is `is_out_of_play` (`deck_type.py:136-139`), so
`GetCRC` reaches neither `IsOnField()` nor `is_status_area` and returns `-1`
before that branch can matter. Boost cards revealed during a villain activation
never enter the digest.

That is a real gap: boost icons and boost-star abilities change the outcome of an
attack, and the digest cannot see the cards that supplied them.

**For the port.** Match the current behaviour. Note the gap; it is a candidate
for v2.

### D6 — `GetAll()` appends removed cards, so `[-1]` is not always the pile top

`py_src/game/deck/deck.py:369-378`, `py_src/game/card/card.py:176-179`

`GetAll()` defaults to `include_removed=True` and concatenates `removed_cards`
after the real list. `GetCRC` calls it bare, so if a deck ever held removed
cards, `GetAll()[-1]` would be the last removed card rather than the top.

**Latent, not live.** The only code that populates `removed_cards` is attachment
detach (`py_src/game/card/face/attribute/can_attach.py:98`), reversed on reattach
(`can_attach.py:76`). Both operate on an `UpgradesArea` — in play, and not a
deck — so no `is_deck` area currently accumulates removed cards.

A related live consequence: a *detached* attachment sitting in an upgrade area's
`removed_cards` still reports `IsOnField()` — the flag belongs to the area, not
the list — and so still contributes a full value to the digest as though attached.

**For the port.** Read the deck list, not the deck list plus removed cards.

### D7 — Merge direction across `GetInfoDict` overrides is inconsistent

There are nine `GetInfoDict` definitions: the base plus eight overrides. Six of
the eight return `local | super()`, so the **base class wins** on a key
collision — `has_attribute.py:66`, `can_place_token.py:87`,
`can_place_counter.py:110`, `has_uses.py:71`, `has_form.py:17`,
`has_victory.py:17`. The other two return `super() | local`, so the **subclass
wins** — `card_type/identity.py:37`, `card_type/minion.py:28`. `GetRenderInfo`
itself writes `{is_exhaust, traits} | info`, so `info` would win over both.

Harmless today — the key sets are disjoint — but it means there is no rule to
port, only a set of accidents. Any future key collision would resolve differently
depending on which class introduced it.

**For the port.** Pick one direction, document it, and assert that keys are
disjoint rather than relying on merge order.

### D8 — Face-down cards in play contribute their hidden state

`py_src/game/card/face/card_face.py:315-322`

`self.crc` is assigned before the `if not self.IsFaceUp(): return {}` guard. The
*return value* of `GetRenderInfo` respects face-down — that is the client-facing
render info — but `self.crc`, which is what the digest reads, does not. A
face-down card in an in-play area contributes the full sum of its real
attributes.

This is a correctness question for a cross-engine oracle in two directions: it
puts hidden information into a value that is written into replay files, and it
means the digest constrains state that no player can observe.

**For the port.** Match it — the corpus depends on the numbers — but record it as
a decision to revisit, because a v2 digest that leaks hidden information is worse
than one that does not.

### D9 — Skipping id 0 is coupled to allocation order

`py_src/game/world/world_render.py:126`

`if id != 0` excludes whatever card was created first, not a card identified by
what it is. Today that is the `rule_a,rule_b` rules pseudo-card, which lives in a
`RemovedArea` and would score `-1` regardless — measured. The guard is
belt-and-braces now and a silent state-dropper if allocation order ever changes.

**For the port.** Exclude the rules card by identity, not by id.

### D10 — Destroyed cards stay in `card_dict` with a stale area pointer

`py_src/game/card/card.py:752-757`, `py_src/game/object/manager.py:75-81`

`Destroy()` removes the card from its area's list and unregisters its effects,
but never touches `card_dict`. The card's `area` attribute still points at the
area it was removed from, so its classification comes from a deck it is no longer
in. If that area is `is_in_play`, the card keeps contributing a value.

`Deck2.Destroy` is the only caller and is not on a normal gameplay path, so this
is not currently observed to fire.

**For the port.** Do not model "destroyed" as "still present with a stale
pointer".

### D11 — Status cards are presence-only, which the format does not make obvious

Measured: the Tough card on Rhino appears as `81:0`. Its field dict is empty, so
its value is zero, and the *only* information it carries is that the key exists.

This is easy to get wrong in a port that filters zero values on output, or that
represents "no state" as absence. **Key presence is semantic.** A card in a status
area must emit an entry even when its value is 0.

### D12 — Several fields are constant for the life of a face

`printed_stage`, `victory`, `is_infinite_health`,
`attack_consequential_damage`, `thwart_consequential_damage`, and every printed
value that no ability modifies, contribute a fixed offset that never changes.
They cannot detect divergence; they can only create collisions and inflate the
value.

Not a defect — the numbers must be reproduced — but it tells you what the digest
is not doing. How much of a card's value is dead weight varies by card type: in
the measured example only `printed_stage` was constant, one point of the main
scheme's 14 and one of Rhino's 23. A card carrying `victory`, infinite health, or
consequential damage carries more.

## What is presentation-only, and what a v2 should carry

The issue asks specifically what is included that should not be in a semantic
cross-engine contract. Three categories:

**Already excluded, correctly.** `curr_ally_limit` and `curr_restricted_limit`
(`card_face.py:319`) are derived counts of what is currently in play — recomputable
from the rest of the state, and excluded by name.

**Included but not semantic state:**

- The per-face constants of `D12`. These are card identity. A digest should carry
  card identity once, explicitly, not smeared into a sum.
- `traits` as a *count of sources* rather than a set. Gaining a trait you already
  have from a second source moves the digest; losing trait A while gaining trait B
  does not.
- `consider_as` collapsed to 0/1. Whether Ant-Man is considered a Giant is
  reduced to "is considered as something".

**Included and actively misleading:** the sentinel overloading of `D3`, and the
fact that the sum's field names are discarded (`D2`).

Everything else is genuine state and belongs in any successor format —
`is_exhaust`, current stats, tokens, counters, threat, damage, engagement, and
which player controls the card.

A v2 digest — out of scope here, but this is the input to that decision — would
carry, per card id: a stable card identity, a position enum (not a negative
number), and a *dictionary* of field values rather than their sum, so that the
existing diff table can name the field instead of printing a net delta. That is a
larger and better-typed payload; the corpus storage decision in
`docs/migration.md` already assumes gzip, which absorbs most of the cost.

## Reimplementation checklist

For a C# port targeting byte-identical output against the frozen corpus, in
dependency order:

1. **Allocate card ids identically.** One counter per world, first card is 0, one
   id per card not per face, linked cards before their parent, ids never reused.
   Everything else depends on this.
2. **Classify each card by area flag**, in the order of `GetCRC`: hand → pile top
   → pile bottom → in play or status → omit. Use the flag tables above; do not
   infer them from area names.
3. **Read pile top as the last element and bottom as the first**, from the deck
   list only, excluding removed cards (`D6`).
4. **Build the field dict** from `is_exhaust` + `traits` + the face's info dict,
   then remove `curr_ally_limit` and `curr_restricted_limit`. Do not bother
   filtering zeros; it does not affect the sum.
5. **Sum the values** with no clamping. Preserve negatives.
6. **Emit every non-omitted card**, including those whose value is 0 (`D11`).
7. **Serialise as `{id:value,...}` in ascending id order, no spaces**, empty dict
   as `{}`.
8. **Compare as strings.**
9. **Emit one digest, not three** (`D1`).

## Candidate follow-up issues

| Proposed | Severity | Summary |
|---|---|---|
| Fix the inverted CRC mismatch verdict | High | `D4`. `all(x for x in diff_ids if x in CRC_IGNORE_IDS)` accepts every mismatch when the ignore list is empty. Live play and non-test replay currently pass on divergence. |
| Design a v2 state digest for cross-engine use | High | `D2`, `D3`, `D8`, `D12`. Per-field values instead of a sum, an explicit position enum instead of negative sentinels, no hidden information. Must land before the corpus is frozen or it is a regeneration. |
| Drop the vestigial CRC slots 1 and 2 | Medium | `D1`. Remove the always-empty slots and the `0.5.9.4` special case, or establish what that case was for. |
| Include boost-area cards in the digest | Medium | `D5`. Boost cards change attack outcomes and are invisible to the oracle. Changes recorded digests, so it needs corpus regeneration. |
| Exclude the rules card by identity, not by id 0 | Low | `D9`. Removes a silent coupling between the digest and allocation order. |
| Read decks without `removed_cards` in `GetCRC` | Low | `D6`. Latent today; also stops detached attachments scoring as in play. |
| Normalise `GetInfoDict` merge direction | Low | `D7`. Eight overrides, two of which merge the other way round. |
| Remove destroyed cards from `card_dict` | Low | `D10`. Not on a live path, but a stale area pointer is a trap for the port. |

## How the measurements were taken

Environment pinned per `docs/determinism-audit.md`
(`PYTHONHASHSEED=0`, `PYTHONIOENCODING=utf-8`, `PYTHONDONTWRITEBYTECODE=1`).

```bash
cd py_src
uv venv --python 3.13
uv pip install -r requirements.lock
.venv/Scripts/python.exe -m tools.determinism.headless rhino spider_man 12345 20
```

That prints the per-step digest trace and the object allocation counts. The
worked example, the eighty-two-cards-seventeen-shown figure, the field
decompositions, and the "slots 1 and 2 are always `{}`" claim came from the same
run driven through `run_headless`'s `on_step` seam with a probe that additionally
recorded all three slots and each card's `crc_dict`, area and classification. The
`97fa1611b360d813…` trace digest matches the value recorded in the determinism
audit, so this is the same game that audit measured.
