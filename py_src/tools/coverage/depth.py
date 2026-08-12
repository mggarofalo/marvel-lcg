"""How far into a game a corpus actually got.

`tools/coverage/report.py` answers "which cards and abilities were exercised".
This answers "how deep did the games go", which is a different question and the
one MARVEL-14 is judged on: random play under-samples exactly the states that
matter most, because it rarely defeats a villain, so villain stage 2 and 3, late
upgrade stacks and win conditions stay untested.

Villain stage is read from the digest each step already carries, so this works on
any saved scene with no instrumentation and no replay. A villain's stage is the
`printed_stage` of whichever villain card is in `VillainArea`; it advances when
the previous stage is defeated, which makes "the highest stage seen in this
scene" a direct measure of how far the players got.

Run:
    python -m tools.coverage.depth replays/
    python -m tools.coverage.depth replays/ --json depth.json
"""

from __future__ import annotations

import argparse
import collections
import glob
import json
import os
from typing import Dict, List

VILLAIN_AREA = "VillainArea"


def SceneFiles(folder: str) -> List[str]:
    return sorted(f for f in glob.glob(os.path.join(folder, "*.json"))
                  if "manifest" not in os.path.basename(f)
                  and "coverage" not in os.path.basename(f))


def DeepestStage(inputs: List[Dict]) -> int:
    """The highest villain stage this scene ever had on the board.

    Read across every step rather than off the last one: a scene can end with
    the villain defeated and nothing in `VillainArea` at all, and reporting 0
    for the game that went furthest would inverse the measurement.
    """
    best = 0
    for step in inputs:
        try:
            document = json.loads(step["digest"])
        except (ValueError, KeyError):
            continue
        for record in document.get("cards", []):
            if record.get("zone") != VILLAIN_AREA:
                continue
            best = max(best, int(record.get("fields", {}).get("printed_stage", 0)))
    return best


def Outcomes(folder: str) -> Dict[str, str]:
    """`scene file -> outcome`, from the manifests sitting beside the scenes."""
    outcomes: Dict[str, str] = {}
    for path in glob.glob(os.path.join(folder, "bot-manifest-*.json")):
        try:
            with open(path, encoding="utf-8") as handle:
                manifest = json.load(handle)
        except (OSError, ValueError):
            continue
        for game in manifest.get("games", []):
            if game.get("file"):
                outcomes[game["file"]] = game.get("outcome", "")
    return outcomes


def Scan(folder: str) -> Dict:
    files = SceneFiles(folder)
    outcomes = Outcomes(folder)

    scenes: List[Dict] = []
    stages: collections.Counter = collections.Counter()
    steps_total = 0

    for path in files:
        try:
            with open(path, encoding="utf-8") as handle:
                inputs = json.load(handle)["inputs"]
        except (OSError, ValueError, KeyError):
            continue
        name = os.path.basename(path)
        stage = DeepestStage(inputs)
        stages[stage] += 1
        steps_total += len(inputs)
        scenes.append({
            "file": name,
            "steps": len(inputs),
            "deepest_stage": stage,
            "outcome": outcomes.get(name, ""),
        })

    count = len(scenes)
    return {
        "scenes": count,
        "steps_total": steps_total,
        "steps_mean": round(steps_total / count, 1) if count else 0,
        "stage_histogram": {str(k): v for k, v in sorted(stages.items())},
        "reached_stage_2": sum(v for k, v in stages.items() if k >= 2),
        "reached_stage_3": sum(v for k, v in stages.items() if k >= 3),
        "scene_details": scenes,
    }


def Report(result: Dict) -> None:
    count = result["scenes"]
    if not count:
        print("no scenes")
        return

    def share(n: int) -> str:
        return f"{n:3d}/{count:<3d} ({100.0 * n / count:5.1f}%)"

    print(f"{count} scene(s), {result['steps_total']} steps, "
          f"mean {result['steps_mean']}")
    print(f"reached villain stage 2  {share(result['reached_stage_2'])}")
    print(f"reached villain stage 3  {share(result['reached_stage_3'])}")
    print("\nstage histogram:")
    for stage, n in result["stage_histogram"].items():
        print(f"  stage {stage}: {n}")

    outcomes = collections.Counter(s["outcome"] for s in result["scene_details"]
                                   if s["outcome"])
    if outcomes:
        print("\noutcomes:")
        for outcome, n in outcomes.most_common():
            print(f"  {n:4d}  {outcome}")


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder")
    parser.add_argument("--json", dest="json_out")
    args = parser.parse_args(argv)

    result = Scan(args.folder)
    if not result["scenes"]:
        print(f"no readable scenes in {args.folder}")
        return 2

    Report(result)
    if args.json_out:
        with open(args.json_out, "w", encoding="utf-8") as handle:
            json.dump(result, handle, indent=1, sort_keys=True)
        print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
