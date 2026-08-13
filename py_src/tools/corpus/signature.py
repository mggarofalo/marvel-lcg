"""Read a worker's crash summary back out of its output.

The bot already prints, at the end of every run that captured anything, the
summary `engine/device/manager/bot/crash.py:FormatSummary` builds:

    2 failure(s), 2 distinct signature(s)
      4083f456  timeout-stall        x1 in 1 game(s)  [seed 30 step 20000]  ...
      976320f8  engine-assert        x1 in 1 game(s)  [seed 26 step 0]  ...

`generate.py` used to keep only the **tail** of that output, capped at 600
characters, as an outcome's `detail`. A `FailedTrace` stack is arbitrarily
long, so on a real run the summary is usually above the cut and the manifest
records that a case failed and nothing about why: on the 321-case run behind
MARVEL-97, 69 of 98 failures carried nothing but the middle of a traceback.
Widening the cap does not fix that -- any cap re-creates it, because the thing
being truncated has no bound. Parsing the summary does, because what comes out
is bounded by construction: a fixed set of fields per distinct signature.

## What is parsed, and how forgivingly

The signature line is matched on the shape it cannot change without the report
changing meaning -- a hex signature, a failure class, `xN in M game(s)`, and a
bracketed location -- and everything around it is treated as noise. That
matters because the line reaches us through the log: it arrives with a `<W>`
level prefix and wrapped in ANSI colour, neither of which is part of the
report. Whitespace between fields is `\\s+` for the same reason; `kind` is
printed through a `:<20` pad, so its trailing run of spaces is a formatting
detail and a longer class name legitimately collapses it to one.

## Three states, not two

A case that failed with no signature at all is ordinary -- a bad hero name, a
harness error, a kill before the reporter ran -- and it must not be confusable
with a summary this parser could not read. So a scan reports:

- `none`     -- no summary header and no signature line. Nothing to recover.
- `parsed`   -- the header is there and every signature it claims was read.
- `partial`  -- evidence is missing: the header says more signatures than were
                read (a truncated line, an unexpected format), or lines were
                read with no header above them (output cut at the top).

`partial` is the state that says *this parser needs looking at*. Without it a
regex that quietly stops matching looks exactly like the bug it was written to
fix, which is the failure mode this module exists to avoid.
"""

from __future__ import annotations

import re
from typing import Any, Dict, List, NamedTuple

# `\x1b[33m<W> ...\x1b[0m` -- the log writes colour around every line it emits.
ANSI = re.compile(r"\x1b\[[0-9;]*[A-Za-z]")

# `2 failure(s), 2 distinct signature(s)` -- printed once, above the lines.
HEADER = re.compile(
    r"(?P<failures>\d+)\s+failure\(s\),\s+"
    r"(?P<signatures>\d+)\s+distinct\s+signature\(s\)")

# One distinct signature. Anchored on nothing: the line reaches us behind a log
# level prefix, and how that prefix is spelled is not this module's business.
LINE = re.compile(
    r"(?P<signature>\b[0-9a-f]{6,64}\b)\s+"
    r"(?P<kind>[A-Za-z][A-Za-z0-9_-]*)\s+"
    r"x(?P<occurrences>\d+)\s+in\s+(?P<games>\d+)\s+game\(s\)\s+"
    r"\[(?P<where>[^\]]*)\]\s*"
    r"(?P<title>.*?)\s*$")

# `[seed 30 step 20000]`, or `[unknown]` when no occurrence was recorded.
WHERE = re.compile(r"seed\s+(?P<seed>-?\d+)\s+step\s+(?P<step>-?\d+)")

NONE = "none"
PARSED = "parsed"
PARTIAL = "partial"


class Signature(NamedTuple):
    """One distinct failure, as the child reported it."""
    signature   : str
    kind        : str
    occurrences : int
    games       : int
    seed        : int | None    # None when the report said `[unknown]`
    step        : int | None
    title       : str

    def ToDict(self) -> Dict[str, Any]:
        return {
            "signature": self.signature,
            "kind": self.kind,
            "occurrences": self.occurrences,
            "games": self.games,
            "seed": self.seed,
            "step": self.step,
            "title": self.title,
        }


class Scan(NamedTuple):
    """Every signature one child process reported, and how sure we are of it.

    `entries` is a **list**, not the first signature: one run plays several
    games, the report says how many distinct signatures it found, and the cases
    worth reconstructing later are exactly the ones that found more than one.
    Keeping the first would discard the rest silently -- the shape of the bug
    this replaces. The order is the child's own: most occurrences first, the
    signature hash breaking ties, so it is stable across runs.
    """
    status      : str               # "none" | "parsed" | "partial"
    failures    : int | None        # what the header claimed, None if absent
    signatures  : int | None
    entries     : List[Signature]

    def ToDict(self) -> Dict[str, Any]:
        return {
            "status": self.status,
            "failures": self.failures,
            "signatures": self.signatures,
            "entries": [entry.ToDict() for entry in self.entries],
        }


EMPTY = Scan(status=NONE, failures=None, signatures=None, entries=[])


def Strip(text: str) -> str:
    """The line without the colour the log wrapped it in."""
    return ANSI.sub("", text)


def ParseLine(line: str) -> Signature | None:
    """One signature line, or None if this line is not one."""
    match = LINE.search(Strip(line))
    if not match:
        return None
    where = WHERE.search(match.group("where"))
    return Signature(
        signature=match.group("signature"),
        kind=match.group("kind"),
        occurrences=int(match.group("occurrences")),
        games=int(match.group("games")),
        seed=int(where.group("seed")) if where else None,
        step=int(where.group("step")) if where else None,
        title=match.group("title"),
    )


def Parse(text: str) -> Scan:
    """Every signature in a child process's combined output.

    Reads the whole output rather than a window of it: this is the fix. A tail
    is what loses the summary in the first place.
    """
    if not text:
        return EMPTY

    failures: int | None = None
    signatures: int | None = None
    entries: List[Signature] = []

    for line in Strip(text).splitlines():
        header = HEADER.search(line)
        if header:
            # Later headers win: a worker plays several games in one process
            # but reports once, and if that ever changes the last report is the
            # one describing the run that finished.
            failures = int(header.group("failures"))
            signatures = int(header.group("signatures"))
            entries = []
            continue
        entry = ParseLine(line)
        if entry:
            entries.append(entry)

    if not entries:
        # Nothing to recover. Whether that is fine depends on whether anything
        # said there should have been: a header claiming failures with no line
        # under it is the parser's problem, and silence is not.
        return Scan(status=NONE if not signatures else PARTIAL,
                    failures=failures, signatures=signatures, entries=[])

    if signatures is None or len(entries) != signatures:
        # Either the header never arrived, or it disagrees with what was read.
        # Both mean the same thing to a reader: do not trust this as complete.
        # Disagreement in *either* direction, because reading more lines than
        # were reported means the pattern matched something that is not a
        # signature, and a parser that over-matches is as broken as one that
        # under-matches.
        status = PARTIAL
    else:
        status = PARSED

    return Scan(status=status, failures=failures,
                signatures=signatures, entries=entries)


def Describe(scan: Scan) -> str:
    """One line for a console, when there is something to say."""
    if not scan.entries:
        return ""
    first = scan.entries[0]
    where = (f"seed {first.seed} step {first.step}"
             if first.seed is not None else "unknown")
    more = (f" (+{len(scan.entries) - 1} more signature(s))"
            if len(scan.entries) > 1 else "")
    return f"{first.signature} {first.kind} [{where}] {first.title}{more}"
