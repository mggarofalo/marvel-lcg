"""Merge bot coverage artefacts into one corpus-wide report.

Every bot run already writes `bot-coverage-<scenario>-<heroes>-<seed>-<games>.json`
beside its scenes. One run answers "what did these games reach". The corpus
question is the union across every run, and that is what this produces.

    python -m tools.coverage.report replays/bot-coverage-*.json
    python -m tools.coverage.report replays --out coverage.json
    python -m tools.coverage.report replays --top 40

A directory argument is walked for `bot-coverage-*.json`. Globs are expanded
here as well as by the shell, because the Windows shell does not expand them.

Ranking is recomputed from the merged observations rather than combined from the
per-run rankings: a factory unreached in every run individually may still be
reached by the corpus, and merging the ranked lists instead of the raw counts
would report it as missing.

Imports nothing from the engine beyond `engine/profile/coverage_report.py`,
which is pure stdlib -- so this runs without booting a game.
"""

from __future__ import annotations

import argparse
import glob
import json
import os
import sys
from typing import Any, Dict, List, Sequence

from engine.profile import coverage_report

ARTEFACT_GLOB = "bot-coverage-*.json"


def Expand(paths: Sequence[str]) -> List[str]:
    """Turn arguments into a sorted, de-duplicated list of artefact files."""
    found: List[str] = []
    for argument in paths:
        if os.path.isdir(argument):
            found.extend(glob.glob(os.path.join(argument, ARTEFACT_GLOB)))
        else:
            matches = glob.glob(argument)
            # An argument that matches nothing is passed through so the caller
            # gets "no such file" naming the path they typed, rather than
            # "no artefacts found" naming nothing.
            found.extend(matches if matches else [argument])
    return sorted(dict.fromkeys(os.path.normpath(path) for path in found))


def Read(path: str) -> Dict[str, Any]:
    with open(path, encoding="utf-8") as handle:
        document = json.load(handle)
    if not isinstance(document, dict):
        raise ValueError(f"{path} is not a coverage report")
    return document


def Merge(paths: Sequence[str], *, dataset: str) -> Dict[str, Any]:
    documents = [Read(path) for path in paths]
    games = coverage_report.GamesOf(documents)

    universe = None
    universe_error = ""
    try:
        universe = coverage_report.LoadUniverse(dataset)
    except coverage_report.DatasetMissing as exc:
        universe_error = str(exc)

    # Every artefact in a merge should come from the same build; if they do not,
    # say which builds rather than silently picking one.
    versions = sorted({str(document.get("engine_version") or "") for document in documents})

    document = coverage_report.Build(
        games,
        generator="merged",
        engine_version=versions[0] if len(versions) == 1 else "+".join(versions),
        universe=universe,
        universe_error=universe_error,
    )
    document["sources"] = [path.replace(os.sep, "/") for path in paths]
    return document


def _main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="+",
                        help=f"coverage artefacts, globs, or directories holding {ARTEFACT_GLOB}")
    parser.add_argument("--dataset", default=coverage_report.DEFAULT_DATASET,
                        help="card dataset to measure against (default: %(default)s)")
    parser.add_argument("--out", default="",
                        help="write the merged report here as JSON")
    parser.add_argument("--top", type=int, default=15,
                        help="how many unreached entries to print (default: %(default)s)")
    args = parser.parse_args(list(argv))

    paths = Expand(args.paths)
    missing = [path for path in paths if not os.path.exists(path)]
    if missing:
        for path in missing:
            print(f"no such file: {path}", file=sys.stderr)
        return 1
    if not paths:
        print(f"no coverage artefacts found ({ARTEFACT_GLOB})", file=sys.stderr)
        return 1

    document = Merge(paths, dataset=args.dataset)

    print(f"merged {len(paths)} run(s)")
    print(coverage_report.Summarize(document))

    universe = document.get("universe") or {}
    if not universe.get("available"):
        print(f"\nno universe: {universe.get('reason', '')}", file=sys.stderr)
    elif args.top > 0:
        never_fired = document.get("never_fired_factories") or []
        print(f"\nunreached triggers ({len(never_fired)}), worst first:")
        for entry in never_fired[:args.top]:
            print(f"  {entry['cards']:>4} cards  {entry['factory']}")

        never_exercised = document.get("never_exercised_cards") or []
        print(f"\nunexercised cards ({len(never_exercised)}), best to reach first:")
        for entry in never_exercised[:args.top]:
            name = entry["name"] or entry["card_id"]
            print(f"  +{entry['score']:<3} {entry['card_id']}  {name}")

    if args.out:
        folder = os.path.dirname(args.out)
        if folder:
            os.makedirs(folder, exist_ok=True)
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(document, handle, indent=2, sort_keys=True)
            handle.write("\n")
        print(f"\nwrote {args.out}")

    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
