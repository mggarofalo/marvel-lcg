"""Emit the cross-language JSON escaping fixture.

Writes `datasets/digest/escaping.json`. Where `vectors.json` says *what* the
digest contains, this says *how a string in it is spelled* -- the one part of the
canonical form the two languages do not agree on for free.

## Why this exists

The digest is compared byte for byte, so the spelling of the JSON is the
contract. Python's `json.dumps` and .NET's `Utf8JsonWriter` disagree in two
places, and neither is configurable away:

  hex case        Python writes `\\u001f`, .NET writes `\\u001F`.
  escape or not   Python with `ensure_ascii=True` escapes every non-ASCII
                  character; .NET's relaxed encoder leaves most of them raw.

Both are mechanical, so the C# side reconciles them with two regex passes rather
than hand-writing an encoder -- see `docs/state-digest-v2.md`. That reconciliation
is only as good as the cases it was checked against, which is what this file is.

## What is in it

  handpicked   20 strings chosen for the specific ways a naive normaliser breaks:
               a literal backslash next to a `u`, odd-length backslash runs, an
               escape adjacent to an astral character, every C0 control.
  fuzz         400 strings over an alphabet stacked with backslashes, `u`, hex
               digits and surrogate halves. Seeded, so the file is byte-stable.

Each case carries the string as code points -- so this file is itself pure ASCII
and cannot be mangled in transit -- and Python's rendering of it, which is the
answer the C# side must reproduce.

Run:
    python -m tools.digest.emit_escaping
    python -m tools.digest.emit_escaping --check   # non-zero if the file is stale

Unlike `emit_vectors`, this boots nothing and runs in milliseconds.
"""

from __future__ import annotations

import argparse
import json
import os
import random
import sys
from typing import Any, Dict, List, Sequence, Tuple

from tools import fixtures

OUTPUT = os.path.join("..", "datasets", "digest", "escaping.json")

# Seeded so the file is byte-stable across runs. Changing it rewrites the
# fixture, which is a deliberate act and shows up as a diff.
FUZZ_SEED = 20260824
FUZZ_COUNT = 400

# Every character that has ever been part of a normaliser bug: the escape
# introducer, the letter that follows it, hex digits, the halves of a surrogate
# pair, and the four classes of character the two writers treat differently.
FUZZ_ALPHABET: Tuple[str, ...] = (
    "\\", "u", "\"", "a", "0", "1", "d", "8", "e",
    "é", "\U0001f600", "\x00", "\x1f", "\x7f",
    " ", " ", "﻿", "\t", "\n",
)

HANDPICKED: Tuple[Tuple[str, str], ...] = (
    # The objection to doing this with a regex at all: string *content* that
    # reads like an escape sequence. Python spells a literal backslash `\\`, so
    # a normaliser that scans for `\u` without consuming backslash pairs first
    # rewrites text it should not touch.
    ("literal-backslash-u", "\\u0041"),
    ("double-backslash-u", "\\\\u0041"),
    ("triple-backslash-u", "\\\\\\u0041"),
    ("many-backslashes", "\\" * 7 + "u1234"),
    ("backslash-then-non-ascii", "\\é"),
    ("trailing-backslash", "ends with \\"),
    ("leading-backslash", "\\ starts"),
    ("backslash-quote", "\\\""),
    ("quote-and-backslash", "\"\\\"\\"),
    # Text that looks like an already-escaped surrogate pair, next to a real one.
    ("looks-like-surrogate", "\\ud83d\\ude00"),
    ("real-astral-after-slash", "\\\U0001f600"),
    ("escape-adjacent-astral", "é\U0001f600é"),
    # The characters the two writers disagree about.
    ("del-adjacent", "\x7f\\u007f"),
    ("nul-then-u", "\x00u0041"),
    ("all-c0-controls", "".join(chr(c) for c in range(0x20))),
    ("short-form-controls", "\t\n\r"),
    ("non-breaking-space", " "),
    ("line-separator", "  sep"),
    ("byte-order-mark", "﻿\\"),
    ("high-bmp", "￿￾�"),
)


def Build() -> List[Dict[str, Any]]:
    cases: List[Tuple[str, str]] = list(HANDPICKED)

    rng = random.Random(FUZZ_SEED)
    for index in range(FUZZ_COUNT):
        length = rng.randint(1, 14)
        cases.append((
            f"fuzz-{index}",
            "".join(rng.choice(FUZZ_ALPHABET) for _ in range(length)),
        ))

    return [
        {
            "name": name,
            # Code points rather than the string itself, so this file is pure
            # ASCII and a consumer rebuilds the exact input rather than
            # inheriting whatever its JSON reader did with the escapes.
            "codepoints": [ord(character) for character in value],
            "expect": json.dumps(value, ensure_ascii=True),
        }
        for name, value in cases
    ]


def Render(cases: List[Dict[str, Any]]) -> str:
    document = {
        "contract": "docs/state-digest-v2.md",
        "generated_by": "python -m tools.digest.emit_escaping",
        "note": ("`expect` is `json.dumps(value, ensure_ascii=True)` -- what the "
                 "digest's canonical form spells this string as. A port writes "
                 "the string with its own JSON writer and normalises to this."),
        "fuzz_seed": FUZZ_SEED,
        "cases": cases,
    }
    return json.dumps(document, indent=2, sort_keys=True, ensure_ascii=True) + "\n"


def _main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true",
                        help="exit non-zero if the checked-in file is stale")
    args = parser.parse_args(argv)

    rendered = Render(Build())

    if args.check:
        verdict = fixtures.Compare(rendered, OUTPUT)
        if verdict != fixtures.FRESH:
            print(fixtures.Explain(verdict, OUTPUT,
                                   "python -m tools.digest.emit_escaping"),
                  file=sys.stderr)
            return 1
        print(f"{OUTPUT} is up to date")
        return 0

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(rendered)
    print(f"wrote {OUTPUT} ({len(rendered.splitlines())} lines, "
          f"{len(HANDPICKED)} handpicked + {FUZZ_COUNT} fuzzed)")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
