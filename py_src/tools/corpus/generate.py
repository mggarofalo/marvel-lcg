"""Play a plan across processes, resumably, and say how it went.

    python -m tools.corpus.generate --games 200 --out ./corpus/
    python -m tools.corpus.generate --games 200 --out ./corpus/ --dry-run
    python -m tools.corpus.generate --games 200 --out ./corpus/   # again: resumes

`plan.py` decides which games to play; this plays them. See MARVEL-15.

## Processes, not threads

The engine is not thread-safe -- `Controller.ChoiceOne` blocks a thread inside
`GetInput`, and the whole job and task machinery exists around that -- so every
case is a fresh `main.py -bot` process. The pool here is a thread pool, but the
threads do nothing except wait on a subprocess, which is what `subprocess` wants
anyway.

Each worker writes into its own folder under the output directory. Two bot runs
sharing one folder would race on `bot-manifest-*.json`, whose name comes from
the scenario, heroes and seed -- distinct per case, so it would not clobber --
but also on the coverage report, and `-verify_replays` would then be reading a
directory being written. Separate folders make the concurrency invisible to
everything downstream, and a corpus is a tree of folders rather than one flat
one.

## Resuming

A run appends one line of JSON to `progress.jsonl` per finished case, keyed by
`Case.id` -- which is built from the scenario, heroes and seeds rather than from
the plan index, so reordering a plan does not invalidate a half-finished corpus.
Starting again skips what is already there. The file is opened in append mode
and flushed per line, so a killed run loses at most the case in flight.

## What a case can do

- **finish** -- its games were played and saved.
- **fail** -- non-zero exit. The bot's own crash capture has already written the
  artefacts; this records that it happened and carries on, because most of what
  self-play finds is a pre-existing engine bug and stopping the corpus on one is
  how a corpus never gets generated. `--fail-fast` stops instead.
- **time out** -- killed at `--case-timeout`. This is the wall-clock cap the
  step cap cannot give you: `bot_max_steps` bounds a game's *decisions*, and a
  game that wedges without taking a decision would sit there forever.

## Reproducibility, and the one thing that is not

Everything that decides the corpus -- the plan seed, the sizes, the inventory
digest, the resolved config of each worker -- is recorded and is clock-free.
The `timing` block is the exception and is deliberately fenced off in its own
key: throughput is the number MARVEL-15 asks for so corpus size can be planned,
and it is measured in seconds on a particular machine. Nothing reproducing a
corpus should read it.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from typing import Any, Dict, List, NamedTuple, Sequence

from tools.corpus import inventory as inventory_module
from tools.corpus.plan import Build, Case, Plan, Summarize
from tools.determinism.pinned_env import build_env

PROGRESS_NAME = "progress.jsonl"
MANIFEST_NAME = "corpus-manifest.json"

# Long enough that a slow four-hero game on a loaded machine is not killed for
# being slow, short enough that a wedged one is noticed the same day.
DEFAULT_CASE_TIMEOUT = 900.0

# The engine is single-threaded per process, so this is a straight core count
# question. Two in hand for the OS and for whoever is using the machine.
def DefaultWorkers() -> int:
    return max(1, (os.cpu_count() or 4) - 2)


class Outcome(NamedTuple):
    case: Case
    status: str             # "ok" | "failed" | "timeout"
    exit_code: int
    seconds: float
    folder: str
    scenes: int
    detail: str

    def ToDict(self) -> Dict[str, Any]:
        return {
            "id": self.case.id,
            "index": self.case.index,
            "scenario": self.case.scenario,
            "heroes": list(self.case.heroes),
            "seed": self.case.seed,
            "games": self.case.games,
            "phase": self.case.phase,
            "status": self.status,
            "exit_code": self.exit_code,
            "seconds": round(self.seconds, 3),
            "folder": self.folder,
            "scenes": self.scenes,
            "detail": self.detail,
        }


def CaseFolder(root: str, case: Case) -> str:
    """One folder per case. Named from the case, not from its index, so a
    resumed run lands in the same place a first run would have."""
    stem = f"{case.index:05d}-{case.scenario}-{case.seed}"
    return os.path.join(root, stem)


def Command(case: Case, folder: str, extra: Sequence[str]) -> List[str]:
    return [
        sys.executable, "main.py", "-bot",
        "-bot_scenario", case.scenario,
        "-bot_heroes", *case.heroes,
        "-bot_seed", str(case.seed),
        "-bot_games", str(case.games),
        "-bot_save_folder", folder.replace("\\", "/") + "/",
        # A corpus has already paid for the checker once per game elsewhere, and
        # it costs roughly 40% of a game's wall time. Overridable: `extra` is
        # appended, and the command line beats nothing here.
        "-no_check_invariants",
        *extra,
    ]


def CountScenes(folder: str) -> int:
    if not os.path.isdir(folder):
        return 0
    return sum(1 for name in os.listdir(folder)
               if name.endswith(".json")
               and not name.startswith(("bot-manifest-", "bot-coverage-")))


def RunCase(case: Case, root: str, timeout: float,
            extra: Sequence[str]) -> Outcome:
    folder = CaseFolder(root, case)
    os.makedirs(folder, exist_ok=True)

    started = time.monotonic()
    try:
        proc = subprocess.run(
            Command(case, folder, extra),
            capture_output=True, text=True, errors="replace",
            env=build_env(), cwd=os.getcwd(), timeout=timeout,
        )
        exit_code, detail = proc.returncode, ""
        if exit_code != 0:
            detail = (proc.stdout + proc.stderr)[-600:]
        status = "ok" if exit_code == 0 else "failed"
    except subprocess.TimeoutExpired:
        exit_code, status = -1, "timeout"
        detail = f"killed after {timeout:.0f}s"

    return Outcome(
        case=case,
        status=status,
        exit_code=exit_code,
        seconds=time.monotonic() - started,
        folder=os.path.relpath(folder, root).replace("\\", "/"),
        scenes=CountScenes(folder),
        detail=detail,
    )


def ReadProgress(path: str) -> Dict[str, Dict[str, Any]]:
    """What a previous run finished, by case id.

    A malformed line is skipped rather than fatal: the file is appended to by a
    process that can be killed at any moment, so a truncated last line is an
    expected state and means exactly one case has to be replayed.
    """
    done: Dict[str, Dict[str, Any]] = {}
    if not os.path.isfile(path):
        return done
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line:
                continue
            try:
                record = json.loads(line)
            except ValueError:
                continue
            if isinstance(record, dict) and record.get("id"):
                done[str(record["id"])] = record
    return done


def Throughput(outcomes: Sequence[Dict[str, Any]], wall_seconds: float,
               workers: int) -> Dict[str, Any]:
    """Games per hour, both as measured and per worker.

    Both, because they answer different questions: the first says how long a
    corpus of size N will take on this machine, and the second says what adding
    machines would buy.
    """
    games = sum(int(record.get("games", 0)) for record in outcomes
                if record.get("status") == "ok")
    case_seconds = sum(float(record.get("seconds", 0.0)) for record in outcomes)
    return {
        "wall_seconds": round(wall_seconds, 3),
        "cpu_seconds": round(case_seconds, 3),
        "workers": workers,
        "games": games,
        "games_per_hour": round(games / wall_seconds * 3600, 1) if wall_seconds > 0 else 0,
        "games_per_hour_per_worker": (
            round(games / case_seconds * 3600, 1) if case_seconds > 0 else 0),
    }


def Generate(plan: Plan, root: str, *, workers: int, timeout: float,
             extra: Sequence[str]=(), fail_fast: bool=False) -> Dict[str, Any]:
    from engine.config_record import ConfigRecord

    os.makedirs(root, exist_ok=True)
    progress_path = os.path.join(root, PROGRESS_NAME)

    done = ReadProgress(progress_path)
    todo = [case for case in plan.cases if case.id not in done]

    print(f"{len(plan.cases)} case(s): {len(done)} already done, "
          f"{len(todo)} to play, {workers} worker(s)")

    started = time.monotonic()
    stop = False

    # Line-buffered append: a killed run loses at most the case in flight.
    with open(progress_path, "a", encoding="utf-8", buffering=1) as progress:
        def Play(case: Case) -> Outcome:
            if stop:
                return Outcome(case, "skipped", 0, 0.0, "", 0, "stopped early")
            return RunCase(case, root, timeout, extra)

        with ThreadPoolExecutor(max_workers=workers) as pool:
            for finished, outcome in enumerate(pool.map(Play, todo), start=1):
                if outcome.status == "skipped":
                    continue
                record = outcome.ToDict()
                progress.write(json.dumps(record, sort_keys=True) + "\n")
                done[outcome.case.id] = record

                mark = {"ok": "ok  ", "failed": "FAIL", "timeout": "TIME"}[outcome.status]
                print(f"[{finished}/{len(todo)}] {mark} {outcome.case.scenario} "
                      f"{'+'.join(outcome.case.heroes)} seed {outcome.case.seed} "
                      f"({outcome.scenes} scene(s), {outcome.seconds:.1f}s)")
                if outcome.status != "ok" and outcome.detail:
                    print(f"        {outcome.detail.splitlines()[-1][:160]}")
                if fail_fast and outcome.status != "ok":
                    print("        --fail-fast: no further cases will start")
                    stop = True

    wall = time.monotonic() - started
    outcomes = [done[case.id] for case in plan.cases if case.id in done]

    manifest = {
        "tool": "corpus-generate",
        "plan": plan.ToDict(),
        "config": ConfigRecord.Snapshot(),
        "cases": len(plan.cases),
        "played": len(outcomes),
        "ok": sum(1 for record in outcomes if record.get("status") == "ok"),
        "failed": sum(1 for record in outcomes if record.get("status") == "failed"),
        "timed_out": sum(1 for record in outcomes if record.get("status") == "timeout"),
        "scenes": sum(int(record.get("scenes", 0)) for record in outcomes),
        # Fenced off: measured in seconds on one machine, and the only thing
        # here that is not reproducible. Nothing regenerating a corpus reads it.
        "timing": Throughput(outcomes, wall, workers),
        "outcomes": outcomes,
    }
    with open(os.path.join(root, MANIFEST_NAME), "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=2, sort_keys=True)
    return manifest


def Report(manifest: Dict[str, Any]) -> List[str]:
    timing = manifest["timing"]
    lines = [
        "",
        f"corpus     {manifest['scenes']} scene(s) from {manifest['ok']}/"
        f"{manifest['cases']} case(s)",
        f"throughput {timing['games_per_hour']} games/hour on "
        f"{timing['workers']} worker(s)  "
        f"({timing['games_per_hour_per_worker']} per worker-hour)",
    ]
    if manifest["failed"] or manifest["timed_out"]:
        lines.append(
            f"unfinished {manifest['failed']} failed, "
            f"{manifest['timed_out']} timed out -- see {MANIFEST_NAME}")
    return lines


def main(argv: List[str] | None=None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", default="./corpus/",
                        help="where the corpus goes (default ./corpus/)")
    parser.add_argument("--games", type=int, default=200,
                        help="target game count; the coverage floor wins if they disagree")
    parser.add_argument("--seed", type=int, default=1,
                        help="plan seed -- decides every game in the corpus")
    parser.add_argument("--floor", type=int, default=1,
                        help="least number of games each scenario and hero must appear in")
    parser.add_argument("--games-per-case", type=int, default=4,
                        help="games per process in the random phase")
    parser.add_argument("--players", default="1,2,3,4",
                        help="player counts to cycle through")
    parser.add_argument("--scenarios", default="",
                        help="comma-separated subset to sample from")
    parser.add_argument("--heroes", default="",
                        help="comma-separated subset to sample from")
    parser.add_argument("--workers", type=int, default=0,
                        help=f"parallel processes (default {DefaultWorkers()} here)")
    parser.add_argument("--case-timeout", type=float, default=DEFAULT_CASE_TIMEOUT,
                        help="wall-clock cap per case, in seconds")
    parser.add_argument("--fail-fast", action="store_true",
                        help="stop starting cases after the first failure")
    parser.add_argument("--dry-run", action="store_true",
                        help="print the plan and play nothing")
    parser.add_argument("--bot-arg", action="append", default=[],
                        help="extra flag passed to every worker; repeatable")
    args = parser.parse_args(argv)

    def Names(text: str) -> List[str]:
        return [part.strip() for part in text.split(",") if part.strip()]

    stock = inventory_module.Read()
    chosen = inventory_module.Subset(stock, Names(args.scenarios), Names(args.heroes))
    problems = inventory_module.Check(chosen)
    if problems:
        for problem in problems:
            print(f"error: {problem}")
        return 2

    print(chosen.Describe())

    try:
        plan = Build(
            chosen,
            seed=args.seed,
            games=args.games,
            floor=args.floor,
            games_per_case=args.games_per_case,
            player_counts=[int(part) for part in Names(args.players)],
        )
    except ValueError as exc:
        print(f"error: {exc}")
        return 2

    for line in Summarize(plan):
        print(line)

    if args.dry_run:
        return 0

    workers = args.workers if args.workers > 0 else DefaultWorkers()
    manifest = Generate(plan, args.out, workers=workers,
                        timeout=args.case_timeout, extra=args.bot_arg,
                        fail_fast=args.fail_fast)
    for line in Report(manifest):
        print(line)

    # A corpus with holes in it is still a corpus, and the manifest says where
    # the holes are. Only a run that produced nothing at all is an error.
    return 0 if manifest["ok"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
