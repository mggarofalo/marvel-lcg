"""Harvest MarvelCDB's FAQ rulings into the vendored snapshot (MARVEL-143).

    python -m tools.cards.harvest_faq              # harvest and write the snapshot
    python -m tools.cards.harvest_faq --dry-run    # report what would be written
    python -m tools.cards.harvest_faq --limit 40   # a short run, for checking the wiring

Run from `py_src/`. Takes about ten minutes and needs the `marvelcdb` CLI on
`PATH` (`go install github.com/mggarofalo/marvelcdb-cli/cmd/marvelcdb@latest`).

## Why this exists, and why nothing imports it

`datasets/marvelsdb/` carries printed card text and errata. It carries no
rulings, and neither does anything else in this repository. That is a hole in
the spec campaign: a spec authored from printed text alone encodes contested
timing by *guess*, is then validated against the Python engine -- which
implements the same guess -- and passes into `specs/trusted.json` looking
validated. Where an official ruling exists it is the only independent check
available, and only while the Python engine is still the reference.

MarvelCDB and the vendored `zzorba/marvelsdb-json-data` snapshot are the same
corpus: measured 2026-08-22, 4,297 of the 4,298 codes they share carry
byte-identical printed text. So the CLI adds nothing on card text. FAQ is the
part it genuinely adds, and the only reason this module exists.

**This module is the one place in the repository that touches the network, and
nothing imports it.** `tools/cards/rulings.py` reads what it writes; the build
path reads neither. That is the boundary rule from AGENTS.md expressed as
structure rather than as a comment, because a rule that only exists in prose is
one refactor away from being untrue. It shells out to the CLI rather than
speaking HTTP so there is no new Python dependency and no second implementation
of MarvelCDB's API to keep correct.

The result is *vendored*, like `datasets/marvelsdb/` -- not *generated*, like
`datasets/cards/`. There is no `--check` gate, because there is nothing a
machine without a network could regenerate and compare.

## Why the exit code cannot decide success

Measured against the CLI, not read out of its help:

    faq 01050            no entries        exit 0
    faq 99999            unknown code      exit 4
    faq 01001a 01050     mixed             exit 0
    faq <40 codes, 4 of them unknown>      exit 1, and stdout is still
                                           complete, valid JSON

So exit 1 is not a failure and exit 0 is not proof of completeness. Worse, a
code with no ruling and a code MarvelCDB has never heard of produce the *same*
observable output -- one stderr line each, no entry.

What this module trusts instead is **accounting**. Every code it asks about must
come back either as an entry on stdout or as a `no FAQ entries for <code>` line
on stderr. A code in neither stream is a code whose answer was lost, and losing
one silently would write a snapshot that says "no ruling" about a card nobody
actually asked about. So that fails the run.

Two consequences worth knowing:

- The codes are taken from `marvelcdb cards list`, so the CLI itself confirms
  every code exists before it is asked about. Exit 4 then means something is
  wrong, not that a code was mistyped.
- `-q` is deliberately **not** passed. It suppresses the `no FAQ entries` lines,
  which are the only evidence a code was asked. stderr is data here.
"""

from __future__ import annotations

import argparse
import datetime
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any, Dict, Iterable, List, Sequence, Tuple

from tools.cards import rulings as rulings_module

SNAPSHOT_DIR = Path("../datasets/marvelcdb-faq")

# One `faq` invocation still makes one request per code, spaced by the CLI's
# rate limiter, so batching saves process startup rather than politeness. Kept
# well under the platform's argument limit: 4,456 codes at seven bytes each
# would be fine on Linux and is not worth finding out about on Windows.
BATCH = 250

# `no FAQ entries for 01050`. Matched exactly rather than by substring, because
# stderr also carries advisory messages that must not be read as answers.
_NO_ENTRIES = re.compile(r"^no FAQ entries for (\S+)$")

# The CLI's own exit codes. 1 is "general failure", which it also returns for a
# batch containing an unknown code, so it is judged by accounting instead.
_EXIT_NOT_FOUND = 4
_EXIT_OFFLINE = 5


class HarvestError(RuntimeError):
    """The harvest could not be completed and no snapshot should be written."""


def _Run(args: Sequence[str]) -> Tuple[int, str, str]:
    result = subprocess.run(
        args, capture_output=True, text=True, encoding="utf-8")
    return result.returncode, result.stdout, result.stderr


def _RequireCli() -> str:
    path = shutil.which("marvelcdb")
    if path is None:
        raise HarvestError(
            "marvelcdb is not on PATH. Install it with\n"
            "    go install github.com/mggarofalo/marvelcdb-cli/cmd/marvelcdb@latest\n"
            'and add "$(go env GOPATH)/bin" to PATH. This tool is an acquisition\n'
            "step, not a build dependency -- see AGENTS.md.")
    return path


def HarvesterVersion() -> str:
    """`marvelcdb v0.1.0`, recorded in the snapshot so a reader can date it."""
    code, out, err = _Run(["marvelcdb", "version"])
    if code != 0:
        raise HarvestError(f"`marvelcdb version` failed ({code}): {err.strip()}")
    first = out.strip().splitlines()[0] if out.strip() else ""
    return first.strip() or "unknown"


def Codes() -> List[str]:
    """Every card code MarvelCDB serves, including encounter cards and reprints.

    Both flags matter. `cards list` hides encounter cards and reprints by
    default, and a villain's ruling is exactly as load bearing as a hero's --
    `01135 Ultron` carries one of the two rulings in the core set.
    """
    code, out, err = _Run(
        ["marvelcdb", "cards", "list", "--encounter", "--duplicates", "-o", "ids"])
    if code != 0:
        raise HarvestError(f"`marvelcdb cards list` failed ({code}): {err.strip()}")
    codes = sorted({line.strip() for line in out.splitlines() if line.strip()})
    if not codes:
        raise HarvestError("`marvelcdb cards list` returned no codes")
    return codes


def _Entries(stdout: str) -> List[Dict[str, Any]]:
    """Parse one batch's stdout.

    The CLI returns a bare object for a single result and an array for several,
    so a batch that happens to find exactly one ruling is shaped differently
    from its neighbours. Empty stdout means the whole batch found nothing.
    """
    text = stdout.strip()
    if not text:
        return []
    payload = json.loads(text)
    if isinstance(payload, dict):
        return [payload]
    if isinstance(payload, list):
        return payload
    raise HarvestError(f"unexpected JSON from `marvelcdb faq`: {type(payload)}")


def _Empty(stderr: str) -> List[str]:
    """The codes stderr reports as having no rulings."""
    found = []
    for line in stderr.splitlines():
        match = _NO_ENTRIES.match(line.strip())
        if match:
            found.append(match.group(1))
    return found


def Batch(codes: Sequence[str]) -> List[Dict[str, Any]]:
    """Ask about one batch of codes and return the entries found.

    Raises unless every code in `codes` is accounted for -- see the module
    docstring. `-q` is not passed: it would suppress the very lines that do the
    accounting.
    """
    code, out, err = _Run(["marvelcdb", "faq", *codes, "-o", "json"])

    if code == _EXIT_NOT_FOUND:
        raise HarvestError(
            f"marvelcdb reports an unknown card code in this batch ({codes[0]}"
            f"..{codes[-1]}). The codes came from `cards list`, so this means "
            f"the corpus moved underneath the harvest -- rerun from the start.")
    if code == _EXIT_OFFLINE:
        raise HarvestError("marvelcdb has no network and no cached response")

    try:
        entries = _Entries(out)
    except json.JSONDecodeError as exc:
        raise HarvestError(
            f"marvelcdb returned unparseable JSON (exit {code}): {exc}") from exc

    answered = {entry.get("code") for entry in entries} | set(_Empty(err))
    missing = [c for c in codes if c not in answered]
    if missing:
        raise HarvestError(
            f"{len(missing)} code(s) came back in neither stdout nor stderr, so "
            f"their answers were lost: {', '.join(missing[:10])}"
            f"{' ...' if len(missing) > 10 else ''}. "
            f"marvelcdb exited {code}. Writing the snapshot now would record "
            f"'no ruling' for cards nobody actually asked about.")
    return entries


def Harvest(codes: Sequence[str], progress=None) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    for start in range(0, len(codes), BATCH):
        chunk = codes[start:start + BATCH]
        entries.extend(Batch(chunk))
        if progress is not None:
            progress(min(start + BATCH, len(codes)), len(codes), len(entries))
    return entries


def _Record(entry: Dict[str, Any]) -> Dict[str, Any]:
    """One entry, verbatim apart from key order.

    Nothing is normalised on the way in -- not the HTML, not the smart quotes,
    not the `updated` timestamp's shape. What MarvelCDB said is what gets
    recorded, so a reader can tell a transcription problem from a harvest
    problem. Keys are ordered only so the file diffs cleanly.
    """
    ordered = {key: entry[key] for key in ("code", "html", "text", "updated")
               if key in entry}
    # Anything MarvelCDB adds later rides along rather than being dropped.
    ordered.update({k: v for k, v in sorted(entry.items()) if k not in ordered})
    return ordered


def Render(entries: Sequence[Dict[str, Any]], queried: Sequence[str],
           harvested: str, harvester: str) -> str:
    """The snapshot, one entry per line.

    The layout is `tools/cards/extract.py:_RenderCards`'s, for its reason: the
    review story for a refresh is `git diff` naming the rulings that changed,
    and a pretty-printed 1 MB file gives a diff nobody can read.

    `queried` is not decoration. It is the difference between "this card has no
    ruling" and "nobody asked about this card", and without it a reader cannot
    tell them apart -- the same distinction `tools/spec/coverage.py` draws
    between `stats_only` and `absent`, where getting it wrong did not look like
    a bug, it looked like a smaller universe.
    """
    lines = [
        "{",
        '"version": 1,',
        f'"harvested": {json.dumps(harvested)},',
        '"source": "https://marvelcdb.com",',
        f'"harvester": {json.dumps(harvester)},',
        ('"note": "Raw MarvelCDB FAQ entries, verbatim. `queried` is every code '
         'asked about, so a code absent from `entries` but present in `queried` '
         'has no ruling rather than an unknown one. Vendored, not generated: '
         'see UPSTREAM.md.",'),
        '"queried": [',
    ]
    lines += [json.dumps(code) + ("," if i < len(queried) - 1 else "")
              for i, code in enumerate(sorted(queried))]
    lines.append("],")
    lines.append('"entries": [')
    ordered = sorted((_Record(e) for e in entries), key=lambda e: e["code"])
    lines += [json.dumps(e, ensure_ascii=False, sort_keys=False)
              + ("," if i < len(ordered) - 1 else "")
              for i, e in enumerate(ordered)]
    lines.append("]")
    lines.append("}")
    return "\n".join(lines) + "\n"


def Write(path: Path, payload: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    # Explicit, and matching every other writer in `tools/`: what lands on disk
    # has to be the same bytes on both CI legs. See `tools/fixtures.py`.
    path.write_text(payload, encoding="utf-8", newline="\n")


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__.split("\n\n")[0],
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", default=str(SNAPSHOT_DIR),
                        help="snapshot directory (default ../datasets/marvelcdb-faq)")
    parser.add_argument("--limit", type=int, default=0,
                        help="ask about only the first N codes, for checking "
                             "the wiring without a full ten-minute run")
    parser.add_argument("--date", default="",
                        help="harvest date to record (default today, UTC)")
    parser.add_argument("--dry-run", action="store_true",
                        help="report what would be written; write nothing")
    args = parser.parse_args(argv)

    try:
        _RequireCli()
        harvester = HarvesterVersion()
        codes = Codes()
        if args.limit:
            codes = codes[:args.limit]

        print(f"{len(codes)} codes from marvelcdb, {harvester}", flush=True)

        def Progress(done: int, total: int, found: int) -> None:
            print(f"  {done}/{total} asked, {found} ruling(s)", flush=True)

        entries = Harvest(codes, progress=Progress)
    except HarvestError as exc:
        print(f"harvest failed: {exc}", file=sys.stderr)
        return 1

    harvested = args.date or datetime.datetime.now(datetime.UTC).date().isoformat()
    payload = Render(entries, codes, harvested, harvester)

    cards = len({e.get("code") for e in entries})
    print(f"{cards} card(s) carry a ruling, of {len(codes)} asked "
          f"({rulings_module.Percent(cards, len(codes))})")

    if args.dry_run:
        print(f"--dry-run: {len(payload)} bytes not written")
        return 0

    target = Path(args.out) / rulings_module.FAQ_FILE
    Write(target, payload)
    print(f"wrote {target}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
