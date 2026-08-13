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

    faction       hero / basic / aggression / justice / leadership /
                  protection / pool / encounter / campaign
    set           for a hero card, which hero it belongs to ("spider_man")
    deck_limit    the most copies one deck may hold
    type          hero, alter_ego, ally, event, player_side_scheme, ...
    traits        the printed trait line, split ("X-Men", "S.H.I.E.L.D.")
    stats         the printed numbers, including the resource icons
                  (`resource_energy`, `resource_physical`, ...)
    deckbuilding  on an identity face, the printed deck-building rule, read
                  into structure by `tools/cards/deckbuilding.py`

That last field used to be a hand-written table here, listing the seven
identities that print such a rule (MARVEL-85). It is now read (MARVEL-88),
because a hand-written table has no way to know when it has fallen behind the
cards: the extract refuses to build a dataset containing an identity line
nobody has classified, so an eighth identity is a build failure rather than a
hero quietly checked under the default rule.

Run from `py_src/`.
"""

from __future__ import annotations

import collections
import itertools
import json
import os
from dataclasses import dataclass, field
from typing import (
    Any, Dict, Iterable, Iterator, List, Mapping, Optional, Sequence, Set,
    Tuple)

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
    # set -> the identity's printed rule, built on first use from the
    # `deckbuilding` blocks the records carry.
    _rules: Optional[Dict[str, "Deckbuilding"]] = field(
        default=None, init=False, repr=False, compare=False)

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

    def Rules(self) -> Dict[str, "Deckbuilding"]:
        """Every printed deck-building rule in this catalogue, keyed by `set`.

        The dataset repeats the block on both faces of an identity, because a
        consumer keys on the hero's `set` and should not have to know which
        face carries the printing. The two must agree; a catalogue where they
        do not is a broken dataset rather than an ambiguous rule, so it fails
        here rather than picking one.
        """
        if self._rules is None:
            table: Dict[str, Deckbuilding] = {}
            for card_id in sorted(self.cards):
                block = self.cards[card_id].get("deckbuilding")
                if not block:
                    continue
                hero_set = str(self.cards[card_id].get("set") or "")
                rule = Deckbuilding.FromDataset(block)
                if table.setdefault(hero_set, rule) != rule:
                    raise DeckError(
                        f"{hero_set}: two identity faces carry different "
                        f"`deckbuilding` blocks")
            self._rules = table
        return self._rules

    def Rule(self, hero_set: str) -> "Deckbuilding":
        """This hero's printed deck-building rule, or the unmodified default."""
        return self.Rules().get(hero_set, NO_EXCEPTION)

    def AspectAllowance(self, hero_set: str) -> int:
        """How many aspects this hero's deck may draw on. One, unless printed."""
        return self.Rule(hero_set).aspects


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
# an aspect you did not choose, up to N* -- so they are one `Allowance` with a
# printed description rather than five special cases, and a sixth such identity
# needs no code here at all: it arrives in the dataset.
#
# A description is always written in printed fields, never in prose. The
# bracket convention in the printed text says which: MarvelSDB renders a
# **trait** as `[[X-MEN]]` and a **resource icon** as `[energy]`, so "[[X-MEN]]
# allies" is `card_type == "ally"` with `traits == ["X-Men"]`, and "a printed
# [energy] resource icon" is `resource == "energy"`, read out of
# `stats.resource_energy`. `[[attack]]`/`[[thwart]]` are likewise traits, not
# the `(attack)` marker in an ability line -- the trait is the superset (249
# events carry both, 20 carry only the trait, none only the marker), and the
# trait is what is printed on the card.
#
# Reading those fields off a card is this module's job. Deciding that
# "[[X-MEN]] allies" means them is `tools/cards/deckbuilding.py`'s, and the
# decision is recorded in the dataset with the sentence it came from.


@dataclass(frozen=True)
class Allowance:
    """Cards matching a printed description may come from unchosen aspects.

    Built from one entry of a card's `deckbuilding.allowances`. `limit` is how
    many, `None` for no cap. Gamora's is counted in cards ("up to 6 attack
    and/or thwart events"); Maria Hill's is counted in *titles* ("the maximum
    number of copies of 3 S.H.I.E.L.D. supports" -- three cards, each at its
    full copy limit), which is what `by_title` selects.

    Cards from the aspect the deck *did* choose never consume an allowance:
    both capped lines say "from aspects other than your chosen aspect", and the
    three uncapped ones say "from any aspect", which is only a widening. The
    dataset carries that distinction as `from`; the checker does not need it,
    because it asks whether *some* choice of aspects makes the deck legal and
    a card in the chosen aspect is never outside it. It is read and kept on the
    record anyway, so a consumer that does need it is not re-deriving it from
    the sentence.
    """
    what: str
    card_type: str = ""
    traits: Tuple[str, ...] = ()
    resource: Optional[str] = None
    source: str = "any_aspect"
    limit: Optional[int] = None
    by_title: bool = False

    @staticmethod
    def FromDataset(spec: Dict[str, Any]) -> "Allowance":
        return Allowance(
            what=str(spec.get("what") or ""),
            card_type=str(spec.get("card_type") or ""),
            traits=tuple(spec.get("traits") or ()),
            resource=spec.get("resource") or None,
            source=str(spec.get("from") or "any_aspect"),
            limit=spec.get("limit"),
            by_title=spec.get("counted_by") == "titles")

    def Matches(self, card: Dict[str, Any]) -> bool:
        """Whether this card is one the printed line lets in.

        Every clause has to hold. An allowance that matched on type alone, or
        on trait alone, would pass every test that shows it *accepting* a card
        and quietly legalise hundreds of decks -- which is why each of the five
        is tested against a near miss as well.
        """
        if self.card_type and str(card.get("type") or "") != self.card_type:
            return False
        if self.traits and not set(self.traits) & set(card.get("traits") or []):
            return False
        if self.resource:
            stats = card.get("stats") or {}
            if int(stats.get(f"resource_{self.resource}") or 0) <= 0:
                return False
        return True


@dataclass(frozen=True)
class Deckbuilding:
    """One identity's printed deck-building line, as the dataset carries it."""
    card_id: str
    printed: str
    aspects: int = 1
    equal: bool = False
    allowances: Tuple[Allowance, ...] = ()
    # Adam Warlock alone: every card that is not his own is capped at 1 copy,
    # under its printed `deck_limit`.
    copy_cap: Optional[int] = None

    @staticmethod
    def FromDataset(block: Dict[str, Any]) -> "Deckbuilding":
        return Deckbuilding(
            card_id=str(block.get("source_card") or ""),
            printed=str(block.get("source_text") or ""),
            aspects=int(block.get("aspects") or 1),
            equal=bool(block.get("equal_aspects")),
            allowances=tuple(Allowance.FromDataset(spec)
                             for spec in block.get("allowances") or ()),
            copy_cap=block.get("copy_limit"))


NO_EXCEPTION = Deckbuilding("", "a deck takes cards from exactly one aspect")


class _DatasetRules(Mapping[str, Deckbuilding]):
    """`DECKBUILDING`, read from `datasets/cards/cards.json` on first use.

    A mapping rather than a dict because the dataset is 6 MB and most callers
    of this module never ask about a printed exception; a module-level `dict`
    would make importing it a file read. Read-only for the same reason the
    table it replaced is gone: the rules are printed on cards, and a caller
    that wants a different set of them wants a different `Catalogue`.
    """

    def __init__(self) -> None:
        self._table: Optional[Dict[str, Deckbuilding]] = None

    def _Table(self) -> Dict[str, Deckbuilding]:
        if self._table is None:
            self._table = Catalogue.Load().Rules()
        return self._table

    def __getitem__(self, hero_set: str) -> Deckbuilding:
        return self._Table()[hero_set]

    def __iter__(self) -> Iterator[str]:
        return iter(self._Table())

    def __len__(self) -> int:
        return len(self._Table())

    def __repr__(self) -> str:
        loaded = "unread" if self._table is None else f"{len(self._table)} rules"
        return f"<DECKBUILDING {loaded} from {CARD_DATASET}>"


# Keyed by the identity's `set`. Note Gamora's is `gam`, not `gamora`.
#
# Seven identities in this snapshot print a deck-building line and all seven
# are here, because the extract will not build a dataset in which an identity
# prints a line nobody has classified. That guard is the reason this is a read
# rather than a table: a table cannot notice an eighth hero.
DECKBUILDING: Mapping[str, Deckbuilding] = _DatasetRules()


def Rule(hero_set: str) -> Deckbuilding:
    """This hero's printed deck-building line, or the unmodified default.

    Reads the checked-in dataset. Prefer `Catalogue.Rule` where a catalogue is
    already in hand -- this exists for callers that only have a hero's `set`.
    """
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
            if allowance.Matches(catalogue.Get(card_id)):
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
    # From the catalogue in hand, not from the checked-in dataset: a deck is
    # checked against the cards it is being checked with.
    rule = catalogue.Rule(hero_set)

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
