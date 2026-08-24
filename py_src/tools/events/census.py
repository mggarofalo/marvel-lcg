"""What actually changes between two steps, measured over the frozen corpus.

MARVEL-160 asks for a semantic event stream in the fold's return signature. The
temptation is to design the event vocabulary from the rules and then discover
what it cannot express. This does it the other way round.

Every recorded step in the corpus carries its full v2 digest, so the set of state
transitions the engine can produce is not a matter of opinion -- it is countable.
Diff consecutive digests and every change falls into one of a small number of
shapes: a card moved zone, changed position within a zone, changed controller,
attached or detached, flipped, or had a named field change. **An event vocabulary
that cannot express one of the shapes below is incomplete, and one that has
members not appearing below is speculative.**

The census also reads the two labels each step already carries:

    event    the trigger that opened the decision, e.g. `WhenPlayerChooseAbility`
    effect   the verb the player chose, e.g. `Choose`, `Change_Form`, `Attack`

Those are the engine's own names for *why* a transition happened, which is the
half a digest cannot show. An event record needs both: the shape, from the diff,
and the cause, from these.

## What this is not

It does not prove the vocabulary is *sufficient* to animate anything. It proves
the vocabulary is *complete* with respect to observable state, which is the part
that can be checked mechanically and is the precondition for MARVEL-163.

Run:
    python -m tools.events.census ~/Source/marvel-lcg-corpus
    python -m tools.events.census <shards> --only rhino --scenes 5
    python -m tools.events.census <shards> --json out.json

Reads shards in place; nothing is expanded to disk.
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import sys
from typing import Any, Dict, Iterator, List, Sequence, Tuple

from tools.corpus.expand import read_shard, shard_paths

# The six positional keys of a card record. `fields` is handled separately
# because its key set is open.
POSITIONAL = ("card", "zone", "owner", "index", "host", "face_up")


class Census:
    """Counters only. Scenes are parsed, counted and dropped."""

    def __init__(self) -> None:
        self.scenes = 0
        self.steps = 0
        self.transitions = 0
        self.silent_steps = 0

        self.shape: collections.Counter = collections.Counter()
        self.moves: collections.Counter = collections.Counter()
        self.fields: collections.Counter = collections.Counter()
        self.field_shape: collections.Counter = collections.Counter()
        self.triggers: collections.Counter = collections.Counter()
        self.verbs: collections.Counter = collections.Counter()
        self.cards_per_step: collections.Counter = collections.Counter()
        self.shapes_per_step: collections.Counter = collections.Counter()

        # One worked example per shape, for the design document. The first one
        # seen, so the choice is not cherry-picked.
        self.example: Dict[str, Dict[str, Any]] = {}

    # -- recording ---------------------------------------------------------

    def _Note(self, shape: str, card: Dict[str, Any], detail: Dict[str, Any],
              trigger: str, verb: str) -> None:
        self.shape[shape] += 1
        self.transitions += 1
        if shape not in self.example:
            self.example[shape] = {
                "card": card.get("card"),
                "object_id": card.get("id"),
                "trigger": trigger,
                "verb": verb,
                **detail,
            }

    def Step(self, before: Dict[int, Dict[str, Any]],
             after: Dict[int, Dict[str, Any]], trigger: str, verb: str) -> None:
        self.steps += 1
        self.triggers[trigger] += 1
        self.verbs[verb] += 1

        touched = 0
        shapes_here = set()

        for object_id in sorted(set(before) | set(after)):
            old = before.get(object_id)
            new = after.get(object_id)

            if old is None:
                self._Note("card.created", new or {},
                           {"zone": (new or {}).get("zone")}, trigger, verb)
                touched += 1
                shapes_here.add("card.created")
                continue
            if new is None:
                # `card_dict` is append-only, so this should never fire. Counted
                # rather than asserted: a census that raises tells you less than
                # one that reports a number nobody expected.
                self._Note("card.vanished", old, {"zone": old.get("zone")},
                           trigger, verb)
                touched += 1
                shapes_here.add("card.vanished")
                continue

            local = self._Compare(old, new, trigger, verb)
            if local:
                touched += 1
                shapes_here.update(local)

        if touched == 0:
            self.silent_steps += 1
        self.cards_per_step[touched] += 1
        self.shapes_per_step[len(shapes_here)] += 1

    def _Compare(self, old: Dict[str, Any], new: Dict[str, Any],
                 trigger: str, verb: str) -> List[str]:
        seen: List[str] = []

        if old.get("card") != new.get("card"):
            self._Note("card.face_changed",
                       new, {"from": old.get("card"), "to": new.get("card")},
                       trigger, verb)
            seen.append("card.face_changed")

        old_zone, new_zone = old.get("zone"), new.get("zone")
        if old_zone != new_zone:
            self._Note("card.moved", new,
                       {"from": old_zone, "to": new_zone,
                        "to_index": new.get("index")}, trigger, verb)
            self.moves[(old_zone, new_zone)] += 1
            seen.append("card.moved")
        elif old.get("index") != new.get("index"):
            # Same zone, different slot. Shuffles and removals both land here,
            # and the event model has to be able to say "the pile reordered"
            # without emitting one event per card.
            self._Note("card.reordered", new,
                       {"zone": new_zone, "from": old.get("index"),
                        "to": new.get("index")}, trigger, verb)
            seen.append("card.reordered")

        if old.get("owner") != new.get("owner"):
            self._Note("card.controller_changed", new,
                       {"from": old.get("owner"), "to": new.get("owner")},
                       trigger, verb)
            seen.append("card.controller_changed")

        old_host, new_host = old.get("host", -1), new.get("host", -1)
        if old_host != new_host:
            shape = ("card.attached" if old_host == -1 else
                     "card.detached" if new_host == -1 else "card.rehosted")
            self._Note(shape, new, {"from": old_host, "to": new_host},
                       trigger, verb)
            seen.append(shape)

        if old.get("face_up") != new.get("face_up"):
            self._Note("card.flipped", new,
                       {"to_face_up": new.get("face_up")}, trigger, verb)
            seen.append("card.flipped")

        old_fields = old.get("fields") or {}
        new_fields = new.get("fields") or {}
        for key in sorted(set(old_fields) | set(new_fields)):
            before_value = old_fields.get(key)
            after_value = new_fields.get(key)
            if before_value == after_value:
                continue

            if before_value is None:
                shape = "field.appeared"
            elif after_value is None:
                shape = "field.disappeared"
            else:
                shape = "field.changed"

            self.fields[key] += 1
            self.field_shape[(key, shape)] += 1
            self._Note(shape, new,
                       {"field": key, "from": before_value, "to": after_value},
                       trigger, verb)
            seen.append(shape)

        return seen


# -- reading -------------------------------------------------------------


def _Label(step: Dict[str, Any]) -> Tuple[str, str]:
    """`(trigger, verb)` for a recorded step.

    `event` is `"m217 WhenPlayerChooseAbility"` -- a monitor id and the trigger
    name. `effect.id` is `"e1 Choose c1 32001b"` -- an effect id, the verb, and
    the card it was taken on. Only the names are wanted; the ids are per-game.
    """
    event = step.get("event") or ""
    trigger = event.split(" ", 1)[1] if " " in event else (event or "(none)")

    effect = step.get("effect")
    verb = "(none)"
    if isinstance(effect, dict):
        parts = (effect.get("id") or "").split(" ")
        if len(parts) >= 2:
            verb = parts[1]
    return trigger, verb


def _ById(serialized: str) -> Dict[int, Dict[str, Any]]:
    document = json.loads(serialized)
    return {record["id"]: record for record in document.get("cards", [])}


def Walk(scene_text: str, census: Census) -> None:
    scene = json.loads(scene_text)
    inputs = scene.get("inputs")
    if not isinstance(inputs, list):
        return

    census.scenes += 1
    previous: Dict[int, Dict[str, Any]] | None = None
    for step in inputs:
        if not isinstance(step, dict):
            continue
        serialized = step.get("digest")
        if not serialized:
            continue
        current = _ById(serialized)
        if previous is not None:
            trigger, verb = _Label(step)
            census.Step(previous, current, trigger, verb)
        previous = current


# -- reporting -----------------------------------------------------------


def _Table(counter: collections.Counter, total: int, limit: int,
           key: Any = str) -> Iterator[str]:
    for name, count in counter.most_common(limit):
        share = 100.0 * count / total if total else 0.0
        yield f"  {key(name):<44} {count:>9,}  {share:5.1f}%"


def Report(census: Census, limit: int) -> str:
    out: List[str] = []
    add = out.append

    add(f"scenes            {census.scenes:>9,}")
    add(f"transitions       {census.steps:>9,}  (consecutive digest pairs)")
    add(f"changes           {census.transitions:>9,}")
    add(f"silent steps      {census.silent_steps:>9,}  "
        f"({100.0 * census.silent_steps / census.steps if census.steps else 0:.1f}% "
        f"of steps changed nothing in the digest)")

    add("\n## Change shapes -- the event vocabulary, from evidence")
    add(f"  {'shape':<44} {'count':>9}  share")
    out.extend(_Table(census.shape, census.transitions, limit))

    add("\n## Zone transitions")
    add(f"  {'from -> to':<44} {'count':>9}  share")
    moved = sum(census.moves.values())
    out.extend(_Table(census.moves, moved, limit,
                      key=lambda pair: f"{pair[0]} -> {pair[1]}"))
    add(f"  ({len(census.moves)} distinct zone pairs)")

    add("\n## Fields that change")
    add(f"  {'field':<44} {'count':>9}  share")
    field_total = sum(census.fields.values())
    out.extend(_Table(census.fields, field_total, limit))
    add(f"  ({len(census.fields)} distinct fields ever change)")

    add("\n## Triggers")
    add(f"  {'trigger':<44} {'count':>9}  share")
    out.extend(_Table(census.triggers, census.steps, limit))
    add(f"  ({len(census.triggers)} distinct)")

    add("\n## Effect verbs")
    add(f"  {'verb':<44} {'count':>9}  share")
    out.extend(_Table(census.verbs, census.steps, limit))
    add(f"  ({len(census.verbs)} distinct)")

    add("\n## Cards touched per step -- how wide one event batch is")
    add(f"  {'cards':<44} {'steps':>9}  share")
    out.extend(_Table(census.cards_per_step, census.steps, limit,
                      key=lambda n: f"{n} card(s)"))

    return "\n".join(out)


def _main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("shards", help="directory of .json.gz corpus shards")
    parser.add_argument("--only", nargs="*", default=(),
                        help="only these shards, by name")
    parser.add_argument("--scenes", type=int, default=0,
                        help="cap scenes per shard (0 = all)")
    parser.add_argument("--limit", type=int, default=40,
                        help="rows per table")
    parser.add_argument("--json", help="also write the raw counters here")
    args = parser.parse_args(argv)

    census = Census()
    paths = shard_paths(args.shards, args.only)
    if not paths:
        print(f"no shards under {args.shards}", file=sys.stderr)
        return 1

    for index, path in enumerate(paths, 1):
        bundle = read_shard(path)
        items = sorted(bundle.items())
        if args.scenes:
            items = items[:args.scenes]
        for _, text in items:
            Walk(text, census)
        print(f"[{index}/{len(paths)}] {os.path.basename(path)}: "
              f"{census.scenes:,} scenes, {census.steps:,} steps",
              file=sys.stderr, flush=True)

    print(Report(census, args.limit))

    if args.json:
        payload = {
            "scenes": census.scenes,
            "steps": census.steps,
            "changes": census.transitions,
            "silent_steps": census.silent_steps,
            "shape": dict(census.shape),
            "moves": {f"{a} -> {b}": n for (a, b), n in census.moves.items()},
            "fields": dict(census.fields),
            "field_shape": {f"{k}:{s}": n
                            for (k, s), n in census.field_shape.items()},
            "triggers": dict(census.triggers),
            "verbs": dict(census.verbs),
            "cards_per_step": dict(census.cards_per_step),
            "example": census.example,
        }
        with open(args.json, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(payload, handle, indent=2, sort_keys=True)
        print(f"\nwrote {args.json}", file=sys.stderr)

    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
