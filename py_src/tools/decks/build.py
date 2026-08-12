"""Build legal decks that carry a given set of target cards. MARVEL-80.

A corpus can only resolve a card that some deck, encounter set or scenario puts
into play. `tools.coverage.reach` measures which cards nothing names: 336 of
3,781 scripted cards, after MARVEL-85. This module addresses the part of that
gap a deck can close.

    of the 336 unreachable cards
      172  aspect or basic player cards -- a deck can hold these
      164  encounter, campaign or factionless -- no player deck can ever hold
           them, and they need encounter-set or scenario coverage instead

That split is the first thing to understand before reading the acceptance
criteria on MARVEL-80. Building decks cannot move the reach number past 91.1%
+ 172/3781; the rest is a different mechanism.

## Why decks are derived from a starter deck rather than synthesised

A deck file carries four things that are *not* deckbuilding decisions -- the
identity, its obligations, its nemesis set, and its set-aside cards. They are
fixed by the hero. Synthesising them means re-deriving data that already exists
correctly in `deck/starter/`, and getting any of it wrong produces a deck that
loads and plays while describing a game the rules do not allow.

So a built deck is a starter deck with its `player_deck` replaced. Everything
the hero fixes is inherited; the only thing generated is the part a player
actually chooses. `hero_deck` is inherited for the same reason.

## The legality guarantee

Nothing here is trusted. Every deck is passed through `rules.Check` before it is
returned, and `BuildDecks` raises rather than returning a deck that breaks a
rule. A deck that reaches a corpus run illegal does not fail loudly -- the
engine shuffles it and plays it, and the result is an oracle entry that looks
exactly like a valid one. See `rules.py`.

Run from `py_src/`:

    python -m tools.decks.build --targets 44013,44014 --out ./deck/generated
    python -m tools.decks.build --unreachable --out ./deck/generated
"""

from __future__ import annotations

import argparse
import collections
import json
import os
from dataclasses import dataclass, field
from typing import Any, Dict, Iterable, List, Optional, Sequence, Set, Tuple

from tools.decks import rules

STARTER_FOLDER = "deck/starter"
DEFAULT_OUTPUT = "deck/generated"

# The aspect a deck is built around. `pool` is Deadpool's and is kept separate
# below -- see `BASE_FOR_ASPECT`.
BUILDABLE_ASPECTS: Tuple[str, ...] = rules.ASPECTS + (rules.POOL,)

# Basic cards belong to no aspect and fit in any deck, so they are spread across
# whatever decks have room rather than forcing a deck of their own.
BASIC = "basic"


class BuildError(Exception):
    """A target set that cannot be built into legal decks."""


@dataclass
class BuiltDeck:
    """One generated deck, and which targets it was built to carry."""
    name: str
    base: str
    aspect: str
    targets: List[str]
    deck: Dict[str, Any]

    @property
    def size(self) -> int:
        return sum(len(self.deck.get(f) or []) for f in rules.DECK_FIELDS)


################################################################################
#


def ReadStarters(folder: str = STARTER_FOLDER) -> Dict[str, Dict[str, Any]]:
    """Every starter deck, keyed by its file stem -- the `-bot_heroes` name."""
    if not os.path.isdir(folder):
        raise BuildError(f"{folder}: not found -- run from py_src/")
    starters: Dict[str, Dict[str, Any]] = {}
    for name in sorted(os.listdir(folder)):
        if name.endswith(".json"):
            starters[name[:-5]] = rules.ReadDeck(os.path.join(folder, name))
    return starters


def DeckAspectOf(catalogue: rules.Catalogue, deck: Dict[str, Any]) -> str:
    """Which single aspect this deck draws on, or "" if it is not exactly one."""
    aspects = rules.DeckAspect(catalogue, [
        str(x) for field_name in rules.DECK_FIELDS
        for x in (deck.get(field_name) or [])])
    return next(iter(aspects)) if len(aspects) == 1 else ""


def BaseForAspect(catalogue: rules.Catalogue,
                  starters: Dict[str, Dict[str, Any]]) -> Dict[str, str]:
    """A starter deck to build each aspect's decks from.

    Chosen rather than hardcoded, and chosen conservatively: a hero with a
    printed deck-building exception is skipped, because building on one means
    the generated deck has to satisfy that hero's rule as well -- Adam Warlock
    caps every non-Warlock card at 1 copy, Spider-Woman needs two aspects at
    equal size. Those are legal decks to build, just not the simplest ones, and
    a builder that produces them by accident is a builder whose output has to be
    read carefully.

    Ties are broken alphabetically so the choice is reproducible.
    """
    chosen: Dict[str, str] = {}
    for name in sorted(starters):
        deck = starters[name]
        hero_set = rules.HeroSet(catalogue, deck)
        if hero_set in rules.DECKBUILDING:
            continue
        aspect = DeckAspectOf(catalogue, deck)
        if aspect and aspect not in chosen:
            chosen[aspect] = name
    return chosen


################################################################################
#


def _Capacity(deck: Dict[str, Any], minimum: int) -> int:
    """How many player-deck slots this base leaves once its hero cards are in."""
    return minimum - len(deck.get("hero_deck") or [])


def _Filler(catalogue: rules.Catalogue, aspect: str, hero_set: str,
            exclude: Set[str]) -> List[str]:
    """Legal cards to pad a deck out to the minimum, most-copies-first.

    Aspect cards before basic ones, so a built deck looks like a deck rather
    than a pile of Basics, and sorted by id so the output is reproducible.
    """
    pool = [card_id for card_id in catalogue.AspectCards(aspect)
            if card_id not in exclude]
    pool += [card_id for card_id in catalogue.BasicCards()
             if card_id not in exclude]
    return pool


def _BuildOne(catalogue: rules.Catalogue, base_name: str, base: Dict[str, Any],
              aspect: str, targets: Sequence[str], index: int,
              minimum: int) -> BuiltDeck:
    """One deck: the targets, padded to `minimum` with legal filler."""
    deck = json.loads(json.dumps(base))          # deep copy, no shared lists
    hero_set = rules.HeroSet(catalogue, deck)
    capacity = _Capacity(deck, minimum)

    if len(targets) > capacity:
        raise BuildError(
            f"{len(targets)} targets will not fit in {capacity} slots")

    player_deck: List[str] = list(targets)
    counts = collections.Counter(player_deck)

    # Pad to the minimum. A target already present may take further copies up to
    # its printed limit, and that is preferred over introducing a new title --
    # it keeps the deck's spread narrow and its behaviour closer to a real one.
    for card_id in list(targets) + _Filler(catalogue, aspect, hero_set,
                                           exclude=set()):
        while (len(player_deck) < capacity
               and counts[card_id] < catalogue.Limit(card_id)):
            player_deck.append(card_id)
            counts[card_id] += 1
        if len(player_deck) >= capacity:
            break

    if len(player_deck) < capacity:
        raise BuildError(
            f"{aspect}: only {len(player_deck)} legal cards for {capacity} slots")

    deck["player_deck"] = sorted(player_deck)
    deck["name"] = f"{base.get('name', base_name)} ({aspect} {index})"
    deck["metadata"] = dict(deck.get("metadata") or {})
    deck["metadata"]["generated"] = {
        "tool": "tools.decks.build",
        "issue": "MARVEL-80",
        "base": base_name,
        "aspect": aspect,
        "targets": sorted(targets),
    }

    built = BuiltDeck(name=f"generated_{aspect}_{index}", base=base_name,
                      aspect=aspect, targets=sorted(targets), deck=deck)

    # The guarantee. A deck that breaks a rule never leaves this function.
    violations = rules.Check(deck, catalogue, minimum=minimum)
    if violations:
        lines = "\n  ".join(str(v) for v in violations)
        raise BuildError(f"built an illegal deck ({built.name}):\n  {lines}")
    return built


def BuildDecks(targets: Iterable[str], catalogue: Optional[rules.Catalogue] = None,
               *, starter_folder: str = STARTER_FOLDER,
               minimum: int = rules.MINIMUM_DECK) -> List[BuiltDeck]:
    """The fewest legal decks that between them carry every buildable target.

    Targets a deck cannot hold -- encounter, campaign, factionless -- are not an
    error and are not silently dropped either: they come back from
    `Unbuildable`, so a caller reports them rather than believing the run
    covered everything.
    """
    catalogue = catalogue or rules.Catalogue.Load()
    starters = ReadStarters(starter_folder)
    bases = BaseForAspect(catalogue, starters)

    by_aspect: Dict[str, List[str]] = collections.defaultdict(list)
    basics: List[str] = []
    for card_id in sorted(set(targets)):
        faction = catalogue.Faction(card_id)
        if faction in BUILDABLE_ASPECTS:
            by_aspect[faction].append(card_id)
        elif faction == BASIC:
            basics.append(card_id)

    # Basic cards ride along in whichever aspect decks have room, so they cost
    # no deck of their own until the aspect decks are full.
    order = sorted(by_aspect) or ([BUILDABLE_ASPECTS[0]] if basics else [])
    built: List[BuiltDeck] = []
    remaining_basics = list(basics)

    for aspect in order:
        if aspect not in bases:
            raise BuildError(f"no starter deck to build {aspect!r} decks from")
        base_name = bases[aspect]
        base = starters[base_name]
        capacity = _Capacity(base, minimum)

        queue = list(by_aspect.get(aspect, []))
        index = 1
        while queue or (remaining_basics and aspect == order[-1]):
            chunk = queue[:capacity]
            queue = queue[capacity:]
            room = capacity - len(chunk)
            if room and remaining_basics:
                chunk += remaining_basics[:room]
                remaining_basics = remaining_basics[room:]
            if not chunk:
                break
            built.append(_BuildOne(catalogue, base_name, base, aspect,
                                   chunk, index, minimum))
            index += 1

    if remaining_basics:
        raise BuildError(
            f"{len(remaining_basics)} basic target(s) had nowhere to go")
    return built


def Unbuildable(targets: Iterable[str],
                catalogue: Optional[rules.Catalogue] = None) -> List[str]:
    """Targets no player deck can hold, whatever it is built out of."""
    catalogue = catalogue or rules.Catalogue.Load()
    return sorted(card_id for card_id in set(targets)
                  if catalogue.Faction(card_id)
                  not in BUILDABLE_ASPECTS + (BASIC,))


################################################################################
#


def Write(built: Sequence[BuiltDeck], folder: str) -> List[str]:
    """Write each deck as `<folder>/<name>.json`. Returns the paths."""
    os.makedirs(folder, exist_ok=True)
    paths: List[str] = []
    for deck in built:
        path = os.path.join(folder, f"{deck.name}.json")
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(deck.deck, handle, indent=4, ensure_ascii=False)
            handle.write("\n")
        paths.append(path)
    return paths


def UnreachableTargets() -> List[str]:
    """The cards `tools.coverage.reach` says nothing names."""
    from engine.profile import coverage_report
    from tools.coverage import reach as reach_tool
    universe = coverage_report.LoadUniverse()
    return reach_tool.Build().Unreachable(universe.cards)


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--targets", default="",
                        help="comma-separated card ids to cover")
    parser.add_argument("--unreachable", action="store_true",
                        help="target every card tools.coverage.reach cannot reach")
    parser.add_argument("--out", default="",
                        help=f"write decks here (default: report only)")
    args = parser.parse_args(argv)

    if args.unreachable:
        targets = UnreachableTargets()
    else:
        targets = [t.strip() for t in args.targets.split(",") if t.strip()]
    if not targets:
        parser.error("give --targets or --unreachable")

    catalogue = rules.Catalogue.Load()
    skipped = Unbuildable(targets, catalogue)
    built = BuildDecks(targets, catalogue)

    covered = sorted({c for deck in built for c in deck.targets})
    print(f"{len(targets)} target(s)")
    print(f"  {len(covered)} carried by {len(built)} generated deck(s)")
    print(f"  {len(skipped)} cannot go in any player deck "
          f"(encounter, campaign or factionless)")
    for deck in built:
        print(f"    {deck.name:28} {deck.size:3} cards, "
              f"{len(deck.targets):3} target(s), base {deck.base}")

    if args.out:
        paths = Write(built, args.out)
        print(f"\nwrote {len(paths)} deck(s) to {args.out}")
    return 0


if __name__ == "__main__":
    import sys
    sys.exit(main())
