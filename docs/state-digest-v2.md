# The state digest, v2

Tracked as `MARVEL-44`. Specified against engine build `0.5.9.205`, CPython 3.13
on Windows 11. This supersedes [state-digest-contract.md](state-digest-contract.md),
which remains the specification of v1 — the format this replaced, and the only
thing that can read a scene saved before `0.5.9.205`.

Every claim with a number in it was measured by
`py_src/tools/determinism/probe_digest_v2.py`; re-run it rather than trusting
this document against a moved tree. How is at the end.

## Why there is a v2

The digest is the oracle. Every recorded replay step carries its value, and on
replay `engine/controller/module/replay.py` recomputes and compares. When the C#
engine diverges, this is the thing that says *which card, at which step*.

Writing v1 down surfaced four structural problems, and none of them were
fixable inside its shape:

**It was a sum, so it collided by construction.** Each card's value was
`sum(self.crc.values())` over a few dozen small integers. Any change that added
*n* to one field and subtracted *n* from another was invisible, and because the
mismatch table printed only the net delta, a collision did not merely hide a
divergence — it hid it silently.

**Negative values collided with the position sentinels.** `health` can go
negative and twenty-one other fields were unclamped, so a card in play could sum
to −2, −3 or −4 and become indistinguishable from a card in hand or at a pile
boundary.

**It described a fraction of the game.** Across the sampled boards below, v1
described **19 of 94 cards**. Everything else — the middle of every deck, boost
cards, set-aside — was absent, so the digest said nothing about deck order and
could not see the boost cards that decide how much damage a villain activation
deals.

**A third of each value was card identity.** Printed constants that no ability
modifies contributed a fixed offset that could never detect divergence, only
inflate the number and widen the collision surface.

Measured over 243 steps across five boards, v1 was blind to **half** of the
per-card state changes that happened in front of it:

| what happened to a card between two steps | count |
|---|---|
| v1's integer moved too | 17 |
| v1 omitted the card entirely | 9 |
| the card moved within a pile, which v1 tracked only as top / bottom | 7 |
| the card's fields changed and the sum cancelled | 1 |

The last row is the collision argument made concrete. It is small here because
the sampling policy declines every decision and so never plays a card — the buff
and damage traffic that makes offsetting changes common barely happens. It is
not zero.

This had to be decided before the corpus was frozen. Changing the digest after
generation invalidates the corpus, exactly as with the RNG (`MARVEL-38`). It was
free at the time it landed because `replays/` was empty.

## What the digest is

One JSON document per step, describing **every card in the world**. Not a hash:
the document itself is what gets recorded and compared, because a hash cannot be
diffed and the diff is the whole point.

Here is step 5 of `rhino / spider_man / seed 12345`, the same game and the same
step the v1 document worked through. Eighty-two cards existed; all eighty-two
appear. Five of them carry state:

```json
{"id":1,"card":"01001b","zone":"HeroArea","owner":0,"index":0,"host":-1,"face_up":true,
 "fields":{"ally_limit":3,"hand_size":6,"health":10,"is_exhaust":0,"is_infinite_health":0,
           "k_first_player_token":1,"k_threat":0,"recover":3,"restricted_limit":2,
           "retaliate":0,"stalwart":0,"steady":0,"surge":0,"t_GENIUS":1,"toughness":0,
           "vulnerable":0}}
{"id":48,"card":"01097b","zone":"MainSchemesArea","owner":-1,"index":0,"host":-1,"face_up":true,
 "fields":{"amplify":0,"assault":0,"escalation_threat":1,"hazard":0,"is_completed":0,
           "is_exhaust":0,"k_threat":5,"printed_stage":1,"surge":0,"target_threat":7}}
{"id":49,"card":"01094","zone":"VillainArea","owner":-1,"index":0,"host":-1,"face_up":true,
 "fields":{"attack":5,"health":14,"printed_stage":1,"scheme":1,"t_BRUTE":1,"t_CRIMINAL":1, …}}
{"id":53,"card":"01099","zone":"UpgradesArea","owner":-1,"index":0,"host":49,"face_up":true,
 "fields":{"boost_const":2, …}}
{"id":81,"card":"tough","zone":"StatusArea","owner":-1,"index":0,"host":49,"face_up":true,
 "fields":{"is_exhaust":0}}
```

Set that beside what v1 recorded for the same step:

```
{1:27,9:-2,37:-2,40:-4,42:-2,44:-3,45:-2,46:-2,47:-2,48:14,49:23,53:2,54:-4,56:-3,68:-4,72:-3,81:0}
```

`49:23` was `attack 5 + health 14 + printed_stage 1 + scheme 1 + traits 2`. Rhino
taking two damage and gaining a trait produced the same 23. Card 53's `2` was
Charge's boost value, and nothing recorded that it was attached to Rhino. Card 81
was the Tough status, whose entire content was that the key existed. And the
sixty-five cards v1 left out included the whole player deck and the whole
encounter deck, in whatever order the shuffle had left them.

The other seventy-seven cards in v2 carry position without state:

```json
{"id":9,"card":"01003","zone":"HandsArea","owner":0,"index":3,"host":-1,"face_up":false,"fields":{}}
{"id":44,"card":"01090","zone":"PlayerDeck","owner":0,"index":33,"host":-1,"face_up":false,"fields":{}}
{"id":72,"card":"01188","zone":"EncounterDeck","owner":-1,"index":26,"host":-1,"face_up":false,"fields":{}}
{"id":0,"card":"rule_a","zone":"RemovedArea","owner":-1,"index":0,"host":-1,"face_up":true,"fields":{}}
```

## What the digest cannot see

One known blind spot, recorded here because it is invisible by construction and
a reader will otherwise assume the digest is total.

**Which play area a card is in.** The digest's `owner` is the card's
*controller*, and every zone name maps to one shared ordered list.

The controller is **almost** the play area, and that is the dangerous part.
Rules Reference `ownership-and-control` §5 says a character that changes control
"is moved to its new controller's play area", so for characters the play area
follows control. Measured over 163,696 card records from twelve corpus scenes,
the two agree **98.1%** of the time.

They are not the same thing in either direction, and every exception is a named
rule:

| | rule | measured |
|---|---|---|
| engaged minions — in your play area, scenario-controlled | `rr:player-s-play-area` §3 | 1,412 records |
| encounter cards dealt to you — likewise | `rr:play-area` §1 | 264 records |
| player side schemes — yours, "placed next to the main scheme in the villain's play area" | `rr:player-side-scheme` §1 | `SideSchemesArea` holds controllers `-1`, `0` and `2` at once |
| upgrades on a card in the villain's play area — yours, not in your play area | `rr:player-s-play-area` §2 | 285 records |
| set-aside and removed cards — a controller, no play area at all | `rr:in-play-and-out-of-play` | 663 + 321 records |

**A 98%-accurate proxy is a worse failure mode than an obviously wrong one.** A
port that read `owner` as the play area would pass nearly everything and fail on
engaged minions, player side schemes and villain attachments — which are exactly
the cards where "whose is it" drives rules, and exactly the failures that surface
after the card ports rather than before.

For the scenarios below it carries no information at all. Every main scheme in
the corpus is scenario-controlled — `MainSchemesArea` is `{-1: 941}` — and
nobody controls a main scheme, so two main schemes in two different play areas
both record `owner: -1`.

Two examples, both published:

- **Protection Racket** (Fear No Evil): *"Each main scheme is in the play area
  of the player who chose it."* Two main schemes, two play areas, one
  `MainSchemesArea` in the digest — told apart only by `index`.
- **The Once and Future Kang**: each player creates their own **game area** at
  stage 3, and cards in one *"cannot affect another game area."*

Measured on the legacy engine, constructed at an ordinary Kang step because no
recorded step reaches the real split: creating a game area and moving 47 cards
into it, main scheme included, left the digest **byte-identical**.

So **a port that put every card in the right zone at the right index but in the
wrong play area would pass every corpus digest check.** The corpus cannot catch
it: 0 of 3,462 steps across all 42 Kang scenes reach a second game area, and
`py_src` does not implement Fear No Evil at all.

This is the same weakness `AreaRef.Id` exists to work around one level up — **the
digest describes areas rather than identifying them** — and it is stated rather
than fixed. `CARD_KEYS` is a frozen format; adding a key changes every recorded
digest and invalidates the corpus, which AGENTS.md non-negotiable 6 makes a
decision to raise. Filed as **MARVEL-174**; the modelling question is
MARVEL-175, and [event-stream.md](event-stream.md#play-areas-and-game-areas)
sets out what the rules actually require.

## The record

Eight keys, all present on every card, always in this order.

| key | type | meaning |
|---|---|---|
| `id` | int | the card's `object_id` |
| `card` | string | `face.paper.card_id` of the **current** face |
| `zone` | string | a `DeckType` member name, optionally suffixed |
| `owner` | int | controlling player, or `-1` for the scenario |
| `index` | int | position within the zone's ordered list |
| `host` | int | the card this one is attached to, or `-1` |
| `face_up` | bool | whether the card is face up |
| `fields` | object | named live state, code-point ordered |

### `id`

`game/object/object.py:8-14`, `game/object/manager.py:22-73`. Cards are numbered
from a per-world counter that is pre-incremented from `-1`, so the first card
created is `0`. **Allocation order is part of the wire format**, and the rules
that govern it are unchanged from v1 — they are set out in full under "The
`object_id` allocation contract" there, and a port must still reproduce them:
one id per card rather than per face, linked cards allocated before their
parent, ids never reused, the counter reset per world.

Two things did change. The v1 guard `if id != 0` is gone: it excluded whatever
card happened to be created first rather than a card identified by what it is,
which was a silent state-dropper waiting for allocation order to shift. And
`cards` is a JSON array in ascending id order, so the ordering is visible in the
document rather than inherited from a Python dict's insertion order.

### `card`

The printed card id of the face that is currently up, e.g. `01094` for Rhino,
`01001b` for Peter Parker's hero side.

This is new. v1 put no identity on the wire at all, so a diff could say `c49`
and no more, and a port whose card 49 was a different card entirely produced a
digest that looked plausible. Because the *current* face supplies it, flipping a
card is an ordinary change rather than a coincidence of two sums agreeing.

### `zone`

The `DeckType` enum member **name** — `HandsArea`, `PlayerDeck`, `VillainArea`,
`EncounterDiscardPile`, `StatusArea`, and so on
(`game/deck/deck_type.py:184-221`). Names rather than the enum's integers,
because a diff has to be readable; a closed enum rather than an area object id,
because area ids are allocation-dependent and carry no meaning.

Two suffixes:

- `<name>/removed` — the card is in that area's `removed_cards` list, which is
  where a detached attachment waits (`can_attach.py:98`). v1 read that list by
  accident: `GetAll()` appended it, so `[-1]` was not reliably the top of a pile.
  Here it is a place of its own and cannot be mistaken for one.
- `<name>/absent` — the card is in neither list. This should not happen; it is
  emitted rather than raised because an oracle that can crash while computing
  itself is worse than one with a visible anomaly.

**This replaces the sentinel encoding entirely.** There are no negative
positions, so there is nothing for a negative `health` to collide with.

### `owner`

The controlling player's `player_id` for a card in play, falling back to the
owner, and `-1` when that is the scenario (`game/card/card.py:204-230`).

v1 smeared this into the sum as `with_player` (`player_id + 1`, absent for
scenario-owned cards), so a change of control moved a number that moved for a
dozen other reasons too. `with_player` is dropped from `fields` because this
key supersedes it; it survives in `GetInfoDict` only because the debug render
panel shows it.

### `index`

The card's position in `area.cards`, or in `area.removed_cards` for a
`/removed` zone.

This is the largest single gain over v1, and it is free: every card already
needed an entry, and this is one more integer on it. v1 knew only "top",
"bottom" and "somewhere", so **a shuffle that left the top and bottom cards in
place was invisible to it**. Under v2 the whole of every pile's order is in the
digest, which matters because the shuffle is where two engines sharing an RNG
contract are most likely to part company, and where a late-surfacing divergence
is hardest to attribute.

### `host`

`area.bind_card.object_id` when the card sits in an area bound to another card —
upgrades, attachments, status areas — and `-1` otherwise.

v1 recorded that a Tough card existed and not whose it was. The status card's
value was `0` and its *presence* was the entire signal, which is easy for a port
to lose by treating "no state" as "no entry".

### `face_up`

Whether the card is face up.

`fields` is populated from the card's true state whether or not it is face up.
That is deliberate and it is the opposite of what v1's structure implied: v1
leaked hidden state by accident (`self.crc` was assigned before the face-up
guard in `GetRenderInfo`) while presenting itself as render info.

The reasoning is that **a differential oracle that cannot see hidden state
cannot catch a divergence at the step it happens, only at the step it surfaces**
— by which point the diff names a symptom several steps downstream. So the
digest records the truth and labels it, and the safety property is moved to
where it belongs: *the digest is never sent to a client*. What the browser
receives is `CardDescriptor.revision`, a `zlib.crc32` over the face-up guarded
render info (`game/card/card.py`), which is a strictly smaller leak than the v1
`crc` field it replaces.

The replay file is not a leak surface either: it already contains the seed and
every input, from which the whole hidden state is derivable.

### `fields`

Named live state, keys sorted by Unicode code point.

Populated for **every card, in every zone**. `{}` means the card has no
registered fields, never that the digest declined to look — a port must not
conflate the two.

v1 computed a value only for cards in play or in a status area. v2 first kept
that boundary and added boost areas — `GetRenderInfo` always had an
`is_boost_area` branch, but `GetCRC` returned `-1` before it could be reached,
so a boost card revealed during a villain activation never entered the digest
even though its icons changed the outcome of the attack.

`MARVEL-59` then removed the boundary entirely. It was inherited from v1 rather
than chosen, and it held because several `GetInfoDict` overrides *looked* unsafe
out of play. The audit found all nine total:

- **`Identity`** reads `GetControlByPlayer`, whose `isinstance(owner, Player)`
  assertion was the stated reason for the guard. It is satisfied out of play:
  the method consults the controller only `if self.IsInPlay()` and otherwise
  falls back to `GetOwner()` — and an identity card is always owned by a player.
  Measured by moving Peter Parker's hero side into the player deck: 16 fields,
  `ally_limit` 3.
- **`Minion`** already guarded itself on `IsInPlay()` and reports `engaged_with`
  as 0 out of play, so its key set does not change with the zone.
- **The base and the six attribute mixins** read printed values, registered
  attributes, tokens, counters and form. None consults a player.

Empirically, 1,003 out-of-play cards across the seven wide-matrix games, in nine
distinct zones, computed without raising.

What it buys is that a card modified before it leaves play — or while sitting in
a deck or set aside — is visible at the step it changes rather than at the step
it returns. **Every per-step digest changed**, which is why this had to land
before the corpus and not after.

The field set is `is_exhaust`, plus `GetInfoTraits()`, plus `GetInfoDict()`.

`GetInfoDict` is built by nine definitions down the class hierarchy, and they
are merged in one direction — **the more derived class wins** — by
`CardFace.MergeInfo`, which **refuses a key claimed by two of them** rather than
letting one silently win. A port needs the refusal more than the direction: two
classes owning one key would drop a field from the wire, and a missing field is
invisible in a diff in a way a changed one is not. The three namespaces in
`GetStateFields` are merged through the same guard. See `MARVEL-49`.

That set is v1's with three changes:

- **`traits` becomes `t_<TRAIT>` keys.** v1 recorded `GetTraitsTotalCount()`, a
  count of *sources*, so losing trait A while gaining trait B did not move it.
  `GetInfoTraits` (`game/card/face/model/trait.py:7-14`) already produced the
  named form for the render descriptor; the digest now uses it.
- **`with_player` is dropped**, superseded by `owner`.
- **`curr_ally_limit` and `curr_restricted_limit` stay excluded.** v1 excluded
  them by name with no stated reason. The reason is that `AllyLimit.CheckLimit`
  (`game/player/limit_monitor/ally_limit.py:50`) writes them at particular
  moments and can leave a mid-resolution value behind, so they pin *when an
  engine refreshes a cache* rather than what the state is. A correct port that
  computed the limit on demand would diverge on them.

**Printed constants stay in.** v1's finding D12 objected to them, and under a
sum the objection was right: they inflated the value and widened the collision
surface without ever being able to detect anything. Once fields are named that
harm disappears — a constant cannot collide with anything, it never appears in a
diff because it never changes, and it costs a few bytes that gzip removes. What
it buys is that both engines are held to parsing the card data the same way,
which is real shared-contract surface. So `printed_stage`, `victory`,
`is_infinite_health` and the consequential-damage pair are all present.

Zero-valued fields are emitted. v1 dropped them, which changed nothing about its
sum and was only ever there to shorten a debug panel. Emitting them makes the
**registered key set** part of the contract, so a port that forgets to register
`recover` fails on the key rather than passing by luck.

Token, counter and form keys (`k_<name>`, `c_<name>`, `f_<name>`) come from game
data, so the key set is open-ended and a port cannot enumerate it from a fixed
schema.

**No recording contains an `f_` key, or any hero face.** Every recorded game
stays in alter-ego form (`rr:identity.1`), so the C# `Hero` row in
`StateFields.Registered` is reasoned from the measured `AlterEgo` row rather
than read off a board, and `defense` is a key no recorded card has. Nothing
emits an `f_` key, because which card would carry it is unknown. Both gaps, and
what would close them, are in [forms.md](forms.md).

## Serialisation

`game/world/digest.py`.

```
{"v":2,"cards":[<record>,<record>,…]}
```

- **No whitespace.** `json.dumps(..., separators=(",", ":"))`.
- **ASCII only.** `ensure_ascii=True`. A port whose JSON writer disagrees
  reconciles afterwards — see
  [Reconciling two JSON writers](#reconciling-two-json-writers).
- **Fixed key order.** Top level `v` then `cards`; within a record, the eight
  keys in the table order; within `fields`, sorted by code point.
- **Cards ascending by `id`.**
- Integers in decimal with no sign on positives; booleans as `true` / `false`.
- The empty document is `{"v":2,"cards":[]}` — **not** the empty string. An
  absent digest and an empty one mean different things to the comparison.

### Reconciling two JSON writers

The original wording of the bullet above claimed `ensure_ascii=True` made a
non-ASCII trait "encode identically in every language." It does not, and the
correction is the reason this section exists.

**No two native JSON writers agree on an escape.** Python emits `\u001f`; .NET
emits `\u001F`. Hex case is not configurable on either side, and it applies to
every escape sequence. On top of that, `ensure_ascii=True` escapes every
non-ASCII character while .NET's relaxed encoder leaves most of them raw. So
byte equality is not reachable by configuration.

It is reachable by **normalising afterwards**, and that is what a port does.
Both differences are mechanical:

1. escape every non-ASCII UTF-16 code unit as lowercase `\uXXXX` — one escape
   per code unit, so an astral character becomes a surrogate pair, which is what
   `ensure_ascii=True` produces;
2. lowercase the hex of any escape the writer already emitted.

Two regex passes. Python is the definition and needs no normalisation of its
own; the port converges on it.

#### The one thing that makes this subtle

A string's *content* can read like an escape sequence. Python spells a literal
backslash `\\`, so the text `\u0041` appears in the document as `\\u0041` — and
a pass that scans for `\u` rewrites text it must not touch.

The fix is an alternation that consumes backslash pairs first:

```
\\\\|\\u([0-9a-fA-F]{4})
```

Because `\\\\` is tried first, a run of backslashes is always consumed in pairs
and a `\u` following an odd backslash is never treated as an escape. This was
the stated reason for believing the job needed a hand-written encoder. It does
not.

#### It was checked, not reasoned about

A fixture carried 420 cases: 20 hand-picked for the ways a normaliser breaks —
literal backslash before `u`, odd-length backslash runs, an escape adjacent to
an astral character, every C0 control — and 400 fuzzed over an alphabet of
backslashes, `u`, hex digits and surrogate halves.

It was generated by the Python engine and went with it. The shape is worth
rebuilding exactly as described, because the ordering argument in `Escape` is
the kind of "obviously correct" that was wrong the first time. MARVEL-251.

The C# implementation passes all 420, and normalising a digest Python itself
wrote is the identity — the other half of the claim, asserted against every
recorded document in `vectors.json`.

#### What this costs, and what it buys

About 6 microseconds per digest, most of it spent matching nothing: card ids,
zone names and field names are all printable ASCII, so in practice neither pass
fires. Measured across the whole domain when this landed — 3,999 card ids, 96
zone names and 257 field names, 4,352 strings — every one is left unescaped by
both writers already.

What it buys is that the digest's contents are **not constrained by a JSON
escaping quirk**. A card name, with its curly apostrophe or its accent, can go
into a digest the day that becomes useful, and the two engines will still agree
byte for byte.

`Fingerprint()` is `sha256` of that text. Nothing records it today, because the
document is what makes a mismatch legible. It is specified so that a corpus
which cannot afford a full document per step has a settled way to store one and
does not have to invent it.

## Comparison

`engine/controller/module/replay.py`.

1. **Byte equality** on the canonical string. That is the whole fast path.
2. **An empty recorded digest warns and passes.** A scene saved before
   `Versions.digest_v2` (`0.5.9.205`) carried the v1 sum under a `crc` key, which
   `Json.ConvertDictToDataclass` drops on load because the field no longer
   exists. There is nothing comparable, so the step replays on its inputs alone
   and the warning says which case it is.
3. **On mismatch**, both documents are parsed and diffed, and the report names
   the card, its identity, any changed record key and every changed field:

   ```
   Digest mismatch (#12 / 47)
   c49 01094
       health                14 -> 12
       t_BRUTE               1 -> -
   ```

   A field on one side only prints as `-`.
4. **The verdict rejects by default.** A mismatch is accepted only when
   `digest_ignore_ids` is non-empty *and* every differing id is in it. An
   unreadable recorded digest is always rejected.

There is **one digest**, not three. v1 returned a list of three slots, compared
the recording against all of them, and accepted a match with any — but nothing
ever wrote slots 1 and 2, so a recorded `{}` passed against whatever the engine
computed, and a scene at version `0.5.9.4` was diffed against an empty dict and
reported every card as having disappeared. Both are gone.

The verdict rule is also where v1's `MARVEL-43` lived:

```python
if all(x for x in diff_ids if x in CRC_IGNORE_IDS.value):
```

The comprehension filters `diff_ids` down to the ignorable ones and `all` then
asks whether *those* are truthy, so an empty ignore list — the default — gave
`all(<empty>) is True` and **every mismatch was accepted**. Live play and every
non-test replay passed on divergence. The membership test belongs outside the
comprehension. `MARVEL-43` moved it into `IsIgnorableMismatch` before v2 landed;
the rule carries over unchanged, with one addition -- a mismatch that names no
differing card is a difference in the envelope, which no card id can excuse.

## What it costs

Measured on the same worlds, so the two formats are compared on identical
states rather than across different games.

| per step | v1 | v2 |
|---|---|---|
| cards described | 19.2 | 94.1 |
| raw bytes | 113 | 11,230 |
| gzip -9 bytes | 2.1 | 94.6 |

Raw, v2 is a hundred times larger. Compressed it is forty-six times larger and
**95 bytes a step**, because the document is extremely repetitive and gzip eats
repetition. That gap is the whole reason the format can afford to be legible.

On thirteen real bot-generated games — the same measurement `MARVEL-4` used to
size the corpus — 491 steps came to 5.7 MB raw and **84 KB gzipped, a 69.6×
compression ratio** against the 8.2× `MARVEL-4` measured for v1-era scenes.
Extrapolating to its 10,000-game corpus at a few hundred steps each:

| | raw | gzip -9 |
|---|---|---|
| `MARVEL-4` estimate, v1 | ~1.5 GB | under 200 MB |
| measured here, v2 | ~37 GB | **~0.53 GB** |

So the corpus roughly triples in compressed size and stays comfortably inside
what a dedicated, write-once repository holds. The `MARVEL-4` decisions — gzip,
separate repo pinned by SHA, hash manifest here, shard by scenario — all still
apply and none of them change.

### What `MARVEL-59` added to that

The figures above were measured while `fields` was populated only for cards in
play. Removing that boundary puts a `fields` object on every card. Measured
across the seven wide-matrix games, one document per game at the same step:

| | in-play only | every zone | factor |
|---|---|---|---|
| raw bytes | 128,404 | 239,735 | **1.87×** |
| gzip -9 bytes | 13,284 | 17,649 | **1.33×** |

Raw payload nearly doubles; compressed it grows by a third, because the added
records are overwhelmingly printed constants and zeros repeated across a deck,
which is exactly what gzip removes. On the corpus extrapolation above that moves
**~0.53 GB to ~0.71 GB gzipped**, which does not change any `MARVEL-4` decision.

That is the trade the boundary was hiding: a third more compressed bytes for an
oracle that can see a card change while it is in a deck instead of only when it
comes back. If the corpus ever does need to shrink, the lever is `Fingerprint()`
above, not narrowing the zones again.

## Reimplementation checklist

For a C# port targeting byte-identical output, in dependency order:

1. **Allocate card ids identically.** One counter per world, first card is `0`,
   one id per card not per face, linked cards before their parent, ids never
   reused. Everything else depends on this; the rules are unchanged from v1 and
   set out there.
2. **Emit every card** in `card_dict`, ascending by id. No exclusions — not the
   rules pseudo-card, not id 0, not the middle of a deck. `card_dict` is
   append-only for the life of a game: nothing removes a card from it, so the
   set of emitted ids is always `0..highest`. A card that is *removed from the
   game* moves to `world.area_removed` and is emitted from there like any other.
   See `D10`.
3. **Name the zone** from the `DeckType` member name, with `/removed` for a card
   in `removed_cards`. Read the two lists directly; do not concatenate them.
4. **Index within the zone list**, from 0.
5. **Resolve `owner`** as controller-then-owner, `-1` for the scenario.
6. **Resolve `host`** from the area's bound card, `-1` when there is none.
7. **Populate `fields` for every card, whatever zone it is in**: `is_exhaust`,
   the `t_<TRAIT>` map, and the face's info dict, minus `with_player`,
   `curr_ally_limit` and `curr_restricted_limit`. Keep zeros. Keep constants.
   Merge the info dict down the hierarchy with the more derived class winning,
   and **treat a key claimed by two levels as a fault rather than resolving it**
   — the sets are disjoint today and a collision would silently drop a field.
   An empty `fields` means the card registers none, not that the zone was
   skipped.
8. **Serialise** exactly as specified above — key order, code-point-sorted
   fields, no whitespace. Use the platform's JSON writer; do not hand-roll an
   escaper. Configure it to escape only what JSON requires (in .NET,
   `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`), then normalise its output as
   in [Reconciling two JSON writers](#reconciling-two-json-writers).
9. **Compare as strings**, and diff structurally only when they differ.
10. **Reject by default.**

**There is no acceptance fixture.** One existed, carrying every step's digest in
full for `rhino / spider_man / 12345` plus per-step `sha256` values for two
further boards, and it was the Python engine's account of those games. The
format above is still what `StateDigest` emits and determinism tests still
compare a game against a second run of itself, so the serialiser is exercised —
but its canonical form is pinned to nothing. MARVEL-251.

## How the measurements were taken

Environment pinned per `docs/determinism-audit.md`
(`PYTHONHASHSEED=0`, `PYTHONIOENCODING=utf-8`, `PYTHONDONTWRITEBYTECODE=1`).

```bash
cd py_src
uv venv --python 3.13
uv pip install -r requirements.lock

# coverage, payload, and the change table
python -m tools.determinism.probe_digest_v2

# the worked example and the acceptance fixture
python -m tools.digest.emit_vectors

# the scene-file and corpus figures
python main.py -bot -bot_games 13 -bot_seed 1000
```

The probe reimplements v1 from its own specification — the engine no longer
contains it — and runs both formats over the same live worlds. It samples five
boards (`rhino`, `klaw` at one and two players, `ultron`) with the decline-only
policy `run_headless` provides, 243 steps in total.

Two of v1's findings did **not** fire in that sample and are reported as latent
rather than measured: no card in play summed into the sentinel range (`D3`), and
no face-down card in play contributed state (`D8`). Both are read from source in
the v1 document; neither needs to fire for v2's structure to be the right answer,
since v2 removes the possibility of either rather than reducing its likelihood.

## What this settles

| v1 finding | how v2 settles it |
|---|---|
| `D1` three slots, two always empty | one digest; the `0.5.9.4` branch is gone |
| `D2` the per-card value is a sum | named fields, never summed |
| `D3` negatives collide with sentinels | positions are a zone name and an index |
| `D4` mismatch accepted by default | fixed in `MARVEL-43`; `IsIgnorableMismatch` carries over unchanged |
| `D5` boost cards invisible | boost areas carry `fields`; since `MARVEL-59`, so does every other zone |
| `D6` `GetAll()` appends removed cards | the two lists are read separately, `/removed` |
| `D8` face-down cards leak | recorded on purpose, labelled, never sent to a client |
| `D9` id 0 excluded by number | nothing is excluded |
| `D11` status cards are presence-only | every card carries a full record |
| `D12` constants are dead weight | true of a sum, not of named fields; kept for the parse check |

Both are settled since:

- `D7` (`MARVEL-49`) — **settled.** The nine `GetInfoDict` definitions merged in
  two different directions: six returned `local | super()`, so the base won,
  while `Identity` and `Minion` returned `super() | local`, so the subclass did.
  Nothing chose that, which is why there was no rule to port.

  Every definition now merges through `CardFace.MergeInfo(super().GetInfoDict(),
  {...})`. The stated direction is **the more derived class wins**, and a
  collision raises `EngineIntegrityError` rather than resolving — so the
  direction is documented but never actually load-bearing. `GetStateFields`
  merges its three namespaces (`is_exhaust`, the `t_` traits, the info dict)
  through the same guard.

  Refusing rather than resolving is the point. Under v1 a collision changed a
  sum; under v2 it drops a named field from the wire, which is a state change
  that looks like no change at all. The key sets are disjoint today across all
  thirty-six registered keys plus the directly added ones — verified by
  `unit_test/test_info_dict_merge.py`, which sweeps every card the digest reads
  on two boards and separately refuses any override that merges by hand. **No
  digest value moved**, which is the proof the sets were disjoint: had they not
  been, flipping the direction would have changed the fixture.
- `D10` (`MARVEL-50`) — **settled.** `Card.Destroy` left the card in `card_dict`
  with a stale `area` pointer, so the digest went on describing a destroyed card
  in the zone it had just been taken out of. v2 made the consequence visible
  rather than fixing it; `MARVEL-50` fixed it.

  `MARVEL-50` made `Destroy` end with `object_manager.RemoveCard`, so the card
  stopped being described rather than being described in the wrong place.
  `MARVEL-70` then asked whether destruction should be modelled at all and
  answered no. **`Card.Destroy`, `Deck2.Destroy` and `ObjectManager.RemoveCard`
  are deleted**, and with them the only code that could take a card out of
  `card_dict`.

  **What a port needs from this is now one sentence: `card_dict` only grows.**
  Marvel Champions has no "destroy" — a card is discarded, removed from the
  game, or defeated, and each of those leaves it in a zone with its id. Removed
  from the game is `world.area_removed`, an ordinary zone that cards can be
  searched from and returned from, and a card there carries a full record like
  any other. There is no state in which a card that once existed is absent from
  the digest.

  **No digest moved**, at either step. The path was dead throughout: `Deck2.
  Destroy` was the only caller of `Card.Destroy`, and nothing called
  `Deck2.Destroy`. That is also why it was worth removing rather than
  documenting — three defects accumulated on it undetected, and a port reading a
  specification for a path nothing takes would have had to decide whether to
  reproduce it. `unit_test/test_card_removal.py` pins the replacement rule.
