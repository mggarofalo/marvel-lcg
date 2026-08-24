"""`tools.rules.coverage` counts what it claims to count.

The tool's whole value is that its numbers are trustworthy enough to plan
against, so the tests are about the counting rules rather than the formatting:
a citation counts only when the scenario is trusted *and still pinned*, an
entry is covered by a citation of any of its clauses, and a tag naming a rule
that does not exist is a typo rather than coverage.

Stub cases rather than the real spec suite, deliberately. These assertions
would otherwise change meaning every time somebody tags a scenario, which is
exactly the work this tool exists to encourage.
"""

from __future__ import annotations

import unittest
from dataclasses import dataclass, field
from typing import Tuple

from tools.rules.coverage import RulesCoverage


@dataclass
class StubCase:
    case_id: str
    tags: Tuple[str, ...] = ()
    source_path: str = "specs/rules/example.feature"
    source_sha256: str = "pinned"


INDEX = {
    "version": "1.8",
    "entries": [
        {"id": "rr:cost", "title": "COST", "page": 13, "fragment": "A cost."},
        {"id": "rr:cost.1", "title": "COST", "page": 13, "fragment": "One."},
        {"id": "rr:cost.2", "title": "COST", "page": 13, "fragment": "Two."},
        {"id": "rr:target", "title": "TARGET", "page": 42, "fragment": "A target."},
        {"id": "rr:atk", "title": "ATK", "page": 8, "redirect": "Basic Power"},
    ],
}


def trusted(*names: str) -> dict:
    return {name: {"sha256": "pinned", "source": "x"} for name in names}


class TestRulesCoverage(unittest.TestCase):

    def test_a_clause_citation_covers_its_entry(self):
        """`@rr:cost.1` is a claim about Cost, so Cost is no longer uncited."""
        case = StubCase("f :: a", ("rr:cost.1",))
        coverage = RulesCoverage(INDEX, [case], trusted("f :: a"), [])
        self.assertTrue(coverage.Covered("rr:cost"))
        self.assertFalse(coverage.Covered("rr:target"))
        self.assertEqual([row["id"] for row in coverage.Uncited()], ["rr:target"])

    def test_redirect_entries_are_not_counted_as_work(self):
        """`ATK -- See: Basic Power` has no text to assert anything about."""
        coverage = RulesCoverage(INDEX, [], {}, [])
        self.assertNotIn("rr:atk", [row["id"] for row in coverage.Uncited()])
        self.assertEqual(coverage.Summary()["entries"], 2)

    def test_an_untrusted_citation_does_not_count(self):
        case = StubCase("f :: a", ("rr:cost.1",))
        coverage = RulesCoverage(INDEX, [case], {}, [])
        self.assertFalse(coverage.Covered("rr:cost"))

    def test_a_quarantined_citation_is_a_suspected_engine_error(self):
        """A failing claim with a citation attached, which is the useful kind."""
        case = StubCase("f :: a", ("rr:cost.1",))
        coverage = RulesCoverage(INDEX, [case], {}, ["f :: a"])
        self.assertFalse(coverage.Covered("rr:cost"))
        self.assertEqual(coverage.suspect["rr:cost.1"], ["f :: a"])
        self.assertEqual(coverage.Summary()["suspect_entries"], 1)

    def test_a_stale_pin_does_not_count_as_coverage(self):
        """Editing a scenario drops its citations until it is re-validated."""
        case = StubCase("f :: a", ("rr:cost.1",), source_sha256="edited")
        coverage = RulesCoverage(INDEX, [case], trusted("f :: a"), [])
        self.assertFalse(coverage.Covered("rr:cost"))
        self.assertEqual(coverage.stale, ["f :: a"])
        self.assertEqual(coverage.Summary()["stale_pins"], 1)

    def test_an_unknown_rule_id_is_a_typo_not_coverage(self):
        case = StubCase("f :: a", ("rr:cosr.1",))
        coverage = RulesCoverage(INDEX, [case], trusted("f :: a"), [])
        self.assertEqual(coverage.unknown_tags["f :: a"], ["rr:cosr.1"])
        self.assertEqual(coverage.Summary()["entries_cited"], 0)

    def test_only_rules_specs_are_counted_as_ungrounded(self):
        """A card scenario's authority is printed text, not the rulebook."""
        rule = StubCase("f :: a", (), source_path="specs/rules/x.feature")
        card = StubCase("f :: b", (), source_path="specs/cards/core/01001.feature")
        coverage = RulesCoverage(INDEX, [rule, card], {}, [])
        self.assertEqual(coverage.ungrounded, ["f :: a"])

    def test_a_cited_rules_spec_is_not_ungrounded(self):
        case = StubCase("f :: a", ("rr:cost.1",))
        coverage = RulesCoverage(INDEX, [case], trusted("f :: a"), [])
        self.assertEqual(coverage.ungrounded, [])

    def test_card_tags_are_not_read_as_rule_citations(self):
        case = StubCase("f :: a", ("card:01043a",))
        coverage = RulesCoverage(INDEX, [case], trusted("f :: a"), [])
        self.assertEqual(coverage.Summary()["entries_cited"], 0)
        self.assertEqual(coverage.unknown_tags, {})


class TestAgainstTheCommittedIndex(unittest.TestCase):
    """One integration check: the tool runs against the real snapshot."""

    def test_builds_and_reports(self):
        import os
        from tools.rules.coverage import RULES_INDEX, build
        if not os.path.exists(RULES_INDEX):
            self.skipTest("no rules index present")
        summary = build().Summary()
        self.assertEqual(summary["rules_reference_version"], "1.8")
        self.assertGreater(summary["entries"], 200)
        self.assertEqual(summary["unknown_tags"], 0)
        self.assertEqual(summary["stale_pins"], 0)

    def test_no_rules_scenario_is_ungrounded(self):
        """A ratchet, not a milestone.

        Every scenario under `specs/rules/` cites a rule or says `@rr:none`,
        and the point of pinning it here is the *next* one: a rules spec
        authored without a citation is a claim about the rulebook grounded in
        nothing but the engine's own behaviour, which is the failure mode this
        whole dataset exists to close. Cheaper to catch when it is written than
        to retrofit later -- see the sequencing note in
        `docs/rules-provenance.md`.
        """
        import os
        from tools.rules.coverage import RULES_INDEX, build
        if not os.path.exists(RULES_INDEX):
            self.skipTest("no rules index present")
        coverage = build()
        self.assertEqual(coverage.ungrounded, [])


if __name__ == "__main__":
    unittest.main()
