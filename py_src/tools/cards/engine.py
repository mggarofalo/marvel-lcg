"""Read `data/cards.json` and `data/sets_info.json` the way the engine does.

`cards/paper.py` and `cards/database.py` cannot be imported outside a full
engine bootstrap, so the load rules are mirrored here. Each mirror names the
engine code it copies; if that code changes, this file has to change with it.

Only the base `data/cards.json` is read. `CardsDB.Initialize` also merges the
`cards_json_custom_file(s)` config values, but those default to empty and hold
user-authored cards -- which are exactly the cards a spec-authoring dataset
must not contain.
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, Iterator, List, Tuple

CARDS_JSON = Path("data/cards.json")
SETS_INFO_JSON = Path("data/sets_info.json")

# `data/*.json` carry a top-level "checksum" string alongside the real payload.
# `Json.LoadInternal` verifies it; here it is just a key that is not a pack.
CHECKSUM_KEY = "checksum"


def CleanName(name: str) -> str:
    """Mirror of `FileManager.CleanName` (`engine/file/manager.py:152`).

    Turns a set name into the directory name its card scripts live under.
    `unit_test/test_card_dataset.py` asserts this agrees with the real one on
    every set name in `data/cards.json`.
    """
    return (name.lstrip('*').strip().lower()
            .replace(" - ", "_").replace(" & ", "_").replace(' ', '_')
            .replace("'", "").replace("-", "_").replace("!", "").replace("?", "")
            .replace("(", "").replace(")", "").replace(".", "").replace(",", "")
            .replace("\"", "").replace("&", "_").replace("#", "").replace("/", ""))


@dataclass
class SourceCard:
    """One entry from `data/cards.json`, loaded the way `Paper.Load` loads it."""

    card_id: str
    pack: str
    type: str
    name: str
    unique: bool
    subtitle: str
    attributes: Dict[str, str]
    traits: List[str]
    set_name: str
    text: str
    # Set when this entry was a `full_link` or `ability_link` stub rather than a
    # card of its own. `("full", "02019")` reads as "this is 02019, reprinted".
    link_kind: str | None = None
    link_target: str | None = None


@dataclass
class SourceData:
    cards: Dict[str, SourceCard] = field(default_factory=dict)
    # card_id -> the card whose script implements it, per `FindAbilities`.
    ability_link: Dict[str, str] = field(default_factory=dict)
    full_link: Dict[str, str] = field(default_factory=dict)
    # pack -> expansion label from `data/sets_info.json`.
    expansions: Dict[str, str] = field(default_factory=dict)
    packs: List[str] = field(default_factory=list)
    # Entries `CardsDB.Initialize` silently drops because the id was already
    # taken. Kept so the anomaly pass can report them.
    duplicate_ids: List[Tuple[str, str]] = field(default_factory=list)
    # `full_link` / `ability_link` values naming a card that does not exist.
    dangling_links: List[Tuple[str, str, str]] = field(default_factory=list)
    checksums: Dict[str, str] = field(default_factory=dict)

    def Ordered(self) -> List[SourceCard]:
        return [self.cards[cid] for cid in sorted(self.cards)]


def _IterPacks(raw: Dict[str, Any]) -> Iterator[Tuple[str, List[Dict[str, Any]]]]:
    for pack, entries in raw.items():
        if pack == CHECKSUM_KEY or not isinstance(entries, list):
            continue
        yield pack, entries


def _LoadPaper(entry: Dict[str, Any], pack: str) -> SourceCard:
    """Mirror of `Paper.Load` (`cards/paper.py:37`).

    Two rules that are easy to miss: a leading `* ` on the name marks the card
    unique and is not part of the name, and `Challenge` cards carry only text --
    they have no subtitle, attributes, traits or set.
    """
    raw_name = str(entry["name"])
    card = SourceCard(
        card_id=entry["card_id"],
        pack=pack,
        type=entry["type"],
        name=raw_name.lstrip("* "),
        unique=raw_name.startswith("* "),
        subtitle="",
        attributes={},
        traits=[],
        set_name="",
        text="",
    )
    if card.type == "Challenge":
        card.text = entry["text"]
    else:
        card.subtitle = entry["subtitle"]
        card.attributes = dict(entry["desc"])
        card.traits = list(entry["traits"])
        card.set_name = entry["set_name"]
        card.text = entry.get("text", "")
    return card


def _LoadExpansions(raw: Dict[str, Any]) -> Dict[str, str]:
    """pack code -> the expansion label it is filed under in `sets_info.json`."""
    expansions: Dict[str, str] = {}
    for label, info in raw.items():
        if label == CHECKSUM_KEY or not isinstance(info, dict):
            continue
        pack = info.get("name")
        if pack and pack not in expansions:
            expansions[pack] = label
    return expansions


def Sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def Load(root: Path = Path(".")) -> SourceData:
    """Build the card table `CardsDB.Initialize` (`cards/database.py:49`) builds."""
    cards_path = root / CARDS_JSON
    sets_path = root / SETS_INFO_JSON

    raw = json.loads(cards_path.read_text(encoding="utf-8"))
    sets_raw = json.loads(sets_path.read_text(encoding="utf-8"))

    data = SourceData(
        expansions=_LoadExpansions(sets_raw),
        checksums={
            CARDS_JSON.as_posix(): Sha256(cards_path),
            SETS_INFO_JSON.as_posix(): Sha256(sets_path),
        },
    )

    # One pass in source order, like the engine. A `full_link` entry copies a
    # card that has already been read; the engine would raise on a forward
    # reference, so one is reported rather than resolved.
    for pack, entries in _IterPacks(raw):
        data.packs.append(pack)
        for entry in entries:
            card_id = entry["card_id"]

            if "full_link" in entry:
                target = entry["full_link"]
                source = data.cards.get(target)
                if source is None:
                    data.dangling_links.append((card_id, "full", target))
                    continue
                if card_id in data.cards:
                    data.duplicate_ids.append((card_id, pack))
                    continue
                data.cards[card_id] = SourceCard(
                    card_id=card_id,
                    # A reprint inherits its source's pack and set, which is
                    # what sends `FindAbilities` to the source's script.
                    pack=source.pack,
                    type=source.type,
                    name=source.name,
                    unique=source.unique,
                    subtitle=source.subtitle,
                    attributes=dict(source.attributes),
                    traits=list(source.traits),
                    set_name=source.set_name,
                    text=source.text,
                    link_kind="full",
                    link_target=target,
                )
                data.full_link[card_id] = target
                continue

            if card_id in data.cards:
                data.duplicate_ids.append((card_id, pack))
                continue
            data.cards[card_id] = _LoadPaper(entry, pack)
            if "ability_link" in entry:
                data.ability_link[card_id] = entry["ability_link"]

    for card_id, target in sorted(data.ability_link.items()):
        card = data.cards[card_id]
        if target in data.cards:
            card.link_kind = "ability"
            card.link_target = target
        else:
            data.dangling_links.append((card_id, "ability", target))

    for card_id, _, _ in data.dangling_links:
        data.ability_link.pop(card_id, None)

    return data
