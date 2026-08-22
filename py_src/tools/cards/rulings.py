"""Read the vendored MarvelCDB FAQ snapshot in `datasets/marvelcdb-faq/`.

    python -m tools.cards.rulings 01001a     # what has been ruled about a card
    python -m tools.cards.rulings --summary  # how many cards carry a ruling
    python -m tools.cards.rulings --unmapped # site codes this repo cannot place

Run from `py_src/`. Stdlib only, offline, and it never touches the network --
`tools/cards/harvest_faq.py` is the only module that does, and nothing imports
that one. See AGENTS.md.

## What a ruling is for

An official ruling is the only independent check on a spec authored from printed
card text (MARVEL-143). Without one, an author reads ambiguous words, writes a
scenario asserting their reading, validates it against the Python engine -- which
implements the same reading -- and the scenario passes into `specs/trusted.json`
having confirmed nothing but that the engine agrees with itself.

`01001a` is the worked example. Spider-Man's printed text says his Interrupt
fires "when a villain initiates an attack"; `01135` Ultron's says "when Ultron
attacks". The printed words suggest two different moments. The ruling says there
is no timing difference and the Forced Interrupt takes priority. Nothing in this
repository except the FAQ can tell an author that.

## Missing is a normal state, not an error

The snapshot is vendored by a manual harvest, so a fresh clone that has not run
one does not have it. Everything here degrades to empty rather than raising:
`tools/spec/coverage.py` calls into it on every run and must not start failing
because a dataset nobody promised is absent. `Loaded()` is how a caller tells
"no rulings" from "no snapshot" when it matters.

## Two codes for one card

MarvelCDB serves a double-sided card under one unsuffixed code where
`marvelsdb-json-data` -- and therefore `datasets/cards/` -- splits it into
printed faces: site `01097` is `01097a` and `01097b` here. Measured 2026-08-22,
76 of the site's codes are shaped that way, almost all of them main schemes.

So a ruling is fanned out to every face rather than dropped or arbitrarily
assigned to the front. A ruling is about the card; which face carries the
sentence being asked about is a printing detail, and an author reading `01097b`
needs the ruling as much as one reading `01097a`.

Codes that resolve to neither a card nor a set of faces are kept and counted
rather than discarded -- `--unmapped` lists them. That population should be
empty; if it is not, the snapshot and the card dataset have drifted apart, which
is worth seeing rather than silently dropping.
"""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, Iterable, List, Sequence, Set

SNAPSHOT_DIR = Path("../datasets/marvelcdb-faq")
CARD_DATASET = Path("../datasets/cards/cards.json")

FAQ_FILE = "faq.json"

# The suffixes `marvelsdb-json-data` uses for the printed faces of one card.
# `c` is rare and real -- `01144` is a three-part main scheme.
FACE_SUFFIXES = ("a", "b", "c")


def Percent(part: int, whole: int) -> str:
    return f"{100.0 * part / whole:.1f}%" if whole else "n/a"


@dataclass
class Ruling:
    """One card's FAQ entry, as MarvelCDB serves it."""
    code: str
    # Markdown. The readable form, and what an author should be shown.
    text: str = ""
    # The same content as HTML. Kept because dropping it would make the
    # snapshot a transcription rather than a mirror.
    #
    # **Do not render this.** It is unescaped markup written by contributors to
    # a community site, arriving here over the network and checked in verbatim.
    # Nothing reads it today: `--print` and every caller use `text`. A consumer
    # that wants to display a ruling should render the markdown, and one that
    # genuinely needs the HTML has to sanitise it first -- the web client
    # already renders `data/cards.json` straight into the page, and this must
    # not become a second, untrusted path into that.
    html: str = ""
    # Upstream's shape: {"date": ..., "timezone_type": ..., "timezone": ...}.
    updated: Any = None


@dataclass
class RulingsData:
    # Keyed by MarvelCDB's code, which is not always a code this repo has.
    rulings: Dict[str, Ruling] = field(default_factory=dict)
    # Every code the harvest asked about. A code here but not in `rulings` has
    # no ruling; a code in neither was never asked.
    queried: Set[str] = field(default_factory=set)
    harvested: str = ""
    harvester: str = ""
    # Codes MarvelCDB returned more than once. The first is kept and the rest
    # recorded, exactly as `marvelsdb.Load` treats a repeated card code -- a
    # dict assignment would drop one silently, and a ruling that vanished
    # because upstream submitted it twice is not a thing to find out later.
    duplicate_codes: List[str] = field(default_factory=list)
    # False when the snapshot is absent, which is not an error. Distinguishes
    # "no rulings exist" from "nobody has harvested".
    present: bool = False

    def Loaded(self) -> bool:
        return self.present

    def Asked(self, code: str) -> bool:
        return code in self.queried


def Absent() -> RulingsData:
    """What a repository without a harvested snapshot looks like."""
    return RulingsData()


def Load(snapshot: Path = SNAPSHOT_DIR) -> RulingsData:
    """Read the snapshot. Absent is fine; malformed is not.

    A missing directory is a clone that has not harvested. A file that is there
    but unreadable is a corrupted vendored dataset, and quietly treating it as
    empty would report every card as having no ruling.
    """
    path = snapshot / FAQ_FILE
    if not path.exists():
        return Absent()

    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict) or "entries" not in payload:
        raise ValueError(
            f"{path} is not a FAQ snapshot -- no `entries`. Regenerate it with "
            f"`python -m tools.cards.harvest_faq`.")

    data = RulingsData(
        queried=set(payload.get("queried") or ()),
        harvested=str(payload.get("harvested") or ""),
        harvester=str(payload.get("harvester") or ""),
        present=True,
    )
    for entry in payload["entries"]:
        code = entry.get("code")
        if not code:
            raise ValueError(f"{path} holds an entry with no `code`")
        if code in data.rulings:
            # Upstream really does this: `05005` is submitted twice, five
            # seconds apart, with the same text and different `updated` stamps.
            # The snapshot mirrors it rather than curating it, so the reader is
            # where the repeat gets resolved -- and reported.
            data.duplicate_codes.append(code)
            continue
        data.rulings[code] = Ruling(
            code=code,
            text=entry.get("text", "") or "",
            html=entry.get("html", "") or "",
            updated=entry.get("updated"),
        )
    # An entry nobody asked about cannot have come from this harvest.
    #
    # Unconditional on purpose. Guarding this with `if data.queried` would make
    # the check evaporate in exactly the case it exists for: a snapshot with
    # rulings and an empty or missing `queried` is the most provenance-free
    # file this loader can be handed, and it would have been the one shape that
    # sailed through. An empty snapshot -- no entries, no queried -- still
    # passes, because there is nothing there to be unaccounted for.
    unasked = sorted(set(data.rulings) - data.queried)
    if unasked:
        raise ValueError(
            f"{path} holds {len(unasked)} ruling(s) for code(s) absent from "
            f"`queried`, so the file was not written by one harvest: "
            f"{', '.join(unasked[:5])}. `queried` records what the harvest "
            f"asked about; without it a ruling has no provenance. Re-harvest "
            f"with `python -m tools.cards.harvest_faq`.")
    return data


def CardIds(dataset: Path = CARD_DATASET) -> Set[str]:
    """Every `card_id` in the generated card dataset."""
    payload = json.loads(dataset.read_text(encoding="utf-8"))
    return {card["card_id"] for card in payload["cards"]}


def Faces(code: str, card_ids: Set[str]) -> List[str]:
    """The dataset card ids a MarvelCDB code refers to.

    Direct hit first: most codes are the same on both sides. Otherwise the code
    is a double-sided card the site serves whole, and it maps to whichever
    printed faces the dataset has. Empty means neither, which is what
    `--unmapped` reports.
    """
    if code in card_ids:
        return [code]
    faces = [code + suffix for suffix in FACE_SUFFIXES if code + suffix in card_ids]
    return faces


def ByCard(data: RulingsData, card_ids: Set[str]) -> Dict[str, List[Ruling]]:
    """Rulings keyed by dataset card id rather than by MarvelCDB code.

    A list rather than one ruling: two site codes can both fan out onto the same
    face only if the data is malformed, but a caller should not have to trust
    that to read the result.
    """
    out: Dict[str, List[Ruling]] = {}
    for code in sorted(data.rulings):
        for card_id in Faces(code, card_ids):
            out.setdefault(card_id, []).append(data.rulings[code])
    return out


def Unmapped(data: RulingsData, card_ids: Set[str]) -> List[str]:
    """Site codes carrying a ruling that this repository cannot place."""
    return [code for code in sorted(data.rulings) if not Faces(code, card_ids)]


def WasAsked(data: RulingsData, card_id: str) -> bool:
    """Whether the harvest asked about this card, under either code shape.

    A face id the site does not serve -- `01097b` -- was asked about under the
    whole-card code `01097`, so checking only the id would report a card as
    unharvested when it was covered.
    """
    if data.Asked(card_id):
        return True
    return card_id[-1:] in FACE_SUFFIXES and data.Asked(card_id[:-1])


def _PrintCard(data: RulingsData, by_card: Dict[str, List[Ruling]],
               card_id: str) -> None:
    found = by_card.get(card_id) or []
    if not found:
        state = "no ruling" if WasAsked(data, card_id) else (
            "not covered by the harvest")
        print(f"{card_id}: {state}")
        return
    for ruling in found:
        header = f"{card_id} (marvelcdb {ruling.code})"
        print(header)
        print("-" * len(header))
        print(ruling.text.strip() or "(entry has no markdown text)")
        print()


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__.split("\n\n")[0],
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("card_id", nargs="*", help="card ids to show rulings for")
    parser.add_argument("--summary", action="store_true",
                        help="how much of the corpus carries a ruling")
    parser.add_argument("--unmapped", action="store_true",
                        help="site codes with a ruling that no card id matches")
    args = parser.parse_args(argv)

    data = Load()
    if not data.Loaded():
        print(f"no snapshot at {SNAPSHOT_DIR / FAQ_FILE}. Harvest one with\n"
              f"    python -m tools.cards.harvest_faq", file=sys.stderr)
        return 1

    card_ids = CardIds()
    by_card = ByCard(data, card_ids)

    if args.summary or not args.card_id:
        unmapped = Unmapped(data, card_ids)
        print(f"harvested   {data.harvested} with {data.harvester}")
        print(f"asked about {len(data.queried)} marvelcdb code(s)")
        print(f"rulings     {len(data.rulings)} "
              f"({Percent(len(data.rulings), len(data.queried))} of codes asked)")
        print(f"cards       {len(by_card)} of {len(card_ids)} in the dataset "
              f"({Percent(len(by_card), len(card_ids))})")
        if data.duplicate_codes:
            print(f"duplicates  {len(data.duplicate_codes)} code(s) served more "
                  f"than once upstream, first kept: "
                  f"{', '.join(sorted(set(data.duplicate_codes)))}")
        if unmapped:
            print(f"unmapped    {len(unmapped)} ruling(s) match no card id -- "
                  f"the snapshot and the card dataset have drifted")

    if args.unmapped:
        for code in Unmapped(data, card_ids):
            print(code)

    for card_id in args.card_id:
        _PrintCard(data, by_card, card_id)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
