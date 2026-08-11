"""Compare per-step digest traces produced on different operating systems.

`check_runs` proves the engine reproduces *itself* in fresh processes. It cannot
prove that Windows and Linux agree, because those two runs happen on two
machines and no single process sees both. This module is that comparison, split
into the two halves CI can execute: `emit` writes one trace per case on each
runner, and `compare` diffs the collected files once every runner has finished.

Why it matters more than it looks: a cross-OS digest divergence would mean the
replay corpus is only valid on the platform that generated it, which constrains
the whole C# validation strategy. The audit found the identity-hash orderings
(team-up units, forced-effect resolution) to be the likeliest to behave
differently under a different allocator, and those are exactly the paths the
decline-only driver walks. See `docs/determinism-audit.md` and MARVEL-35.

Run:
    python -m tools.determinism.cross_os emit --out trace-linux.json
    python -m tools.determinism.cross_os compare trace-linux.json trace-windows.json
"""

from __future__ import annotations

import argparse
import json
import platform
import sys
from typing import Dict, List, Tuple

from tools.determinism.check_runs import MATRIX_SMOKE, MATRIX_WIDE, Case, run_once

# The fields of a run that must match across operating systems.
# `persisted_index` is in here because id allocation order is part of the digest
# contract, and `error` is because an engine that raises on one OS and not the
# other has diverged even when both traces are otherwise equal.
#
# `persisted_index` rather than the whole `object_index`: the full index counts
# transient query objects whose ids are never written down, so it moves whenever
# anything asks the engine a question -- twice in one day, both times benign.
# See `headless.PERSISTED_ID_CATEGORIES` and MARVEL-75. The full index still
# travels in the trace file for diagnosis, it just does not decide a verdict.
COMPARED_FIELDS = ("digest", "step_count", "persisted_index", "game_over", "error")

# Recorded for the failure report, never compared -- these are *expected* to
# differ between runners. Keeping them out of COMPARED_FIELDS is the whole
# reason the two sets are named separately.
def _platform_block(label: str) -> Dict[str, str]:
    return {
        "label": label,
        "system": platform.system(),
        "release": platform.release(),
        "machine": platform.machine(),
        "python": platform.python_version(),
    }


def case_key(case: Case) -> str:
    campaign, heroes, seed = case
    return f"{campaign}/{'+'.join(heroes)}/{seed}"


def emit(label: str, matrix: str, max_steps: int) -> Dict:
    cases = MATRIX_SMOKE if matrix == "smoke" else MATRIX_WIDE
    traces: Dict[str, Dict] = {}

    for case in cases:
        key = case_key(case)
        result = run_once(case, max_steps, pin=True)
        if result["step_count"] == 0:
            # Identical empty traces are not evidence of agreement. Fail here
            # rather than let `compare` report a green cross-OS match built out
            # of two runs that never reached a decision.
            raise RuntimeError(f"{key}: 0 steps -- the driver never reached a decision")
        traces[key] = result
        print(f"emitted {key}  ({result['step_count']} steps, "
              f"digest {result['digest'][:16]})")

    return {
        "platform": _platform_block(label),
        "matrix": matrix,
        "max_steps": max_steps,
        "cases": traces,
    }


def describe_digests(a: str, b: str) -> str:
    """A card-by-card, field-by-field account of two differing digests.

    A v2 digest is a full board serialisation -- tens of kilobytes -- so
    printing the two of them is not a report, it is a haystack. `digest.Diff`
    is the engine's own differ, the one the replay loop prints on a mismatch,
    and it names the card and the field: `c49 01120  health 3 -> 2`.

    Imported lazily and defensively. This module's comparison is pure data and
    the fast tier tests it without booting anything; and a trace file can hold
    a digest that does not parse -- truncated, hand-edited, or written by an
    older format -- in which case a truncated raw view is still better than an
    exception out of the failure path.
    """
    try:
        import engine  # noqa: F401  the game packages resolve only after this
        from game.world import digest

        _, report = digest.Diff(a, b)
        return report or "(digests differ but the differ found no differing card)"
    except ValueError as exc:
        return f"(digest unreadable: {exc})\n      A: {a[:200]}\n      B: {b[:200]}"
    except ImportError:
        return f"A: {a[:200]}\n      B: {b[:200]}"


def first_divergence(a: Dict, b: Dict) -> str:
    """Locate the earliest disagreement between two traces of the same case."""
    for field in COMPARED_FIELDS:
        if field in ("digest", "step_count"):
            continue
        if a.get(field) != b.get(field):
            return f"{field} differs:\n      A: {a.get(field)}\n      B: {b.get(field)}"

    steps_a, steps_b = a.get("steps", []), b.get("steps", [])
    for i in range(min(len(steps_a), len(steps_b))):
        if steps_a[i] != steps_b[i]:
            return (
                f"first divergent step {i} ({steps_a[i].get('e')})\n"
                + describe_digests(steps_a[i].get("digest", ""),
                                   steps_b[i].get("digest", ""))
            )
    if len(steps_a) != len(steps_b):
        return f"step counts differ: {len(steps_a)} vs {len(steps_b)}"
    # Reached only when the run digest disagrees but no step does, which means
    # the digest covers something the step list does not. Say so plainly rather
    # than reporting "equal".
    return (
        "run digests differ but every step matches -- the run digest covers "
        f"something not in the step list\n      A: {a.get('digest')}\n"
        f"      B: {b.get('digest')}"
    )


def compare(traces: List[Tuple[str, Dict]]) -> int:
    reference_name, reference = traces[0]
    ref_label = reference["platform"]["label"]
    print(f"reference: {reference_name} ({ref_label}, "
          f"{reference['platform']['system']} "
          f"python {reference['platform']['python']})\n")

    failures = 0
    for name, other in traces[1:]:
        label = other["platform"]["label"]
        print(f"vs {name} ({label}, {other['platform']['system']} "
              f"python {other['platform']['python']})")

        if reference["matrix"] != other["matrix"]:
            failures += 1
            print(f"  FAIL different matrices: {reference['matrix']} vs "
                  f"{other['matrix']} -- nothing comparable")
            continue

        ref_keys, other_keys = set(reference["cases"]), set(other["cases"])
        if ref_keys != other_keys:
            failures += 1
            missing = sorted(ref_keys - other_keys)
            extra = sorted(other_keys - ref_keys)
            print(f"  FAIL case sets differ  missing={missing} extra={extra}")
            continue

        for key in sorted(ref_keys):
            a, b = reference["cases"][key], other["cases"][key]
            if all(a.get(f) == b.get(f) for f in COMPARED_FIELDS):
                print(f"  PASS {key}  ({a['step_count']} steps, "
                      f"digest {a['digest'][:16]})")
            else:
                failures += 1
                print(f"  FAIL {key}")
                report = first_divergence(a, b)
                print("       " + report.replace("\n", "\n       "))
        print()

    if failures:
        print(f"{failures} cross-OS divergence(s)")
        print("A divergence means the corpus is only valid on the platform that "
              "produced it. Do not paper over it -- file it and stop.")
        return 1
    print(f"all cases agree across {len(traces)} platform(s)")
    return 0


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    emitter = sub.add_parser("emit", help="write this platform's trace file")
    emitter.add_argument("--out", required=True)
    emitter.add_argument("--label", default="local",
                         help="runner name, recorded for the failure report")
    emitter.add_argument("--matrix", choices=("smoke", "wide"), default="wide")
    emitter.add_argument("--max-steps", type=int, default=400)

    comparer = sub.add_parser("compare", help="diff two or more trace files")
    comparer.add_argument("files", nargs="+")

    args = parser.parse_args(argv)

    if args.command == "emit":
        trace = emit(args.label, args.matrix, args.max_steps)
        with open(args.out, "w", encoding="utf-8") as handle:
            json.dump(trace, handle, sort_keys=True, indent=1)
        print(f"\nwrote {args.out}")
        return 0

    if len(args.files) < 2:
        print("compare needs at least two trace files; one platform proves "
              "nothing about another", file=sys.stderr)
        return 2

    loaded: List[Tuple[str, Dict]] = []
    for path in args.files:
        with open(path, encoding="utf-8") as handle:
            loaded.append((path, json.load(handle)))
    return compare(loaded)


if __name__ == "__main__":
    raise SystemExit(main())
