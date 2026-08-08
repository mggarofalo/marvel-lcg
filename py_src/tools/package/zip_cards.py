"""Zip the card scripts into `cards-<version>.zip`.

Moved verbatim from `unit_test/test_task.py::test_zip_cards` (MARVEL-55). The
behaviour is deliberately unchanged so the move stays reviewable as a move; two
defects it carries were measured and filed rather than fixed here:

- **MARVEL-56** -- `CARD_FOLDERS` is a hand-maintained list of 69 directories,
  but 396 directories under `cards/pack/` contain card scripts. The other 327
  (every `aoa/` subfolder, every `*_nemesis` folder) are silently omitted.
- **MARVEL-57** -- `date_time` is assigned after `write()` has already emitted
  the local file header, so the uniform timestamp reaches only the central
  directory and the archive is not byte-reproducible.

Arcnames are flat: `os.path.dirname()` of a trailing-slash path is the folder
itself, so every file lands at the zip root. Harmless today -- card filenames
are ids, and all 921 are distinct -- but see MARVEL-56 before widening the set.

Run from `py_src/`:

    python -m tools.package.zip_cards
"""

import argparse
import os
import time
import zipfile
from pathlib import Path

# The pack directories that go into the archive. Hand-maintained and known
# incomplete -- see MARVEL-56.
CARD_FOLDERS = [
    './cards/pack/angel/',
    './cards/pack/angel/angel/',
    './cards/pack/ant/',
    './cards/pack/ant/ant_man/',
    './cards/pack/bkw/',
    './cards/pack/cap/',
    './cards/pack/core/',
    './cards/pack/cw/',
    './cards/pack/cw/hulkling/',
    './cards/pack/cw/tigra/',
    './cards/pack/cyclops/',
    './cards/pack/cyclops/cyclops/',
    './cards/pack/deadpool/',
    './cards/pack/deadpool/deadpool/',
    './cards/pack/drax/',
    './cards/pack/drax/drax/',
    './cards/pack/drs/',
    './cards/pack/falcon/',
    './cards/pack/gam/',
    './cards/pack/gambit/gambit/',
    './cards/pack/hlk/',
    './cards/pack/hlk/hulk/',
    './cards/pack/iceman/',
    './cards/pack/iceman/frostbite/',
    './cards/pack/ironheart/',
    './cards/pack/jubilee/',
    './cards/pack/jubilee/jubilee/',
    './cards/pack/msm/',
    './cards/pack/msm/ms_marvel/',
    './cards/pack/mts/',
    './cards/pack/mut_gen/',
    './cards/pack/ncrawler/',
    './cards/pack/ncrawler/nightcrawler/',
    './cards/pack/nebu/',
    './cards/pack/nebu/nebula/',
    './cards/pack/next_evol/',
    './cards/pack/nova/',
    './cards/pack/nova/nova/',
    './cards/pack/phoenix/',
    './cards/pack/phoenix/phoenix/',
    './cards/pack/psylocke/',
    './cards/pack/qsv/',
    './cards/pack/qsv/quicksilver/',
    './cards/pack/rogue/',
    './cards/pack/scw/',
    './cards/pack/scw/scarlet_witch/',
    './cards/pack/silk/',
    './cards/pack/sm/',
    './cards/pack/spdr/',
    './cards/pack/spiderham/',
    './cards/pack/stld/',
    './cards/pack/stld/star_lord/',
    './cards/pack/storm/',
    './cards/pack/thor/',
    './cards/pack/thor/thor/',
    './cards/pack/trors/',
    './cards/pack/valk/',
    './cards/pack/vision/',
    './cards/pack/vision/vision/',
    './cards/pack/vnm/',
    './cards/pack/vnm/venom/',
    './cards/pack/warm/',
    './cards/pack/warm/war_machine/',
    './cards/pack/winter/',
    './cards/pack/wolv/',
    './cards/pack/wsp/',
    './cards/pack/wsp/wasp/',
    './cards/pack/x23/',
    './cards/pack/x23/x_23/',
]

# Package scaffolding and campaign definitions are not card scripts.
EXCLUDED_NAMES = {'__init__.py', 'campaign.py'}

UNIFORM_TIMESTAMP = time.mktime(time.strptime("2022-01-01 00:00:00", "%Y-%m-%d %H:%M:%S"))


def ZipCards(output: Path, folders: list[str] = CARD_FOLDERS) -> int:
    """Write the card scripts in `folders` to `output`. Returns the file count."""
    written = 0

    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for folder in folders:
            for file in sorted(os.listdir(folder)):
                file_path = os.path.join(folder, file)
                if not os.path.isfile(file_path) or file in EXCLUDED_NAMES:
                    continue

                arcname = os.path.relpath(file_path, os.path.dirname(folder))
                zipf.write(file_path, arcname)

                # Reaches the central directory only -- see MARVEL-57.
                zip_info = zipf.getinfo(arcname)
                zip_info.date_time = time.localtime(UNIFORM_TIMESTAMP)[:6]
                written += 1

    return written


def DefaultOutput() -> Path:
    from build import Build
    return Path(f"./cards-{Build.MAJOR}.{Build.MINOR}.{Build.PATCH}.{Build.BUILD}.zip")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, default=None,
                        help="output archive (default ./cards-<version>.zip)")
    args = parser.parse_args(argv)

    output = args.out if args.out is not None else DefaultOutput()
    written = ZipCards(output)

    print(f"zipped {written} card scripts into {output}")
    print(f"  from {len(CARD_FOLDERS)} pack folders (known incomplete -- see MARVEL-56)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
