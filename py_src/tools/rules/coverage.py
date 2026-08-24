"""How much of the rulebook the engine is actually proved against.

    python -m tools.rules.coverage                 # the summary
    python -m tools.rules.coverage --uncited       # rules nothing asserts
    python -m tools.rules.coverage --ungrounded    # specs citing no rule
    python -m tools.rules.coverage --suspect       # cited rules whose spec fails
    python -m tools.rules.coverage --out rules-coverage.json

Run from `py_src/`. The third acceptance item of `docs/rules-provenance.md`.

## The question this exists to make askable

`tools.spec.coverage` answers "which *cards* has somebody written a claim
about". This answers something the repository could not ask at all before
MARVEL-154: **which rules has somebody written a claim about, and is the claim
grounded in anything but the engine's own behaviour.**

AGENTS.md names the hazard and `docs/rules-provenance.md` designs the fix: a
spec authored from ambiguous printed words, validated against a Python engine
implementing the same reading of the same words, enters `specs/trusted.json`
having confirmed only that the engine agrees with itself. A citation is what
breaks that circle, because the Rules Reference is not downstream of the
engine.

Three numbers, and they are different questions:

  * **Uncited entries** -- rules no trusted scenario claims anything about.
    The honest measure of how much of the rulebook is unverified. It starts
    high; that is the point of measuring it.
  * **Ungrounded specs** -- scenarios under `specs/rules/` with no `@rr:` tag.
    These assert engine behaviour and cite nothing, so passing tells you only
    that the engine is self-consistent.
  * **Suspect entries** -- a cited rule whose citing scenario is quarantined.
    A failing claim *with a citation attached*, which is the difference between
    a bug report and an argument.

## Coverage means a trusted scenario

Same rule as `tools.spec.coverage`, for the same reason: a quarantined
scenario is a claim that failed, so counting it would make the number improve
when authoring goes wrong.

## An uncited entry is not automatically work

261 entries is the whole glossary, and a good deal of it is vocabulary --
`rr:you-your`, `rr:and` -- that no scenario should be expected to assert. This
tool reports; it does not assign. Triage is a person's job, and
`--uncited --sort` orders by how much *engine surface* an entry plausibly
touches (its clause count) so the reading starts somewhere useful.
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import sys
from typing import Any, Dict, Iterable, List, Sequence

RULES_INDEX = os.path.join("..", "datasets", "rules-reference", "index.json")
SPEC_ROOT = "specs"
RULES_SPEC_ROOT = os.path.join("specs", "rules")
TRUSTED = os.path.join("specs", "trusted.json")
QUARANTINE = os.path.join("specs", "quarantine.json")

TAG_PREFIX = "rr:"

# `@rr:none` — this scenario asserts something the rulebook does not speak to,
# and saying so is a claim rather than an omission. `specs/rules/option-binding`
# is the case it was added for: it pins which *option* an assertion binds to
# when two cards share the label "Play", which is spec vocabulary, not a rule.
# Without this, such a file sits in the ungrounded count forever and the number
# never reaches zero however much work is done -- so the number stops meaning
# anything.
NONE_TAG = "rr:none"


def read_json(path: str) -> Dict[str, Any]:
    if not os.path.exists(path):
        return {}
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def load_index(path: str = RULES_INDEX) -> Dict[str, Any]:
    index = read_json(path)
    if not index:
        raise SystemExit(
            f"no rules index at {path}. It is a vendored snapshot -- see "
            "datasets/rules-reference/UPSTREAM.md.")
    return index


def cited_ids(case) -> List[str]:
    """The `@rr:` tags on a scenario, as citation ids."""
    return [tag for tag in case.tags
            if tag.startswith(TAG_PREFIX) and tag != NONE_TAG]


def declines_citation(case) -> bool:
    return NONE_TAG in case.tags


class RulesCoverage:
    """The join: every citable rule, and the scenarios claiming it."""

    def __init__(self, index: Dict[str, Any], cases: Sequence[Any],
                 trusted: Dict[str, Any], quarantined: Iterable[str]) -> None:
        self.index = index
        self.records = {record["id"]: record for record in index["entries"]}
        self.entries = [r for r in index["entries"] if r["id"].count(".") == 0]

        quarantined = set(quarantined)
        self.stale: List[str] = []

        self.cited: Dict[str, List[str]] = collections.defaultdict(list)
        self.suspect: Dict[str, List[str]] = collections.defaultdict(list)
        # A tag naming an id the index does not have is a typo in a scenario,
        # not a covered rule. Reported rather than counted -- the same
        # treatment `tools.spec.coverage` gives an unknown `@card:` tag.
        self.unknown_tags: Dict[str, List[str]] = collections.defaultdict(list)
        self.ungrounded: List[str] = []
        self.declined: List[str] = []

        for case in cases:
            citations = cited_ids(case)
            if declines_citation(case):
                self.declined.append(case.case_id)
            elif not citations and self._is_rules_spec(case):
                self.ungrounded.append(case.case_id)
            for citation in citations:
                if citation not in self.records:
                    self.unknown_tags[case.case_id].append(citation)
                elif self._is_trusted(case, trusted):
                    self.cited[citation].append(case.case_id)
                elif case.case_id in quarantined:
                    self.suspect[citation].append(case.case_id)

        for case in cases:
            if (case.case_id in trusted and cited_ids(case)
                    and not self._is_trusted(case, trusted)):
                self.stale.append(case.case_id)

    @staticmethod
    def _is_trusted(case, trusted: Dict[str, Any]) -> bool:
        """Trusted *and still pinned to the source it was validated against*.

        `trusted.json` records a `sha256` per scenario precisely so that
        editing the scenario drops it out. Checking only the name would credit
        a rule with a citation carried by a scenario whose text has since
        changed, which is the exact failure provenance pinning exists to
        prevent -- and it would do it silently, because the name is what
        survives an edit.
        """
        record = trusted.get(case.case_id)
        if not isinstance(record, dict):
            return bool(record)
        pinned = record.get("sha256")
        return not pinned or pinned == getattr(case, "source_sha256", "")

    @staticmethod
    def _is_rules_spec(case) -> bool:
        """Authored as a claim about a rule rather than about a card.

        Only these are counted as ungrounded. A card scenario asserting what
        one card does is not obliged to cite the rulebook -- its authority is
        the printed text, already pinned through `datasets/cards/`.
        """
        source = str(getattr(case, "source_path", "") or "")
        return "rules" in source.replace("\\", "/").split("/")

    def Covered(self, entry_id: str) -> bool:
        """An entry counts if it, or any clause of it, has a trusted citation."""
        if self.cited.get(entry_id):
            return True
        return any(self.cited.get(record_id)
                   for record_id in self.records
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
        covered = [e for e in real if self.Covered(e["id"])]
        citable = [r for r in self.records.values() if not r.get("redirect")]
        cited_records = [r for r in citable if self.cited.get(r["id"])]
        return {
            "rules_reference_version": self.index.get("version"),
            "entries": len(real),
            "entries_cited": len(covered),
            "entries_uncited": len(real) - len(covered),
            "citable_records": len(citable),
            "records_cited": len(cited_records),
            "ungrounded_rules_scenarios": len(self.ungrounded),
            "suspect_entries": len(self.suspect),
            "unknown_tags": sum(len(v) for v in self.unknown_tags.values()),
            "stale_pins": len(self.stale),
            "declined": len(self.declined),
        }


def build(index_path: str = RULES_INDEX) -> RulesCoverage:
    from tools.spec.run_case import LoadCases

    index = load_index(index_path)
    cases = LoadCases(SPEC_ROOT)
    trusted = read_json(TRUSTED).get("scenarios") or {}
    quarantined = (read_json(QUARANTINE).get("scenarios") or {}).keys()
    return RulesCoverage(index, cases, trusted, quarantined)


def _percent(part: int, whole: int) -> str:
    return f"{(100.0 * part / whole):.1f}%" if whole else "n/a"


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="How much of the Rules Reference is proved against.")
    parser.add_argument("--index", default=RULES_INDEX)
    parser.add_argument("--uncited", action="store_true")
    parser.add_argument("--ungrounded", action="store_true")
    parser.add_argument("--suspect", action="store_true")
    parser.add_argument("--sort", action="store_true",
                        help="with --uncited, densest entries first")
    parser.add_argument("--limit", type=int, default=40)
    parser.add_argument("--out", help="write the full report as JSON")
    args = parser.parse_args(argv)

    coverage = build(args.index)
    summary = coverage.Summary()

    if args.uncited:
        rows = coverage.Uncited()
        if args.sort:
            rows.sort(key=lambda row: (-row["clauses"], row["id"]))
        print(f"{len(rows)} entries with no trusted citation:\n")
        for row in rows[:args.limit]:
            print(f"  {row['id']:<44} p{row['page']:<4} "
                  f"{row['clauses']:>3} clause(s)  {row['title']}")
        if len(rows) > args.limit:
            print(f"\n  ... and {len(rows) - args.limit} more "
                  f"(--limit {len(rows)} for all)")
        return 0

    if args.ungrounded:
        print(f"{len(coverage.ungrounded)} scenarios under {RULES_SPEC_ROOT}/ "
              f"citing no rule:\n")
        for case_id in coverage.ungrounded[:args.limit]:
            print(f"  {case_id}")
        if len(coverage.ungrounded) > args.limit:
            print(f"\n  ... and {len(coverage.ungrounded) - args.limit} more")
        return 0

    if args.suspect:
        if not coverage.suspect:
            print("no cited rule has a failing scenario.")
            return 0
        print(f"{len(coverage.suspect)} cited rules with a quarantined "
              f"scenario -- suspected engine rules errors:\n")
        for entry_id, cases in sorted(coverage.suspect.items()):
            record = coverage.records[entry_id]
            print(f"  {entry_id}  {record.get('fragment', '')[:70]}")
            for case_id in cases:
                print(f"      {case_id}")
        return 0

    print(f"Rules Reference v{summary['rules_reference_version']}\n")
    print(f"  entries              {summary['entries_cited']:>5} / "
          f"{summary['entries']:<5} cited "
          f"({_percent(summary['entries_cited'], summary['entries'])})")
    print(f"  citable records      {summary['records_cited']:>5} / "
          f"{summary['citable_records']:<5} cited "
          f"({_percent(summary['records_cited'], summary['citable_records'])})")
    print()
    print(f"  ungrounded rules scenarios   {summary['ungrounded_rules_scenarios']:>5}"
          "   assert the engine, cite nothing")
    print(f"  suspected engine rules errors {summary['suspect_entries']:>4}"
          "   cited rule, failing scenario")
    if summary["declined"]:
        print(f"  deliberately uncited           {summary['declined']:>4}"
              "   @rr:none -- no rule to cite")
    if summary["stale_pins"]:
        print(f"  citations on stale pins        {summary['stale_pins']:>4}"
              "   scenario edited since it was trusted")
        for case_id in coverage.stale:
            print(f"      {case_id}")
    if summary["unknown_tags"]:
        print(f"  unknown @rr: tags             {summary['unknown_tags']:>4}"
              "   typo in a scenario")
        for case_id, tags in sorted(coverage.unknown_tags.items()):
            for tag in tags:
                print(f"      {tag}  in  {case_id}")

    if args.out:
        report = dict(summary)
        report["uncited"] = coverage.Uncited()
        report["ungrounded"] = list(coverage.ungrounded)
        report["declined"] = list(coverage.declined)
        report["suspect"] = {k: list(v) for k, v in coverage.suspect.items()}
        report["unknown_tags"] = {k: list(v)
                                  for k, v in coverage.unknown_tags.items()}
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(report, handle, indent=2, ensure_ascii=False)
            handle.write("\n")
        print(f"\nwrote {args.out}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
