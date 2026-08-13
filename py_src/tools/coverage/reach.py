"""Which game setups would put a card into play, and which cards none would.

Coverage reports say what a corpus *did* reach. This says what it *could*, and
the difference is what MARVEL-16 needs to bias the next round toward: a card no
setup can bring into a game is not a gap in the sampling, it is a gap in the
shipped data, and no amount of self-play will close it.

    python -m tools.coverage.reach                       # the summary
    python -m tools.coverage.reach --out reach.json      # the whole map
    python -m tools.coverage.reach --unreachable         # what nothing reaches
    python -m tools.coverage.reach --corpus ./corpus/    # check the map against reality

Pure data join over the same files the engine loads. Nothing here boots an
engine or plays a game, so it costs a second and can be re-run after any change
to `deck/` or `data/`.

## The number this exists to produce

Of 3781 cards with an engine script, **3617 (95.7%) are reachable** from the 63
starter decks, the 8 generated ones MARVEL-80 added, and the shipped scenarios
and encounter sets put together. The remaining 164 are what self-play cannot get
to at any corpus size, and they are the list MARVEL-16 asks to hand to Spec
Extraction.

It was 3447 (91.2%) against 334 unreachable before MARVEL-80 built decks that
carry the cards no shipped deck names.

**44 of those 164 are wrong**, and `--corpus` says so: they are `twc` cards a
card script pulls in by literal id, which the paragraph below explains and
MARVEL-98 exists to fix. Treat the residue as an upper bound on the authoring
list until it does.

## This is a lower bound, and cross-checking it is how it gets fixed

A file-based map can only see what a file names. Two things it cannot:

- **Cards the engine creates.** `tough`, `stunned` and `confused` are in no deck.
- **Decks a card script builds from literal ids.** `data/scenarios/the_wrecking_crew.json`
  has an empty `villain` and `encounters`, listing only the four villains in
  `set_aside`; the rest of their encounter cards appear in no deck or set file
  at all. They are **Python literals in the main scheme's script** --
  `cards/pack/twc/07001a.py:32` opens a four-element list of id strings and
  feeds it to `CardFactory.GenerateCards`, then sets `skip_create_encounter_deck`
  so the data-driven builder at `game/world/world.py:231` never runs. There is
  no metadata join to replicate: nothing queries `cards.json` for set
  membership. Closing this means scanning card scripts for literal id lists,
  which is MARVEL-98. A 321-case corpus plays **44** such `twc` cards.

So `--corpus` exists: it compares the map against what a corpus *actually*
resolved and lists every card that was played despite being called unreachable.
That check is not decoration -- it is what caught the `player_deck` key being
missing from `HERO_KEYS`, which understated reachability by 756 cards and would
otherwise have shipped as a confident, wrong 71%.

## Sources

A card is reachable if some file names it:

| kind | folder | keys |
|---|---|---|
| `hero` | `deck/starter/` | `hero`, `hero_deck`, `set_aside`, `obligations`, `nemesis_set` |
| `encounter-set` | `data/encounter_sets/`, `data/nemesis/` | `encounters`, `set_aside` |
| `scenario` | `data/scenarios/`, `data/challenges/` | `villain`, `schemes`, `encounters`, `set_aside`, `advanced`, `back_side` |

Ids are comma-joined in several of those keys -- `"01097a,01097b"` is one entry
naming a two-sided card -- so every value is split before use. A card that is
only ever the *back* of another is reachable through the front, which is why
the split matters rather than being tidying.

Reachable is not the same as *played*: a nemesis set only enters a game if that
hero is in it and the obligation comes up, and a modular set only if the
scenario draws it. This is an upper bound, which is the useful direction -- what
it calls unreachable really is.
"""

from __future__ import annotations

import argparse
import glob
import json
import os
import sys
from typing import Any, Dict, Iterable, List, NamedTuple, Sequence, Set

from engine.profile import coverage_report

HERO_FOLDERS = (os.path.join("deck", "starter"),)

# Decks built by `tools.decks.build` to carry cards no shipped deck names
# (MARVEL-80). Kept as a separate source kind rather than folded into
# HERO_FOLDERS, so the shipped ceiling stays readable next to the built one --
# a single merged number would make it impossible to tell whether reach moved
# because the game changed or because this tool ran.
GENERATED_HERO_FOLDERS = (os.path.join("deck", "generated"),)
ENCOUNTER_FOLDERS = (os.path.join("data", "encounter_sets"),
                     os.path.join("data", "nemesis"))
SCENARIO_FOLDERS = (os.path.join("data", "scenarios"),
                    os.path.join("data", "challenges"))

# Every key in these files that holds card ids. `unit_test/test_coverage_reach.py`
# fails if a file grows one that is not listed here, because a missed key does
# not look like a bug -- it looks like a smaller universe. `player_deck` was
# missed exactly that way: it is the aspect and basic half of a starter deck,
# 25 of its 40 cards, and leaving it out understated what self-play can reach by
# 756 cards. What gave it away was a corpus resolving cards this file called
# unreachable.
#
# `encounter_sets` and `modular_sets` are deliberately **not** here: they hold
# the *names* of other files, which are read as sources in their own right.
# Feeding them to `Ids` would put set names into the card id set.
HERO_KEYS = ("hero", "hero_deck", "player_deck", "set_aside", "obligations",
             "nemesis_set")
ENCOUNTER_KEYS = ("encounters", "set_aside")
SCENARIO_KEYS = ("villain", "schemes", "encounters", "set_aside", "challenges")


class Source(NamedTuple):
    kind: str       # "hero" | "encounter-set" | "scenario"
    name: str
    cards: Set[str]


class Map(NamedTuple):
    sources: List[Source]

    @property
    def reachable(self) -> Set[str]:
        found: Set[str] = set()
        for source in self.sources:
            found |= source.cards
        return found

    def Of(self, kind: str) -> List[Source]:
        return [source for source in self.sources if source.kind == kind]

    def Sources(self, card_id: str) -> List[Source]:
        return [source for source in self.sources if card_id in source.cards]

    def Unreachable(self, universe: Sequence[str]) -> List[str]:
        return sorted(set(universe) - self.reachable)

    def Yield(self, kind: str, wanted: Iterable[str]) -> List[tuple]:
        """(name, how many of `wanted` it would bring in), best first.

        The input to coverage-directed planning: pick the setup that brings the
        most cards nothing has played yet.
        """
        target = set(wanted)
        scored = [(source.name, len(source.cards & target))
                  for source in self.Of(kind)]
        # Sorted by name inside each score so the order is stable, which keeps a
        # plan built from this reproducible.
        return sorted(scored, key=lambda pair: (-pair[1], pair[0]))


def Ids(values: Any) -> Set[str]:
    """Card ids out of one JSON key.

    Several keys hold comma-joined ids for a multi-sided card
    (`"01097a,01097b"`), so splitting is part of reading them rather than
    something a caller should have to know.
    """
    found: Set[str] = set()
    for value in values or []:
        if not isinstance(value, str):
            continue
        for part in value.split(","):
            part = part.strip()
            if part:
                found.add(part)
    return found


def ReadFolder(folders: Sequence[str], keys: Sequence[str],
               kind: str) -> List[Source]:
    sources: List[Source] = []
    for folder in folders:
        for path in sorted(glob.glob(os.path.join(folder, "*.json"))):
            try:
                with open(path, encoding="utf-8") as handle:
                    document = json.load(handle)
            except (OSError, ValueError):
                # A file that will not parse names no cards. The engine would
                # fail on it far more loudly than this needs to.
                continue
            if not isinstance(document, dict):
                continue
            cards: Set[str] = set()
            for key in keys:
                cards |= Ids(document.get(key))
            sources.append(Source(kind, os.path.basename(path)[:-len(".json")],
                                  cards))
    return sources


def Build(*, generated: bool=True) -> Map:
    """The reach map. `generated=False` gives the shipped-only baseline."""
    sources = ReadFolder(HERO_FOLDERS, HERO_KEYS, "hero")
    if generated:
        sources += ReadFolder(GENERATED_HERO_FOLDERS, HERO_KEYS,
                              "hero-generated")
    return Map(sources=(
        sources
        + ReadFolder(ENCOUNTER_FOLDERS, ENCOUNTER_KEYS, "encounter-set")
        + ReadFolder(SCENARIO_FOLDERS, SCENARIO_KEYS, "scenario")
    ))


def Describe(reach: Map, universe: Sequence[str]) -> List[str]:
    known = set(universe)
    reachable = reach.reachable & known
    lines = [
        f"universe          {len(known)} card(s) with an engine script",
        f"reachable         {len(reachable)} "
        f"({len(reachable) / len(known):.1%})" if known else "reachable 0",
        f"unreachable       {len(known) - len(reachable)}",
        "",
    ]
    for kind in ("hero", "hero-generated", "encounter-set", "scenario"):
        sources = reach.Of(kind)
        cards: Set[str] = set()
        for source in sources:
            cards |= source.cards
        lines.append(f"{kind:<16}  {len(sources):>4} file(s), "
                     f"{len(cards & known):>4} card(s)")
    return lines


def Document(reach: Map, universe: coverage_report.Universe) -> Dict[str, Any]:
    known = set(universe.cards)
    unreachable = reach.Unreachable(universe.cards)
    by_pack: Dict[str, int] = {}
    for card_id in unreachable:
        pack = universe.packs.get(card_id) or "(unknown)"
        by_pack[pack] = by_pack.get(pack, 0) + 1

    return {
        "tool": "coverage-reach",
        "universe": len(known),
        "reachable": len(reach.reachable & known),
        "unreachable": len(unreachable),
        "sources": {
            kind: {source.name: sorted(source.cards & known)
                   for source in reach.Of(kind)}
            for kind in ("hero", "encounter-set", "scenario")
        },
        # Sorted by count then name: a pack with 200 unreachable cards is a
        # different finding from one with two, and the reader wants the first.
        "unreachable_by_pack": dict(sorted(by_pack.items(),
                                           key=lambda pair: (-pair[1], pair[0]))),
        "unreachable_cards": [
            {"card_id": card_id,
             "name": universe.names.get(card_id, ""),
             "pack": universe.packs.get(card_id, "")}
            for card_id in unreachable
        ],
    }


def CrossCheck(reach: Map, universe: 'coverage_report.Universe',
               corpus: str) -> List[str]:
    """Cards a corpus resolved that this map calls unreachable.

    Every one is a hole in the map rather than a surprise about the corpus, so
    a non-empty answer is a bug report about this file. The known-and-explained
    residue is 29: three engine-created status cards and 26 Wrecking Crew cards
    whose decks are built from `data/cards.json`.
    """
    from tools.coverage import report as report_module

    paths = report_module.Expand([corpus])
    if not paths:
        return [f"no coverage artefacts under {corpus}"]

    document = report_module.Merge(
        paths, dataset=coverage_report.DEFAULT_DATASET)
    known = set(universe.cards)
    resolved = set((document.get("counts") or {}).get("cards_resolved") or {}) & known
    reachable = reach.reachable & known
    outside = sorted(resolved - reachable)

    lines = [
        f"corpus resolved   {len(resolved)} "
        f"({len(resolved) / len(known):.1%} of the universe, "
        f"{len(resolved) / len(reachable):.1%} of what is reachable)",
        f"played anyway     {len(outside)} card(s) this map calls unreachable",
    ]
    for card_id in outside[:40]:
        lines.append(f"    {card_id:<10} {universe.packs.get(card_id, ''):<14} "
                     f"{universe.names.get(card_id, '')}")
    if len(outside) > 40:
        lines.append(f"    ... and {len(outside) - 40} more")
    return lines


def main(argv: List[str] | None=None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", default="", help="write the full map as JSON")
    parser.add_argument("--unreachable", action="store_true",
                        help="list what no setup reaches, grouped by pack")
    parser.add_argument("--top", type=int, default=20,
                        help="how many packs to list with --unreachable")
    parser.add_argument("--corpus", default="",
                        help="check the map against what a corpus really played")
    args = parser.parse_args(argv)

    try:
        universe = coverage_report.LoadUniverse()
    except coverage_report.DatasetMissing as exc:
        print(f"error: {exc}")
        return 2

    reach = Build()
    if not reach.sources:
        print("error: no deck or data files found -- run from py_src/")
        return 2

    for line in Describe(reach, universe.cards):
        print(line)

    document = Document(reach, universe)

    if args.unreachable:
        print("\nunreachable by pack:")
        for pack, count in list(document["unreachable_by_pack"].items())[:args.top]:
            print(f"  {count:>4}  {pack}")
        remaining = len(document["unreachable_by_pack"]) - args.top
        if remaining > 0:
            print(f"  ... and {remaining} more pack(s); --out for the full list")

    if args.corpus:
        print()
        for line in CrossCheck(reach, universe, args.corpus):
            print(line)

    if args.out:
        with open(args.out, "w", encoding="utf-8") as handle:
            json.dump(document, handle, indent=2, sort_keys=True)
        print(f"\nwrote {args.out}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
