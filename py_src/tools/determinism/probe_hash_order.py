"""Probe: which container orderings are reproducible across processes?

This does not touch the engine. It establishes the ground truth the audit
relies on, so the claim can be re-checked on a new Python build or a new OS
instead of being taken on trust.

Three cases:

  str   -- `set` of strings. Ordered by siphash, keyed on PYTHONHASHSEED.
  int   -- `set` of small ints. `hash(i) == i`, so order is stable.
  obj   -- `set` of plain objects. Ordered by identity hash, i.e. by address.

The `obj` case is the one that matters: the engine holds sets of `CardFace`
and `Player`, neither of which defines `__hash__`.

Run:  python -m tools.determinism.probe_hash_order
"""

from __future__ import annotations

import os
import subprocess
import sys
from typing import List


class _Obj:
    __slots__ = ("n",)

    def __init__(self, n: int) -> None:
        self.n = n

    def __repr__(self) -> str:
        return f"o{self.n}"


def _signatures(perturb: int) -> dict[str, str]:
    # Incidental allocation, then freed. Stands in for anything that shifts
    # the allocator: an extra log line, a longer card name, another player.
    junk = ["x" * (i % 97) for i in range(perturb)]
    del junk

    strings = {
        "Spider-Man", "Captain America", "Iron Man", "Black Panther",
        "Ms. Marvel", "She-Hulk", "Thor", "Hulk", "Wasp", "Quicksilver",
    }
    ints = set(range(1, 11))
    objs = set(_Obj(i) for i in range(10))

    return {
        "str": ",".join(strings),
        "int": ",".join(str(x) for x in ints),
        "obj": ",".join(repr(x) for x in objs),
    }


def _child(perturb: int) -> int:
    sig = _signatures(perturb)
    print("|".join(f"{k}={v}" for k, v in sorted(sig.items())))
    return 0


def _spawn(runs: int, hash_seed: str | None, perturb: int) -> List[str]:
    env = dict(os.environ)
    if hash_seed is None:
        env.pop("PYTHONHASHSEED", None)
    else:
        env["PYTHONHASHSEED"] = hash_seed
    out: List[str] = []
    for _ in range(runs):
        proc = subprocess.run(
            [sys.executable, "-m", "tools.determinism.probe_hash_order", "--child", str(perturb)],
            capture_output=True,
            text=True,
            env=env,
            cwd=os.getcwd(),
        )
        if proc.returncode != 0:
            raise RuntimeError(proc.stderr)
        out.append(proc.stdout.strip())
    return out


def _parse(lines: List[str]) -> dict[str, set]:
    seen: dict[str, set] = {"str": set(), "int": set(), "obj": set()}
    for line in lines:
        for part in line.split("|"):
            key, _, value = part.partition("=")
            seen[key].add(value)
    return seen


def main(runs: int = 8) -> int:
    failures = 0

    for label, seed in (("PYTHONHASHSEED unset", None), ("PYTHONHASHSEED=0", "0")):
        seen = _parse(_spawn(runs, seed, perturb=0))
        print(f"\n{label}  ({runs} fresh processes)")
        for key in ("str", "int", "obj"):
            n = len(seen[key])
            print(f"  set of {key:<4} : {n} distinct ordering(s) {'stable' if n == 1 else 'UNSTABLE'}")

    # Even with the hash seed pinned, identity ordering only holds while the
    # allocation history is byte-identical. Show how little it takes to move.
    print("\nPYTHONHASHSEED=0, varying incidental allocation")
    orders = set()
    for perturb in (0, 1, 7, 64, 1000, 5000):
        seen = _parse(_spawn(1, "0", perturb=perturb))
        order = next(iter(seen["obj"]))
        orders.add(order)
        print(f"  perturb={perturb:<6} obj={order}")
    if len(orders) > 1:
        print(f"  -> {len(orders)} distinct orderings. Identity-hashed set order is")
        print("     incidentally stable, not deterministic. Pinning PYTHONHASHSEED")
        print("     does not fix it.")
        failures += 1
    else:
        print("  -> unchanged across perturbations on this build (re-check on others)")

    return failures


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--child":
        raise SystemExit(_child(int(sys.argv[2])))
    raise SystemExit(0 if main() == 0 else 0)  # informational, never gates
