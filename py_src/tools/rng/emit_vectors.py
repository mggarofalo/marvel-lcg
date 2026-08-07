"""Emit the cross-language RNG test vectors.

Writes `datasets/rng/vectors.json`, the acceptance fixture for MARVEL-8. Every
case is expressed in terms a C# implementer can reproduce without reading
Python: a seed, an operation, and the expected result. Sequence inputs are
always `[0, 1, ..., length - 1]` so no input data has to be shipped alongside.

The interesting case is `mixed`. Per-function vectors can all pass while the
stream position is still wrong -- a function that consumes one word too many
looks fine in isolation and breaks everything after it. `mixed` interleaves the
operations and checkpoints the raw stream between them, so a position error
shows up as the first failing step rather than as a whole game diverging.

Run:
    python -m tools.rng.emit_vectors
    python -m tools.rng.emit_vectors --check   # non-zero if the file is stale

The output is byte-stable: same code, same file, no timestamps.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from typing import Any, Dict, List

OUTPUT = os.path.join("..", "datasets", "rng", "vectors.json")

# One arbitrary but fixed seed set. 42 is the value `docs/rng-contract.md`
# quotes and the one numpy's stream was compared against; 0 is included
# because it is the edge of the seeding routine.
SEEDS = [0, 1, 42, 12345, 20260807]


class Counting:
    """Wraps the generator to count raw words, so vectors can pin consumption."""

    def __init__(self, rng: Any) -> None:
        self.rng = rng
        self.words = 0
        original = rng.NextUInt32

        def Counted() -> int:
            self.words += 1
            return original()

        rng.NextUInt32 = Counted  # type: ignore[method-assign]

    def Reset(self) -> int:
        used, self.words = self.words, 0
        return used


def _new(seed: int) -> Any:
    from engine.lib.mt19937 import Mt19937

    return Mt19937(seed)


def _sequence(length: int) -> List[int]:
    return list(range(length))


def _next_uint32() -> List[Dict[str, Any]]:
    cases = []
    for seed in SEEDS:
        rng = _new(seed)
        cases.append({
            "seed": seed,
            "count": 8,
            "expect": [rng.NextUInt32() for _ in range(8)],
        })
    # Cross the first twist boundary: word 624 is the first of the second
    # block, which is where an off-by-one in `index` shows up.
    rng = _new(42)
    for _ in range(623):
        rng.NextUInt32()
    cases.append({
        "seed": 42,
        "skip": 623,
        "count": 4,
        "expect": [rng.NextUInt32() for _ in range(4)],
        "why": "spans the twist boundary at word 624",
    })
    return cases


def _next_below() -> List[Dict[str, Any]]:
    # 1 exercises the no-special-case rule; 2 and 256 never reject; 3, 52 and
    # 1000 do; 2**32 is the maximum and never rejects.
    cases = []
    for n in [1, 2, 3, 52, 256, 1000, 2**32]:
        for seed in [42, 20260807]:
            rng = _new(seed)
            counting = Counting(rng)
            expect = [rng.NextBelow(n) for _ in range(10)]
            cases.append({
                "seed": seed,
                "n": n,
                "count": 10,
                "expect": expect,
                "words_consumed": counting.Reset(),
            })
    return cases


def _shuffle() -> List[Dict[str, Any]]:
    cases = []
    for length in [0, 1, 2, 5, 52]:
        for seed in [1, 42]:
            rng = _new(seed)
            counting = Counting(rng)
            items = _sequence(length)
            rng.Shuffle(items)
            cases.append({
                "seed": seed,
                "length": length,
                "expect": items,
                "words_consumed": counting.Reset(),
            })
    return cases


def _choice() -> List[Dict[str, Any]]:
    cases = []
    for length in [1, 7, 100]:
        for seed in [3, 42]:
            rng = _new(seed)
            sequence = _sequence(length)
            cases.append({
                "seed": seed,
                "length": length,
                "count": 5,
                "expect": [rng.Choice(sequence) for _ in range(5)],
            })
    return cases


def _choose_without_replacement() -> List[Dict[str, Any]]:
    cases = []
    for length, k in [(1, 0), (1, 1), (9, 4), (20, 3), (20, 20), (52, 7)]:
        for seed in [7, 42]:
            rng = _new(seed)
            counting = Counting(rng)
            expect = rng.ChooseWithoutReplacement(_sequence(length), k)
            cases.append({
                "seed": seed,
                "length": length,
                "k": k,
                "expect": expect,
                "words_consumed": counting.Reset(),
            })
    return cases


def _engine_choice2() -> List[Dict[str, Any]]:
    """The facade's short-circuit, which C# must reproduce.

    `Random.RandomChoice2(seq, x)` returns the input order and consumes
    nothing when `x == len(seq)`. It is the one place the engine layer diverges
    from the core, so it gets its own vectors.
    """
    from engine.lib.random import Random

    cases = []
    for length, x in [(4, 4), (4, 0), (4, 1), (4, 3), (1, 1)]:
        Random.SetSeed(99)
        counting = Counting(Random.rand)
        expect = Random.RandomChoice2(_sequence(length), x)
        cases.append({
            "seed": 99,
            "length": length,
            "x": x,
            "expect": expect,
            "words_consumed": counting.Reset(),
        })
    return cases


def _mixed() -> Dict[str, Any]:
    seed = 20260807
    rng = _new(seed)
    counting = Counting(rng)
    steps: List[Dict[str, Any]] = []

    def Step(entry: Dict[str, Any]) -> None:
        entry["words_consumed"] = counting.Reset()
        steps.append(entry)

    Step({"op": "next_uint32", "result": rng.NextUInt32()})
    Step({"op": "next_below", "n": 6, "result": rng.NextBelow(6)})

    items = _sequence(5)
    rng.Shuffle(items)
    Step({"op": "shuffle", "length": 5, "result": items})

    Step({"op": "choice", "length": 7, "result": rng.Choice(_sequence(7))})
    Step({
        "op": "choose_without_replacement", "length": 9, "k": 4,
        "result": rng.ChooseWithoutReplacement(_sequence(9), 4),
    })
    Step({"op": "next_below", "n": 3, "result": rng.NextBelow(3)})

    items = _sequence(13)
    rng.Shuffle(items)
    Step({"op": "shuffle", "length": 13, "result": items})

    # A raw word at the end: if any step above consumed the wrong number of
    # words, this is what disagrees even when every result above matched.
    Step({"op": "next_uint32", "result": rng.NextUInt32()})

    return {"seed": seed, "steps": steps}


def Build() -> Dict[str, Any]:
    from engine.lib import Ver

    Ver.Initialize()
    return {
        "contract": "docs/rng-contract.md",
        "generated_by": "py_src/tools/rng/emit_vectors.py",
        "engine_build": str(Ver.version),
        "note": "Sequence inputs are always [0, 1, ..., length - 1].",
        "cases": {
            "next_uint32": _next_uint32(),
            "next_below": _next_below(),
            "shuffle": _shuffle(),
            "choice": _choice(),
            "choose_without_replacement": _choose_without_replacement(),
            "engine_choice2": _engine_choice2(),
            "mixed": _mixed(),
        },
    }


def Serialise(vectors: Dict[str, Any]) -> str:
    return json.dumps(vectors, indent=2, sort_keys=True) + "\n"


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true",
                        help="do not write; exit non-zero if the file is stale")
    args = parser.parse_args(argv)

    content = Serialise(Build())

    existing = None
    if os.path.exists(OUTPUT):
        with open(OUTPUT, "r", encoding="utf-8") as handle:
            existing = handle.read()

    if existing == content:
        print(f"{OUTPUT} is up to date")
        return 0

    if args.check:
        state = "missing" if existing is None else "stale"
        print(f"{OUTPUT} is {state}; run: python -m tools.rng.emit_vectors",
              file=sys.stderr)
        return 1

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(content)
    print(f"wrote {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
