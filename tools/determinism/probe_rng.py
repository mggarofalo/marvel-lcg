"""Probe: is the seeded RNG reproducible, and do the two backends agree?

`engine/lib/random.py` dispatches on the `disable_numpy_random` config flag,
which defaults to False. So `numpy.random` is the production RNG and
`engine/lib/mt19937.py` is dead code unless the flag is set.

This probe draws a fixed sequence from each backend and prints a digest. Run it
in several processes: the digest for a given backend must not move. The two
backends' digests are expected to differ -- they are different generators, and
`mt19937.shuffle` is not Fisher-Yates. Which one the C# port must reproduce is
a separate decision (MARVEL-25); this probe just makes the divergence concrete
and gives each backend a golden value to regress against.

Run:  python -m tools.determinism.probe_rng
      python -m tools.determinism.probe_rng --child numpy
      python -m tools.determinism.probe_rng --child custom
"""

from __future__ import annotations

import hashlib
import os
import subprocess
import sys
from typing import List

SEED = 20260806
DRAWS = 200


def _draw_sequence(backend: str) -> str:
    """Exercise every entry point the game uses, in a fixed order."""
    from engine.lib import Ver

    Ver.Initialize()
    from engine.config import ConfigVariables

    ConfigVariables.Initialize()

    import engine.lib.random as engine_random

    engine_random.DISABLE_NUMPY_RANDOM.value = backend == "custom"

    Random = engine_random.Random
    Random.SetSeed(SEED)

    pool = list(range(64))
    out: List[str] = []

    for i in range(DRAWS):
        out.append(str(Random.RandomChoice(pool)))

        picks = Random.RandomChoice2(pool, 3)
        out.append(",".join(str(x) for x in picks))

        deck = list(range(12))
        Random.Shuffle(deck)
        out.append(",".join(str(x) for x in deck))

    return hashlib.sha256("\n".join(out).encode("utf-8")).hexdigest()


def _child(backend: str) -> int:
    print(f"{backend}={_draw_sequence(backend)}")
    return 0


def _spawn(backend: str, runs: int) -> List[str]:
    env = dict(os.environ)
    results: List[str] = []
    for _ in range(runs):
        proc = subprocess.run(
            [sys.executable, "-m", "tools.determinism.probe_rng", "--child", backend],
            capture_output=True,
            text=True,
            env=env,
            cwd=os.getcwd(),
        )
        if proc.returncode != 0:
            results.append("ERROR: " + proc.stderr.strip().splitlines()[-1])
        else:
            results.append(proc.stdout.strip().split("=", 1)[1])
    return results


def main(runs: int = 5) -> int:
    failures = 0
    digests = {}
    for backend in ("numpy", "custom"):
        seen = _spawn(backend, runs)
        distinct = sorted(set(seen))
        digests[backend] = distinct[0] if len(distinct) == 1 else None
        status = "stable" if len(distinct) == 1 else f"UNSTABLE ({len(distinct)} distinct)"
        print(f"{backend:<7} across {runs} processes: {status}")
        for d in distinct:
            print(f"    {d}")
        if len(distinct) != 1:
            failures += 1

    if digests["numpy"] and digests["custom"]:
        same = digests["numpy"] == digests["custom"]
        print(f"\nbackends agree: {same}")
        if same:
            print("  Unexpected -- the two generators should not produce the same stream.")
    return failures


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--child":
        raise SystemExit(_child(sys.argv[2]))
    raise SystemExit(1 if main() else 0)
