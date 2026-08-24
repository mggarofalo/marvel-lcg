"""MARVEL-163 -- does the event stream actually reproduce the corpus?

`tools/events/model.py` established that the vocabulary is *lossless* with
respect to a digest: derive events by diffing two digests, apply them, get the
digest back. That proved the records can express every observable change, and
it left one thing unproven and one thing failing.

Unproven: a digest diff and an engine are not the same source. Deriving from
the diff cannot be wrong about what changed, because the diff *is* what
changed. The question this asks is whether the same events, derived from
**engine state**, reproduce the state the corpus recorded.

Failing: position, at 61.1%. `docs/event-stream.md` predicted the cause -- a
digest does not identify an area -- and predicted this tool would fix it. That
prediction is checkable, and checking it is most of the value here.

## What it does

Replays scenes from the frozen corpus in this process
(`tools/replay/observe.py`), and at each decision:

  1. snapshots engine state with real area identity (`tools/events/state.py`);
  2. asserts the snapshot **serialises to the digest the engine computed at the
     same instant**, byte for byte;
  3. derives events from the previous snapshot to this one, applies them to the
     previous snapshot, and compares the result to this one.

Step 2 is not ceremony. Without it, a round trip that passes proves a property
of this file's idea of state rather than of the game's, and a round trip that
fails cannot be attributed. With it, the board is known to be the digest the
corpus gate already verified, so a failure in step 3 is the event stream's.

The comparison in step 3 is made twice, and the pair is the whole point:

  digest form   the v2 document, which is the corpus's own criterion.
  placement     every card in the same *area object* as well, which the digest
                cannot express and an animation depends on.

## Run

    python -m tools.events.verify ~/Source/marvel-lcg-corpus --per-shard 1
    python -m tools.events.verify <shards> --only rhino --scenes 3
    python -m tools.events.verify <shards> --per-shard 1 --json verify.json

One scene is roughly a second. The whole corpus is 1,773 of them, so this is a
tool rather than a unit test -- `unit_test/test_event_model.py` states the same
properties on boards small enough to read.
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import sys
from typing import Any, Dict, List, Optional, Sequence, Tuple

from tools.events import model, state
from tools.replay import observe


class Verdict:
    """Counters, and the first few failures in full."""

    # Enough to diagnose a failure, few enough that a broken run does not
    # print a corpus.
    KEEP_FAILURES = 5

    def __init__(self) -> None:
        self.scenes = 0
        self.scenes_completed = 0
        self.steps = 0
        self.transitions = 0

        self.unfaithful = 0
        self.digest_ok = 0
        self.placement_ok = 0

        self.events = 0
        self.kinds: collections.Counter = collections.Counter()
        self.silent = 0

        # (zone, owner, host) -> steps on which more than one area shared it.
        self.collisions: collections.Counter = collections.Counter()
        self.collision_example: Dict[Any, Any] = {}
        self.areas: set = set()

        self.failures: List[Dict[str, Any]] = []
        self.errors: List[str] = []

    def Fail(self, detail: Dict[str, Any]) -> None:
        if len(self.failures) < self.KEEP_FAILURES:
            self.failures.append(detail)

    @property
    def ok(self) -> bool:
        return (self.unfaithful == 0
                and self.digest_ok == self.transitions
                and self.placement_ok == self.transitions
                and not self.errors)


def _Collisions(board: model.Board, verdict: Verdict) -> None:
    """Does `(zone, owner, host)` name one area, or several?

    `AreaRef` in `src/Marvel.Rules/Events/AreaRef.cs` is that triple. If the
    engine ever has two areas sharing one, the triple is a description rather
    than an address and the type needs an identity beside it -- so this counts
    rather than assumes.
    """
    by_triple: Dict[Tuple[str, int, int], set] = collections.defaultdict(set)
    for record in board.values():
        area = record["area"]
        by_triple[(area["zone"], area["owner"], area["host"])].add(area["id"])

    for triple, ids in by_triple.items():
        verdict.areas.add(triple)
        if len(ids) > 1:
            verdict.collisions[triple] += 1
            verdict.collision_example.setdefault(triple, sorted(ids))


def _Placement(board: model.Board) -> Dict[int, Tuple[Any, int]]:
    """Where every card sits, as an area identity and an index."""
    return {object_id: (record["area"]["id"], record["index"])
            for object_id, record in board.items()}


def _Residue(produced: model.Board, expected: model.Board,
             limit: int = 6) -> List[str]:
    """The first few cards that did not come out where they should have."""
    lines: List[str] = []
    left, right = _Placement(produced), _Placement(expected)
    for object_id in sorted(set(left) | set(right)):
        if left.get(object_id) == right.get(object_id):
            continue
        lines.append(f"c{object_id} {left.get(object_id)} -> want {right.get(object_id)}")
        if len(lines) >= limit:
            break
    return lines


def Verify(root: str, *, only: Sequence[str] = (), scenes: int = 0,
           per_shard: int = 0, max_steps: int = 0) -> Verdict:
    verdict = Verdict()

    for scene, text in observe.scenes(root, only=only, limit=scenes,
                                      per_shard=per_shard):
        verdict.scenes += 1
        previous: Optional[model.Board] = None
        # Carried across the callback boundary: the labels belong to the input
        # that produced the *next* state, so they are read one step late.
        pending: Tuple[str, str] = ("", "")

        def on_step(obs: observe.Observation) -> None:
            nonlocal previous, pending

            board = state.Snapshot(obs.world)
            verdict.steps += 1

            # Step 2. The snapshot must be the digest, or nothing below means
            # anything.
            if state.Serialize(board) != obs.world.render.CalculateDigest():
                verdict.unfaithful += 1
                verdict.Fail({
                    "scene": obs.scene, "step": obs.step,
                    "why": "the snapshot does not serialise to the engine's digest",
                })
                previous = board
                return

            _Collisions(board, verdict)

            if previous is not None:
                trigger, verb = pending
                verdict.transitions += 1
                events = model.Derive(previous, board, trigger, verb)
                verdict.events += len(events)
                for event in events:
                    verdict.kinds[event["kind"]] += 1
                if not events:
                    verdict.silent += 1

                produced = model.Apply(previous, events)

                if model.Serialize(produced) == model.Serialize(board):
                    verdict.digest_ok += 1
                else:
                    verdict.Fail({
                        "scene": obs.scene, "step": obs.step,
                        "why": "the digest form did not reproduce",
                        "residue": _Residue(produced, board),
                    })

                if _Placement(produced) == _Placement(board):
                    verdict.placement_ok += 1
                else:
                    verdict.Fail({
                        "scene": obs.scene, "step": obs.step,
                        "why": "placement did not reproduce",
                        "trigger": trigger, "verb": verb,
                        "residue": _Residue(produced, board),
                    })

            previous = board
            pending = (obs.trigger, obs.verb)

        result = observe.replay_text(scene, text, on_step, max_steps=max_steps)
        if result.completed:
            verdict.scenes_completed += 1
        if result.error:
            verdict.errors.append(f"{scene}: {result.error}")

    return verdict


def Report(verdict: Verdict) -> List[str]:
    def share(count: int, total: int) -> str:
        return f"{count}/{total} ({100.0 * count / total:.1f}%)" if total else "0/0"

    lines = [
        f"scenes            {verdict.scenes} ({verdict.scenes_completed} replayed to the end)",
        f"steps             {verdict.steps}",
        f"transitions       {verdict.transitions}",
        f"events            {verdict.events}",
        f"silent steps      {share(verdict.silent, verdict.transitions)}",
        "",
        f"snapshot == digest    {share(verdict.steps - verdict.unfaithful, verdict.steps)}",
        f"digest reproduced     {share(verdict.digest_ok, verdict.transitions)}",
        f"placement reproduced  {share(verdict.placement_ok, verdict.transitions)}",
        "",
        "event kinds:",
    ]
    for kind in sorted(model.VOCABULARY):
        count = verdict.kinds.get(kind, 0)
        mark = " " if count else "  never fired"
        lines.append(f"  {kind:<17} {count}{mark}")

    lines.append("")
    lines.append(f"areas by (zone, owner, host): {len(verdict.areas)} distinct triples, "
                 f"{len(verdict.collisions)} of them naming more than one area")
    for triple, count in verdict.collisions.most_common(10):
        example = verdict.collision_example.get(triple, [])
        lines.append(f"  {triple} on {count} steps, e.g. areas {example}")

    for failure in verdict.failures:
        lines.append("")
        lines.append(f"FAILED {os.path.basename(failure['scene'])} step {failure['step']}: "
                     f"{failure['why']}")
        for line in failure.get("residue", []):
            lines.append(f"    {line}")

    for error in verdict.errors:
        lines.append(f"ERROR {error}")

    return lines


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Verify the event stream against the frozen corpus (MARVEL-163).")
    parser.add_argument("shards", help="the corpus shard directory")
    parser.add_argument("--only", nargs="*", default=(),
                        help="shard names, e.g. rhino klaw")
    parser.add_argument("--scenes", type=int, default=0,
                        help="stop after this many scenes in total")
    parser.add_argument("--per-shard", type=int, default=0,
                        help="take this many scenes from each shard")
    parser.add_argument("--max-steps", type=int, default=0,
                        help="stop each scene after this many decisions")
    parser.add_argument("--json", default="", help="write the verdict here")
    args = parser.parse_args(list(argv) if argv is not None else None)

    verdict = Verify(args.shards, only=args.only, scenes=args.scenes,
                     per_shard=args.per_shard, max_steps=args.max_steps)

    for line in Report(verdict):
        print(line)

    if args.json:
        with open(args.json, "w", encoding="utf-8") as handle:
            json.dump({
                "scenes": verdict.scenes,
                "steps": verdict.steps,
                "transitions": verdict.transitions,
                "events": verdict.events,
                "silent": verdict.silent,
                "unfaithful": verdict.unfaithful,
                "digest_ok": verdict.digest_ok,
                "placement_ok": verdict.placement_ok,
                "kinds": dict(verdict.kinds),
                "area_triples": len(verdict.areas),
                "collisions": {str(k): v for k, v in verdict.collisions.items()},
                "collision_example": {str(k): v for k, v in verdict.collision_example.items()},
                "failures": verdict.failures,
                "errors": verdict.errors,
            }, handle, indent=2, sort_keys=True)

    return 0 if verdict.ok else 1


if __name__ == "__main__":
    sys.exit(main())
