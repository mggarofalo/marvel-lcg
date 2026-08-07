"""Run spec cases against the Python engine and print what happened.

    python -m tools.spec.run_case specs/scenarios/spider_man.json
    python -m tools.spec.run_case specs/scenarios/
    python -m tools.spec.run_case specs/scenarios/ --json

Run it from `py_src/` -- every data path in the engine is relative to it.

Exit code is 0 when every case passed and 1 otherwise, so this is usable as a
gate on its own.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
from typing import List, Sequence

from tools.spec.case import LoadJsonCases, SpecCase, SpecCaseError
from tools.spec.harness import CaseResult, OUTCOME_PASS, RunCase

CASE_SUFFIXES = (".json",)


def LoadCases(path: str) -> List[SpecCase]:
    """Every case under `path`, which may be a file or a directory."""
    if os.path.isdir(path):
        cases: List[SpecCase] = []
        for name in sorted(os.listdir(path)):
            child = os.path.join(path, name)
            if os.path.isdir(child) or name.endswith(CASE_SUFFIXES):
                cases.extend(LoadCases(child))
        return cases

    if path.endswith(CASE_SUFFIXES):
        with open(path, "r", encoding="utf-8") as handle:
            return LoadJsonCases(handle.read(), source_path=path)

    raise SpecCaseError(f"{path}: not a case file ({', '.join(CASE_SUFFIXES)})")


def Report(results: Sequence[CaseResult], elapsed: float) -> None:
    for result in results:
        print(result.Describe())

    total = len(results)
    print()
    if not total:
        print("no cases")
        return
    passed = sum(1 for result in results if result.passed)
    print(f"{passed}/{total} passed in {elapsed:.2f}s "
          f"({elapsed / total * 1000:.0f} ms/case)")


def ReportJson(results: Sequence[CaseResult]) -> None:
    print(json.dumps([
        {
            "case": result.case.case_id,
            "source": result.case.source_path,
            "outcome": result.outcome,
            "message": result.message,
            "failures": [
                {"then": failure.step.Describe(), "actual": failure.actual,
                 "message": failure.message, "unresolvable": failure.unresolvable}
                for failure in result.Failures()
            ],
        }
        for result in results
    ], indent=2, default=str))


def main(argv: "List[str]|None" = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="+",
                        help=".feature / .json case files, or directories of them")
    parser.add_argument("--json", action="store_true", help="machine-readable output")
    parser.add_argument("--name", default="",
                        help="only run cases whose name contains this text")
    parser.add_argument("--max-decisions", type=int, default=200,
                        help="give up on a case after this many engine decisions")
    args = parser.parse_args(argv)

    cases: List[SpecCase] = []
    try:
        for path in args.paths:
            cases.extend(LoadCases(path))
    except (SpecCaseError, OSError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2

    if args.name:
        wanted = args.name.lower()
        cases = [case for case in cases if wanted in case.case_id.lower()]

    if not cases:
        print("error: no cases matched", file=sys.stderr)
        return 2

    start = time.perf_counter()
    results = [RunCase(case, max_decisions=args.max_decisions) for case in cases]
    elapsed = time.perf_counter() - start

    if args.json:
        ReportJson(results)
    else:
        Report(results, elapsed)

    return 0 if all(result.outcome == OUTCOME_PASS for result in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
