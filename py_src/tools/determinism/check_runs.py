"""Run the same game N times in fresh processes and diff the per-step digests.

This is the acceptance test for MARVEL-7: same seed plus same inputs must give
byte-identical per-step digests across runs, across processes, and across
operating systems.

Today it drives the engine with the decline-only `NullDeviceManager` from
`headless.py`, because no bot exists yet. That already covers game setup, the
villain phase, and every forced ability -- the parts of the engine where the
ordering hazards live. When the real bot lands (MARVEL-5), point `--driver` at
it and the comparison logic here is unchanged.

Run:
    python -m tools.determinism.check_runs --runs 100
    python -m tools.determinism.check_runs --runs 20 --matrix wide
    python -m tools.determinism.check_runs --runs 10 --no-pin   # show what
                                                                # unpinned does
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from collections import defaultdict
from typing import Dict, List, Tuple

from tools.determinism.pinned_env import build_env

Case = Tuple[str, List[str], int]

# Solo, two-hero and four-hero games. Multi-hero cases matter most: several of
# the ordering hazards in the audit only bite when two forced abilities from
# different cards trigger on the same message.
MATRIX_SMOKE: List[Case] = [
    ("rhino", ["spider_man"], 12345),
    ("klaw", ["captain_marvel", "she_hulk"], 999),
]

MATRIX_WIDE: List[Case] = MATRIX_SMOKE + [
    ("ultron", ["iron_man", "thor", "black_panther", "captain_america"], 4242),
    ("the_wrecking_crew", ["ms_marvel", "hawkeye"], 77),
    ("crossbones", ["black_widow", "doctor_strange", "hulk"], 20260806),
    ("mutagen_formula", ["cyclops", "jubilee"], 31337),
    ("thanos", ["gamora", "drax"], 555),
]


def run_once(case: Case, max_steps: int, pin: bool) -> Dict:
    campaign, heroes, seed = case
    env = build_env() if pin else dict(os.environ)
    proc = subprocess.run(
        [
            sys.executable,
            "-m",
            "tools.determinism.headless",
            campaign,
            ",".join(heroes),
            str(seed),
            str(max_steps),
        ],
        capture_output=True,
        text=True,
        env=env,
        cwd=os.getcwd(),
        errors="replace",
    )
    marker = "<<<RESULT>>>"
    for line in proc.stdout.splitlines():
        if line.startswith(marker):
            return json.loads(line[len(marker):])
    raise RuntimeError(
        f"no result from {campaign}/{heroes}/{seed}\n"
        f"stdout tail: {proc.stdout[-800:]}\nstderr tail: {proc.stderr[-800:]}"
    )


def first_divergent_step(a: Dict, b: Dict) -> str:
    steps_a, steps_b = a["steps"], b["steps"]
    for i in range(min(len(steps_a), len(steps_b))):
        if steps_a[i] != steps_b[i]:
            return (
                f"step {i} ({steps_a[i]['e']})\n"
                f"      run A: {steps_a[i]['digest']}\n"
                f"      run B: {steps_b[i]['digest']}"
            )
    if len(steps_a) != len(steps_b):
        return f"step counts differ: {len(steps_a)} vs {len(steps_b)}"
    return "traces equal but object_index differs: " \
           f"{a['object_index']} vs {b['object_index']}"


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runs", type=int, default=10)
    parser.add_argument("--matrix", choices=("smoke", "wide"), default="smoke")
    parser.add_argument("--max-steps", type=int, default=400)
    parser.add_argument("--no-pin", action="store_true",
                        help="run without the pinned environment (diagnostic)")
    args = parser.parse_args(argv)

    cases = MATRIX_SMOKE if args.matrix == "smoke" else MATRIX_WIDE
    pin = not args.no_pin

    print(f"{args.runs} run(s) per case, {len(cases)} case(s), "
          f"environment {'pinned' if pin else 'NOT pinned'}\n")

    failures = 0
    for case in cases:
        campaign, heroes, seed = case
        label = f"{campaign} / {'+'.join(heroes)} / seed {seed}"
        by_digest: Dict[str, List[Dict]] = defaultdict(list)
        errors: List[str] = []

        for _ in range(args.runs):
            try:
                result = run_once(case, args.max_steps, pin)
            except RuntimeError as exc:
                errors.append(str(exc))
                continue
            by_digest[result["digest"]].append(result)

        if errors:
            failures += 1
            print(f"FAIL {label}\n     {len(errors)} run(s) produced no result")
            print("     " + errors[0].splitlines()[0])
            continue

        sample = next(iter(by_digest.values()))[0]
        if sample["error"]:
            print(f"WARN {label}\n     engine raised: {sample['error']}")

        if sample["step_count"] == 0:
            # A run that never reached a decision proves nothing; identical
            # empty traces are not evidence of determinism.
            failures += 1
            print(f"FAIL {label}  0 steps -- the driver never reached a decision")
            continue

        if len(by_digest) == 1:
            print(f"PASS {label}  ({sample['step_count']} steps, "
                  f"digest {sample['digest'][:16]})")
        else:
            failures += 1
            groups = sorted(by_digest.items(), key=lambda kv: -len(kv[1]))
            print(f"FAIL {label}  {len(by_digest)} distinct digests")
            for digest, runs in groups:
                print(f"     {len(runs):>3} run(s)  {digest[:16]}")
            print("     " + first_divergent_step(groups[0][1][0], groups[1][1][0]))

    print()
    if failures:
        print(f"{failures} case(s) nondeterministic")
        return 1
    print("all cases reproduced byte-identically")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
