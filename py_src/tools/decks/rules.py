"""Marvel Champions deckbuilding rules, read from the printed card dataset.

MARVEL-80. The point of this module is the *checker*, not the builder.

A generated deck that breaks the rules does not necessarily fail. The engine
will happily shuffle it and play it, and the result is a corpus entry that
describes a game the rules do not allow -- an oracle entry that is worse than
not generating it, because it looks exactly like a valid one. So a deck is
checked before it is written, the check refuses rather than warns, and every
violation names the card and the rule.

Rules are read from `datasets/cards/cards.json` (`tools/cards/extract.py`), never
from the engine's `data/cards.json` -- see AGENTS.md. The fields that carry
them:

    faction     hero / basic / aggression / justice / leadership / protection /
                pool / encounter / campaign
    set         for a hero card, which hero it belongs to ("spider_man")
    deck_limit  the most copies one deck may hold
    type        hero, alter_ego, ally, event, ...

Run from `py_src/`.
"""

from __future__ import annotations

import collections
import json
import os
from dataclasses import dataclass, field
from typing import Any, Dict, Iterable, List, Optional, Sequence, Set, Tuple

CARD_DATASET = "../datasets/cards/cards.json"

# The four aspects, plus Deadpool's. A deck takes cards from exactly one.
ASPECTS: Tuple[str, ...] = ("aggression", "justice", "leadership", "protection")
POOL = "pool"

# Factions a player deck may draw on at all. Everything else -- encounter,
# campaign -- belongs to the scenario side and is a hard error in a deck.
PLAYER_FACTIONS: Tuple[str, ...] = ("hero", "basic") + ASPECTS + (POOL,)

# The printed minimum. A deck may be larger; it may not be smaller.
MINIMUM_DECK = 40

# Deck fields that hold the cards a player draws. `hero`, `obligations`,
# `nemesis_set` and `set_aside` are the identity and its nemesis set: they are
# fixed by the hero rather than chosen, so they are not deckbuilding decisions
# and are not counted toward the minimum.
DECK_FIELDS: Tuple[str, ...] = ("hero_deck", "player_deck")


class DeckError(Exception):
    """A deck that cannot be checked at all -- malformed, or an unknown card."""


@dataclass(frozen=True)
class Violation:
    rule: str
    detail: str

    def __str__(self) -> str:
        return f"{self.rule}: {self.detail}"


@dataclass
class Catalogue:
    """The dataset, indexed the way deckbuilding asks about it."""

    cards: Dict[str, Dict[str, Any]] = field(default_factory=dict)

    @staticmethod
    def Load(path: str = CARD_DATASET) -> "Catalogue":
        if not os.path.exists(path):
            raise DeckError(f"{path}: not found -- run from py_src/")
        with open(path, "r", encoding="utf-8") as handle:
            data = json.load(handle)
        return Catalogue({card["card_id"]: card for card in data["cards"]})

    def Get(self, card_id: str) -> Dict[str, Any]:
        try:
            return self.cards[card_id]
        except KeyError:
            raise DeckError(f"no card {card_id!r} in the dataset")

    def Faction(self, card_id: str) -> str:
        return str(self.Get(card_id).get("faction") or "").lower()

    def Set(self, card_id: str) -> str:
        return str(self.Get(card_id).get("set") or "")

    def Name(self, card_id: str) -> str:
        return str(self.Get(card_id).get("name") or card_id)

    def Limit(self, card_id: str) -> int:
        """How many copies one deck may hold.

        `deck_limit` is absent for most cards, because most cards are encounter
        cards that never enter a deck. For a player card that is missing it the
        printed default is 3, and defaulting *down* would reject legal decks --
        so the permissive default is the correct one here and the strictness
        lives in the faction and set checks instead.
        """
        limit = self.Get(card_id).get("deck_limit")
        return int(limit) if limit else 3

    def Playable(self, card_id: str) -> bool:
        return self.Faction(card_id) in PLAYER_FACTIONS

    def HeroCardsFor(self, hero_set: str) -> List[str]:
        return sorted(card_id for card_id, card in self.cards.items()
                      if str(card.get("faction") or "").lower() == "hero"
                      and str(card.get("set") or "") == hero_set
                      and str(card.get("type") or "") not in ("hero", "alter_ego"))

    def AspectCards(self, aspect: str) -> List[str]:
        return sorted(card_id for card_id, card in self.cards.items()
                      if str(card.get("faction") or "").lower() == aspect)

    def BasicCards(self) -> List[str]:
        return sorted(card_id for card_id, card in self.cards.items()
                      if str(card.get("faction") or "").lower() == "basic")


################################################################################
#


# Heroes whose printed deck-building line changes how many aspects they may
# take. Keyed by the identity's `set`, with the card the rule is printed on.
#
# Derived rather than invented: exactly two identities in the whole dataset
# carry a line matching /deck[- ]?building/, and both are here. The table is
# written out instead of parsed because two English sentences are not a grammar,
# and a regex over them would fail silently on the third one to be printed --
# `test_decks.py` fails if a new identity prints such a line and is not listed.
ASPECT_EXCEPTIONS: Dict[str, Tuple[int, str, str]] = {
    # set: (how many aspects, the card it is printed on, the printed rule)
    "spider_woman": (2, "04031b",
                     "Choose two aspects instead of one during deck-building. "
                     "You must include an equal number of cards from those "
                     "aspects in your deck."),
    "warlock": (4, "21031b",
                "During deck-building, your deck must include an equal number "
                "of cards from all 4 aspects. You cannot include more than 1 "
                "copy of any non-Adam Warlock card."),
}

DECKBUILDING_LINE = "deck-building"


def AspectAllowance(hero_set: str) -> int:
    """How many aspects this hero's deck may draw on. One, unless printed."""
    return ASPECT_EXCEPTIONS.get(hero_set, (1, "", ""))[0]


def DeckAspect(catalogue: Catalogue, card_ids: Iterable[str]) -> Set[str]:
    """Which aspects the cards in a deck belong to."""
    return {catalogue.Faction(card_id) for card_id in card_ids
            if catalogue.Faction(card_id) in ASPECTS or
            catalogue.Faction(card_id) == POOL}


def HeroSet(catalogue: Catalogue, deck: Dict[str, Any]) -> str:
    """The `set` the identity belongs to, which its hero cards must match."""
    identity = deck.get("hero") or []
    if not identity:
        raise DeckError("deck has no hero")
    # The engine writes the two faces as one comma-joined entry.
    first = str(identity[0]).split(",")[0].strip()
    return catalogue.Set(first)


def Check(deck: Dict[str, Any], catalogue: Optional[Catalogue] = None,
          *, minimum: int = MINIMUM_DECK) -> List[Violation]:
    """Every way this deck breaks the rules. Empty means legal.

    Returns rather than raises, so a caller can report all of them at once --
    fixing one violation at a time through six runs is how a generator gets
    abandoned. `Validate` is the raising wrapper.
    """
    catalogue = catalogue or Catalogue.Load()
    violations: List[Violation] = []

    card_ids: List[str] = []
    for field_name in DECK_FIELDS:
        card_ids.extend(str(x) for x in (deck.get(field_name) or []))

    if not card_ids:
        return [Violation("empty", "the deck holds no cards")]

    hero_set = HeroSet(catalogue, deck)

    # -- size ---------------------------------------------------------------
    if len(card_ids) < minimum:
        violations.append(Violation(
            "size", f"{len(card_ids)} cards, the minimum is {minimum}"))

    # -- aspects ------------------------------------------------------------
    aspects = DeckAspect(catalogue, card_ids)
    allowed = AspectAllowance(hero_set)
    if len(aspects) > allowed:
        listing = ", ".join(sorted(aspects))
        printed = ASPECT_EXCEPTIONS.get(hero_set)
        rule = (f"{hero_set} may take {allowed} ({printed[1]}: {printed[2]})"
                if printed else "a deck takes cards from exactly one aspect")
        violations.append(Violation(
            "aspect", f"cards from {len(aspects)} aspects ({listing}); {rule}"))

    # -- faction and set ----------------------------------------------------
    for card_id in sorted(set(card_ids)):
        faction = catalogue.Faction(card_id)
        name = catalogue.Name(card_id)
        if not catalogue.Playable(card_id):
            violations.append(Violation(
                "faction", f"{card_id} ({name}) is a {faction or 'factionless'} "
                           f"card and cannot go in a player deck"))
            continue
        if faction == "hero" and catalogue.Set(card_id) != hero_set:
            violations.append(Violation(
                "hero-specific", f"{card_id} ({name}) belongs to "
                                 f"{catalogue.Set(card_id) or '(no set)'}, "
                                 f"not to {hero_set}"))

    # -- copy limits --------------------------------------------------------
    for card_id, count in sorted(collections.Counter(card_ids).items()):
        limit = catalogue.Limit(card_id)
        if count > limit:
            violations.append(Violation(
                "copies", f"{count} copies of {card_id} "
                          f"({catalogue.Name(card_id)}), the limit is {limit}"))

    return violations


def Validate(deck: Dict[str, Any], catalogue: Optional[Catalogue] = None,
             *, where: str = "deck", minimum: int = MINIMUM_DECK) -> None:
    """Raise unless the deck is legal.

    The loud half of MARVEL-80. A deck that reaches a corpus run illegal
    produces a scene describing a game the rules do not allow, and it looks
    exactly like a valid one -- so this is called before a deck is written, not
    after it is played.
    """
    violations = Check(deck, catalogue, minimum=minimum)
    if not violations:
        return
    lines = "\n  ".join(str(v) for v in violations)
    raise DeckError(f"{where} is not a legal deck:\n  {lines}")


################################################################################
#


def ReadDeck(path: str) -> Dict[str, Any]:
    with open(path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def CheckFolder(folder: str, catalogue: Optional[Catalogue] = None
                ) -> List[Tuple[str, List[Violation]]]:
    """Every deck under `folder`, with whatever each one breaks."""
    catalogue = catalogue or Catalogue.Load()
    rows: List[Tuple[str, List[Violation]]] = []
    for dirpath, _dirs, files in os.walk(folder):
        for name in sorted(files):
            if not name.endswith(".json"):
                continue
            path = os.path.join(dirpath, name)
            try:
                rows.append((path, Check(ReadDeck(path), catalogue)))
            except DeckError as exc:
                rows.append((path, [Violation("unreadable", str(exc))]))
    return rows


################################################################################
#


def main(argv: Optional[List[str]] = None) -> int:
    import argparse

    parser = argparse.ArgumentParser(
        description="Check decks against the printed deckbuilding rules.")
    parser.add_argument("paths", nargs="*", default=["deck/starter"],
                        help="deck .json files or folders of them")
    parser.add_argument("--quiet", action="store_true",
                        help="only print the decks that break a rule")
    args = parser.parse_args(argv)

    catalogue = Catalogue.Load()
    rows: List[Tuple[str, List[Violation]]] = []
    for path in args.paths:
        if os.path.isdir(path):
            rows.extend(CheckFolder(path, catalogue))
        else:
            try:
                rows.append((path, Check(ReadDeck(path), catalogue)))
            except DeckError as exc:
                rows.append((path, [Violation("unreadable", str(exc))]))

    illegal = [(path, violations) for path, violations in rows if violations]
    for path, violations in rows:
        if violations:
            print(f"ILLEGAL  {path}")
            for violation in violations:
                print(f"         {violation}")
        elif not args.quiet:
            print(f"ok       {path}")

    print(f"\n{len(rows)} deck(s), {len(illegal)} illegal")
    return 1 if illegal else 0


if __name__ == "__main__":
    import sys
    sys.exit(main())
