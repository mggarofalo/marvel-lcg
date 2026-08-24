"""What "stale" means, for every generated fixture with a `--check` gate.

Six tools regenerate a checked-in file and compare it with the copy in the
repository (MARVEL-73):

    python -m tools.rng.emit_vectors --check        datasets/rng/vectors.json
    python -m tools.digest.emit_vectors --check     datasets/digest/vectors.json
    python -m tools.digest.emit_escaping --check    datasets/digest/escaping.json
    python -m tools.events.emit_vocabulary --check  datasets/events/vocabulary.json
    python -m tools.cards.extract --check           datasets/cards/*.json
    python -m tools.setup.emit_setup --check        datasets/setup/setup.json

All six answer the same question, so all six ask it the same way, here.
A contributor has to be able to predict which gate a difference trips, and
before this module they could not: the first three had three comparisons
written three different ways and nobody could say what any of them tolerated.

**The comparison is byte for byte.** Every one of these writers opens its file
with `newline="\\n"` and `encoding="utf-8"`, so what it produces is fully
determined down to the byte; a checker that accepts anything else accepts a
file the writer would not have written. These are the fixtures the C# port is
accepted against, so "the checked-in file is exactly what this generator
produces" is the claim worth making, and it is the only claim that is cheap to
state and impossible to misread.

That makes line endings part of the comparison, which is the one difference a
contributor does not cause and cannot see. `git`'s `core.autocrlf` defaults to
true on Windows, so a clone made before `.gitattributes` pinned `eol=lf`
(MARVEL-67) has a working tree full of CRLF, and every byte comparison in the
repo fails on files nobody touched. So it gets its own verdict: still a
failure, never silently forgiven, but reported as what it is rather than as
staleness. Diagnosing that took an issue of its own once; it should not take a
second one.

What a byte comparison catches that a parsed-JSON comparison would not:

- key order, in a file whose whole review story is that `git diff` shows the
  cards that changed (`tools/cards/extract.py:_RenderCards`);
- the one-record-per-line layout, hand-rolled for the same reason;
- number formatting -- `1.0` against `1`, and any integer that became a float;
- duplicate keys, which `json.loads` silently keeps the last of;
- Unicode escaping, so a `ensure_ascii` flip is visible rather than invisible.

None of those change what Python reads back. All of them change what a C#
implementer reads, which is who the fixture is for.
"""

from __future__ import annotations

import os
from pathlib import Path

# Verdicts. `FRESH` is the only one that is not a failure.
FRESH = "fresh"
MISSING = "missing"
STALE = "stale"
LINE_ENDINGS = "line_endings"

SUMMARY = {
    FRESH: "up to date",
    MISSING: "missing",
    STALE: "stale",
    LINE_ENDINGS: "CRLF line endings",
}


def Normalise(raw: bytes) -> bytes:
    """A text file's content, independent of how the checkout wrote it: CRLF
    to LF, and nothing else.

    Two callers, and the distinction between them is the point. `Compare` uses
    it only to tell one kind of failure from another -- never to decide that a
    file is up to date. `tools/cards/engine.py:Sha256` uses it on the *inputs*
    it records provenance for, where the hash has to name content rather than a
    checkout.

    Safe on all of them because they are JSON: a carriage return inside a
    string is escaped as the two characters `\\r`, so the only bare CR bytes in
    any of these files are line terminators.
    """
    return raw.replace(b"\r\n", b"\n")


def Compare(rendered: str, path: str | os.PathLike[str]) -> str:
    """How the file at `path` relates to the `rendered` text a generator built."""
    path = Path(path)
    if not path.exists():
        return MISSING
    on_disk = path.read_bytes()
    expected = rendered.encode("utf-8")
    if on_disk == expected:
        return FRESH
    if Normalise(on_disk) == Normalise(expected):
        return LINE_ENDINGS
    return STALE


def Explain(verdict: str, path: str | os.PathLike[str], command: str) -> str:
    """The failure message: what is wrong, and the command that fixes it."""
    if verdict == LINE_ENDINGS:
        return (
            f"{path} holds exactly the right content with the wrong line "
            f"endings: CRLF, where the generator writes LF.\n"
            f"Nothing is stale. Your checkout rewrote them -- git's "
            f"core.autocrlf is true by default on Windows.\n"
            f".gitattributes pins eol=lf, so this is a clone that predates it. "
            f"Re-normalise the working tree:\n"
            f"    git config core.autocrlf false\n"
            f"    git rm --cached -r -q . && git reset --hard\n"
            f"Or regenerate the file in place: {command}"
        )
    if verdict == MISSING:
        return f"{path} is missing; run: {command}"
    return f"{path} is stale; run: {command}"
