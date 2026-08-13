"""Emit the cross-language state-digest test vectors.

Writes `datasets/digest/vectors.json`, the acceptance fixture for a C# port of
`docs/state-digest-v2.md`, and the tripwire that stops the Python side moving the
format without saying so. The RNG contract has the same pair
(`tools/rng/emit_vectors.py`, `datasets/rng/vectors.json`) for the same reason.

Two kinds of case, because the two failure modes are different:

  `worked`      every step's digest in full. An implementer debugs against this:
                a byte difference is visible, and the canonical serialisation can
                be checked directly rather than inferred.
  `fingerprint` `sha256` of each step's digest, for the other boards. Breadth
                without the bulk -- a full trace is roughly 11 KB per step.

Run:
    python -m tools.digest.emit_vectors
    python -m tools.digest.emit_vectors --check   # non-zero if the file is stale

The output is byte-stable: same code, same engine, same file, no timestamps.
Run it under the pinned environment (`tools/determinism/pinned_env.py`); it
boots the engine and plays, so it takes seconds rather than milliseconds and
does not belong in the fast unit tier.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
from typing import Any, Dict, List, Sequence, Tuple

from tools import fixtures
from tools.determinism.pinned_env import is_pinned

OUTPUT = os.path.join("..", "datasets", "digest", "vectors.json")

# (campaign, heroes, seed, max_steps, full). `full` marks the one board whose
# digests are stored in their entirety.
Case = Tuple[str, List[str], int, int, bool]

CASES: List[Case] = [
    ("rhino", ["spider_man"], 12345, 20, True),
    ("klaw", ["she_hulk"], 2026, 12, False),
    ("ultron", ["black_panther"], 4242, 12, False),
]


def _Run(campaign: str, heroes: Sequence[str], seed: int, max_steps: int) -> List[str]:
    """Per-step digests from one headless game, in order."""
    from tools.determinism.headless import run_headless

    digests: List[str] = []

    def decide(player_id: int, payload: Any) -> str:
        from engine import Engine
        world = Engine.game.world
        if world is not None:
            digests.append(world.render.CalculateDigest())
        return "{}"

    run_headless(campaign, heroes, seed, max_steps=max_steps, decide=decide)
    return digests


def Build() -> Dict[str, Any]:
    from engine.lib import Ver
    from game.world import digest

    cases: List[Dict[str, Any]] = []
    for campaign, heroes, seed, max_steps, full in CASES:
        digests = _Run(campaign, heroes, seed, max_steps)
        case: Dict[str, Any] = {
            "campaign": campaign,
            "heroes": list(heroes),
            "seed": seed,
            "max_steps": max_steps,
            "steps": len(digests),
            # One value covering the whole trace, so a port can fail fast before
            # working out which step diverged.
            "trace_sha256": hashlib.sha256(
                "\n".join(digests).encode("utf-8")
            ).hexdigest(),
            "step_sha256": [digest.Fingerprint(text) for text in digests],
        }
        if full:
            case["step_digests"] = digests
        cases.append(case)

    return {
        "contract": "docs/state-digest-v2.md",
        "digest_version": digest.DIGEST_VERSION,
        "engine_build": str(Ver.version),
        "generated_by": "py_src/tools/digest/emit_vectors.py",
        "note": "step_digests is the canonical serialisation, byte for byte.",
        "cases": cases,
    }


def Render(document: Dict[str, Any]) -> str:
    return json.dumps(document, indent=2, sort_keys=True) + "\n"


def _main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true",
                        help="exit non-zero if the checked-in file is stale")
    args = parser.parse_args(list(argv))

    if not is_pinned():
        print("warning: not running under the pinned environment "
              "(see tools/determinism/pinned_env.py)", file=sys.stderr)

    rendered = Render(Build())

    if args.check:
        # Compared byte for byte with the file on disk. `tools/fixtures.py`
        # holds that decision and its consequences, and is the one place all
        # three fixture gates take their meaning of "stale" from. Byte equality
        # is the claim worth making here in particular: `step_digests` is the
        # canonical serialisation a C# port is accepted against character for
        # character.
        verdict = fixtures.Compare(rendered, OUTPUT)
        if verdict != fixtures.FRESH:
            print(fixtures.Explain(verdict, OUTPUT,
                                   "python -m tools.digest.emit_vectors"),
                  file=sys.stderr)
            return 1
        print(f"{OUTPUT} is up to date")
        return 0

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(rendered)
    print(f"wrote {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
