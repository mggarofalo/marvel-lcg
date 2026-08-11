"""Find the decisions that changed nothing, from scenes already on disk.

A saved scene records, per step, the decision that was asked and the state digest
at the moment it was asked. So a decision that changed nothing is one whose
digest equals the next step's -- no instrumentation, no engine run, and it works
on any corpus including one generated months ago.

That definition is operational rather than semantic. It answers "did this
decision advance the game", which is exactly the question `NoProgressGuard` acts
on, and it does not claim the ability *can never* change anything: a targeting
sub-choice legitimately changes nothing until the effect it belongs to resolves.
Read the counts as "how often this shape resolved to nothing", not as a proof of
inertness.

Run:
    python -m tools.noop.scan replays/
    python -m tools.noop.scan replays/ --json inventory.json
"""

from __future__ import annotations

import argparse
import collections
import glob
import json
import os
import re
import sys
from typing import Dict, List, Tuple

CARD_DATASET = os.path.join("..", "datasets", "cards", "cards.json")

# "e12 Ask c48 06001b" -> ability "Ask", card "06001b". The ordinals are
# per-game object ids and carry no meaning across scenes.
EFFECT_ID = re.compile(r'e\d+ (\S+) c\d+ (\S+)')


def LoadCardNames() -> Dict[str, str]:
    """`card_id -> printed name`, or an empty map if the dataset is absent.

    Printed text, not `data/cards.json` -- see AGENTS.md, "Critical constraints".
    """
    try:
        with open(CARD_DATASET, encoding="utf-8") as handle:
            data = json.load(handle)
    except (OSError, ValueError):
        return {}
    names = {}
    for card in data.get("cards", []):
        subname = card.get("subname") or ""
        label = card.get("name", "")
        names[card.get("card_id")] = f"{label} ({subname})" if subname else label
    return names


def SceneFiles(folder: str) -> List[str]:
    return sorted(f for f in glob.glob(os.path.join(folder, "*.json"))
                  if "manifest" not in os.path.basename(f)
                  and "coverage" not in os.path.basename(f))


def Scan(files: List[str]) -> Dict:
    by_shape: collections.Counter = collections.Counter()
    by_event: collections.Counter = collections.Counter()
    declines: collections.Counter = collections.Counter()
    runs: collections.Counter = collections.Counter()
    transitions = 0
    unchanged = 0
    scenes = 0

    for path in files:
        try:
            with open(path, encoding="utf-8") as handle:
                inputs = json.load(handle)["inputs"]
        except (OSError, ValueError, KeyError):
            continue
        scenes += 1
        run = 0
        for index in range(len(inputs) - 1):
            transitions += 1
            step, following = inputs[index], inputs[index + 1]
            if step["digest"] != following["digest"]:
                if run:
                    runs[run] += 1
                run = 0
                continue

            unchanged += 1
            run += 1
            event = step["event"].split(" ", 1)[-1]
            by_event[event] += 1
            effect_id = step.get("effect", {}).get("id", "")
            if not effect_id:
                # No effect chosen -- the answer was a decline. Legitimately
                # changes nothing, and is the single largest class.
                declines[event] += 1
                continue
            match = EFFECT_ID.match(effect_id)
            if match:
                by_shape[(match.group(1), match.group(2))] += 1
        if run:
            runs[run] += 1

    return {
        "scenes": scenes,
        "transitions": transitions,
        "unchanged": unchanged,
        "longest_run": max(runs) if runs else 0,
        "run_histogram": {str(k): v for k, v in sorted(runs.items())},
        "by_event": dict(by_event.most_common()),
        "declines": dict(declines.most_common()),
        "by_ability": [
            {"ability": ability, "card_id": card, "count": count}
            for (ability, card), count in by_shape.most_common()
        ],
    }


def Report(result: Dict, names: Dict[str, str]) -> None:
    share = 100.0 * result["unchanged"] / result["transitions"] if result["transitions"] else 0.0
    print(f"{result['scenes']} scene(s), {result['transitions']} decision transitions")
    print(f"{result['unchanged']} left the digest unchanged ({share:.1f}%)")
    print(f"longest consecutive no-progress run: {result['longest_run']}")
    print("\nrun length  count")
    for length, count in result["run_histogram"].items():
        print(f"  {length:>9}  {count}")

    print("\ndeclines (no effect chosen), by event:")
    for event, count in result["declines"].items():
        print(f"  {count:5d}  {event}")

    print("\nan effect was chosen and nothing changed:")
    print(f"  {'count':>5}  {'ability':28s} {'card':8s} name")
    for row in result["by_ability"]:
        name = names.get(row["card_id"], "")
        print(f"  {row['count']:5d}  {row['ability']:28s} {row['card_id']:8s} {name}")


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder", help="a folder of saved scenes")
    parser.add_argument("--json", dest="json_out", help="also write the raw counts here")
    args = parser.parse_args(argv)

    files = SceneFiles(args.folder)
    if not files:
        print(f"no scenes in {args.folder}", file=sys.stderr)
        return 2

    result = Scan(files)
    if result["transitions"] == 0:
        print(f"{len(files)} file(s) in {args.folder}, none with readable inputs",
              file=sys.stderr)
        return 2

    Report(result, LoadCardNames())

    if args.json_out:
        with open(args.json_out, "w", encoding="utf-8") as handle:
            json.dump(result, handle, indent=1, sort_keys=True)
        print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
