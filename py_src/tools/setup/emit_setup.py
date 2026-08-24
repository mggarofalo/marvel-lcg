"""Emit the scenario, hero and encounter-set data as a cross-language dataset.

Writes `datasets/setup/setup.json`, the fourth thing a C# engine needs before it
can deal a board and the only one that was still trapped inside `py_src/`. The
other three are already datasets: the RNG stream (`datasets/rng/`), the digest
format (`datasets/digest/`) and the cards themselves (`datasets/cards/`).

**This is a projection, not a translation.** Every record is produced by loading
the file through the same dataclass the engine loads it through --
`CampaignDescriptor`, `HeroDescriptor`, `EncounterSetDescriptor` -- so a key the
engine ignores is a key this file does not contain. `deck/starter/spider_man.json`
carries `set_aside` and `metadata`, `HeroDescriptor` declares neither, and
`Json.ConvertDictToDataclass` drops both; so does this. A port that read the raw
files would implement fields the oracle does not have.

The one field dropped on purpose is `version`. It stamps the file format the
Python engine wrote, `UpdateVersion` is `pass` on all three descriptors, and
carrying it would make the dataset churn on a bump that changes no setup.

Names, not paths. The engine resolves a bare name against an ordered list of
folders (`engine/file/manager.py:FindJsonPath`), so the name is the identifier
and the folder is an implementation detail. `RESOLUTION` below mirrors that
order, and a name found twice is reported rather than silently taking one --
shadowing that nobody has looked at is how two engines end up dealing different
boards from the same request.

Three of the folders the engine searches are not read: `.` and `./data/`,
which hold no file of any of these kinds, and `./deck/`, which is gitignored
and so differs per checkout. They are listed with their reasons in `EXCLUDED`,
and the omission is pinned by a test that reads the search order out of
`FindJsonPath` rather than trusting this paragraph.

Run:
    python -m tools.setup.emit_setup
    python -m tools.setup.emit_setup --check   # non-zero if the file is stale
"""

from __future__ import annotations

import argparse
import dataclasses
import json
import os
import sys
from typing import Any, Dict, List, Sequence, Tuple

from tools import fixtures

OUTPUT = os.path.join("..", "datasets", "setup", "setup.json")

SETUP_VERSION = 1

# The field every descriptor carries and no port should: see the module docstring.
DROPPED = ("version",)

# (group, [folders in the engine's resolution order]). A sub-sequence of what
# `FindJsonPath` actually walks for the matching load type, in the same order,
# minus `EXCLUDED` below. `unit_test.test_setup_dataset` reads the real order
# out of the engine and holds this against it, so a folder added to
# `engine/file/manager.py` fails a test rather than silently going uncovered.
RESOLUTION: Tuple[Tuple[str, Tuple[str, ...]], ...] = (
    ("campaigns", ("data/scenarios", "data/challenges", "data/scenarios_custom")),
    ("heroes", ("deck/starter",)),
    ("encounter_sets", ("data/encounter_sets", "data/nemesis")),
)

# The engine load type each group is resolved as, so the test can ask
# `FindJsonPath` itself rather than re-deriving the list from the constants.
LOAD_TYPE: Dict[str, str] = {
    "campaigns": "Campaign",
    "heroes": "Hero",
    "encounter_sets": "EncounterSet",
}

# The folders the engine searches that this tool does not read, and why. Two of
# them hold no file of the kind being resolved, so reading them would put junk
# in the dataset rather than leave a gap in it. The third could hold one, and is
# excluded anyway:
#
#   `.`      the working directory, which `FindJsonPath` prepends to every
#            list and searches *first*. In `py_src/` it holds `launch.json`
#            and nothing else, so reading it would emit a `launch` campaign, a
#            `launch` hero and a `launch` encounter set out of one editor
#            config. Because it is searched first rather than last, a real
#            collision here would win in the engine and never appear in
#            `shadowed` -- so it is pinned by a test rather than by a comment.
#   `data`   holds `cards.json`, `cards_custom.json`, `sets_info.json` and
#            `translate_cn.json`. None is a scenario or an encounter set.
#   `deck`   gitignored, so it holds whatever decks the developer built this
#            morning. A byte-compared fixture cannot read a folder whose
#            contents differ per checkout.
EXCLUDED: Dict[str, Tuple[str, ...]] = {
    "campaigns": (".", "data"),
    "heroes": (".", "deck"),
    "encounter_sets": (".", "data"),
}


def _Descriptor(group: str) -> Any:
    """The dataclass the engine loads this group's files through."""
    from game.scene.replay.campaign import CampaignDescriptor
    from game.scene.replay.encounter_set import EncounterSetDescriptor
    from game.scene.replay.hero import HeroDescriptor

    return {
        "campaigns": CampaignDescriptor,
        "heroes": HeroDescriptor,
        "encounter_sets": EncounterSetDescriptor,
    }[group]


def _Project(record: Any) -> Dict[str, Any]:
    """A descriptor instance as plain data, minus the dropped fields."""
    projected = dataclasses.asdict(record)
    for key in DROPPED:
        projected.pop(key, None)
    return projected


def _Load(path: str, group: str) -> Dict[str, Any]:
    from engine.lib import Json

    with open(path, encoding="utf-8") as handle:
        return _Project(Json.LoadsAs(handle.read(), _Descriptor(group)))


def _Posix(folder: str, name: str) -> str:
    """`folder/name.json`, always forward-slashed whatever the host uses.

    This string is written into a fixture compared byte for byte, so it must
    not carry the host's path separator -- the same reason `.gitattributes`
    pins `eol=lf` (MARVEL-67, MARVEL-73). Nothing is shadowed today, so the
    difference would have sat latent until the first collision landed and the
    Windows and Linux legs disagreed about a file neither of them changed.
    """
    return f"{folder.replace(os.sep, '/')}/{name}.json"


def _Names(folder: str) -> List[str]:
    """The `.json` files in `folder`, by name, in code-point order.

    Sorted rather than `os.listdir` order: the fixture is compared byte for
    byte, and directory order is a property of the filesystem.
    """
    if not os.path.isdir(folder):
        return []
    return sorted(
        name[: -len(".json")]
        for name in os.listdir(folder)
        if name.endswith(".json")
    )


def BuildGroup(group: str, folders: Sequence[str]) -> Tuple[Dict[str, Any], List[str]]:
    """`(name -> record, shadowed)`, resolving in the engine's folder order.

    The first folder to hold a name wins, which is what `FindJsonPath` does.
    Every later hit is reported: it is a name whose meaning depends on a search
    order, and the point of this dataset is that a port does not have to know
    the search order.
    """
    records: Dict[str, Any] = {}
    shadowed: List[str] = []
    for folder in folders:
        for name in _Names(folder):
            if name in records:
                shadowed.append(_Posix(folder, name))
                continue
            records[name] = _Load(os.path.join(folder, f"{name}.json"), group)
    return records, shadowed


def Build() -> Dict[str, Any]:
    document: Dict[str, Any] = {
        "contract": "docs/setup-dataset.md",
        "generated_by": "python -m tools.setup.emit_setup",
        "setup_version": SETUP_VERSION,
        "note": ("Projected through the engine's own descriptor dataclasses, so "
                 "a key the engine ignores is absent here. Names resolve in the "
                 "order `engine/file/manager.py:FindJsonPath` searches."),
        "resolution": {group: list(folders) for group, folders in RESOLUTION},
    }

    counts: Dict[str, int] = {}
    shadowed: Dict[str, List[str]] = {}
    for group, folders in RESOLUTION:
        records, hidden = BuildGroup(group, folders)
        document[group] = records
        counts[group] = len(records)
        if hidden:
            shadowed[group] = hidden

    document["counts"] = counts
    document["shadowed"] = shadowed
    return document


def Render(document: Dict[str, Any]) -> str:
    return json.dumps(document, indent=2, sort_keys=True, ensure_ascii=True) + "\n"


def _main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true",
                        help="exit non-zero if the checked-in file is stale")
    args = parser.parse_args(list(argv))

    document = Build()
    rendered = Render(document)

    if args.check:
        verdict = fixtures.Compare(rendered, OUTPUT)
        if verdict != fixtures.FRESH:
            print(fixtures.Explain(verdict, OUTPUT,
                                   "python -m tools.setup.emit_setup"),
                  file=sys.stderr)
            return 1
        print(f"{OUTPUT} is up to date")
        return 0

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(rendered)
    counts = document["counts"]
    print(f"wrote {OUTPUT} ("
          + ", ".join(f"{counts[group]} {group}" for group, _ in RESOLUTION)
          + ")")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
