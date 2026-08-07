"""Naming a card in a spec, and finding it in a live world.

A `CardRef` is written the way an author thinks about a card:

    "01094"                     card id
    "Rhino"                     printed name
    "Swinging Web Kick #2"      the second copy, in object-id order
    "Rhino in VillainArea"      qualified by zone
    "01005 in hand"             zone aliases are accepted

Resolution walks `world.object_manager.card_dict`, so it finds cards **on the
field**. `RunPuzzle.FindOrCreateFace` does not -- its `FindCardOnField` call is
commented out (`game/puzzle/puzzle.py:43`), so a bare `Puzzle.Damage("01094", 3)`
against the villain in play silently creates a *second* Rhino in the aside deck
and damages that one instead. The harness resolves refs here and hands
`RunPuzzle` an already-resolved `CardFace`, which takes that path out of play
without changing engine code.

Matching is deliberately narrower than `CardFace.IsName`: card id or printed
name, over every printed face of the card, case-insensitively. "Considered as"
aliases are not honoured, because a spec should mean the card it names.

An ambiguous ref is an error that lists every candidate. It is never a silent
first match -- a spec that says "Rhino" when two Rhinos are in play is a spec
that has not decided what it is testing.
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


def MatchesKey(card: Any, key: str) -> bool:
    """Card id or printed name, over every printed face, case-insensitively."""
    wanted = key.strip().lower()
    for face in card.printed_faces:
        if face.paper.card_id.lower() == wanted:
            return True
        if str(face.name).lower() == wanted:
            return True
        if str(face.printed_name).lower() == wanted:
            return True
    return False


################################################################################
#

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


def ResolveCard(world: Any, ref: "CardRef|str") -> Any:
    """Exactly one card, or a `CardRefError` naming every candidate."""
    if isinstance(ref, str):
        ref = CardRef.Parse(ref)

    found = FindCards(world, ref)

    if ref.ordinal:
        if len(found) < ref.ordinal:
            raise CardRefError(
                f"{ref.Describe()}: wanted copy #{ref.ordinal} but found "
                f"{len(found)} card(s) matching {ref.key!r}"
                + (f" in {ref.zone}" if ref.zone else ""))
        return found[ref.ordinal - 1]

    if not found:
        near = NearMisses(world, ref)
        hint = f"; did you mean {', '.join(near)}?" if near else ""
        where = f" in {ref.zone}" if ref.zone else ""
        raise CardRefError(f"{ref.Describe()}: no card matches {ref.key!r}{where}{hint}")

    if len(found) > 1:
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
        for face in card.printed_faces:
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
