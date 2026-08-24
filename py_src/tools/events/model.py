"""The event vocabulary, and the proof that it explains the corpus.

`tools/events/census.py` counts the *shapes* of change between two digests. This
turns those shapes into a candidate event vocabulary and then tries to break it:

    Derive(before, after)  ->  events
    Apply(before, events)  ->  a digest document
    Apply(before, Derive(before, after)) == after      for every step in the corpus

That is the property MARVEL-163 will assert against events the engine *emits*.
Asserting it now against events *derived from the diff* proves something weaker
but load-bearing: **the vocabulary is lossless.** Every observable transition can
be expressed by these records and nothing is left over. A vocabulary that fails
this cannot be fixed later by a better interpreter.

## Why position is not an event

The naive reading of the census is that `card.reordered` -- 17% of all observed
changes -- needs an event. It mostly does not.

A zone is an ordered list. Taking a card out of the middle of a deck shifts every
card above it down by one, and the digest records that as a position change for
each of them. Those are *consequences* of the move, not separate things that
happened, and an animation that played them would be lying.

So `Derive` models the mechanics instead: apply the moves, compact the source
zones, insert into the destinations, and only emit `AreaReordered` for a zone
whose resulting order still does not match. What survives is a genuine
reordering -- a shuffle -- and it is one event for the zone rather than one per
card.

The measured effect of that distinction is in `docs/event-stream.md`.

## The vocabulary

    CardsCreated      cards entering the world
    CardsMoved        a batch crossing zones, one record per (from, to) pair
    AreaReordered     a shuffle: the zone's whole new order, one event
    CardFormChanged   the card is now a different face
    CardsFlipped      face up or face down
    CardAttached      gained a host
    CardDetached      lost its host
    ControlChanged    a different player controls it
    FieldSet          one named value, from and to; either may be absent

Every event also carries the step's `Trigger` and `Verb` -- the engine's own
names for *why*. A digest cannot show cause, and cause is the half an animation
needs.
"""

from __future__ import annotations

import collections
import json
from typing import Any, Dict, Iterable, List, Sequence, Tuple

Record = Dict[str, Any]
Board = Dict[int, Record]
Event = Dict[str, Any]

# The closed set of event kinds, and the payload keys each carries beyond the
# universal `kind`, `trigger` and `verb`.
#
# Declared rather than inferred so it can be a *contract*: `emit_vocabulary.py`
# writes it to `datasets/events/vocabulary.json`, the C# side asserts its
# `[JsonDerivedType]` set matches, and `unit_test/test_event_model.py` asserts
# `Derive` never emits anything outside it and that every member actually fires
# somewhere in the corpus. A vocabulary with a member nothing produces is
# speculation; one missing a member the engine produces is a silent hole.
VOCABULARY = {
    "CardsCreated":    ("area", "cards"),
    "CardsMoved":      ("from", "to", "cards"),
    "AreaReordered":   ("area", "order"),
    "CardFormChanged": ("card", "from", "to"),
    "CardsFlipped":    ("cards", "face_up"),
    "CardAttached":    ("card", "host"),
    "CardDetached":    ("card", "host"),
    "ControlChanged":  ("card", "from", "to"),
    "FieldSet":        ("card", "field", "from", "to"),
}

# Present on every event. `trigger` and `verb` are the engine's own names for
# *why* -- the half a digest can never show.
UNIVERSAL = ("kind", "trigger", "verb")


# -- deriving ------------------------------------------------------------


def _Area(record: Record) -> Tuple[str, Any, int, str]:
    """Which ordered list a card sits in.

    **Not the zone name.** `HandsArea` is one name for as many areas as there
    are players, and `UpgradesArea` for as many as there are hosts. The engine
    indexes a card within its *area* object, so an area is identified by the
    zone name plus the player who controls it plus the card it hangs off.

    Getting this wrong merges two players' hands into one list and renumbers
    across the join, which is what the first version of this file did.

    Two kinds of board reach this. A **digest board** has only what a digest
    records, so the triple is the best available guess and the fourth slot is
    empty -- `_Split` fills it when the guess collides. An **engine board**
    (`tools/events/state.py`) carries the area's real object id, and then there
    is no guessing: the fourth slot is the identity and the first three are
    description. MARVEL-163 is the difference between the two.
    """
    area = record.get("area")
    if area is not None:
        return (area["zone"], area["owner"], area["host"], str(area["id"]))
    return (record["zone"], record["owner"], record["host"], "")


def _Descriptor(area: Tuple[str, Any, int, str]) -> Dict[str, Any]:
    """An area key, as it travels on an event.

    `id` is empty on a digest board and is the area's own object id on an
    engine board. It is carried rather than dropped because `Apply` places
    cards by the descriptor alone -- an attach changes both a card's host and
    its area, and recomputing the key mid-flight would make the result depend
    on which of the two events happened to be processed first.
    """
    return {"zone": area[0], "owner": area[1], "host": area[2], "id": area[3]}


def _Split(board: Board, members: List[int]) -> List[List[int]]:
    """Partition one colliding bucket into the areas it is really made of.

    Digest v2 does not identify an area -- it records a zone name, a controller
    and a host, and for every zone but one that triple is unique. The exception
    is `AsideDeck`: a three-hero game has three of them, all with `owner` -1 and
    `host` -1, each indexed from zero. Measured over the corpus, no other zone
    collides.

    Object ids are allocated per area as the game is built, so the cards of one
    aside deck are a contiguous run. Sort by id and start a new run wherever the
    index stops increasing. This is a heuristic, and it is only defensible
    because the round trip in `unit_test/test_event_model.py` checks it against
    every step of the corpus rather than asserting it here.
    """
    runs: List[List[int]] = []
    current: List[int] = []
    last = -1
    for object_id in sorted(members):
        index = board[object_id]["index"]
        if current and index <= last:
            runs.append(current)
            current = []
        current.append(object_id)
        last = index
    if current:
        runs.append(current)
    return runs


def _Zones(board: Board) -> Dict[Any, List[int]]:
    """area -> object ids in index order.

    Ordered by the recorded `index`, so an area whose indices are not a dense
    `0..n-1` run still produces a stable order rather than raising. The absent
    marker `-1` sorts first and never participates in compaction.
    """
    buckets: Dict[Tuple[str, int, int], List[int]] = collections.defaultdict(list)
    for object_id, record in board.items():
        buckets[_Area(record)].append(object_id)

    zones: Dict[Any, List[int]] = {}
    for key, members in buckets.items():
        indices = [board[object_id]["index"] for object_id in members]
        if len(set(indices)) == len(indices):
            zones[key] = sorted(
                members, key=lambda object_id: (board[object_id]["index"], object_id))
            continue
        # Colliding: two or more areas share this triple. Split them, and
        # qualify the key with the lowest id in each run so the parts stay
        # distinguishable across a step.
        for run in _Split(board, members):
            zones[key[:3] + (f"#{min(run)}",)] = sorted(
                run, key=lambda object_id: (board[object_id]["index"], object_id))
    return zones


def _Settle(zones: Dict[Any, List[int]],
            moves: Sequence[Tuple[int, Any, Any, int]]) -> Dict[Any, List[int]]:
    """Place every mover, in the one order that makes a landing index mean
    what it says.

    **A landing index is a position in the area as the step leaves it, not as
    the area stands part-way through.** That is forced by where the number
    comes from: it is read off the recorded next state, where all of the step's
    arrivals are already present. So every removal happens before any
    insertion, and the insertions run in destination-index order.

    Getting that wrong is not theoretical. In one Rhino game, an encounter
    discard pile received five cards in a single step from four different
    areas. Applying each source's batch as it came inserted the third arrival
    at the index the fifth was going to occupy, and three cards came out in the
    wrong order -- with no event to blame, because `Derive` had predicted the
    positions correctly and therefore emitted no `AreaReordered`.

    `Derive` and `Apply` both call this for exactly that reason. When the
    prediction and the placement are two pieces of code, they can disagree, and
    a disagreement between them is invisible: the derived stream looks complete
    and the applied result is wrong. See MARVEL-163.
    """
    for object_id, source, _, _ in moves:
        if source is not None and object_id in zones.get(source, ()):
            zones[source].remove(object_id)

    for object_id, _, target, index in sorted(moves, key=lambda m: (str(m[2]), m[3])):
        members = zones.setdefault(target, [])
        position = len(members) if index < 0 or index > len(members) else index
        members.insert(position, object_id)

    return zones


def _PredictPositions(before: Board, moves: List[Tuple[int, Any, Any, int]]
                      ) -> Dict[Any, List[int]]:
    """Where every card lands if the only thing that happened was the moves.

    Anything that still does not match afterwards is a real reorder.
    """
    return _Settle({area: list(members) for area, members in _Zones(before).items()},
                   moves)


def Derive(before: Board, after: Board, trigger: str = "", verb: str = "") -> List[Event]:
    """The events that turn `before` into `after`."""
    events: List[Event] = []

    def emit(kind: str, **payload: Any) -> None:
        events.append({"kind": kind, "trigger": trigger, "verb": verb, **payload})

    created = sorted(set(after) - set(before))
    if created:
        # Grouped by the area they appeared in, so the payload matches
        # `CardsMoved` and a consumer never has to special-case creation.
        by_area: Dict[Tuple[str, int, int], List[Record]] = collections.defaultdict(list)
        for object_id in created:
            by_area[_Area(after[object_id])].append(after[object_id])
        for area, records in sorted(by_area.items(), key=str):
            emit("CardsCreated",
                 area=_Descriptor(area),
                 cards=records)

    # Moves first: everything positional is downstream of them.
    moves: List[Tuple[int, Any, Any, int]] = []
    for object_id in sorted(set(before) & set(after)):
        old, new = before[object_id], after[object_id]
        if _Area(old) != _Area(new):
            moves.append((object_id, _Area(old), _Area(new), new["index"]))

    by_pair: Dict[Tuple[Any, Any], List[Tuple[int, int]]] = collections.defaultdict(list)
    for object_id, source, target, index in moves:
        by_pair[(source, target)].append((object_id, index))
    for (source, target), cards in sorted(by_pair.items(), key=lambda item: str(item[0])):
        emit("CardsMoved",
             **{"from": _Descriptor(source),
                "to": _Descriptor(target),
                "cards": sorted(cards, key=lambda pair: pair[1])})

    # Anything the moves do not account for is a genuine reordering.
    predicted = _PredictPositions(before, moves)
    actual = _Zones(after)
    for area in sorted(set(predicted) | set(actual), key=str):
        want = actual.get(area, [])
        got = predicted.get(area, [])
        if want != got:
            emit("AreaReordered",
                 area=_Descriptor(area),
                 order=want)

    # Everything else is per card and position-independent.
    for object_id in sorted(set(before) & set(after)):
        old, new = before[object_id], after[object_id]

        if old["card"] != new["card"]:
            emit("CardFormChanged", card=object_id,
                 **{"from": old["card"], "to": new["card"]})

        if old["face_up"] != new["face_up"]:
            emit("CardsFlipped", cards=[object_id], face_up=new["face_up"])

        if old["host"] != new["host"]:
            if old["host"] == -1:
                emit("CardAttached", card=object_id, host=new["host"])
            elif new["host"] == -1:
                emit("CardDetached", card=object_id, host=old["host"])
            else:
                emit("CardDetached", card=object_id, host=old["host"])
                emit("CardAttached", card=object_id, host=new["host"])

        if old["owner"] != new["owner"]:
            emit("ControlChanged", card=object_id,
                 **{"from": old["owner"], "to": new["owner"]})

        old_fields = old.get("fields") or {}
        new_fields = new.get("fields") or {}
        for key in sorted(set(old_fields) | set(new_fields)):
            if old_fields.get(key) != new_fields.get(key):
                emit("FieldSet", card=object_id, field=key,
                     **{"from": old_fields.get(key), "to": new_fields.get(key)})

    return events


# -- applying ------------------------------------------------------------


def _Key(descriptor: Dict[str, Any]) -> Tuple[str, Any, int, str]:
    return (descriptor["zone"], descriptor["owner"], descriptor["host"],
            str(descriptor.get("id", "")))


def _Place(record: Record, descriptor: Dict[str, Any]) -> None:
    """Move one card into the area `descriptor` names.

    On an **engine board** the area is its own object, so only the area is
    written; the card's `owner` is its controller and changes through
    `ControlChanged` or not at all. On a **digest board** `owner` is part of
    the area key, so a move implies it -- `Derive` emits the `ControlChanged`
    as well, and applying both is idempotent.
    """
    record["zone"] = descriptor["zone"]
    record["host"] = descriptor["host"]
    if "area" in record:
        record["area"] = dict(descriptor)
    else:
        record["owner"] = descriptor["owner"]


def Apply(before: Board, events: Sequence[Event]) -> Board:
    """`before` with `events` applied, as a fresh board.

    Placement is driven entirely by the area descriptors the events carry, never
    by recomputing an area key from a record mid-flight -- an attach changes both
    the host and the area, and recomputing would make the result depend on which
    of the two events was processed first.
    """
    board: Board = {
        object_id: {**record, "fields": dict(record.get("fields") or {})}
        for object_id, record in before.items()
    }
    zones = {area: list(members) for area, members in _Zones(board).items()}

    # Everything positional is settled together, before anything else runs.
    # `_Settle` says why; the short version is that a landing index describes
    # the area the step leaves behind, so the arrivals cannot be spliced in one
    # event at a time.
    arrivals: List[Tuple[int, Any, Any, int]] = []
    for event in events:
        if event["kind"] == "CardsCreated":
            target = _Key(event["area"])
            for record in event["cards"]:
                copy = {**record, "fields": dict(record.get("fields") or {})}
                board[copy["id"]] = copy
                if copy["index"] >= 0:
                    arrivals.append((copy["id"], None, target, copy["index"]))
        elif event["kind"] == "CardsMoved":
            target = _Key(event["to"])
            for object_id, index in event["cards"]:
                _Place(board[object_id], event["to"])
                arrivals.append((object_id, _Key(event["from"]), target, index))
    _Settle(zones, arrivals)

    for event in events:
        kind = event["kind"]

        if kind in ("CardsCreated", "CardsMoved"):
            continue

        elif kind == "AreaReordered":
            zones[_Key(event["area"])] = list(event["order"])

        elif kind == "CardFormChanged":
            board[event["card"]]["card"] = event["to"]

        elif kind == "CardsFlipped":
            for object_id in event["cards"]:
                board[object_id]["face_up"] = event["face_up"]

        elif kind == "CardAttached":
            board[event["card"]]["host"] = event["host"]

        elif kind == "CardDetached":
            board[event["card"]]["host"] = -1

        elif kind == "ControlChanged":
            board[event["card"]]["owner"] = event["to"]

        elif kind == "FieldSet":
            fields = board[event["card"]]["fields"]
            if event["to"] is None:
                fields.pop(event["field"], None)
            else:
                fields[event["field"]] = event["to"]

        else:
            raise ValueError(f"unknown event kind: {kind!r}")

    # Positions are rebuilt from the area lists rather than tracked per card, so
    # a card that never moved still gets the index its neighbours' movement
    # implies. That is the whole point: those shifts are consequences, not
    # events.
    for area, members in zones.items():
        for index, object_id in enumerate(members):
            if _Area(board[object_id]) == area:
                board[object_id]["index"] = index

    return board


# -- comparing -----------------------------------------------------------


def Serialize(board: Board) -> str:
    """The v2 canonical form of a board, for byte comparison."""
    from game.world import digest

    cards = [
        {key: record[key] for key in digest.CARD_KEYS if key in record}
        for _, record in sorted(board.items())
    ]
    for record in cards:
        record["fields"] = {k: record["fields"][k] for k in sorted(record["fields"])}
    return digest.Serialize({"v": digest.DIGEST_VERSION, "cards": cards})


def RoundTrip(before: Board, after: Board, trigger: str = "", verb: str = ""
              ) -> Tuple[bool, List[Event], str, str]:
    """`(matched, events, produced, expected)` for one step."""
    events = Derive(before, after, trigger, verb)
    produced = Serialize(Apply(before, events))
    expected = Serialize(after)
    return produced == expected, events, produced, expected
