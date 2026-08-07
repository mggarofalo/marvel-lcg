"""Read the vendored MarvelSDB snapshot in `datasets/marvelsdb/`.

MarvelSDB is the authoritative source of printed card text. Its `code` field is
the same identifier the Python engine calls `card_id`, so the two join directly.

Two upstream shapes need resolving before the data is usable:

- **Reprints.** A card printed again in a later pack appears as a stub carrying
  only `code`, `pack_code`, `position`, `quantity` and `duplicate_of`. Its text
  lives on the card it duplicates. This is the same relationship the engine
  spells `full_link`.
- **Codes without names.** `text`/`name` are absent on those stubs; after
  resolving `duplicate_of` they are absent only on genuinely blank entries.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List

SNAPSHOT_DIR = Path("../datasets/marvelsdb")

# Upstream keys that describe the card rather than its printed numbers.
# Everything not listed here lands in `MarvelCard.stats` verbatim, so a stat
# field added upstream flows through without a code change.
_IDENTITY_KEYS = frozenset({
    "code", "name", "subname", "is_unique", "type_code", "faction_code",
    "traits", "text", "flavor", "illustrator", "pack_code", "set_code",
    "card_set_code", "position", "set_position", "quantity", "deck_limit",
    "duplicate_of", "back_link", "octgn_id", "hidden", "double_sided",
    "back_name", "back_text", "errata", "spoiler",
    "deck_options", "deck_requirements",
    # Presentation only -- upstream stores card-frame colours and art offsets
    # here. Nothing in a spec depends on them.
    "meta",
    # An upstream typo: card 28022 spells its printed text `scheme text`.
    # Handled in `_Read` and reported as an anomaly rather than silently
    # dropping a card's only text.
    "scheme text",
})

_TEXT_TYPO_KEY = "scheme text"

_METADATA_FILES = (
    "packs.json", "sets.json", "types.json", "factions.json",
    "subtypes.json", "settypes.json", "packtypes.json",
)


@dataclass
class MarvelCard:
    code: str
    pack_code: str
    position: int
    quantity: int
    name: str = ""
    subname: str = ""
    type_code: str = ""
    faction_code: str = ""
    # Upstream stores traits as one string, "Hero for Hire." or
    # "Avenger. Genius.". Split here so consumers never re-parse it.
    traits: List[str] = field(default_factory=list)
    text: str = ""
    flavor: str = ""
    illustrator: str = ""
    set_code: str = ""
    set_position: int | None = None
    deck_limit: int | None = None
    is_unique: bool = False
    hidden: bool = False
    errata: str = ""
    spoiler: bool = False
    # The second printed face, when upstream stores it inline rather than as a
    # separate card reached through `back_link`.
    back_name: str = ""
    back_text: str = ""
    back_link: str = ""
    duplicate_of: str = ""
    stats: Dict[str, Any] = field(default_factory=dict)
    # True when this entry was a reprint stub and its printed content was
    # copied from the card named by `duplicate_of`.
    reprint: bool = False


@dataclass
class MarvelData:
    cards: Dict[str, MarvelCard] = field(default_factory=dict)
    pack_names: Dict[str, str] = field(default_factory=dict)
    set_names: Dict[str, str] = field(default_factory=dict)
    type_names: Dict[str, str] = field(default_factory=dict)
    faction_names: Dict[str, str] = field(default_factory=dict)
    # Codes appearing more than once across the pack files.
    duplicate_codes: List[str] = field(default_factory=list)
    # Reprint stubs whose `duplicate_of` names a card that is not in the data.
    dangling_duplicates: List[tuple[str, str]] = field(default_factory=list)
    # Cards whose printed text arrived under the upstream `scheme text` typo.
    text_key_typos: List[str] = field(default_factory=list)
    commit: str = ""


def SplitTraits(raw: str) -> List[str]:
    """`"Avenger. Genius."` -> `["Avenger", "Genius"]`."""
    return [t.strip() for t in raw.split(".") if t.strip()]


def _Read(entry: Dict[str, Any], typos: List[str]) -> MarvelCard:
    text = entry.get("text", "")
    if not text and _TEXT_TYPO_KEY in entry:
        text = entry[_TEXT_TYPO_KEY]
        typos.append(entry["code"])

    return MarvelCard(
        code=entry["code"],
        pack_code=entry["pack_code"],
        position=entry["position"],
        quantity=entry["quantity"],
        name=entry.get("name", ""),
        subname=entry.get("subname", ""),
        type_code=entry.get("type_code", ""),
        faction_code=entry.get("faction_code", ""),
        traits=SplitTraits(entry.get("traits", "")),
        text=text,
        flavor=entry.get("flavor", ""),
        illustrator=entry.get("illustrator", ""),
        set_code=entry.get("set_code", ""),
        set_position=entry.get("set_position"),
        deck_limit=entry.get("deck_limit"),
        is_unique=bool(entry.get("is_unique", False)),
        hidden=bool(entry.get("hidden", False)),
        errata=entry.get("errata", ""),
        spoiler=bool(entry.get("spoiler", False)),
        back_name=entry.get("back_name", ""),
        back_text=entry.get("back_text", ""),
        back_link=entry.get("back_link", ""),
        duplicate_of=entry.get("duplicate_of", ""),
        stats={k: v for k, v in sorted(entry.items()) if k not in _IDENTITY_KEYS},
    )


def _NameMap(path: Path) -> Dict[str, str]:
    rows = json.loads(path.read_text(encoding="utf-8"))
    return {row["code"]: row["name"] for row in rows}


def Load(snapshot: Path) -> MarvelData:
    data = MarvelData()

    for name in _METADATA_FILES:
        path = snapshot / name
        if not path.exists():
            raise FileNotFoundError(
                f"{path} is missing -- the vendored snapshot is incomplete. "
                f"See {snapshot / 'UPSTREAM.md'}."
            )

    data.pack_names = _NameMap(snapshot / "packs.json")
    data.set_names = _NameMap(snapshot / "sets.json")
    data.type_names = _NameMap(snapshot / "types.json")
    data.faction_names = _NameMap(snapshot / "factions.json")

    for path in sorted((snapshot / "pack").glob("*.json")):
        for entry in json.loads(path.read_text(encoding="utf-8")):
            card = _Read(entry, data.text_key_typos)
            if card.code in data.cards:
                data.duplicate_codes.append(card.code)
                continue
            data.cards[card.code] = card

    # Reprints borrow everything printed from the card they duplicate. Pack,
    # position and quantity stay their own -- that is what makes them a reprint
    # rather than the same card.
    for code in sorted(data.cards):
        card = data.cards[code]
        if not card.duplicate_of:
            continue
        source = data.cards.get(card.duplicate_of)
        if source is None:
            data.dangling_duplicates.append((code, card.duplicate_of))
            continue
        card.reprint = True
        card.name = card.name or source.name
        card.subname = card.subname or source.subname
        card.type_code = card.type_code or source.type_code
        card.faction_code = card.faction_code or source.faction_code
        card.traits = card.traits or list(source.traits)
        card.text = card.text or source.text
        card.flavor = card.flavor or source.flavor
        card.set_code = card.set_code or source.set_code
        card.deck_limit = card.deck_limit if card.deck_limit is not None else source.deck_limit
        card.is_unique = card.is_unique or source.is_unique
        card.errata = card.errata or source.errata
        card.back_name = card.back_name or source.back_name
        card.back_text = card.back_text or source.back_text
        card.back_link = card.back_link or source.back_link
        card.stats = card.stats or dict(source.stats)

    return data


def ReadPinnedCommit(snapshot: Path) -> str:
    """Pull the pinned upstream SHA out of `UPSTREAM.md`.

    The SHA belongs in the dataset header, so a consumer holding only
    `datasets/cards/cards.json` can still say which card text it was built from.
    """
    upstream = snapshot / "UPSTREAM.md"
    for line in upstream.read_text(encoding="utf-8").splitlines():
        if line.startswith("| Commit "):
            return line.split("`")[1]
    raise ValueError(f"no `| Commit | \\`<sha>\\` |` row in {upstream}")
