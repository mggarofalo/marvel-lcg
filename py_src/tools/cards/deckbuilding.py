"""Printed deck-building rules, parsed once and pinned by text hash (MARVEL-88).

Downstream code used to learn deck-building rules by grepping English card
text, and that is how MARVEL-85 was filed wrong. Across the 141 identity faces
in this dataset a deliberately broad net matches **48 lines on 37 heroes**, of
which **7 are deck-building rules** and **41 are ordinary abilities that merely
touch a deck** -- "Search your deck and discard pile for Mjolnir", "shuffle 1
[[Ice]] card from your discard pile into your deck". The broad net is ~15%
precise; the narrow one (`/deck[- ]?building/`) was ~29% complete. No regex
separates *a rule about building a deck* from *an ability that touches a deck*:
that is a semantic distinction, and every future card gets to phrase it freshly.

So this module does not try to be a parser of English. It does three things:

1. **Holds the parse.** `RULES` is the seven printed lines, read by a human and
   written down as structure. This is the same work the hand-written table in
   `tools/decks/rules.py` used to do, moved next to the printed text it is
   derived from and emitted into `datasets/cards/cards.json` so that every
   consumer -- the Python checker, the deck builder, the C# engine -- reads
   fields instead of re-deriving them from prose.

2. **Pins each parse to the exact printed line, by hash.** A rule is matched by
   `(card_id, LineHash(line))`. Reword the card and the hash moves, the rule is
   no longer matched, and the extract fails. A parse can therefore never
   silently outlive the sentence it was made from.

3. **Refuses to be silent about anything else.** Every line matching
   `BROAD_NET` must be either one of the seven parses or an explicit entry in
   `REVIEWED` -- "somebody looked at this line and it is not a deck-building
   rule", also keyed by card id and line hash. A line in neither fails
   `python -m tools.cards.extract`. That is the whole point of the module: it
   converts *"I searched and found nothing"* into *"something changed and
   nobody has looked at it"*, which is the distinction that has cost this
   project three wrong populations (MARVEL-16, MARVEL-68, MARVEL-85).

The limit of the guard, stated plainly: a deck-building line that matches none
of the words in `BROAD_NET` is not reachable from here, exactly as it was not
reachable by any grep. The net is broad on purpose -- 41 false positives are a
one-time cost paid in an auditable table, and a missed rule is not.

Scope: identity faces only (`type` of `hero` or `alter_ego`). Every printed
deck-building rule in Marvel Champions is on an identity, and widening the scan
to all 3,794 cards would trade a 41-row allowlist for a four-figure one.
"""

from __future__ import annotations

import hashlib
import re
from dataclasses import dataclass, field
from typing import Any, Dict, Iterator, List, Optional, Tuple

from tools.cards.text import ToPlainText

# Deliberately broad: it matches 48 lines to catch 7. See the module docstring
# for why a tighter net is the wrong trade.
BROAD_NET = re.compile(
    r"include|deck-?building|your deck|instead of one|aspect|max \d|per deck"
    r"|cannot include|must include",
    re.IGNORECASE)

IDENTITY_TYPES = ("hero", "alter_ego")

# 64 bits of SHA-256. This is a change detector over a table of ~50 short
# strings, not a signature: the thing it must catch is a card being reworded,
# and no adversary is choosing the printed text.
HASH_LENGTH = 16

ANY_ASPECT = "any_aspect"
OTHER_ASPECTS = "other_aspects"

BY_CARDS = "cards"
BY_TITLES = "titles"

# The plural noun each `card_type` is printed as, used to check an authored
# parse against the sentence it claims to come from.
_TYPE_WORDS = {
    "ally": "allies",
    "event": "events",
    "support": "supports",
    "upgrade": "upgrades",
    "resource": "resources",
    "player_side_scheme": "player side schemes",
}


class DeckbuildingError(Exception):
    """A line nobody has classified, or a parse that has rotted."""


def LineHash(line: str) -> str:
    """The pin. A line that changes by one character gets a different hash."""
    return hashlib.sha256(line.encode("utf-8")).hexdigest()[:HASH_LENGTH]


def Lines(record: Dict[str, Any]) -> List[str]:
    """The printed lines of one card, as the net sees them.

    `back_text` is included because upstream sometimes stores a second printed
    face inline instead of as its own card. No identity uses it in this
    snapshot; scanning it costs nothing and closes the hole before it opens.
    """
    text = record.get("text_plain") or ""
    back = ToPlainText(record.get("back_text") or "")
    out: List[str] = []
    for block in (text, back):
        out.extend(line.strip() for line in block.split("\n") if line.strip())
    return out


def Matches(line: str) -> bool:
    return bool(BROAD_NET.search(line))


################################################################################
# The parse
#


@dataclass(frozen=True)
class Allowance:
    """Cards matching a description may come from aspects the deck did not pick.

    The description is written in printed fields, never in prose. The bracket
    convention says which field: MarvelSDB renders a **trait** as `[[X-MEN]]`
    and a **resource icon** as `[energy]`, so "[[X-MEN]] allies" is
    `card_type == "ally"` with `traits == ["X-Men"]`, while "a printed [energy]
    resource icon" is `resource == "energy"`. Traits are spelled as the dataset
    spells them (`X-Men`, `S.H.I.E.L.D.`), not as the card prints them.

    `limit` is how many, `None` for no cap. `counted_by` says in what unit:
    Gamora's six are counted in cards ("up to 6 attack and/or thwart events"),
    Maria Hill's three in titles ("the maximum number of copies of 3
    S.H.I.E.L.D. supports" -- three cards, each at its full copy limit).

    `source` records which half of the printed sentence this is -- "from any
    aspect" is a pure widening, "from aspects other than your chosen aspect" is
    a cap on cards you did not already have. Both are carried because they are
    printed differently; a checker that asks "is there a legal choice of
    aspects?" happens to treat them the same, and says so where it does.
    """
    what: str
    card_type: str
    traits: Tuple[str, ...] = ()
    resource: Optional[str] = None
    source: str = ANY_ASPECT
    limit: Optional[int] = None
    counted_by: str = BY_CARDS

    def ToJson(self) -> Dict[str, Any]:
        return {
            "what": self.what,
            "card_type": self.card_type,
            "traits": list(self.traits),
            "resource": self.resource,
            "from": self.source,
            "limit": self.limit,
            "counted_by": self.counted_by,
        }


@dataclass(frozen=True)
class Rule:
    """One identity's printed deck-building line, read into structure."""
    card_id: str
    source_text: str
    aspects: int = 1
    equal_aspects: bool = False
    copy_limit: Optional[int] = None
    allowances: Tuple[Allowance, ...] = ()

    @property
    def source_hash(self) -> str:
        return LineHash(self.source_text)

    def ToJson(self) -> Dict[str, Any]:
        return {
            "aspects": self.aspects,
            "equal_aspects": self.equal_aspects,
            "copy_limit": self.copy_limit,
            "allowances": [a.ToJson() for a in self.allowances],
            "source_card": self.card_id,
            "source_hash": self.source_hash,
            "source_text": self.source_text,
        }


# The seven. Every `source_text` is the printed line verbatim, including the
# ability name the card prints in front of it, because the line is what the
# hash pins -- trimming it to the sentence would let the untrimmed half change
# unnoticed.
#
# Two shapes. The first widens *how many aspects* a deck draws on (Spider-Woman
# two, Adam Warlock all four) and in both printed cases requires them to be the
# same size. The second keeps the single aspect and lets **cards matching a
# description** in from the others; five identities print that, and they differ
# only in the description and the cap.
RULES: Tuple[Rule, ...] = (
    Rule(
        card_id="04031b",
        source_text=(
            "Double Agent — Choose two aspects instead of one during "
            "deck-building. You must include an equal number of cards from "
            "those aspects in your deck."),
        aspects=2, equal_aspects=True),
    Rule(
        card_id="18001b",
        source_text=(
            "Skilled Tactician — You may include up to 6 [[attack]] and/or "
            "[[thwart]] events in your deck from aspects other than your "
            "chosen aspect."),
        allowances=(Allowance(
            what="attack and/or thwart events",
            card_type="event", traits=("Attack", "Thwart"),
            source=OTHER_ASPECTS, limit=6, counted_by=BY_CARDS),)),
    Rule(
        card_id="21031b",
        source_text=(
            "Avatar of Life - During deck-building, your deck must include an "
            "equal number of cards from all 4 aspects. You cannot include more "
            "than 1 copy of any non-Adam Warlock card."),
        aspects=4, equal_aspects=True, copy_limit=1),
    Rule(
        card_id="33001b",
        source_text=(
            "You may include [[X-MEN]] allies from any aspect in your deck."),
        allowances=(Allowance(
            what="X-Men allies",
            card_type="ally", traits=("X-Men",), source=ANY_ASPECT),)),
    Rule(
        card_id="40001b",
        source_text=(
            "You may include player side schemes from any aspect in your "
            "deck."),
        allowances=(Allowance(
            what="player side schemes",
            card_type="player_side_scheme", source=ANY_ASPECT),)),
    Rule(
        card_id="50001b",
        source_text=(
            "You may include the maximum number of copies of 3 "
            "[[S.H.I.E.L.D.]] supports in your deck from aspects other than "
            "your chosen aspect."),
        allowances=(Allowance(
            what="S.H.I.E.L.D. supports",
            card_type="support", traits=("S.H.I.E.L.D.",),
            source=OTHER_ASPECTS, limit=3, counted_by=BY_TITLES),)),
    Rule(
        card_id="58001b",
        source_text=(
            "Ionic Energy Being — You may include events with a printed "
            "[energy] resource icon from any aspect in your deck."),
        allowances=(Allowance(
            what="events with a printed energy resource icon",
            card_type="event", resource="energy", source=ANY_ASPECT),)),
)


################################################################################
# The reviewed set
#


@dataclass(frozen=True)
class Reviewed:
    """A line that matches the net and is **not** a deck-building rule.

    `quote` is the head of the line as it was reviewed. It is redundant with
    the hash and kept anyway: a table of 41 opaque digests is not something a
    human can audit, and auditing it is the only reason it exists.
    """
    card_id: str
    line_hash: str
    kind: str
    quote: str


# Why each of these matched: they all say "your deck", and none of them says
# anything about what may go in one.
#
#   search        searches the deck (and often the discard pile) for a card
#   shuffle_back  puts a card from the discard pile back into the deck
#   top_of_deck   looks at, plays or rearranges the top of the deck
#   mill          discards cards off the top of the deck
#   aspect_word   an ability that triggers on aspect cards -- caught by the
#                 `aspect` arm of the net, which exists for Spider-Woman
REVIEWED: Tuple[Reviewed, ...] = (
    Reviewed("01029b", "8c16ad758a1beb48", "top_of_deck",
             "Futurist — Action: Look at the top 3 cards of your deck. ..."),
    Reviewed("01040b", "0cc3ad9605c4fe88", "search",
             "Foresight — Setup: Search your deck for a [[Black ..."),
    Reviewed("03001b", "39ab5f8314a58608", "search",
             "Setup: Search your deck and discard pile for the Captain ..."),
    Reviewed("04001b", "879e3a3a7e7f9db8", "search",
             "Weapon of Choice — Action: Spend 1 resource of any type ..."),
    Reviewed("04031a", "bf60ee915db7c096", "aspect_word",
             "\"Superhuman Agility\" — Interrupt: When you play an ..."),
    Reviewed("05001b", "c2c4460ef6b35c32", "mill",
             "Teen Spirit — Action: Discard cards from the top of your ..."),
    Reviewed("06001b", "a0533abc6bb77157", "search",
             "Worthy — Action: Search your deck and discard pile for ..."),
    Reviewed("13001b", "92e7d308331d5ad8", "shuffle_back",
             "G.I.R.L. — Action: Shuffle up to 2 cards with a printed ..."),
    Reviewed("17001b", "a9ce58077992e7b7", "search",
             "Setup: Search your deck and discard pile for a copy of ..."),
    Reviewed("17001b", "c8a43e1ece9ede90", "top_of_deck",
             "Smooth Talker — Action: Choose a card in your hand. Swap ..."),
    Reviewed("18001b", "ee02eaa8c91e9c49", "top_of_deck",
             "Action: Look at the top card of your deck. If that card ..."),
    Reviewed("20001b", "155f08ab11acfa3a", "mill",
             "Armed and Ready - Setup: Discard cards from the top of ..."),
    Reviewed("23001b", "594dd228c22c3d99", "shuffle_back",
             "Action: Choose a War Machine card in your discard pile ..."),
    Reviewed("27001b", "7683e8ebd45c5f83", "shuffle_back",
             "Action: Choose to either shuffle Ticket to the ..."),
    Reviewed("27030b", "d7375864f6433645", "shuffle_back",
             "Response: After you change to this form, shuffle 1 ..."),
    Reviewed("28001b", "bb2e7346c9196086", "search",
             "Alter-Ego Action: Spend 1 resource of any type → search ..."),
    Reviewed("32001b", "6ed01ff71c650425", "search",
             "Setup: Search your deck for a copy of Organic Steel and ..."),
    Reviewed("32001b", "a5ba18efa0eb2a78", "shuffle_back",
             "Aspiring Artist - Response: After you change to this ..."),
    Reviewed("33001b", "712b19a75e6636b8", "search",
             "Constant Training - Action: Search your deck for a ..."),
    Reviewed("40001b", "9c8cecbafb2f5329", "search",
             "Soldier X — Setup: Search your deck and discard pile for ..."),
    Reviewed("40037a", "b62f6a9e1000e0a4", "top_of_deck",
             "Action: Choose a card in your hand. Swap that card with ..."),
    Reviewed("41001b", "3bbe43eda06831ee", "shuffle_back",
             "Action: Exhaust 1 [[PSI-ENERGY]] upgrade → shuffle 1 ..."),
    Reviewed("43001b", "2b1fc52cb5aa6b1d", "shuffle_back",
             "Action: Shuffle either the Honey Badger ally or the ..."),
    Reviewed("44001b", "b5bd859204533b05", "search",
             "Break the Fourth Wall — Action: Discard a card from your ..."),
    Reviewed("45001a", "0768635ed0b40c23", "mill",
             "Energy Absorption — Response: After Bishop takes any ..."),
    Reviewed("45030a", "3f9ae847ec5d6e4e", "top_of_deck",
             "Once per phase, you may play the top card of your deck ..."),
    Reviewed("45030a", "a5f5b67a1e2b9548", "top_of_deck",
             "Play with the top card of your deck faceup."),
    Reviewed("45030b", "1eb0d1865a8ff2c1", "top_of_deck",
             "Interrupt: When you change to hero form, choose a ..."),
    Reviewed("46001b", "d0466b2d7e913482", "shuffle_back",
             "Cool Off — Response: After you change to this form, ..."),
    Reviewed("47001b", "f5866a0042e957d1", "search",
             "Mall Rat — Action: Search your deck for the Shopping ..."),
    Reviewed("48001b", "0e14a221444eb573", "search",
             "Action: Search your deck for a copy of Bamf! and add it ..."),
    Reviewed("49001a", "c18b67529e69aa13", "mill",
             "Magnetic Pull — Action: Discard cards from the top of ..."),
    Reviewed("49001b", "4ea9da5b49661ded", "shuffle_back",
             "Survivor — Response: After you change to this form, ..."),
    Reviewed("50001b", "96b0678ea7773f88", "search",
             "Action: Exhaust Maria Hill → search your deck for a ..."),
    Reviewed("51001b", "f4846dfa437adcc6", "search",
             "Inventor — Action: Exhaust Shuri → search your deck for ..."),
    Reviewed("53001b", "1cd435744f4697f1", "search",
             "Birds of a Feather — Action: Discard 1 card from your ..."),
    Reviewed("54001b", "c911367069aab66e", "search",
             "Cybernetically Enhanced — Action: Spend 1 resource of ..."),
    Reviewed("56001b", "f7635fcbf397665f", "search",
             "Undercover Work — Action: Search your deck and discard ..."),
    Reviewed("56029b", "1206a214cfa2c627", "search",
             "Shape-Changer — Action: Search your deck and discard ..."),
    Reviewed("60037a", "7f782ebf661d6451", "aspect_word",
             "Watch and Learn — Response: After a player plays an ..."),
    Reviewed("60037b", "fbd12833bfee5a1e", "aspect_word",
             "Practice Makes Perfect — Interrupt: When you change to ..."),
)


################################################################################
# Checking an authored parse against the sentence it came from
#


def _ParseComplaints(rule: Rule) -> List[str]:
    """Ways a hand-authored parse disagrees with its own printed line.

    A typo in `RULES` -- a trait spelled the way the card prints it rather than
    the way the dataset does, a cap of 8 read off a line that says 6 -- would
    otherwise be invisible: the parse is the only description of the rule there
    is, so nothing else contradicts it. These are the checks that are cheap and
    exact, and they are worth the twenty lines because the alternative is that
    a wrong number reaches a deck checker and rejects legal decks.
    """
    complaints: List[str] = []
    lowered = rule.source_text.lower()

    for allowance in rule.allowances:
        word = _TYPE_WORDS.get(allowance.card_type)
        if word is None:
            complaints.append(
                f"card_type {allowance.card_type!r} has no printed plural in "
                f"_TYPE_WORDS")
        elif word not in lowered:
            complaints.append(
                f"the line does not print {word!r}, but the parse says "
                f"card_type {allowance.card_type!r}")
        for trait in allowance.traits:
            if f"[[{trait.lower()}]]" not in lowered:
                complaints.append(
                    f"the line does not print the trait [[{trait}]]")
        if allowance.resource and f"[{allowance.resource}]" not in lowered:
            complaints.append(
                f"the line does not print the resource icon "
                f"[{allowance.resource}]")
        if allowance.limit is not None and str(allowance.limit) not in lowered:
            complaints.append(
                f"the line does not print the number {allowance.limit}, but "
                f"the parse caps {allowance.what!r} there")
        if allowance.counted_by not in (BY_CARDS, BY_TITLES):
            complaints.append(f"unknown counted_by {allowance.counted_by!r}")
        if allowance.source not in (ANY_ASPECT, OTHER_ASPECTS):
            complaints.append(f"unknown from {allowance.source!r}")

    if rule.copy_limit is not None and str(rule.copy_limit) not in lowered:
        complaints.append(
            f"the line does not print the number {rule.copy_limit}, but the "
            f"parse caps copies there")
    if rule.aspects < 1:
        complaints.append(f"aspects is {rule.aspects}")
    if not Matches(rule.source_text):
        complaints.append(
            "the line does not match BROAD_NET, so the guard would never "
            "reach this parse")
    return complaints


def SelfCheck() -> List[str]:
    """Everything wrong with the tables themselves, before any card is read."""
    problems: List[str] = []
    seen: Dict[Tuple[str, str], str] = {}
    for rule in RULES:
        for complaint in _ParseComplaints(rule):
            problems.append(f"{rule.card_id}: {complaint}")
        key = (rule.card_id, rule.source_hash)
        if key in seen:
            problems.append(f"{rule.card_id}: two rules pin the same line")
        seen[key] = "rule"
    for row in REVIEWED:
        if len(row.line_hash) != HASH_LENGTH:
            problems.append(f"{row.card_id}: {row.line_hash!r} is not a hash")
        key = (row.card_id, row.line_hash)
        if key in seen:
            problems.append(
                f"{row.card_id}: {row.line_hash} is both {seen[key]} and "
                f"reviewed-as-not-a-rule")
        seen[key] = "reviewed"
    return problems


################################################################################
# The scan
#


@dataclass
class ScanResult:
    # card_id -> the block to emit, for every identity face of a hero whose
    # identity prints a rule.
    blocks: Dict[str, Dict[str, Any]] = field(default_factory=dict)
    # (card_id, hash) of every line the net matched, and how it was classified.
    classified: Dict[Tuple[str, str], str] = field(default_factory=dict)
    matched_lines: int = 0
    problems: List[str] = field(default_factory=list)


def _Snippet(card_id: str, line: str) -> str:
    """A paste-ready row, so the fix for a failure is mechanical."""
    quote = line if len(line) <= 57 else line[:57].rsplit(" ", 1)[0] + " ..."
    return (f'    Reviewed("{card_id}", "{LineHash(line)}", "<kind>",\n'
            f'             "{quote.replace(chr(34), chr(92) + chr(34))}"),')


def Scan(records: List[Dict[str, Any]]) -> ScanResult:
    """Classify every identity line the net matches. Nothing raises here.

    Three ways this reports a problem, and all three are the same problem seen
    from a different side -- a printed sentence that no human has read:

      unreviewed  the net matched a line that is in neither table
      missing     a parse in `RULES` pins a line no card prints any more
      stale       a `REVIEWED` row pins a line no card prints any more
    """
    result = ScanResult(problems=list(SelfCheck()))

    by_rule = {(r.card_id, r.source_hash): r for r in RULES}
    by_review = {(r.card_id, r.line_hash): r for r in REVIEWED}
    # The identity `set` a rule was found on -- `spider_woman`, `gam` -- which
    # is what a consumer keys on.
    found: Dict[str, Rule] = {}
    seen: set[Tuple[str, str]] = set()

    identities = [r for r in records if r.get("type") in IDENTITY_TYPES]
    names = {r["card_id"]: r.get("name") or r["card_id"] for r in identities}

    for record in sorted(identities, key=lambda r: r["card_id"]):
        card_id = record["card_id"]
        for line in Lines(record):
            if not Matches(line):
                continue
            result.matched_lines += 1
            key = (card_id, LineHash(line))
            seen.add(key)
            if key in by_rule:
                rule = by_rule[key]
                hero_set = record.get("set") or ""
                if found.get(hero_set, rule) is not rule:
                    result.problems.append(
                        f"{hero_set}: two identity faces print different "
                        f"deck-building rules; a consumer keying on `set` "
                        f"cannot tell which applies")
                found[hero_set] = rule
                result.classified[key] = "rule"
            elif key in by_review:
                result.classified[key] = by_review[key].kind
            else:
                result.classified[key] = "unreviewed"
                result.problems.append(
                    f"{card_id} ({names[card_id]}) prints a line matching "
                    f"BROAD_NET that nobody has classified:\n"
                    f"      {line}\n"
                    f"    Decide what it is. If it is a deck-building "
                    f"rule, add a Rule to tools/cards/deckbuilding.py. If it "
                    f"is not, add:\n"
                    f"{_Snippet(card_id, line)}")

    for key, rule in sorted(by_rule.items()):
        if key not in seen:
            result.problems.append(
                f"{rule.card_id}: the parsed deck-building line is no longer "
                f"printed on the card -- it was reworded, or the card is gone. "
                f"Re-read it and update the Rule:\n      {rule.source_text}")

    for key, row in sorted(by_review.items()):
        if key not in seen:
            result.problems.append(
                f"{row.card_id}: reviewed line {row.line_hash} is no longer "
                f"printed ({row.quote!r}). Re-read the card and update or drop "
                f"the Reviewed row.")

    if result.problems:
        return result

    # A rule is printed on one face and governs the whole identity, so it is
    # emitted on every face of that identity's `set`. Consumers key on `set`
    # (`tools/decks/rules.py` reads the hero's set out of the deck file) and
    # would otherwise have to know which face carries the printing.
    for record in identities:
        rule = found.get(record.get("set") or "")
        if rule is not None:
            result.blocks[record["card_id"]] = rule.ToJson()
    return result


def Apply(records: List[Dict[str, Any]]) -> ScanResult:
    """Write the block onto every record, or refuse to build the dataset.

    Raising rather than warning is the requirement, not a preference. A warning
    printed during a build that already prints twenty lines is a warning nobody
    reads, and the failure it describes -- a new hero's deck-building rule
    quietly absent -- is one that shows up much later as legal decks being
    called illegal.
    """
    result = Scan(records)
    if result.problems:
        listing = "\n\n  ".join(result.problems)
        raise DeckbuildingError(
            f"{len(result.problems)} deck-building line(s) need a human:\n\n"
            f"  {listing}\n\n"
            f"See tools/cards/deckbuilding.py. Nothing was written.")
    for record in records:
        record["deckbuilding"] = result.blocks.get(record["card_id"])
    return result


def SummaryOf(result: ScanResult) -> Dict[str, Any]:
    """The counts that make the reviewed set auditable from `summary.json`."""
    kinds: Dict[str, int] = {}
    for kind in result.classified.values():
        kinds[kind] = kinds.get(kind, 0) + 1
    return {
        "rule": (
            "Every identity line matching BROAD_NET "
            f"(`{BROAD_NET.pattern}`) is either parsed into a deck-building "
            "rule or listed as reviewed-and-not-one, each pinned to a hash of "
            "the printed line. A line in neither fails the extract -- see "
            "tools/cards/deckbuilding.py."),
        "identity_faces_with_a_rule": len(result.blocks),
        "matched_lines": result.matched_lines,
        "parsed_rules": len(RULES),
        "reviewed_lines": len(REVIEWED),
        "by_classification": dict(sorted(kinds.items())),
        "heroes_with_a_rule": sorted(
            {block["source_card"] for block in result.blocks.values()}),
    }


def Iterate(records: List[Dict[str, Any]]) -> Iterator[Tuple[str, str]]:
    """Every (card_id, line) the net matches. Used by the tests."""
    for record in records:
        if record.get("type") not in IDENTITY_TYPES:
            continue
        for line in Lines(record):
            if Matches(line):
                yield record["card_id"], line
