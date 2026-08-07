"""Probe: is the seeded RNG reproducible across processes?

Before MARVEL-38 this probe existed to measure a divergence: `engine/lib/random.py`
dispatched on `disable_numpy_random`, so the engine had two incompatible
generators and which one you got depended on a config flag and on whether numpy
happened to be installed. The probe printed both digests to make that concrete.

There is one generator now, specified in `docs/rng-contract.md`. So what is
left to check is the property that actually matters: the same seed produces the
same stream in a fresh process, every time. Cross-*language* agreement is a
different question and is settled by `datasets/rng/vectors.json`, replayed by
`unit_test/test_rng.py`.

Run:  python -m tools.determinism.probe_rng
      python -m tools.determinism.probe_rng --runs 20
      python -m tools.determinism.probe_rng --child
"""

from __future__ import annotations

import argparse
import hashlib
import os
import subprocess
import sys
from typing import List

from tools.determinism.pinned_env import build_env

SEED = 20260806
DRAWS = 200


def _draw_sequence() -> str:
    """Exercise every entry point the game uses, in a fixed order."""
    from engine.lib import Ver

    Ver.Initialize()
    from engine.config import ConfigVariables

    ConfigVariables.Initialize()

    from engine.lib.random import Random

    Random.SetSeed(SEED)

    pool = list(range(64))
    out: List[str] = []

    for _ in range(DRAWS):
        out.append(str(Random.RandomChoice(pool)))

        picks = Random.RandomChoice2(pool, 3)
        out.append(",".join(str(x) for x in picks))

        deck = list(range(12))
        Random.Shuffle(deck)
        out.append(",".join(str(x) for x in deck))

    return hashlib.sha256("\n".join(out).encode("utf-8")).hexdigest()


def _child() -> int:
    print(f"digest={_draw_sequence()}")
    return 0


def _spawn(runs: int) -> List[str]:
    env = build_env()
    results: List[str] = []
    for _ in range(runs):
        proc = subprocess.run(
            [sys.executable, "-m", "tools.determinism.probe_rng", "--child"],
            capture_output=True,
            text=True,
            env=env,
            cwd=os.getcwd(),
            errors="replace",
        )
        if proc.returncode != 0:
            tail = proc.stderr.strip().splitlines()
            results.append("ERROR: " + (tail[-1] if tail else "no output"))
        else:
            results.append(proc.stdout.strip().split("=", 1)[1])
    return results


def main(runs: int = 5) -> int:
    seen = _spawn(runs)
    distinct = sorted(set(seen))

    print(f"across {runs} processes: "
          f"{'stable' if len(distinct) == 1 else f'UNSTABLE ({len(distinct)} distinct)'}")
    for digest in distinct:
        print(f"    {digest}")

    if len(distinct) != 1:
        return 1
    print("\nCross-language agreement is a separate question -- see "
          "datasets/rng/vectors.json and unit_test/test_rng.py.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--child":
        raise SystemExit(_child())
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runs", type=int, default=5)
    raise SystemExit(main(parser.parse_args().runs))
