"""Zip the card scripts into `cards-<version>.zip`.

Moved from `unit_test/test_task.py::test_zip_cards` in MARVEL-55, which kept
the behaviour unchanged so the move stayed reviewable as a move, and filed the
two defects it carried. Both are fixed here.

**MARVEL-56 -- the folder list.** `CARD_FOLDERS` was a hand-maintained list of
69 directories. 396 directories under `cards/pack/` contain card scripts, so
327 were silently omitted -- every `aoa/` subfolder, every `*_nemesis` folder,
2,534 of 3,455 files. The loop iterated the list rather than the tree, so a
pack that shipped without its scripts was indistinguishable from a pack that
was never added. The folders are now derived by walking `cards/pack/`.

**MARVEL-57 -- the timestamp.** The uniform 2022-01-01 stamp was assigned
*after* `ZipFile.write()`, which has already emitted the local file header
using the file's mtime. `getinfo()` returns the entry in `filelist`, which is
only re-serialised into the central directory at close. So every entry carried
two different timestamps and the archive was not byte-reproducible -- while
looking uniform, because most readers show the central-directory value. The
entry is now built with its timestamp already set and written with
`writestr()`, before any bytes are emitted.

`external_attr` is pinned too. `write()` copies the file's mode, so the archive
otherwise varied with the umask of whoever built it.

**Arcnames stay flat.** `os.path.dirname()` of a trailing-slash path is the
folder itself, so every file lands at the zip root. Widening the folder set
makes that a real risk rather than a theoretical one, so rather than change the
archive's shape -- nothing in this repo reads it, but something outside it may
-- `ZipCards` now fails loudly on a duplicate arcname. Measured at the time of
the change: 3,455 files, zero basename collisions.

Run from `py_src/`:

    python -m tools.package.zip_cards
"""

import argparse
import os
import zipfile
from pathlib import Path

PACK_ROOT = Path("cards/pack")

# Package scaffolding and campaign definitions are not card scripts.
EXCLUDED_NAMES = {'__init__.py', 'campaign.py'}

# A fixed date, written as the tuple the zip format actually stores, so it does
# not round-trip through the local timezone on the way in.
UNIFORM_DATE_TIME = (2022, 1, 1, 0, 0, 0)

# rw-r--r--, in the high 16 bits where zip keeps the POSIX mode. `write()` would
# otherwise copy each file's own mode and make the archive umask-dependent.
UNIFORM_EXTERNAL_ATTR = 0o644 << 16


def CardFolders(root: Path = PACK_ROOT) -> list[str]:
    """Every directory under `root` holding at least one card script.

    Derived rather than listed. The hand-maintained list this replaced named 69
    of 396 such directories and had no way to report the other 327 -- see
    MARVEL-56.
    """
    folders = {path.parent for path in root.rglob("*.py")
               if path.name not in EXCLUDED_NAMES}
    return [f"./{folder.as_posix()}/" for folder in sorted(folders)]


def ZipCards(output: Path, folders: list[str] | None = None) -> int:
    """Write the card scripts in `folders` to `output`. Returns the file count."""
    if folders is None:
        folders = CardFolders()
    written = 0
    seen: dict[str, str] = {}

    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for folder in folders:
            for file in sorted(os.listdir(folder)):
                file_path = os.path.join(folder, file)
                if not os.path.isfile(file_path) or file in EXCLUDED_NAMES:
                    continue

                arcname = os.path.relpath(file_path, os.path.dirname(folder))
                if arcname in seen:
                    # Arcnames are flat, so two files with the same basename in
                    # different packs would silently overwrite each other. See
                    # MARVEL-56.
                    raise ValueError(
                        f"duplicate arcname {arcname!r}: "
                        f"{seen[arcname]} and {file_path}")
                seen[arcname] = file_path

                # Build the entry with its timestamp already set and write the
                # bytes through it. Assigning `date_time` after `write()` only
                # reaches the central directory -- MARVEL-57.
                info = zipfile.ZipInfo(arcname, date_time=UNIFORM_DATE_TIME)
                info.compress_type = zipfile.ZIP_DEFLATED
                info.external_attr = UNIFORM_EXTERNAL_ATTR
                with open(file_path, 'rb') as handle:
                    zipf.writestr(info, handle.read())
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
    folders = CardFolders()
    written = ZipCards(output, folders)

    print(f"zipped {written} card scripts into {output}")
    print(f"  from {len(folders)} pack folders, derived by walking {PACK_ROOT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
