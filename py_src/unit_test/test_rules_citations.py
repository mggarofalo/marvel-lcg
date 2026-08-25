"""`tools.rules.citations` reads what the C# tests actually claim.

The parser is a regex over C# source, so the tests that matter are the shapes
it has to survive: several `[Rule]` above one method, a citation on a class
rather than a method, and an id it does not recognise. The last one is the
important case -- an unknown id must reach the report, not be dropped, because
the whole point of the number is that it cannot be improved by a typo.
"""

from __future__ import annotations

import os
import tempfile
import unittest

from tools.rules.citations import Coverage, Read, SourceFiles

INDEX = {
    "version": "1.8",
    "entries": [
        {"id": "rr:forced", "title": "FORCED", "page": 20},
        {"id": "rr:forced.4", "title": "FORCED", "page": 20},
        {"id": "rr:target", "title": "TARGET", "page": 42},
        {"id": "rr:target.1", "title": "TARGET", "page": 42},
        {"id": "rr:you-your", "title": "YOU, YOUR", "page": 49},
    ],
}

SOURCE = '''using Xunit;

namespace Marvel.Rules.Tests;

public sealed class ForcedTests
{
    [Rule("rr:forced.4")]
    [Rule("rr:target.1")]
    [Theory]
    [InlineData(1)]
    public void ForcedInterruptsGoFirst(int n)
    {
        Assert.Equal(n, n);
    }

    [Rule("rr:nonexistent.9")]
    [Fact]
    public void SomethingElse()
    {
    }
}
'''


class ReadTest(unittest.TestCase):

    def _Read(self, text: str):
        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory, "Sample.cs")
            with open(path, "w", encoding="utf-8", newline="\n") as handle:
                handle.write(text)
            return Read(path)

    def test_binds_each_citation_to_the_test_below_it(self):
        found = self._Read(SOURCE)
        self.assertEqual(
            [(c.rule_id, c.site) for c in found],
            [("rr:forced.4", "ForcedInterruptsGoFirst"),
             ("rr:target.1", "ForcedInterruptsGoFirst"),
             ("rr:nonexistent.9", "SomethingElse")])

    def test_reports_the_line_a_reader_would_open(self):
        found = self._Read(SOURCE)
        self.assertEqual(found[0].line, 7)

    def test_a_citation_on_a_class_is_attributed_to_the_file(self):
        text = '[Rule("rr:forced")]\npublic sealed class Whole { }\n'
        found = self._Read(text)
        self.assertEqual([(c.rule_id, c.site) for c in found],
                         [("rr:forced", "Sample")])


class CoverageTest(unittest.TestCase):

    def _Coverage(self, source: str = SOURCE) -> Coverage:
        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory, "Sample.cs")
            with open(path, "w", encoding="utf-8", newline="\n") as handle:
                handle.write(source)
            return Coverage(INDEX, Read(path))

    def test_an_unknown_id_is_reported_and_not_counted(self):
        coverage = self._Coverage()
        self.assertEqual([c.rule_id for c in coverage.unknown],
                         ["rr:nonexistent.9"])
        self.assertNotIn("rr:nonexistent.9", coverage.cited)

    def test_a_clause_citation_covers_its_entry(self):
        # `rr:forced.4` is cited and `rr:forced` is not, but the entry counts:
        # somebody has made a claim about FORCED.
        coverage = self._Coverage()
        self.assertTrue(coverage.Covered("rr:forced"))
        self.assertTrue(coverage.Covered("rr:target"))
        self.assertFalse(coverage.Covered("rr:you-your"))

    def test_uncited_lists_the_entry_nothing_claims(self):
        rows = self._Coverage().Uncited()
        self.assertEqual([row["id"] for row in rows], ["rr:you-your"])

    def test_summary_counts_entries_and_records_separately(self):
        summary = self._Coverage().Summary()
        self.assertEqual(summary["entries"], 3)
        self.assertEqual(summary["entries_cited"], 2)
        self.assertEqual(summary["records_cited"], 2)
        self.assertEqual(summary["citations"], 2)


class RepositoryTest(unittest.TestCase):
    """The real tree, not a fixture."""

    def test_every_citation_in_the_repository_names_a_real_rule(self):
        # The same claim `RuleCitationTests.EveryCitedRuleExists` makes in the
        # C# suite. Duplicated here so that a citation cannot rot in a branch
        # where nobody has a .NET SDK.
        from tools.rules.citations import Build
        coverage = Build()
        self.assertEqual(
            [(c.site, c.rule_id) for c in coverage.unknown], [],
            "citations naming no rule in the Rules Reference")

    def test_the_test_tree_is_found(self):
        self.assertTrue(SourceFiles(), "no C# test sources found")


if __name__ == "__main__":
    unittest.main()
