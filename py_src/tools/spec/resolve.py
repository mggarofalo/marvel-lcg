"""Naming a card in a spec, and finding it in a live world.

A `CardRef` is written the way an author thinks about a card:

    "01094"                     card id
    "Rhino"                     printed name
    "Swinging Web Kick #2"      the second copy the scenario created
    "Rhino in VillainArea"      qualified by zone
    "01005 in hand"             zone aliases are accepted

Resolution walks `world.object_manager.card_dict`, so it sees every zone and
every card. `RunPuzzle.FindFaceByName` now walks it too and is zone-complete as
well, ordered board first (MARVEL-51 -- before that it never looked at the board
at all, and `Puzzle.Damage("01094", 3)` against the villain in play created a
*second* Rhino in the aside deck and damaged that one -- then MARVEL-61 for the
zones that fix did not reach).

Neither this nor `FindFaceByName` is a **Search** in the rules sense; that term
belongs to `game.operate.search.Search`, which is what card text compiles to.
Both of these resolve a name someone wrote to the card they meant, outside the
game and without touching it.

The harness still resolves refs here and hands `RunPuzzle` an already-resolved
`CardFace`, because a spec asks for more than a name lookup: zone qualifiers,
`#N` over creation order, named subjects like "me" and "the main scheme",
matching that ignores "considered as" aliases, and failure messages that name
candidates and near misses. None of that belongs in the engine's puzzle
resolver, and a spec must never be answered by a card the engine invented.

Matching is deliberately narrower than `CardFace.IsName`: card id or name, over
every face the card answers to, case-insensitively. "Considered as" aliases are
not honoured, because a spec should mean the card it names.

The faces a card answers to are its **printed** faces plus **the face it is
presenting right now**, which are the same thing except where the engine has put
one card's face on another card's object -- see `NameableFaces` and MARVEL-102.

An ambiguous ref is an error that lists every candidate. It is never a silent
first match -- a spec that says "Rhino" when two Rhinos are in play is a spec
that has not decided what it is testing.

## What `#N` counts (MARVEL-42)

`#N` indexes the matching copies **in the order the scenario created them**.
Concretely that is ascending object id, because `CardFactory` allocates ids in
call order and `ApplyGiven` calls it in the order the Given lists cards. The
*guarantee* is creation order; object id is only how it is implemented.

The distinction matters because `docs/migration.md` lists object-id allocation
order as part of the cross-engine contract -- something the two engines must be
made to agree on, not something a spec may quietly lean on. A spec that means
"the second card I put in my hand" is portable. A spec that means "whichever
card the allocator happened to reach second" is not.

Two consequences:

- **Position within a zone is not used, deliberately.** It looks like the more
  physical choice, but it is perturbed by any shuffle: measured on a four-card
  player deck, `Deck2.Shuffle` moved two copies from positions [0, 2] to [2, 1]
  while their object ids stayed [5, 7]. Shuffles are RNG-driven, and the two
  engines do not share an RNG yet (MARVEL-38), so a zone-position ordinal would
  name a different card in C# than in Python.

- **An ordinal is only allowed over cards the scenario created.** The two cards
  named "Rhino" in a bare Rhino setup are the stage-1 villain in play and the
  stage-2 card in the villain deck; both were allocated by the engine and
  nothing in the scenario decides which is `#1`. Those refs must name a zone.
  Cards a Given created are ordered by the Given that created them, which the
  author wrote and can see. `MarkEngineBaseline` draws the line, and it is
  provenance rather than zone: two copies stay `#1` and `#2` after one of them
  moves into play, which is what makes "put both minions into play" writable.

- **An ordinal over a current-face name is legal and means the same thing.**
  `"Drone Minion #2"` is the second card *the scenario created* that is
  presenting a drone face right now -- not the second card to become one. The
  ordinal never appeals to the order faces were swapped, so nothing about
  MARVEL-102 changes what `#N` promises: it is still creation order, which is
  still what the author wrote and can see. A refusal was considered and
  rejected, because the rule is well defined here and a two-drone board is
  exactly the case the issue was raised for.

  What *is* new is that the set an ordinal indexes can grow while the scene
  runs, because a card starts answering to "Drone Minion" partway through. A
  printed name's match set is fixed once the `Given` block has run; a
  current-face name's is not. So an ordinal over one is a claim about the board
  at that beat, and `"Drone Minion #1"` and `"Aunt May #1"` may well be
  different cards. Where a scenario has the underlying identity to hand, naming
  it is the more stable spelling.
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from typing import Any, Dict, List, Sequence, Tuple


class CardRefError(Exception):
    """A ref that names no card, or more than one."""


class AmbiguousCardRef(CardRefError):
    """A ref that names more than one card.

    Kept distinct from "names nothing" because callers react differently: a ref
    that finds nothing may legitimately mean "put this card into the game", but
    a ref that finds several is always the author failing to say which.
    """


# Friendly names for the `DeckType` members an author is likely to write.
ZONE_ALIASES: Dict[str, str] = {
    "hand": "HandsArea",
    "deck": "PlayerDeck",
    "discard": "DiscardPile",
    "discard pile": "DiscardPile",
    "encounter deck": "EncounterDeck",
    "encounter discard": "EncounterDiscardPile",
    "villain area": "VillainArea",
    "main scheme": "MainSchemesArea",
    "side scheme": "SideSchemesArea",
    "hero area": "HeroArea",
    "allies": "AlliesArea",
    "supports": "SupportsArea",
    "upgrades": "UpgradesArea",
    "engaged": "EngagedEnemiesArea",
    "removed": "RemovedArea",
    "victory display": "VictoryDisplay",
    "set aside": "AsideDeck",
}

# "in play" is a predicate over deck flags, not a single zone.
ZONE_IN_PLAY = "play"

REF_PATTERN = re.compile(
    r"""^\s*
        (?P<key>.+?)
        (?:\s*\#(?P<ordinal>\d+))?
        (?:\s+in\s+(?P<zone>[A-Za-z][A-Za-z _]*))?
        \s*$""",
    re.VERBOSE,
)


@dataclass(frozen=True)
class CardRef:
    text: str
    key: str
    zone: str = ""
    ordinal: int = 0

    @staticmethod
    def Parse(text: str) -> "CardRef":
        match = REF_PATTERN.match(text)
        if not match:
            raise CardRefError(f"cannot parse card reference {text!r}")
        key = match.group("key").strip().strip('"')
        if not key:
            raise CardRefError(f"card reference {text!r} names nothing")
        ordinal = int(match.group("ordinal") or 0)
        zone = (match.group("zone") or "").strip()
        return CardRef(text=text, key=key, zone=zone, ordinal=ordinal)

    def Describe(self) -> str:
        return self.text.strip()


################################################################################
#

def NormaliseZone(zone: str) -> str:
    """A written zone to a `DeckType` member name, or the in-play predicate."""
    lowered = zone.strip().lower().replace("_", " ")
    collapsed = " ".join(lowered.split())
    if collapsed in (ZONE_IN_PLAY, "in play"):
        return ZONE_IN_PLAY
    if collapsed in ZONE_ALIASES:
        return ZONE_ALIASES[collapsed]
    # Fall back to the `DeckType` member name itself, spelled any which way.
    return collapsed.replace(" ", "")


def ZoneName(card: Any) -> str:
    """The zone a card is in, as a `DeckType` member name."""
    return card.area.deck_type.name


def MatchesZone(card: Any, zone: str) -> bool:
    wanted = NormaliseZone(zone)
    if wanted == ZONE_IN_PLAY:
        return bool(card.IsOnField())
    return ZoneName(card).lower() == wanted.lower()


def NameableFaces(card: Any) -> List[Any]:
    """Every face a spec may name this card by.

    Its printed faces, plus **the face it is presenting right now** when that is
    not one of them. The two are the same card for every ordinary card, and
    differ exactly when the engine has put one card's face on another card's
    object -- `Card.SetAsCard` without `remove_legacy` replaces `card.face` and
    leaves `printed_faces` alone, so both identities are live at once.

    The case that forced it is the facedown DRONE (MARVEL-102).
    `Enemies.PutYouDeckTopCardAsFacedownMinion` takes the top card of a player's
    deck and calls `face.card.SetAsCard(ultron_facedown_drone)`: the card keeps
    its printed identity ("Aunt May") while presenting a minion the game
    displays as "Drone Minion". Indexing only the printed faces made that name
    unnameable -- while the validator's own failure message for the minion
    activation prompt *listed* "Drone Minion" among the legal targets, because
    `Label` reads `card.face`. The harness printed a name it would not accept,
    which is the same shape as MARVEL-94.

    Both names stay live, deliberately. "Aunt May" is what the scenario put on
    top of the deck and "Drone Minion" is what is now engaged with the hero, and
    a scenario has reason to say either.
    """
    faces = list(card.printed_faces)
    current = getattr(card, "face", None)
    if current is not None and not any(current is face for face in faces):
        faces.append(current)
    return faces


def MatchesKey(card: Any, key: str) -> bool:
    """Card id or name, over every face the card answers to, case-insensitively."""
    wanted = key.strip().lower()
    for face in NameableFaces(card):
        if face.paper.card_id.lower() == wanted:
            return True
        if str(face.name).lower() == wanted:
            return True
        if str(face.printed_name).lower() == wanted:
            return True
    return False


################################################################################
#

################################################################################
# Named subjects
#
# A spec should be able to say "I" and "the main scheme" without knowing which
# card either one is. Card ids are printed identifiers and fine in a spec;
# *object* ids never are, and neither is making an author look up the main
# scheme's stage-B id to write a sentence about threat.

SELF_NAMES = ("me", "i", "my identity", "my hero", "the hero", "my character")
MAIN_SCHEME_NAMES = ("the main scheme", "main scheme")


def IsSelf(key: str) -> bool:
    return key.strip().lower() in SELF_NAMES


def IsMainScheme(key: str) -> bool:
    return key.strip().lower() in MAIN_SCHEME_NAMES


def ResolveNamed(world: Any, key: str) -> Any:
    """The card a named subject refers to, or None if it is not a named one."""
    if IsSelf(key):
        player = world.GetFirstPlayer()
        return player.GetIdentity().card
    if IsMainScheme(key):
        from game.operate.worlds import Worlds
        schemes = Worlds.GetAllMainSchemes(world)
        if not schemes:
            raise CardRefError("there is no main scheme in play")
        return schemes[0].card
    return None


################################################################################
# Option labels
#
# The engine's option names are identifiers built from the label string in the
# Python card script -- `Deal_4_damage_to_an_enemy` -- not from printed text.
# A spec reads English and the runner normalises, so a scenario never asserts
# an engine identifier and the C# engine is free to spell its own the same way.

def NormaliseLabel(text: str) -> str:
    return " ".join(str(text).replace("_", " ").split()).casefold()


def AllCards(world: Any) -> List[Any]:
    """Every card in the world, in object-id order so results are stable."""
    card_dict = world.object_manager.card_dict
    return [card_dict[oid] for oid in sorted(card_dict) if oid != 0]


def FindCards(world: Any, ref: "CardRef|str") -> List[Any]:
    """Every card matching `ref`, in object-id order. May be empty."""
    if isinstance(ref, str):
        ref = CardRef.Parse(ref)
    found = [card for card in AllCards(world) if MatchesKey(card, ref.key)]
    if ref.zone:
        found = [card for card in found if MatchesZone(card, ref.zone)]
    return found


BASELINE_ATTR = "spec_engine_allocated_ids"


def MarkEngineBaseline(world: Any) -> None:
    """Record which cards the engine made, before the scenario makes any.

    Called once between `GameSetup` and `ApplyGiven`. Everything allocated after
    this point exists because the scenario asked for it, which is what makes an
    ordinal over those cards mean something the scenario can see.
    """
    setattr(world, BASELINE_ATTR, frozenset(world.object_manager.card_dict))


def IsEngineAllocated(world: Any, card: Any) -> bool:
    """Was this card put there by scenario setup rather than by a Given?

    Absent a baseline -- a world built by something other than the harness --
    nothing is treated as engine-allocated, so ordinals keep working for callers
    that never set one.
    """
    return card.object_id in getattr(world, BASELINE_ATTR, frozenset())


def RejectEngineOrdinal(world: Any, found: Sequence[Any], ref: "CardRef") -> None:
    """Refuse an ordinal over cards the scenario did not create (MARVEL-42).

    `#N` promises "the Nth copy the scenario created". Over cards the engine
    allocated during setup there is no such order to appeal to: the two cards
    named "Rhino" in a Rhino setup are the stage-1 villain and the stage-2 card
    in the villain deck, and nothing in the scenario decides which is first.
    Those refs have to name a zone instead.

    Only applies once the ordinal actually has to choose. `"Rhino #1 in
    VillainArea"` has already narrowed to one card, so the ordinal is redundant
    rather than unsafe and is left alone.
    """
    if len(found) <= 1:
        return

    engine_made = [card for card in found if IsEngineAllocated(world, card)]
    if not engine_made:
        return

    zones = sorted({ZoneName(card) for card in engine_made})
    candidates = ", ".join(Label(card) for card in found)
    raise AmbiguousCardRef(
        f"{ref.Describe()}: #{ref.ordinal} would index cards the scenario did "
        f"not create, so which one it names is the engine's allocation order "
        f"rather than anything the scenario says. Name the zone instead — "
        f"\"{ref.key} in {zones[0]}\". Candidates: {candidates}")


def ResolveCard(world: Any, ref: "CardRef|str") -> Any:
    """Exactly one card, or a `CardRefError` naming every candidate."""
    if isinstance(ref, str):
        ref = CardRef.Parse(ref)

    named = ResolveNamed(world, ref.key)
    if named is not None:
        return named

    found = FindCards(world, ref)

    if ref.ordinal:
        if len(found) < ref.ordinal:
            raise CardRefError(
                f"{ref.Describe()}: wanted copy #{ref.ordinal} but found "
                f"{len(found)} card(s) matching {ref.key!r}"
                + (f" in {ref.zone}" if ref.zone else ""))
        RejectEngineOrdinal(world, found, ref)
        return found[ref.ordinal - 1]

    if not found:
        near = NearMisses(world, ref)
        hint = f"; did you mean {', '.join(near)}?" if near else ""
        where = f" in {ref.zone}" if ref.zone else ""
        raise CardRefError(f"{ref.Describe()}: no card matches {ref.key!r}{where}{hint}")

    if len(found) > 1:
        # A card name that matches several cards, only one of which is on the
        # board, means the one on the board. "Rhino" in a scenario about the
        # fight is the Rhino in play, not the stage-2 card still in the villain
        # deck. Two Rhinos actually in play is a real ambiguity and still errors.
        in_play = [card for card in found if card.IsOnField()]
        if len(in_play) == 1:
            return in_play[0]

        candidates = ", ".join(Label(card) for card in found)
        raise AmbiguousCardRef(
            f"{ref.Describe()}: matches {len(found)} cards ({candidates}). "
            f"Add a zone (\"{ref.key} in HandsArea\") or an ordinal "
            f"(\"{ref.key} #1\") to say which one.")

    return found[0]


def ResolveFace(world: Any, ref: "CardRef|str") -> Any:
    """The current face of the one card `ref` names."""
    return ResolveCard(world, ref).face


def ResolveAll(world: Any, refs: Sequence[str]) -> List[Any]:
    return [ResolveCard(world, ref) for ref in refs]


################################################################################
#

def Label(card: Any) -> str:
    """A card as a spec author would recognise it: name, id and zone."""
    face = card.face
    return f"{face.name} ({face.paper.card_id}) in {ZoneName(card)}"


def NearMisses(world: Any, ref: "CardRef", limit: int = 3) -> List[str]:
    """Cards whose name contains the ref's key -- enough to catch a typo."""
    wanted = ref.key.strip().lower()
    if len(wanted) < 3:
        return []
    seen: List[str] = []
    for card in AllCards(world):
        for face in NameableFaces(card):
            name = str(face.name)
            if wanted in name.lower() and name not in seen:
                seen.append(f"{name!r} ({face.paper.card_id})")
                break
        if len(seen) >= limit:
            break
    return seen


def CardIndex(world: Any) -> Dict[int, Any]:
    """object_id -> card, for turning a `BotOption.bind_id` back into a card."""
    return {oid: card for oid, card in world.object_manager.card_dict.items() if oid != 0}


def DescribeOption(world: Any, option: Any) -> str:
    """One selectable effect, phrased for a failure message."""
    index = CardIndex(world)
    card = index.get(option.bind_id)
    where = f" on {Label(card)}" if card is not None else ""
    reason = f" [unavailable: {option.failure_reason}]" if option.failure_reason else ""
    return f"{option.name}{where}{reason}"


def SplitRefs(text: str) -> Tuple[str, ...]:
    """A comma-separated, optionally quoted list of refs."""
    parts = [part.strip().strip('"').strip() for part in text.split(",")]
    return tuple(part for part in parts if part)
