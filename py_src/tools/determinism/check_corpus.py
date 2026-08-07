"""Replay the corpus N times in fresh processes and check every step digest.

This is the other half of the MARVEL-7 acceptance test, and it cannot run until
a corpus exists (MARVEL-5 generates it; `replays/` is empty and untracked
today). Everything here is written and ready; it needs input, not code.

How the check works. Each recorded step in a replay file carries the
`World.CalculateCRC()` value that was current when the input was taken.
`engine/controller/module/replay.py` recomputes that digest on replay and
compares it, printing a key-by-key diff on mismatch. So the recorded values are
a fixed reference: if all N runs agree with the recording, all N agree with each
other, which is exactly the property the oracle needs.

`main.py -test` replays every file in the configured replay folders. It signals
success only through its output -- `Log.Assert` prints an error, it does not set
an exit code -- so this wrapper parses the log.

Run:
    python -m tools.determinism.check_corpus --runs 100
    python -m tools.determinism.check_corpus --runs 5 --replay-folder ./replays/
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path
from typing import List, NamedTuple

from tools.determinism.pinned_env import build_env

SUMMARY = re.compile(r"--- Test End --- \((\d+)/(\d+)\)")
# Printed by InputModule.GetReplayOperation immediately before the key-by-key
# digest diff.
DIFF_HEADER = re.compile(r"Key \| Read \| Curr")


class RunOutcome(NamedTuple):
    passed: int
    total: int
    diverged: bool
    excerpt: str


def _strip_ansi(text: str) -> str:
    return re.sub(r"\x1b\[[0-9;]*m", "", text)


def run_once(replay_folder: str | None, extra_args: List[str]) -> RunOutcome:
    cmd = [sys.executable, "main.py", "-test"]
    if replay_folder:
        cmd += ["-replay_folders", replay_folder]
    cmd += extra_args

    proc = subprocess.run(
        cmd,
        capture_output=True,
        text=True,
        errors="replace",
        env=build_env(),
        cwd=str(Path.cwd()),
    )
    out = _strip_ansi(proc.stdout + proc.stderr)

    match = SUMMARY.search(out)
    passed, total = (int(match.group(1)), int(match.group(2))) if match else (0, 0)

    diverged = bool(DIFF_HEADER.search(out))
    excerpt = ""
    if diverged:
        start = DIFF_HEADER.search(out).start()  # type: ignore[union-attr]
        excerpt = out[start:start + 900]
    elif not match:
        excerpt = out[-900:]

    return RunOutcome(passed, total, diverged, excerpt)


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

    print(f"{len(cases)} replay file(s), {args.runs} run(s), environment pinned\n")

    failures = 0
    seen_totals = set()
    for i in range(args.runs):
        outcome = run_once(args.replay_folder, args.engine_args)
        seen_totals.add((outcome.passed, outcome.total))
        if outcome.diverged or outcome.passed != outcome.total or outcome.total == 0:
            failures += 1
            print(f"FAIL run {i + 1}: {outcome.passed}/{outcome.total} cases")
            if outcome.excerpt:
                print("     " + outcome.excerpt.replace("\n", "\n     "))
        else:
            print(f"pass run {i + 1}: {outcome.passed}/{outcome.total} cases")

    print()
    if failures:
        print(f"{failures}/{args.runs} run(s) diverged from the recorded digests")
        return 1
    if len(seen_totals) != 1:
        print(f"runs disagreed on how many cases exist: {sorted(seen_totals)}")
        return 1
    print(f"all {args.runs} runs reproduced every recorded per-step digest")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
