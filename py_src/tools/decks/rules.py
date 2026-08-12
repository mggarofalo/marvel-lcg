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
    type        hero, alter_ego, ally, event, player_side_scheme, ...
    traits      the printed trait line, split ("X-Men", "S.H.I.E.L.D.")
    stats       the printed numbers, including the resource icons
                (`resource_energy`, `resource_physical`, ...)

Run from `py_src/`.
"""

from __future__ import annotations

import collections
import itertools
import json
import os
from dataclasses import dataclass, field
from typing import (
    Any, Callable, Dict, Iterable, List, Optional, Sequence, Set, Tuple)

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


# The printed deck-building modifiers, in two shapes.
#
# The first shape widens *how many aspects* a deck may draw on -- Spider-Woman
# takes two, Adam Warlock all four -- and in both printed cases requires those
# aspects to be the same size.
#
# The second shape is narrower and much more common: the deck still has one
# aspect, but cards **matching a description** are let in from the others.
# Cyclops takes X-Men allies, Cable player side schemes, Wonder Man events with
# an energy icon; Gamora takes at most 6 attack/thwart events and Maria Hill at
# most 3 S.H.I.E.L.D. supports. Those are all one rule -- *cards matching P from
# an aspect you did not choose, up to N* -- so they are modelled as one rule
# with a predicate rather than five special cases, and a sixth such identity
# needs a table row and no code.
#
# Every predicate reads a printed field. The bracket convention in the printed
# text says which: MarvelSDB renders a **trait** as `[[X-MEN]]` and a **resource
# icon** as `[energy]`, so "[[X-MEN]] allies" is `type == ally and "X-Men" in
# traits` while "a printed [energy] resource icon" is `stats.resource_energy`.
# `[[attack]]`/`[[thwart]]` are likewise traits, not the `(attack)` marker in an
# ability line -- the trait is the superset (249 events carry both, 20 carry only
# the trait, none only the marker), and the trait is what is printed on the card.

ALLY = "ally"
EVENT = "event"
SUPPORT = "support"
PLAYER_SIDE_SCHEME = "player_side_scheme"

TRAIT_XMEN = "X-Men"
TRAIT_SHIELD = "S.H.I.E.L.D."
TRAIT_ATTACK = "Attack"
TRAIT_THWART = "Thwart"


def _Is(card_type: str, *traits: str) -> Callable[[Dict[str, Any]], bool]:
    """A card of this type, carrying at least one of these traits."""
    def Match(card: Dict[str, Any]) -> bool:
        if str(card.get("type") or "") != card_type:
            return False
        return not traits or bool(set(traits) & set(card.get("traits") or []))
    return Match


def _EnergyEvent(card: Dict[str, Any]) -> bool:
    """An event with a printed [energy] resource icon."""
    if str(card.get("type") or "") != EVENT:
        return False
    return int((card.get("stats") or {}).get("resource_energy") or 0) > 0


@dataclass(frozen=True)
class Allowance:
    """Cards matching `predicate` may come from aspects the deck did not choose.

    `limit` is how many, `None` for no cap. Gamora's is counted in cards ("up to
    6 attack and/or thwart events"); Maria Hill's is counted in *titles* ("the
    maximum number of copies of 3 S.H.I.E.L.D. supports" -- three cards, each at
    its full copy limit), which is what `by_title` selects.

    Cards from the aspect the deck *did* choose never consume an allowance:
    both capped lines say "from aspects other than your chosen aspect", and the
    three uncapped ones say "from any aspect", which is only a widening.
    """
    what: str
    predicate: Callable[[Dict[str, Any]], bool]
    limit: Optional[int] = None
    by_title: bool = False


@dataclass(frozen=True)
class Deckbuilding:
    """One identity's printed deck-building line."""
    card_id: str
    printed: str
    aspects: int = 1
    equal: bool = False
    allowances: Tuple[Allowance, ...] = ()
    # Adam Warlock alone: every card that is not his own is capped at 1 copy,
    # under its printed `deck_limit`.
    copy_cap: Optional[int] = None


# Derived rather than invented: seven identities in the whole dataset print a
# line matching any of `DECKBUILDING_LINES`, and all seven are here. The table
# is written out instead of parsed because seven English sentences are not a
# grammar, and a regex over them would fail silently on the eighth --
# `test_decks.py` fails if a new identity prints such a line and is not listed.
#
# Keyed by the identity's `set`. Note Gamora's is `gam`, not `gamora`.
DECKBUILDING: Dict[str, Deckbuilding] = {
    "spider_woman": Deckbuilding(
        "04031b",
        "Choose two aspects instead of one during deck-building. You must "
        "include an equal number of cards from those aspects in your deck.",
        aspects=2, equal=True),
    "warlock": Deckbuilding(
        "21031b",
        "During deck-building, your deck must include an equal number of cards "
        "from all 4 aspects. You cannot include more than 1 copy of any "
        "non-Adam Warlock card.",
        aspects=4, equal=True, copy_cap=1),
    "cyclops": Deckbuilding(
        "33001b",
        "You may include [[X-MEN]] allies from any aspect in your deck.",
        allowances=(Allowance("X-Men allies", _Is(ALLY, TRAIT_XMEN)),)),
    "cable": Deckbuilding(
        "40001b",
        "You may include player side schemes from any aspect in your deck.",
        allowances=(Allowance("player side schemes",
                              _Is(PLAYER_SIDE_SCHEME)),)),
    "gam": Deckbuilding(
        "18001b",
        "You may include up to 6 [[attack]] and/or [[thwart]] events in your "
        "deck from aspects other than your chosen aspect.",
        allowances=(Allowance("attack and/or thwart events",
                              _Is(EVENT, TRAIT_ATTACK, TRAIT_THWART),
                              limit=6),)),
    "maria_hill": Deckbuilding(
        "50001b",
        "You may include the maximum number of copies of 3 [[S.H.I.E.L.D.]] "
        "supports in your deck from aspects other than your chosen aspect.",
        allowances=(Allowance("S.H.I.E.L.D. supports",
                              _Is(SUPPORT, TRAIT_SHIELD),
                              limit=3, by_title=True),)),
    "wonder_man": Deckbuilding(
        "58001b",
        "You may include events with a printed [energy] resource icon from any "
        "aspect in your deck.",
        allowances=(Allowance("events with a printed energy resource icon",
                              _EnergyEvent),)),
}

NO_EXCEPTION = Deckbuilding("", "a deck takes cards from exactly one aspect")

# Every phrasing an identity has used to print a deck-building modifier. The
# guard in `test_decks.py` greps identity text for these and fails if it finds a
# `set` that `DECKBUILDING` does not list -- widening this set is how an eighth
# identity gets caught rather than silently checked under the default.
DECKBUILDING_LINES: Tuple[str, ...] = (
    "you may include", "deck-building", "deckbuilding", "deck building",
    "instead of one", "in your deck",
)


def Rule(hero_set: str) -> Deckbuilding:
    """This hero's printed deck-building line, or the unmodified default."""
    return DECKBUILDING.get(hero_set, NO_EXCEPTION)


def AspectAllowance(hero_set: str) -> int:
    """How many aspects this hero's deck may draw on. One, unless printed."""
    return Rule(hero_set).aspects


def DeckAspect(catalogue: Catalogue, card_ids: Iterable[str]) -> Set[str]:
    """Which aspects the cards in a deck belong to."""
    return {catalogue.Faction(card_id) for card_id in card_ids
            if catalogue.Faction(card_id) in ASPECTS or
            catalogue.Faction(card_id) == POOL}


def _AgainstChoice(catalogue: Catalogue, card_ids: Sequence[str],
                   chosen: Tuple[str, ...], rule: Deckbuilding
                   ) -> Tuple[int, List[Violation]]:
    """What breaks if this deck chose exactly these aspects.

    Returns a cost as well as the violations. The cost counts *cards* rather
    than violations, because it is what picks the choice to report: two copies
    of one off-aspect card is one violation but a worse deck than one copy of
    another, and a choice that leaves 4 cards stranded must beat one that leaves
    26 even when both read as two lines.
    """
    violations: List[Violation] = []
    cost = 0
    listing = ", ".join(chosen) or "none"

    # -- cards from an aspect the deck did not choose ------------------------
    outside = [card_id for card_id in card_ids
               if catalogue.Faction(card_id) in ASPECTS + (POOL,)
               and catalogue.Faction(card_id) not in chosen]

    taken: Dict[int, List[str]] = {i: [] for i in range(len(rule.allowances))}
    stranded: List[str] = []
    for card_id in sorted(outside):
        for index, allowance in enumerate(rule.allowances):
            if allowance.predicate(catalogue.Get(card_id)):
                taken[index].append(card_id)
                break
        else:
            stranded.append(card_id)

    for card_id, count in sorted(collections.Counter(stranded).items()):
        cost += count
        violations.append(Violation(
            "aspect", f"{count} x {card_id} ({catalogue.Name(card_id)}) is a "
                      f"{catalogue.Faction(card_id)} card and the deck's "
                      f"aspect is {listing}"))

    # -- more taken under an allowance than it permits -----------------------
    for index, allowance in enumerate(rule.allowances):
        if allowance.limit is None:
            continue
        used = (len(set(taken[index])) if allowance.by_title
                else len(taken[index]))
        if used > allowance.limit:
            unit = "title" if allowance.by_title else "card"
            cost += used - allowance.limit
            violations.append(Violation(
                "allowance",
                f"{used} {unit}s of {allowance.what} from outside {listing}; "
                f"{rule.card_id} allows {allowance.limit}"))

    # -- the chosen aspects must be the same size ----------------------------
    #
    # Only aspects the deck actually drew on are balanced. A Spider-Woman deck
    # that used one aspect is checked as a one-aspect deck rather than told its
    # unused second aspect is empty: rejecting a legal deck is the failure mode
    # this module is most careful about, and nothing else here reads the deck's
    # unwritten choice either.
    if rule.equal and len(chosen) > 1:
        sizes = {aspect: sum(1 for card_id in card_ids
                             if catalogue.Faction(card_id) == aspect)
                 for aspect in chosen}
        if len(set(sizes.values())) > 1:
            cost += sum(sizes.values()) - min(sizes.values()) * len(sizes)
            counts = ", ".join(f"{a} {n}" for a, n in sorted(sizes.items()))
            violations.append(Violation(
                "balance", f"{counts}; {rule.card_id} requires an equal number "
                           f"of cards from each chosen aspect"))

    return cost, violations


def _CheckAspects(catalogue: Catalogue, card_ids: Sequence[str],
                  rule: Deckbuilding) -> List[Violation]:
    """The aspect rules, read against the best choice the deck could have made.

    A deck file does not record which aspect its builder chose, so the checker
    asks the only question it can: **is there a choice that makes this deck
    legal?** It tries every combination of the aspects present and reports the
    cheapest, which is both the right answer for a legal deck (cost 0 exists)
    and the most useful one for an illegal deck (the reading that blames the
    fewest cards). There are at most 6 combinations, so this is free.
    """
    present = sorted(DeckAspect(catalogue, card_ids))
    size = min(rule.aspects, len(present))
    best: Optional[Tuple[int, List[Violation]]] = None
    for chosen in itertools.combinations(present, size):
        result = _AgainstChoice(catalogue, card_ids, chosen, rule)
        if best is None or result[0] < best[0]:
            best = result
        if best[0] == 0:
            break
    return best[1] if best else []


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
    rule = Rule(hero_set)

    # -- size ---------------------------------------------------------------
    if len(card_ids) < minimum:
        violations.append(Violation(
            "size", f"{len(card_ids)} cards, the minimum is {minimum}"))

    # -- aspects ------------------------------------------------------------
    violations.extend(_CheckAspects(catalogue, card_ids, rule))

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
    #
    # Printed `deck_limit`, except where the identity caps it lower: Adam
    # Warlock's "you cannot include more than 1 copy of any non-Adam Warlock
    # card" is the only printed line that does, and it spares his own cards.
    for card_id, count in sorted(collections.Counter(card_ids).items()):
        limit = catalogue.Limit(card_id)
        why = f"the limit is {limit}"
        if rule.copy_cap is not None and catalogue.Set(card_id) != hero_set:
            if rule.copy_cap < limit:
                limit = rule.copy_cap
                why = (f"{rule.card_id} caps every card that is not "
                       f"{hero_set}'s at {limit}")
        if count > limit:
            violations.append(Violation(
                "copies", f"{count} copies of {card_id} "
                          f"({catalogue.Name(card_id)}), {why}"))

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
