"""Which cards have behavioral specs, at what depth, and what is left.

    python -m tools.spec.coverage                    # the summary
    python -m tools.spec.coverage --tier interactive # what to author next
    python -m tools.spec.coverage --pack core
    python -m tools.spec.coverage --out coverage.json

MARVEL-68's fourth acceptance item. `tools/coverage/report.py` answers "which
cards did the *corpus* exercise"; this answers "which cards has somebody written
down a claim about", and only the second survives the Python engine.

Run from `py_src/`.

## Coverage means a trusted scenario, not a scenario

A card counts as covered when at least one scenario tagged `@card:<id>` is in
`specs/trusted.json`. A quarantined scenario is reported separately and never as
coverage: it is a claim that failed, so counting it would mean the number goes up
when authoring goes wrong.

## Depth comes from the script, not from a quota

"Three scenarios per card" is the wrong rule -- it over-serves 531 declarative
scripts and under-serves the 440 that stop mid-resolution to ask a question.
`datasets/cards/cards.json` carries `engine.script` per card, built by
`tools/cards/extract.py`, and the tier is read from it:

    interactive   calls PlayerAsk / ChooseAbilities / MayChooseOneAbility /
                  AskSpendResources, so the card asks the player a question
    imperative    a handler that does something, but never suspends
    declarative   a script with no function defined inside another, so no
                  branch a scenario could take differently
    stats_only    no script, but the engine has the card -- printed stats and
                  keywords, implemented generically rather than per card
    absent        the engine has no such card, or nothing to assert about it

The budget attached to each tier is a target for planning, not a gate. A card
that needs four scenarios gets four; the tier says which cards deserve the
argument.

**`stats_only` is specifiable and it took a bug to notice.** The first version of
this tier rule read "no script" as "nothing to specify" and put 563 cards outside
the denominator. Two of them were Hydra Mercenary and Sandman -- whose Guard and
Toughness `specs/rules/keywords.feature` already pins, because those keywords are
implemented by `game/card/face/attribute/` for every card that prints them rather
than by a script per card. A card with no script still has hit points, an attack
value and its keywords. What is genuinely unspecifiable is `absent`: 345 cards
in the printed dataset that the engine does not implement at all.

That distinction moves the denominator from 3781 to 3996, and getting it wrong
in the other direction is the MARVEL-16 failure repeated -- a missed population
does not look like a bug, it looks like a smaller universe.
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import sys
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

CARD_DATASET = "../datasets/cards/cards.json"
SPEC_ROOT = "specs"
TRUSTED = "specs/trusted.json"
QUARANTINE = "specs/quarantine.json"

# tier -> (scenarios a card of this tier is planned for, why)
TIERS: Dict[str, Tuple[int, str]] = {
    "interactive": (4, "suspends mid-resolution for a player answer"),
    "imperative":  (2, "a handler that does something, but never asks"),
    "declarative": (1, "declarative factory calls, no branch to take"),
    "stats_only":  (1, "no script; printed stats and keywords only"),
    "absent":      (0, "the engine has no such card"),
}

TIER_ORDER: Tuple[str, ...] = (
    "interactive", "imperative", "declarative", "stats_only", "absent")

# Everything except `absent`. The denominator of the campaign.
SPECIFIABLE: Tuple[str, ...] = TIER_ORDER[:-1]


def Tier(card: Dict[str, Any]) -> str:
    """Which depth tier this card sits in.

    Script shape first, because that is what decides how much a card can do.
    A card with no script is not automatically out of scope: the engine
    implements keywords and stats generically, so a scripted Hydra Mercenary is
    not needed for its Guard to be worth pinning.
    """
    engine = card.get("engine") or {}
    script = engine.get("script")
    if script:
        if script.get("player_choice_calls"):
            return "interactive"
        if script.get("has_imperative_handler"):
            return "imperative"
        return "declarative"
    # No script. The engine either has the card or it does not, and only the
    # second is out of scope -- a scenario cannot name a card the engine has
    # never heard of.
    if card.get("in_engine") and engine.get("attributes"):
        return "stats_only"
    return "absent"


################################################################################
#


def ReadJson(path: str) -> Dict[str, Any]:
    with open(path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def Cards(path: str = CARD_DATASET) -> List[Dict[str, Any]]:
    if not os.path.exists(path):
        raise SystemExit(f"{path}: not found -- run from py_src/")
    return list(ReadJson(path)["cards"])


def Manifest(path: str) -> Dict[str, Any]:
    """A validation-runner manifest, or empty if it has never been written."""
    if not os.path.exists(path):
        return {}
    return dict(ReadJson(path).get("scenarios") or {})


def TaggedCards(root: str = SPEC_ROOT) -> Dict[str, List[str]]:
    """case_id -> the card ids its `@card:` tags name.

    The manifests key by `case_id` and carry no tags, so the tags have to come
    from the scenarios themselves. Parsing them is also the only way to notice a
    scenario that names a card id no card has.
    """
    from tools.spec.run_case import LoadCases

    tagged: Dict[str, List[str]] = {}
    for case in LoadCases(root):
        if case.card_tags:
            tagged[case.case_id] = list(case.card_tags)
    return tagged


################################################################################
#


class Coverage:
    """The join: every card, its tier, and the scenarios claiming it."""

    def __init__(self, cards: Sequence[Dict[str, Any]],
                 tagged: Dict[str, List[str]],
                 trusted: Iterable[str], quarantined: Iterable[str]) -> None:
        self.cards = {card["card_id"]: card for card in cards}
        self.tier = {card_id: Tier(card) for card_id, card in self.cards.items()}

        trusted, quarantined = set(trusted), set(quarantined)
        self.trusted: Dict[str, List[str]] = collections.defaultdict(list)
        self.quarantined: Dict[str, List[str]] = collections.defaultdict(list)
        # A tag naming a card the dataset does not have is a typo in a scenario,
        # not a covered card. Reported rather than counted.
        self.unknown_tags: Dict[str, List[str]] = collections.defaultdict(list)

        for case_id, card_ids in tagged.items():
            for card_id in card_ids:
                if card_id not in self.cards:
                    self.unknown_tags[case_id].append(card_id)
                elif case_id in trusted:
                    self.trusted[card_id].append(case_id)
                elif case_id in quarantined:
                    self.quarantined[card_id].append(case_id)

    def Specifiable(self) -> List[str]:
        """Cards a scenario could be written for -- everything the engine has."""
        return [card_id for card_id, tier in self.tier.items()
                if tier in SPECIFIABLE]

    def Covered(self, card_id: str) -> bool:
        return bool(self.trusted.get(card_id))

    def ByTier(self) -> Dict[str, Dict[str, int]]:
        rows: Dict[str, Dict[str, int]] = {
            tier: {"cards": 0, "covered": 0, "scenarios": 0, "quarantined": 0,
                   "planned": 0}
            for tier in TIER_ORDER}
        for card_id, tier in self.tier.items():
            row = rows[tier]
            row["cards"] += 1
            row["planned"] += TIERS[tier][0]
            row["scenarios"] += len(self.trusted.get(card_id, ()))
            row["quarantined"] += len(self.quarantined.get(card_id, ()))
            if self.Covered(card_id):
                row["covered"] += 1
        return rows

    def ByPack(self) -> Dict[str, Dict[str, int]]:
        rows: Dict[str, Dict[str, int]] = collections.defaultdict(
            lambda: {"cards": 0, "covered": 0})
        for card_id, tier in self.tier.items():
            if tier not in SPECIFIABLE:
                continue
            pack = str(self.cards[card_id].get("pack") or "?")
            rows[pack]["cards"] += 1
            if self.Covered(card_id):
                rows[pack]["covered"] += 1
        return dict(rows)

    def Uncovered(self, tier: str = "", pack: str = "") -> List[Dict[str, Any]]:
        """Specifiable cards with no trusted scenario, worst tier first.

        Ordered by tier and then by script size, so the head of the list is
        where a scenario buys the most. Not by card id -- that would walk the
        core set to exhaustion before touching anything else, and the tail is
        where port bugs hide.
        """
        rows: List[Dict[str, Any]] = []
        for card_id in self.Specifiable():
            if self.Covered(card_id):
                continue
            card = self.cards[card_id]
            card_tier = self.tier[card_id]
            if tier and card_tier != tier:
                continue
            if pack and str(card.get("pack") or "") != pack:
                continue
            script = (card.get("engine") or {}).get("script") or {}
            rows.append({
                "card_id": card_id,
                "name": card.get("name") or "",
                "type": card.get("type_name") or "",
                "pack": card.get("pack") or "",
                "tier": card_tier,
                "lines": int(script.get("lines") or 0),
                "asks": list(script.get("player_choice_calls") or ()),
                "quarantined": len(self.quarantined.get(card_id, ())),
            })
        rows.sort(key=lambda r: (TIER_ORDER.index(r["tier"]), -r["lines"],
                                 r["card_id"]))
        return rows

    def ToDict(self) -> Dict[str, Any]:
        specifiable = self.Specifiable()
        covered = [card_id for card_id in specifiable if self.Covered(card_id)]
        return {
            "note": ("Coverage is a card with at least one scenario in "
                     "specs/trusted.json tagged @card:<id>. A quarantined "
                     "scenario is a claim that failed and is never coverage."),
            "totals": {
                "cards": len(self.cards),
                "specifiable": len(specifiable),
                "covered": len(covered),
                "scenarios": sum(len(v) for v in self.trusted.values()),
                "quarantined": sum(len(v) for v in self.quarantined.values()),
            },
            "by_tier": self.ByTier(),
            "by_pack": self.ByPack(),
            "unknown_tags": {k: v for k, v in self.unknown_tags.items()},
        }


################################################################################
#


def Build() -> Coverage:
    return Coverage(
        cards=Cards(),
        tagged=TaggedCards(),
        trusted=Manifest(TRUSTED),
        quarantined=Manifest(QUARANTINE),
    )


def Percent(part: int, whole: int) -> str:
    return f"{100.0 * part / whole:.1f}%" if whole else "n/a"


def Report(coverage: Coverage, tier: str, pack: str, top: int) -> None:
    data = coverage.ToDict()
    totals = data["totals"]

    print(f"cards             {totals['cards']}")
    print(f"specifiable       {totals['specifiable']} (the engine has the card)")
    print(f"covered           {totals['covered']} "
          f"({Percent(totals['covered'], totals['specifiable'])})")
    print(f"trusted scenarios {totals['scenarios']}")
    if totals["quarantined"]:
        print(f"quarantined       {totals['quarantined']} "
              f"(claims that failed -- not coverage)")
    print()

    print(f"{'tier':<13} {'cards':>6} {'covered':>8} {'scenarios':>10} "
          f"{'planned':>8}  what it means")
    for name in TIER_ORDER:
        row = data["by_tier"][name]
        if not row["cards"]:
            continue
        print(f"{name:<13} {row['cards']:>6} "
              f"{row['covered']:>8} {row['scenarios']:>10} {row['planned']:>8}"
              f"  {TIERS[name][1]}")
    print()

    if coverage.unknown_tags:
        print("scenarios tagging a card id the dataset does not have:")
        for case_id, ids in sorted(coverage.unknown_tags.items()):
            print(f"  {case_id}: {', '.join(ids)}")
        print()

    rows = coverage.Uncovered(tier=tier, pack=pack)
    if not rows:
        print("nothing uncovered matches that filter")
        return

    label = " ".join(x for x in (tier, pack) if x) or "all tiers"
    print(f"next up ({label}) -- {len(rows)} uncovered, "
          f"largest script first:")
    for row in rows[:top]:
        asks = f"  asks: {', '.join(row['asks'])}" if row["asks"] else ""
        flag = "  QUARANTINED" if row["quarantined"] else ""
        print(f"  {row['card_id']:<8} {row['tier']:<12} {row['lines']:>4}L "
              f"{row['pack']:<10} {row['name'][:34]:<34}{asks}{flag}")
    if len(rows) > top:
        print(f"  ... {len(rows) - top} more")


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__.split("\n\n")[0],
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tier", default="", choices=("",) + TIER_ORDER,
                        help="only list uncovered cards in this depth tier")
    parser.add_argument("--pack", default="", help="only list this pack")
    parser.add_argument("--top", type=int, default=25,
                        help="how many uncovered cards to list (default 25)")
    parser.add_argument("--out", default="", help="write the full report as JSON")
    args = parser.parse_args(argv)

    coverage = Build()
    Report(coverage, tier=args.tier, pack=args.pack, top=args.top)

    if args.out:
        payload = coverage.ToDict()
        payload["uncovered"] = coverage.Uncovered(tier=args.tier, pack=args.pack)
        with open(args.out, "w", encoding="utf-8") as handle:
            json.dump(payload, handle, indent=2, sort_keys=True)
            handle.write("\n")
        print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
