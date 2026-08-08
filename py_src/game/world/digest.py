"""The v2 state digest -- what every replay step records, and what the oracle
compares.

**Treat this file as a wire format.** It is specified in
`docs/state-digest-v2.md`; that document, not this code, is what a C# implementer
reads. Changing anything here changes every recorded digest and invalidates the
corpus.

It replaced v1 (`World.CalculateCRC`, specified in
`docs/state-digest-contract.md`) under MARVEL-44, before the corpus existed. v1
reduced each card to one integer -- the plain arithmetic sum of a few dozen state
fields -- with negative values doubling as position sentinels. Two consequences
made it a weak oracle: any change that added *n* to one field and subtracted *n*
from another was invisible, and the mismatch table could only ever print a net
delta, so a collision hid a divergence silently.

v2 keeps the shape that made v1 useful -- one record per card, compared on every
step, diffed card by card on mismatch -- and changes the three things that made it
lossy:

  1. a **dictionary of named fields** instead of their sum, so a diff can say
     `health 14 -> 12` rather than `c49 23 -> 21`;
  2. an explicit **zone and index** instead of a negative sentinel, which also
     puts every card in the game into the digest rather than seventeen of
     eighty-two, and makes deck order visible;
  3. **card identity on the wire**, so a divergence names Rhino rather than c49
     and a face flip is a change rather than a coincidence.

What is deliberately *not* here: `curr_ally_limit` and `curr_restricted_limit`.
Those are caches refreshed at particular moments by `AllyLimit.CheckLimit`, so
they pin *when an engine recomputes a limit* rather than what the game state is.
A correct port that computed them on demand would diverge on them. Everything
that is a function of game state -- including printed constants, which cannot
collide once fields are named -- is included.
"""

from __future__ import annotations

import hashlib
import json
from typing import Any, Dict, List, Tuple

DIGEST_VERSION = 2

# A digest with no cards in it. Not the empty string: an absent digest and an
# empty one mean different things to `InputModule`.
EMPTY_DIGEST = '{"v":2,"cards":[]}'

# Per-card key order. Part of the serialisation contract -- see `Serialize`.
CARD_KEYS: Tuple[str, ...] = (
    "id", "card", "zone", "owner", "index", "host", "face_up", "fields",
)

# Zone suffixes. A card is normally in its area's `cards` list; these name the
# two other places it can be.
SUFFIX_REMOVED = "/removed"
SUFFIX_ABSENT = "/absent"


################################################################################
# Building


def Calculate(world: Any) -> str:
    """The digest of `world`, ready to record or compare."""
    return Serialize(BuildDocument(world))


def BuildDocument(world: Any) -> Dict[str, Any]:
    """Every card in the world, ascending by object id.

    Nothing is excluded. v1 skipped id 0 by number -- which excluded whatever
    card happened to be created first rather than a card identified by what it
    is -- and omitted every card that was neither in play, in hand, nor at a pile
    boundary. Both are gone: the rules pseudo-card and the middle of every deck
    are ordinary entries.
    """
    card_dict = world.object_manager.card_dict
    positions = _BuildPositionIndex(card_dict)
    cards = [
        _Record(object_id, card_dict[object_id], positions)
        for object_id in sorted(card_dict)
    ]
    return {"v": DIGEST_VERSION, "cards": cards}


def Serialize(document: Dict[str, Any]) -> str:
    """Canonical form. A port must reproduce this text, not an equivalent structure.

    No whitespace, ASCII-escaped (so a card name or trait outside ASCII encodes
    identically in every language), object keys in insertion order -- which is
    `CARD_KEYS` for a card record and code-point order inside `fields`, both
    established when the record is built.
    """
    return json.dumps(document, separators=(",", ":"), ensure_ascii=True, sort_keys=False)


def Parse(serialized: str) -> Dict[str, Any]:
    """Inverse of `Serialize`. Raises `ValueError` on anything unreadable.

    Checks the shape rather than only that the text is JSON, because the caller
    is `Diff`, and `Diff` runs on a value read out of a corpus file that may be
    truncated or from another format. A corrupt recording has to come back as a
    rejected step, not as an exception through the replay loop -- so everything
    unreadable is `ValueError`, which is what `OnDigestMismatch` catches.
    """
    def refuse(why: str) -> ValueError:
        return ValueError(f"not a v{DIGEST_VERSION} digest ({why}): {serialized[:80]!r}")

    document = json.loads(serialized)
    if not isinstance(document, dict):
        raise refuse("not an object")
    cards = document.get("cards")
    if not isinstance(cards, list):
        raise refuse("no 'cards' array")
    for record in cards:
        if not isinstance(record, dict):
            raise refuse("a card record is not an object")
        if not isinstance(record.get("id"), int):
            raise refuse("a card record has no integer 'id'")
    return document


def Fingerprint(serialized: str) -> str:
    """`sha256` of the canonical form.

    Defined here so that a corpus which cannot afford a full document per step
    has a settled way to store one -- see `docs/state-digest-v2.md`. Nothing in
    the engine records it today; the engine stores the document, because the
    document is what makes a mismatch legible.
    """
    return hashlib.sha256(serialized.encode("utf-8")).hexdigest()


def _BuildPositionIndex(card_dict: Dict[int, Any]) -> Dict[int, Tuple[str, int]]:
    """object_id -> (zone, index), walking each area once rather than per card."""
    positions: Dict[int, Tuple[str, int]] = {}
    seen: set[int] = set()
    for card in card_dict.values():
        area = card.area
        key = id(area)
        if key in seen:
            continue
        seen.add(key)
        name = area.deck_type.name
        for index, member in enumerate(area.cards):
            positions[member.object_id] = (name, index)
        # `removed_cards` is where a detached attachment waits. v1 read it by
        # accident -- `GetAll()` appended it, so `[-1]` was not always the top of
        # a pile. Here it is a zone of its own and cannot be mistaken for one.
        for index, member in enumerate(area.removed_cards):
            positions[member.object_id] = (name + SUFFIX_REMOVED, index)
    return positions


def _Record(object_id: int, card: Any, positions: Dict[int, Tuple[str, int]]) -> Dict[str, Any]:
    area = card.area
    zone, index = positions.get(object_id, (area.deck_type.name + SUFFIX_ABSENT, -1))
    return {
        "id": object_id,
        "card": card.face.paper.card_id,
        "zone": zone,
        "owner": _OwnerId(card),
        "index": index,
        "host": area.bind_card.object_id if area.bind_card is not None else -1,
        "face_up": bool(card.state.is_face_up),
        "fields": _Fields(card),
    }


def _OwnerId(card: Any) -> int:
    """Controlling player, falling back to the owner. `-1` for the scenario.

    v1 smeared this into the sum as `with_player` (`player_id + 1`, absent when
    the area belonged to the scenario), which meant a change of control moved a
    number that also moved for a dozen other reasons.
    """
    who = card.GetController() if card.IsOnField() else None
    if who is None:
        who = card.GetOwner()
    return -1 if who.is_scenario else who.player_id


def _Fields(card: Any) -> Dict[str, int]:
    """Named live state, code-point ordered. Empty for cards out of play.

    The guard is the same one v1 used to decide whether to compute a value at
    all, plus boost areas: `GetRenderInfo` has always had an `is_boost_area`
    branch, but `GetCRC` returned `-1` before it could be reached, so a boost
    card revealed during a villain activation never entered the digest even
    though its icons changed the outcome of the attack.

    It stops there rather than covering every zone because several
    `GetInfoDict` overrides read state that only exists in play --
    `Identity.GetInfoDict` goes through `GetControlByPlayer`, `Minion` guards on
    `IsInPlay()` -- and an oracle that can raise while computing itself is worse
    than one with a documented edge. Widening it means auditing those overrides
    first.
    """
    flags = card.area.flags
    if not (flags.is_in_play or flags.is_status_area or flags.is_boost_area):
        return {}
    fields = card.face.GetStateFields()
    return {key: fields[key] for key in sorted(fields)}


################################################################################
# Comparing


def Diff(recorded: str, current: str) -> Tuple[List[int], str]:
    """`(ids that differ, a printable report)`.

    Only called when the two strings are already known to be unequal. The report
    names the card and the field, which is the whole reason v2 exists: v1 could
    say `c49 23 -> 21` and no more.
    """
    before = _ById(Parse(recorded))
    after = _ById(Parse(current))

    changed: List[int] = []
    lines: List[str] = []
    for object_id in sorted(set(before) | set(after)):
        a = before.get(object_id)
        b = after.get(object_id)
        if a == b:
            continue
        changed.append(object_id)
        lines.extend(_RecordDiff(object_id, a, b))
    return changed, "\n".join(lines)


def _ById(document: Dict[str, Any]) -> Dict[int, Dict[str, Any]]:
    return {record["id"]: record for record in document["cards"]}


def _RecordDiff(object_id: int, a: Dict[str, Any] | None, b: Dict[str, Any] | None) -> List[str]:
    # Read with `.get` throughout: `a` came out of a corpus file, and a report
    # about a divergence must not fail on a record that is missing a key.
    if a is None:
        assert b is not None
        return [f"c{object_id} {b.get('card')}  only in the current state ({b.get('zone')})"]
    if b is None:
        return [f"c{object_id} {a.get('card')}  only in the recording ({a.get('zone')})"]

    lines = [f"c{object_id} {a.get('card')}"]
    for key in CARD_KEYS:
        if key in ("id", "fields") or a.get(key) == b.get(key):
            continue
        lines.append(f"    {key:<22}{a.get(key)} -> {b.get(key)}")

    fields_a: Dict[str, int] = a.get("fields") or {}
    fields_b: Dict[str, int] = b.get("fields") or {}
    for key in sorted(set(fields_a) | set(fields_b)):
        if fields_a.get(key) == fields_b.get(key):
            continue
        lines.append(f"    {key:<22}{_Cell(fields_a, key)} -> {_Cell(fields_b, key)}")
    return lines


def _Cell(fields: Dict[str, int], key: str) -> str:
    return "-" if key not in fields else str(fields[key])
