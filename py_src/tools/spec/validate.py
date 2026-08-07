"""Differential spec extraction: no scenario is trusted until the engine agrees.

A scenario authored from printed card text is a guess. This runner turns the
guess into evidence by playing it against the Python engine while that engine is
still the reference, and it refuses to let anything into the trusted suite that
has not passed.

    python -m tools.spec.validate                    # validate and write reports
    python -m tools.spec.validate --trusted-only     # run the trusted suite (CI gate)
    python -m tools.spec.validate --triage triage.json

Four verdicts, decided by what the run observed rather than by judgement:

    PASS                    every Then held
    FAIL-spec-wrong         the scenario could not be executed as written -- a
                            Given named a card that is not in the game, a When
                            was never offered, a Then asked about something the
                            board does not have. The engine never offered what
                            the scenario describes, so the likeliest explanation
                            is that the author misread the card.
    FAIL-engine-suspected   the scenario executed cleanly -- every Given applied,
                            every When matched an offered option -- and a Then
                            disagreed anyway. The engine did something; it just
                            does not match the printed text.
    ERROR                   the engine raised, or logged a failure it swallowed.

A disagreement is never dismissed. Both failing verdicts go to the triage queue
with the decisions the policy saw and the board it ended on, which is what an
adjudicator needs to decide "spec bug" or "engine bug".

**The quarantine cannot be bypassed.** `trusted.json` is written only here, only
from PASS, and every entry carries the hash of the scenario source it was
validated against. Edit a scenario and it leaves the trusted suite on the next
run. There is no flag that adds an entry by hand, on purpose: a suite you can
talk your way into is not an oracle.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional, Sequence, Tuple

from tools.spec.case import SpecCase, SpecCaseError
from tools.spec.harness import (
    CaseResult, OUTCOME_ASSERTION, OUTCOME_ERROR, OUTCOME_PASS, OUTCOME_UNPLAYABLE, RunCase)
from tools.spec.run_case import LoadCases

DEFAULT_SCENARIOS = "./specs/scenarios"
DEFAULT_TRUSTED = "./specs/trusted.json"
DEFAULT_QUARANTINE = "./specs/quarantine.json"
DEFAULT_HISTORY = "./specs/history.jsonl"

VERDICT_PASS = "PASS"
VERDICT_SPEC_WRONG = "FAIL-spec-wrong"
VERDICT_ENGINE_SUSPECTED = "FAIL-engine-suspected"
VERDICT_ERROR = "ERROR"

VERDICTS = (VERDICT_PASS, VERDICT_SPEC_WRONG, VERDICT_ENGINE_SUSPECTED, VERDICT_ERROR)

VERDICT_OF_OUTCOME = {
    OUTCOME_PASS: VERDICT_PASS,
    OUTCOME_UNPLAYABLE: VERDICT_SPEC_WRONG,
    OUTCOME_ASSERTION: VERDICT_ENGINE_SUSPECTED,
    OUTCOME_ERROR: VERDICT_ERROR,
}

MANIFEST_VERSION = 1


def Verdict(result: CaseResult) -> str:
    return VERDICT_OF_OUTCOME.get(result.outcome, VERDICT_ERROR)


################################################################################
# Report

@dataclass
class Judgement:
    case: SpecCase
    verdict: str
    reason: str
    result: CaseResult

    @property
    def trusted(self) -> bool:
        return self.verdict == VERDICT_PASS

    def Describe(self) -> str:
        head = f"{self.verdict:<22} {self.case.case_id}"
        if self.verdict == VERDICT_PASS:
            return head
        body = self.reason.replace("\n", "\n     ").strip()
        return f"{head}\n     {body}"

    def TriageRecord(self) -> Dict[str, Any]:
        """Everything an adjudicator needs to call it a spec bug or an engine bug."""
        return {
            "case": self.case.case_id,
            "verdict": self.verdict,
            "source": self.case.source_path,
            "source_sha256": self.case.source_sha256,
            "scenario": self.case.scenario,
            "heroes": list(self.case.heroes),
            "seed": self.case.seed,
            "given": [step.Describe() for step in self.case.given],
            "when": [step.Describe() for step in self.case.when],
            "then": [step.Describe() for step in self.case.then],
            "reason": self.reason,
            "failed_assertions": [
                {"then": failure.step.Describe(), "actual": failure.actual,
                 "detail": failure.message}
                for failure in self.result.Failures()
            ],
            "decisions": [record.Describe() for record in self.result.decisions],
            "board": self.result.state.ToDict() if self.result.state else None,
            "engine_log": self.result.engine_log,
        }


@dataclass
class Summary:
    counts: Dict[str, int] = field(default_factory=lambda: {v: 0 for v in VERDICTS})
    judgements: List[Judgement] = field(default_factory=list)
    elapsed: float = 0.0

    @property
    def total(self) -> int:
        return sum(self.counts.values())

    @property
    def disagreements(self) -> int:
        return self.total - self.counts[VERDICT_PASS]

    @property
    def disagreement_rate(self) -> float:
        return (self.disagreements / self.total) if self.total else 0.0

    def Add(self, judgement: Judgement) -> None:
        self.counts[judgement.verdict] = self.counts.get(judgement.verdict, 0) + 1
        self.judgements.append(judgement)

    def Describe(self) -> str:
        parts = [f"{self.counts[verdict]} {verdict}" for verdict in VERDICTS
                 if self.counts.get(verdict)]
        return (f"{self.total} scenario(s) in {self.elapsed:.2f}s: "
                f"{', '.join(parts) or 'nothing'} "
                f"(disagreement rate {self.disagreement_rate:.1%})")


################################################################################
# Judging

def Judge(result: CaseResult) -> Judgement:
    verdict = Verdict(result)
    reason = BuildReason(result, verdict)
    return Judgement(case=result.case, verdict=verdict, reason=reason, result=result)


def BuildReason(result: CaseResult, verdict: str) -> str:
    if verdict == VERDICT_PASS:
        return ""

    lines: List[str] = []
    if result.message:
        lines.append(result.message)
    for failure in result.Failures():
        lines.append(failure.Describe())
    return "\n".join(lines) or "no reason recorded"


def Validate(cases: Sequence[SpecCase], *, max_decisions: int = 200) -> Summary:
    summary = Summary()
    start = time.perf_counter()
    for case in cases:
        summary.Add(Judge(RunCase(case, max_decisions=max_decisions)))
    summary.elapsed = time.perf_counter() - start
    return summary


################################################################################
# Manifests

def ReadManifest(path: str) -> Dict[str, Any]:
    if not os.path.exists(path):
        return {"version": MANIFEST_VERSION, "scenarios": {}}
    with open(path, "r", encoding="utf-8") as handle:
        data = json.load(handle)
    if not isinstance(data, dict) or "scenarios" not in data:
        raise SpecCaseError(f"{path}: not a spec manifest")
    return data


def WriteManifest(path: str, scenarios: Dict[str, Any], *, note: str) -> None:
    directory = os.path.dirname(path)
    if directory:
        os.makedirs(directory, exist_ok=True)
    payload = {
        "version": MANIFEST_VERSION,
        "note": note,
        "scenarios": dict(sorted(scenarios.items())),
    }
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, indent=2, sort_keys=True)
        handle.write("\n")


TRUSTED_NOTE = ("Written only by tools/spec/validate.py, only from PASS verdicts. "
                "Each entry is pinned to the hash of the scenario source it was "
                "validated against: edit the scenario and it leaves this file on "
                "the next run. Do not hand-edit.")

QUARANTINE_NOTE = ("Scenarios that did not pass against the Python engine. They "
                   "are not part of the trusted suite. Each one is either a spec "
                   "bug or an engine bug; both are worth finding.")


def BuildManifests(summary: Summary) -> Tuple[Dict[str, Any], Dict[str, Any]]:
    trusted: Dict[str, Any] = {}
    quarantine: Dict[str, Any] = {}

    for judgement in summary.judgements:
        entry = {
            "source": judgement.case.source_path,
            "sha256": judgement.case.source_sha256,
        }
        if judgement.trusted:
            trusted[judgement.case.case_id] = entry
        else:
            quarantine[judgement.case.case_id] = dict(
                entry, verdict=judgement.verdict, reason=FirstLine(judgement.reason))

    return trusted, quarantine


def FirstLine(text: str, limit: int = 300) -> str:
    line = " ".join(text.split())
    return line[:limit] + ("..." if len(line) > limit else "")


################################################################################
# Trusted suite

@dataclass
class TrustedSelection:
    cases: List[SpecCase] = field(default_factory=list)
    stale: List[str] = field(default_factory=list)
    """Trusted entries whose scenario source has changed or gone."""
    untrusted: List[str] = field(default_factory=list)
    """Scenarios on disk that have never passed."""


def SelectTrusted(cases: Sequence[SpecCase], manifest: Dict[str, Any]) -> TrustedSelection:
    """Only the scenarios that passed, and only while their source is unchanged."""
    entries: Dict[str, Any] = manifest.get("scenarios", {})
    selection = TrustedSelection()
    seen: set = set()

    for case in cases:
        entry = entries.get(case.case_id)
        if entry is None:
            selection.untrusted.append(case.case_id)
            continue
        seen.add(case.case_id)
        if entry.get("sha256") != case.source_sha256:
            selection.stale.append(
                f"{case.case_id} (source changed since it was validated)")
            continue
        selection.cases.append(case)

    for case_id in entries:
        if case_id not in seen:
            selection.stale.append(f"{case_id} (no longer on disk)")

    return selection


################################################################################
# History

def AppendHistory(path: str, summary: Summary, *, label: str) -> Dict[str, Any]:
    """One line per run, so a rising disagreement rate is visible.

    The timestamp here is a report field, not a gameplay input -- the engine's
    determinism rules are about what can change a game's outcome, and nothing in
    this file feeds back into a run.
    """
    entry = {
        "timestamp": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "label": label,
        "total": summary.total,
        "counts": dict(summary.counts),
        "disagreement_rate": round(summary.disagreement_rate, 4),
    }
    directory = os.path.dirname(path)
    if directory:
        os.makedirs(directory, exist_ok=True)
    with open(path, "a", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(entry, sort_keys=True) + "\n")
    return entry


def ReadHistory(path: str) -> List[Dict[str, Any]]:
    if not os.path.exists(path):
        return []
    entries: List[Dict[str, Any]] = []
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if line:
                entries.append(json.loads(line))
    return entries


def CheckDrift(history: Sequence[Dict[str, Any]], rate: float, tolerance: float) -> str:
    """A rising disagreement rate means the authoring process is drifting."""
    if not history:
        return ""
    previous = history[-1]
    before = float(previous.get("disagreement_rate", 0.0))
    if rate > before + tolerance:
        return (f"disagreement rate rose from {before:.1%} to {rate:.1%} "
                f"(tolerance {tolerance:.1%})")
    return ""


################################################################################
# CLI

def main(argv: "List[str]|None" = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("paths", nargs="*", default=None,
                        help=f"scenario files or directories (default {DEFAULT_SCENARIOS})")
    parser.add_argument("--trusted-only", action="store_true",
                        help="run only scenarios already in the trusted suite")
    parser.add_argument("--trusted", default=DEFAULT_TRUSTED)
    parser.add_argument("--quarantine", default=DEFAULT_QUARANTINE)
    parser.add_argument("--history", default=DEFAULT_HISTORY)
    parser.add_argument("--triage", default="",
                        help="write a triage record per disagreement to this file")
    parser.add_argument("--no-write", action="store_true",
                        help="report only; do not update the manifests or history")
    parser.add_argument("--check-drift", type=float, default=-1.0, metavar="TOLERANCE",
                        help="fail if the disagreement rate rose by more than this "
                             "since the previous run (e.g. 0.05)")
    parser.add_argument("--max-decisions", type=int, default=200)
    args = parser.parse_args(argv)

    paths = args.paths or [DEFAULT_SCENARIOS]

    try:
        cases: List[SpecCase] = []
        for path in paths:
            cases.extend(LoadCases(path))
    except (SpecCaseError, OSError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2

    if not cases:
        print("error: no scenarios found", file=sys.stderr)
        return 2

    if args.trusted_only:
        return RunTrusted(cases, args)

    summary = Validate(cases, max_decisions=args.max_decisions)

    for judgement in summary.judgements:
        print(judgement.Describe())

    print()
    print(summary.Describe())

    trusted, quarantine = BuildManifests(summary)
    print(f"trusted suite: {len(trusted)} scenario(s); "
          f"quarantined: {len(quarantine)}")

    if args.triage:
        WriteTriage(args.triage, summary)
        print(f"triage queue: {args.triage}")

    exit_code = 0

    if not args.no_write:
        WriteManifest(args.trusted, trusted, note=TRUSTED_NOTE)
        WriteManifest(args.quarantine, quarantine, note=QUARANTINE_NOTE)
        history = ReadHistory(args.history)
        if args.check_drift >= 0:
            drift = CheckDrift(history, summary.disagreement_rate, args.check_drift)
            if drift:
                print(f"DRIFT: {drift}")
                exit_code = 1
        AppendHistory(args.history, summary, label="validate")

    return exit_code


def RunTrusted(cases: Sequence[SpecCase], args: Any) -> int:
    """The gate: every trusted scenario must still pass, on every run."""
    try:
        manifest = ReadManifest(args.trusted)
    except (SpecCaseError, OSError, json.JSONDecodeError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2

    selection = SelectTrusted(cases, manifest)

    for case_id in selection.stale:
        print(f"DROPPED  {case_id}")
    for case_id in selection.untrusted:
        print(f"UNTRUSTED {case_id}")

    if not selection.cases:
        print("\nthe trusted suite is empty -- run `python -m tools.spec.validate` first")
        return 1

    summary = Validate(selection.cases, max_decisions=args.max_decisions)
    for judgement in summary.judgements:
        print(judgement.Describe())

    print()
    print(summary.Describe())

    if summary.disagreements:
        print("a trusted scenario stopped passing: the engine changed, "
              "or the suite was wrong to trust it")
        return 1
    if selection.stale:
        print(f"{len(selection.stale)} scenario(s) dropped out of the trusted suite; "
              f"re-run `python -m tools.spec.validate` to re-validate them")
        return 1
    return 0


def WriteTriage(path: str, summary: Summary) -> None:
    records = [judgement.TriageRecord() for judgement in summary.judgements
               if not judgement.trusted]
    directory = os.path.dirname(path)
    if directory:
        os.makedirs(directory, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump({"count": len(records), "records": records},
                  handle, indent=2, sort_keys=True, default=str)
        handle.write("\n")


if __name__ == "__main__":
    raise SystemExit(main())
