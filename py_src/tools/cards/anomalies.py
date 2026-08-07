"""Everything about the sources that a spec author should know before starting.

An anomaly here is not a bug to fix now. It is a place where the data does not
say what you would assume it says -- a card whose engine text has quietly lost a
sentence, a script with nothing printed to write a spec against, a card the
engine has never heard of. Writing specs without this list means writing some
number of confident, wrong ones.

Each record is `{kind, id, detail}`, sorted, so the file diffs cleanly and a
regression shows up as an added line.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, List

# Ordered by how much they should worry a spec author, most first.
KINDS = (
    "engine_text_diverges",
    "engine_text_missing",
    "engine_text_corrupt",
    "engine_markup_escaped",
    "card_not_in_marvelsdb",
    "card_not_implemented",
    "script_without_text",
    "no_text_anywhere",
    "unclaimed_script",
    "engine_pack_without_expansion",
    "engine_duplicate_card_id",
    "engine_dangling_link",
    "upstream_text_key_typo",
    "upstream_duplicate_code",
    "upstream_dangling_duplicate",
)

DESCRIPTIONS: Dict[str, str] = {
    "engine_text_diverges":
        "The engine's card text says something different from the printed text, "
        "after formatting is normalised away. Author the spec from the printed "
        "text; the difference is either an engine bug or a stale transcription.",
    "engine_text_missing":
        "The card has printed text but the engine stores none for it.",
    "engine_text_corrupt":
        "The engine's copy contains U+FFFD -- a character lost in an encoding "
        "round-trip. Never author from this text.",
    "engine_markup_escaped":
        "The engine's copy contains escaped-slash markup such as `<\\/b>`, which "
        "no renderer treats as a closing tag.",
    "card_not_in_marvelsdb":
        "The engine has this card but MarvelSDB does not, so there is no "
        "authoritative text to check it against. Engine internals (status "
        "tokens, rules inserts) and the fan-made challenge cards land here.",
    "card_not_implemented":
        "MarvelSDB has this card and no engine script implements it.",
    "script_without_text":
        "A script implements this card but neither source has printed text for "
        "it, so there is nothing to write a spec against.",
    "no_text_anywhere":
        "Neither source has printed text and no script implements it.",
    "unclaimed_script":
        "A file under `cards/pack/` that no card resolves to. Campaign and setup "
        "modules are expected here; anything else is dead code or a broken path.",
    "engine_pack_without_expansion":
        "A pack in `data/cards.json` that `data/sets_info.json` does not list, so "
        "its cards have no expansion label from the engine's own data.",
    "engine_duplicate_card_id":
        "A `card_id` that appears more than once in `data/cards.json`. "
        "`CardsDB.Initialize` keeps the first and silently drops the rest.",
    "engine_dangling_link":
        "A `full_link` or `ability_link` naming a card that does not exist.",
    "upstream_text_key_typo":
        "The MarvelSDB entry spells its printed text `scheme text` instead of "
        "`text`. Read as the card's text here; worth fixing upstream.",
    "upstream_duplicate_code":
        "A `code` appearing in more than one MarvelSDB pack file.",
    "upstream_dangling_duplicate":
        "A MarvelSDB reprint whose `duplicate_of` names a card that is not in "
        "the snapshot.",
}


@dataclass(frozen=True, order=True)
class Anomaly:
    kind: str
    id: str
    detail: str = ""


class Collector:
    def __init__(self) -> None:
        self._found: List[Anomaly] = []

    def Add(self, kind: str, id: str, detail: str = "") -> None:
        if kind not in DESCRIPTIONS:
            raise KeyError(f"undocumented anomaly kind {kind!r}")
        self._found.append(Anomaly(kind, id, detail))

    def Counts(self) -> Dict[str, int]:
        counts = {kind: 0 for kind in KINDS}
        for anomaly in self._found:
            counts[anomaly.kind] += 1
        return counts

    def Grouped(self) -> List[Dict[str, object]]:
        """Anomalies by kind, in KINDS order, each group sorted by id."""
        by_kind: Dict[str, List[Anomaly]] = {kind: [] for kind in KINDS}
        for anomaly in self._found:
            by_kind[anomaly.kind].append(anomaly)
        groups = []
        for kind in KINDS:
            found = sorted(by_kind[kind])
            groups.append({
                "kind": kind,
                "description": DESCRIPTIONS[kind],
                "count": len(found),
                "cards": [
                    {"id": a.id, "detail": a.detail} if a.detail else {"id": a.id}
                    for a in found
                ],
            })
        return groups
