"""Emit the event vocabulary as a cross-language contract.

Writes `datasets/events/vocabulary.json`. The C# `GameEvent` hierarchy in
`src/Marvel.Rules/Events/` asserts its `[JsonDerivedType]` discriminators and
their JSON property names against this file, so the two cannot drift without a
test going red.

The vocabulary itself is not invented. It is the set that explains every state
change in the frozen corpus with nothing left over and no member that never
fires -- measured by `tools/events/census.py`, checked by
`unit_test/test_event_model.py`. See `docs/event-stream.md`.

Run:
    python -m tools.events.emit_vocabulary
    python -m tools.events.emit_vocabulary --check
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from typing import Sequence

from tools import fixtures
from tools.events.model import UNIVERSAL, VOCABULARY

OUTPUT = os.path.join("..", "datasets", "events", "vocabulary.json")


def Render() -> str:
    document = {
        "contract": "docs/event-stream.md",
        "generated_by": "python -m tools.events.emit_vocabulary",
        "note": ("The closed set of event kinds the fold returns. `universal` "
                 "keys are on every event; `kinds` maps a kind to the payload "
                 "keys it adds."),
        "universal": list(UNIVERSAL),
        "kinds": {kind: list(keys) for kind, keys in sorted(VOCABULARY.items())},
    }
    return json.dumps(document, indent=2, sort_keys=True, ensure_ascii=True) + "\n"


def _main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true",
                        help="exit non-zero if the checked-in file is stale")
    args = parser.parse_args(argv)

    rendered = Render()
    if args.check:
        verdict = fixtures.Compare(rendered, OUTPUT)
        if verdict != fixtures.FRESH:
            print(fixtures.Explain(verdict, OUTPUT,
                                   "python -m tools.events.emit_vocabulary"),
                  file=sys.stderr)
            return 1
        print(f"{OUTPUT} is up to date")
        return 0

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(rendered)
    print(f"wrote {OUTPUT} ({len(VOCABULARY)} kinds)")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
