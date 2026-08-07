"""Probe: how often does sorting local effects actually change their order?

`EventManager.FindLocalEffects` gathers on-card effects by walking
`Message2.related_faces`, a `Set[CardFace]` that iterates by memory address,
and now sorts the result by `Effect.object_id` (MARVEL-31).

Per-step digests are unchanged across the whole `check_runs` matrix after that
sort. On its own that is ambiguous: it could mean the sort agrees with the
address order the engine happened to produce, or it could mean the situation
never arose. This probe distinguishes the two by counting, for every call:

  batches            calls that gathered at least one effect
  multi              calls that gathered two or more -- where order can matter
  reordered          calls where sorting moved something

`reordered = 0` with `multi > 0` says the sort was exercised and agreed with
the address order in this run. `multi = 0` says the run proves nothing, and the
digest evidence should not be read as coverage.

Run:  python -m tools.determinism.probe_local_effect_order
      python -m tools.determinism.probe_local_effect_order --matrix wide
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from typing import Any, Dict, List

from tools.determinism.check_runs import MATRIX_SMOKE, MATRIX_WIDE
from tools.determinism.pinned_env import build_env

MARKER = "<<<PROBE>>>"


class Counters:
    def __init__(self) -> None:
        self.batches = 0
        self.multi = 0
        self.reordered = 0
        self.largest = 0

    def Record(self, gathered: List[Any], ordered: List[Any]) -> None:
        if not gathered:
            return
        self.batches += 1
        self.largest = max(self.largest, len(gathered))
        if len(gathered) > 1:
            self.multi += 1
            if gathered != ordered:
                self.reordered += 1

    def AsDict(self) -> Dict[str, int]:
        return {
            "batches": self.batches,
            "multi": self.multi,
            "reordered": self.reordered,
            "largest": self.largest,
        }


def _child(campaign: str, heroes: List[str], seed: int, max_steps: int) -> int:
    from tools.determinism.headless import _initialize_engine, run_headless

    _initialize_engine()

    from game.event.manager import EventManager

    counters = Counters()
    find_local_effects = EventManager.FindLocalEffects

    def Instrumented(message: Any) -> List[Any]:
        # Both halves are the production functions -- the probe never restates
        # the gather, so it cannot drift away from what the engine does.
        # `GatherLocalEffects` runs twice per message; this is a probe.
        gathered = EventManager.GatherLocalEffects(message)
        ordered = find_local_effects(message)
        counters.Record(gathered, ordered)
        return ordered

    EventManager.FindLocalEffects = staticmethod(Instrumented)  # type: ignore[assignment]

    result = run_headless(campaign, heroes, seed, max_steps=max_steps)

    payload = counters.AsDict()
    payload["steps"] = len(result.steps)
    payload["error"] = result.error
    print(MARKER + json.dumps(payload))
    return 0


def _spawn(campaign: str, heroes: List[str], seed: int, max_steps: int) -> Dict[str, Any]:
    proc = subprocess.run(
        [
            sys.executable,
            "-m",
            "tools.determinism.probe_local_effect_order",
            "--child",
            campaign,
            ",".join(heroes),
            str(seed),
            str(max_steps),
        ],
        capture_output=True,
        text=True,
        env=build_env(),
        cwd=os.getcwd(),
        errors="replace",
    )
    for line in proc.stdout.splitlines():
        if line.startswith(MARKER):
            return json.loads(line[len(MARKER):])
    raise RuntimeError(
        f"no result from {campaign}/{heroes}/{seed}\n"
        f"stderr tail: {proc.stderr[-800:]}"
    )


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--matrix", choices=("smoke", "wide"), default="smoke")
    parser.add_argument("--max-steps", type=int, default=400)
    args = parser.parse_args(argv)

    cases = MATRIX_SMOKE if args.matrix == "smoke" else MATRIX_WIDE

    totals = Counters()
    for campaign, heroes, seed in cases:
        stats = _spawn(campaign, list(heroes), seed, args.max_steps)
        totals.batches += stats["batches"]
        totals.multi += stats["multi"]
        totals.reordered += stats["reordered"]
        totals.largest = max(totals.largest, stats["largest"])
        label = f"{campaign} / {'+'.join(heroes)} / seed {seed}"
        print(
            f"{label}\n"
            f"    {stats['steps']:>4} steps  "
            f"{stats['batches']:>6} batches  "
            f"{stats['multi']:>5} multi  "
            f"{stats['reordered']:>4} reordered  "
            f"largest {stats['largest']}"
        )
        if stats["error"]:
            print(f"    engine raised: {stats['error']}")

    print(
        f"\ntotal: {totals.batches} batches, {totals.multi} with two or more "
        f"effects, {totals.reordered} reordered by the sort "
        f"(largest batch {totals.largest})"
    )
    if totals.multi == 0:
        print("No multi-effect batch was reached -- this run does not exercise the sort.")
        return 1
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--child":
        raise SystemExit(
            _child(sys.argv[2], sys.argv[3].split(","), int(sys.argv[4]), int(sys.argv[5]))
        )
    raise SystemExit(main())
