"""Which forced-ability batch shapes the shipped card pool can produce at all.

`probe_forced_selection` answers "did this run reach the shape", by playing. This
answers "can any run reach it", by reading every card the engine can load. The
two are complementary and the second is the one that settles a persistent zero:
a probe reporting `with_delay = 0` could mean the driver is too shallow or that
the shape does not exist, and only this distinguishes them.

## The shape MARVEL-39 needs

`EventManager.SelectForcedEffect` filters delay abilities out of the candidate
list and asks the first player only when two or more candidates remain. So the
defect needs a delay ability sharing a batch with **two** non-delay candidates.
A batch is one message at one `TimingPriority`, and delay abilities are always
`TimingPriority.Rule` (`ability_type.py`, "Fix 50068"), so every co-batched
ability must be Rule priority and forced too.

Measured on the current pool: **six messages host a Rule-priority delay ability,
and the most non-delay Rule-priority forced candidates on any of them is one.**
The shape is not reachable from shipped cards -- not by a seed, and not by a
puzzle built out of real ones. That is why MARVEL-39's coverage is
`unit_test/test_forced_effect_selection.py`, which constructs the batch directly,
and why MARVEL-69 did not build a puzzle for it.

Re-run this when a pack is added. A second Challenge-typed forced ability landing
on one of those six messages would make the shape reachable, and then a puzzle
becomes worth building.

Run:  python -m tools.determinism.scan_forced_shapes
      python -m tools.determinism.scan_forced_shapes --json shapes.json
"""

from __future__ import annotations

import argparse
import collections
import json
from typing import Dict, List, Set


def Scan() -> Dict:
    from tools.determinism.headless import _initialize_engine

    _initialize_engine()

    from cards.database import CardsDB
    from game.ability.ability_type import TimingPriority

    by_message: Dict[str, Dict[str, Set[str]]] = collections.defaultdict(
        lambda: {"delay": set(), "candidate": set()})
    scanned = 0
    unloadable = 0

    for card_id, paper in CardsDB.papers.items():
        try:
            abilities = CardsDB.FindAbilities(
                card_id, getattr(paper, "pack", ""), getattr(paper, "set_name", ""))
        except Exception:
            # A card whose script will not import cannot contribute a shape, and
            # a scan that dies on one is worth less than a scan that counts it.
            unloadable += 1
            continue
        scanned += 1

        for ability in abilities or []:
            if getattr(ability, "priority", None) != TimingPriority.Rule:
                continue
            flags = getattr(ability, "flags", None)
            if flags is None:
                continue
            when = getattr(ability, "when", None)
            name = getattr(when, "__name__", str(when))
            if getattr(flags, "is_delay_ability", False):
                by_message[name]["delay"].add(card_id)
            elif getattr(flags, "is_forced", False):
                by_message[name]["candidate"].add(card_id)

    hosts = {
        name: {
            "delay": len(sides["delay"]),
            "candidates": sorted(sides["candidate"]),
        }
        for name, sides in by_message.items() if sides["delay"]
    }
    worst = max((len(v["candidates"]) for v in hosts.values()), default=0)

    return {
        "scanned": scanned,
        "unloadable": unloadable,
        "delay_hosting_messages": dict(sorted(hosts.items())),
        "most_candidates": worst,
        "marvel_39_reachable": worst >= 2,
    }


def Report(result: Dict) -> None:
    print(f"scanned {result['scanned']} cards ({result['unloadable']} unloadable)")
    print(f"messages hosting a Rule-priority delay ability: "
          f"{len(result['delay_hosting_messages'])}\n")
    print(f"{'message':46s} {'delay':>6} {'cands':>6}")
    for name, sides in result["delay_hosting_messages"].items():
        count = len(sides["candidates"])
        mark = "  <-- two candidates: the shape is reachable" if count >= 2 else ""
        print(f"{name:46s} {sides['delay']:6d} {count:6d}{mark}")
        if sides["candidates"]:
            print(f"{'':46s}        {sides['candidates']}")
    print()
    print("most non-delay Rule-priority forced candidates on a delay-hosting "
          f"message: {result['most_candidates']}")
    if result["marvel_39_reachable"]:
        print("MARVEL-39's shape IS reachable from shipped cards -- a puzzle is "
              "now worth building, and probe_forced_selection should find it.")
    else:
        print("MARVEL-39's shape needs two, so it is NOT reachable from shipped "
              "cards. Its coverage is unit_test/test_forced_effect_selection.py.")


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", dest="json_out")
    args = parser.parse_args(argv)

    result = Scan()
    Report(result)

    if args.json_out:
        with open(args.json_out, "w", encoding="utf-8") as handle:
            json.dump(result, handle, indent=1, sort_keys=True)
        print(f"\nwrote {args.json_out}")

    # Always zero. This reports a property of the card pool, and neither answer
    # is a failure -- "the shape became reachable" is news, not a broken build.
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
