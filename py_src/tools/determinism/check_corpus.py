"""Replay the corpus N times in fresh processes and check every step digest.

This is the other half of the MARVEL-7 acceptance test, and it cannot run until
a corpus exists (MARVEL-5 generates it; `replays/` is empty and untracked
today). Everything here is written and ready; it needs input, not code.

How the check works. Each recorded step in a replay file carries the
`World.CalculateDigest()` value that was current when the input was taken.
`engine/controller/module/replay.py` recomputes that digest on replay and
compares it, printing a card-by-card, field-by-field diff on mismatch. So the
recorded values are a fixed reference: if all N runs agree with the recording,
all N agree with each other, which is exactly the property the oracle needs.

`main.py -verify_replays` does the replaying, exits non-zero on any divergence
and writes a JSON report. This wrapper runs it repeatedly in pinned, separate
processes and compares the reports -- the part a single run cannot check, since
a divergence that only appears on the third attempt is still a divergence.

Until MARVEL-28 this wrapper drove `main.py -test`, which did not replay
anything at all: it blocked in `WaitUntilGameStart()` until the subprocess was
killed, and the log-scraping below found no summary line and reported 0/0.

Run:
    python -m tools.determinism.check_corpus --runs 100
    python -m tools.determinism.check_corpus --runs 5 --replay-folder ./replays/
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Any, Dict, List, NamedTuple

from tools.determinism.pinned_env import build_env


class RunOutcome(NamedTuple):
    passed: int
    total: int
    ok: bool
    failures: List[str]
    excerpt: str


def run_once(replay_folder: str | None, extra_args: List[str],
             report_path: Path) -> RunOutcome:
    cmd = [sys.executable, "main.py", "-verify_replays",
           "-verify_report_file", str(report_path)]
    if replay_folder:
        cmd += ["-verify_folders", replay_folder]
    cmd += extra_args

    proc = subprocess.run(
        cmd,
        capture_output=True,
        text=True,
        errors="replace",
        env=build_env(),
        cwd=str(Path.cwd()),
    )

    document = _read_report(report_path)
    if document is None:
        # No report at all: the engine did not get far enough to write one, so
        # the process output is the only evidence of what went wrong.
        tail = (proc.stdout + proc.stderr)[-900:]
        return RunOutcome(0, 0, False, ["no report was written"], tail)

    failures = [
        f"{case.get('file')}: {case.get('status')}"
        + (f" -- {case['detail']}" if case.get("detail") else "")
        for case in document.get("cases", [])
        if case.get("status") != "pass"
    ]
    # The exit code and the report must agree; if they do not, trust neither.
    ok = bool(document.get("ok")) and proc.returncode == 0
    excerpt = ""
    if bool(document.get("ok")) != (proc.returncode == 0):
        excerpt = (f"report says ok={document.get('ok')} but the process exited "
                   f"{proc.returncode}")
        ok = False

    return RunOutcome(int(document.get("passed", 0)), int(document.get("total", 0)),
                      ok, failures, excerpt)


def _read_report(path: Path) -> Dict[str, Any] | None:
    if not path.is_file():
        return None
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runs", type=int, default=10)
    parser.add_argument("--replay-folder", default=None,
                        help="overrides replay_folders for this check")
    parser.add_argument("engine_args", nargs="*",
                        help="extra flags passed through to main.py")
    args = parser.parse_args(argv)

    folder = Path(args.replay_folder or "./replays/")
    cases = sorted(folder.glob("*.json")) if folder.is_dir() else []
    if not cases:
        print(f"No replay files in {folder}. The corpus does not exist yet "
              f"(MARVEL-5). Nothing to check.")
        return 2

    print(f"{len(cases)} file(s) in {folder}, {args.runs} run(s), environment pinned\n")

    failures = 0
    seen_totals: set[tuple[int, int]] = set()
    with tempfile.TemporaryDirectory(prefix="verify-corpus-") as work:
        for i in range(args.runs):
            report_path = Path(work) / f"verify-{i}.json"
            outcome = run_once(args.replay_folder, args.engine_args, report_path)
            seen_totals.add((outcome.passed, outcome.total))
            if not outcome.ok:
                failures += 1
                print(f"FAIL run {i + 1}: {outcome.passed}/{outcome.total} scenes")
                for line in outcome.failures:
                    print(f"     {line}")
                if outcome.excerpt:
                    print("     " + outcome.excerpt.replace("\n", "\n     "))
            else:
                print(f"pass run {i + 1}: {outcome.passed}/{outcome.total} scenes")

    print()
    if failures:
        print(f"{failures}/{args.runs} run(s) diverged from the recorded digests")
        return 1
    if len(seen_totals) != 1:
        print(f"runs disagreed on how many scenes exist: {sorted(seen_totals)}")
        return 1
    print(f"all {args.runs} runs reproduced every recorded per-step digest")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
