"""Which rules the C# engine's tests hold it to, and which nothing holds it to.

    python -m tools.rules.citations                # the summary
    python -m tools.rules.citations --uncited      # entries nothing cites
    python -m tools.rules.citations --cited        # every citation, by rule
    python -m tools.rules.citations --out cov.json

Run from `py_src/`.

## Why this is separate from `tools.rules.coverage`

`tools.rules.coverage` answers the question for the *Python* engine, whose
claims are Gherkin scenarios and whose trust is `specs/trusted.json`. This
answers it for the *C# engine*, whose claims are xUnit tests carrying
`[Rule("rr:...")]` and whose trust is a green build. Same rulebook, same
arithmetic, two bodies of work -- and the numbers are worth reading side by
side, because the C# port is being rebuilt from the rulebook while the Python
engine was not.

## What counts

A citation counts when the test carrying it passes, and in C# that is the same
statement as "CI is green": a failing test fails the build. There is no
quarantine to model and no `--suspect` list, which is the one way this is
simpler than its Python sibling.

Citations are read from the source rather than by reflecting over the built
assemblies. That keeps the tool runnable without a .NET SDK present, and it
lets a citation be reported with the file and line a reader can open. The
counterpart obligation -- that every cited id names a real rule -- is *not*
enforced here, because a report that quietly drops a bad citation is how a
mistyped citation survives. It is enforced in the C# suite by
`RuleCitationTests.EveryCitedRuleExists`, so a bad citation fails a build
rather than shrinking a number nobody was watching.

## An uncited entry is not automatically work

Same caveat as the Python tool, for the same reason: a good deal of the
glossary is vocabulary (`rr:you-your`, `rr:and`) that no test should be
expected to assert. This reports; triage is a person's job. `--uncited --sort`
orders by clause count, which is a rough proxy for how much engine surface an
entry touches, so the reading starts somewhere useful.
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import re
import sys
from typing import Any, Dict, Iterable, List

from tools.rules.coverage import load_index

TESTS_ROOT = os.path.join("..", "tests")

# `[Rule("rr:forced.4")]`. Deliberately not anchored to the line start: the
# attribute is indented, and may share a line with nothing else.
CITATION = re.compile(r'\[Rule\(\s*"([^"]+)"')

# The declaration a run of citations sits above. Test names in this repository
# are sentences, so the name is worth carrying into the report.
DECLARATION = re.compile(r'\b(?:void|Task)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(')


class Citation:
    """One `[Rule(...)]`, and where a reader can find it."""

    def __init__(self, rule_id: str, path: str, line: int, site: str) -> None:
        self.rule_id = rule_id
        self.path = path
        self.line = line
        self.site = site

    def __repr__(self) -> str:  # pragma: no cover - debugging aid
        return f"Citation({self.rule_id}, {self.site})"

    def AsDict(self) -> Dict[str, Any]:
        return {"rule": self.rule_id, "path": self.path,
                "line": self.line, "test": self.site}


def SourceFiles(root: str = TESTS_ROOT) -> List[str]:
    """Every C# file under `tests/`, skipping build output."""
    found = []
    for directory, subdirectories, files in os.walk(root):
        subdirectories[:] = [d for d in subdirectories if d not in ("bin", "obj")]
        for name in sorted(files):
            if name.endswith(".cs"):
                found.append(os.path.join(directory, name))
    return sorted(found)


def Read(path: str) -> List[Citation]:
    """The citations in one file, each bound to the test it sits above."""
    with open(path, encoding="utf-8") as handle:
        lines = handle.read().split("\n")

    found: List[Citation] = []
    pending: List[Citation] = []
    for number, line in enumerate(lines, start=1):
        for rule_id in CITATION.findall(line):
            pending.append(Citation(rule_id, path, number, "?"))
            continue
        declaration = DECLARATION.search(line)
        if declaration and pending:
            for citation in pending:
                citation.site = declaration.group(1)
            found.extend(pending)
            pending = []

    # Citations left over sat on a class rather than a method -- the whole
    # fixture claims the rule. Attributed to the file, which is what a reader
    # would open.
    for citation in pending:
        citation.site = os.path.basename(citation.path).removesuffix(".cs")
    found.extend(pending)
    return found


class Coverage:
    """The join: every citable rule, and the C# tests claiming it."""

    def __init__(self, index: Dict[str, Any], citations: Iterable[Citation]) -> None:
        self.index = index
        self.records = {record["id"]: record for record in index["entries"]}
        self.entries = [r for r in index["entries"] if r["id"].count(".") == 0]

        self.cited: Dict[str, List[Citation]] = collections.defaultdict(list)
        self.unknown: List[Citation] = []
        for citation in citations:
            if citation.rule_id in self.records:
                self.cited[citation.rule_id].append(citation)
            else:
                self.unknown.append(citation)

    def Covered(self, entry_id: str) -> bool:
        """An entry counts if it, or any clause of it, is cited."""
        if self.cited.get(entry_id):
            return True
        return any(self.cited.get(record_id) for record_id in self.records
                   if record_id.startswith(entry_id + "."))

    def Uncited(self) -> List[Dict[str, Any]]:
        rows = []
        for entry in self.entries:
            if entry.get("redirect") or self.Covered(entry["id"]):
                continue
            clauses = sum(1 for record_id in self.records
                          if record_id.startswith(entry["id"] + "."))
            rows.append({"id": entry["id"], "title": entry["title"],
                         "page": entry["page"], "clauses": clauses,
                         "fragment": entry.get("fragment", "")})
        return rows

    def Summary(self) -> Dict[str, Any]:
        real = [e for e in self.entries if not e.get("redirect")]
        citable = [r for r in self.records.values() if not r.get("redirect")]
        return {
            "rules_reference_version": self.index.get("version"),
            "entries": len(real),
            "entries_cited": sum(1 for e in real if self.Covered(e["id"])),
            "citable_records": len(citable),
            "records_cited": sum(1 for r in citable if self.cited.get(r["id"])),
            "citations": sum(len(v) for v in self.cited.values()),
            "unknown": [c.AsDict() for c in self.unknown],
        }


def Build(root: str = TESTS_ROOT) -> Coverage:
    citations: List[Citation] = []
    for path in SourceFiles(root):
        citations.extend(Read(path))
    return Coverage(load_index(), citations)


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--uncited", action="store_true",
                        help="entries no C# test cites")
    parser.add_argument("--cited", action="store_true",
                        help="every citation, grouped by rule")
    parser.add_argument("--sort", action="store_true",
                        help="with --uncited, order by clause count")
    parser.add_argument("--out", metavar="PATH", help="write the report as JSON")
    parser.add_argument("--tests", default=TESTS_ROOT, help="the tests root")
    args = parser.parse_args(argv)

    coverage = Build(args.tests)
    summary = coverage.Summary()

    if args.uncited:
        rows = coverage.Uncited()
        if args.sort:
            rows.sort(key=lambda row: (-row["clauses"], row["id"]))
        print(f"{len(rows)} entries no C# test cites\n")
        for row in rows:
            print(f"  {row['id']:<44} {row['clauses']:>3} clauses  p{row['page']}")
        return 0

    if args.cited:
        print(f"{summary['citations']} citations over "
              f"{summary['records_cited']} rules\n")
        for rule_id in sorted(coverage.cited):
            print(f"  {rule_id}")
            for citation in coverage.cited[rule_id]:
                print(f"      {citation.site}  ({citation.path}:{citation.line})")
        return 0

    print(f"Rules Reference v{summary['rules_reference_version']}\n")
    print(f"  entries        {summary['entries_cited']:>6} / {summary['entries']:<5} "
          f"cited ({100 * summary['entries_cited'] / summary['entries']:.1f}%)")
    print(f"  citable records{summary['records_cited']:>6} / {summary['citable_records']:<5} "
          f"cited ({100 * summary['records_cited'] / summary['citable_records']:.1f}%)")
    print(f"\n  citations made {summary['citations']:>6}")
    if summary["unknown"]:
        print(f"\n  {len(summary['unknown'])} citations name no such rule "
              "-- RuleCitationTests should be failing:")
        for row in summary["unknown"]:
            print(f"      {row['test']} cites {row['rule']}  ({row['path']}:{row['line']})")

    if args.out:
        report = dict(summary)
        report["cited"] = {k: [c.AsDict() for c in v]
                           for k, v in sorted(coverage.cited.items())}
        report["uncited"] = coverage.Uncited()
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(report, handle, indent=2, ensure_ascii=False)
            handle.write("\n")
        print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
