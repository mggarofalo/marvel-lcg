"""The committed pack-rules snapshot is internally consistent.

Same footing as `test_rules_index.py` and for the same reason: the source is
61 copyrighted PDFs that are not in this repository, so there is no
regenerate-and-compare gate and these tests carry the whole weight.

They also pin the *shape* the corpus is supposed to have — one self-contained
rule per record, references flowing one way — because that shape is the thing
that makes it usable and it is easy to lose in a parser change.
"""

from __future__ import annotations

import json
import os
import re
import unittest

ROOT = os.path.join("..", "datasets", "rules-packs")
INDEX = os.path.join(ROOT, "index.json")
RR_INDEX = os.path.join("..", "datasets", "rules-reference", "index.json")

PUA = re.compile(r'[-]')


class TestPackRules(unittest.TestCase):

    def setUp(self):
        if not os.path.exists(INDEX):
            self.skipTest(f"{ROOT} is not present")
        with open(INDEX, encoding="utf-8") as handle:
            self.index = json.load(handle)
        self.records = self.index["entries"]

    def test_counts_are_self_consistent(self):
        self.assertEqual(self.index["record_count"], len(self.records))
        self.assertEqual(len(self.index["packs"]), self.index["documents"])

    def test_ids_are_unique(self):
        ids = [r["id"] for r in self.records]
        self.assertEqual(len(ids), len(set(ids)))

    def test_every_id_is_in_the_pack_tier(self):
        for record in self.records:
            with self.subTest(record["id"]):
                self.assertTrue(record["id"].startswith("pack:"))

    def test_every_record_has_a_document_on_disk(self):
        for record in self.records:
            _, pack, rest = record["id"].split(":", 2)
            section = rest.split(".", 1)[0]
            path = os.path.join(ROOT, pack, f"{section}.md")
            with self.subTest(record["id"]):
                self.assertTrue(os.path.exists(path), path)

    def test_named_rules_are_anchored_in_their_section(self):
        """`pack:mc02:new-rules.when-...` must resolve to a place in the file."""
        for record in self.records:
            _, pack, rest = record["id"].split(":", 2)
            if "." not in rest:
                continue
            section, anchor = rest.split(".", 1)
            with open(os.path.join(ROOT, pack, f"{section}.md"),
                      encoding="utf-8") as handle:
                text = handle.read()
            with self.subTest(record["id"]):
                self.assertIn(f'<a id="{anchor}"></a>', text)

    def test_every_record_carries_a_hash_and_a_fragment(self):
        for record in self.records:
            with self.subTest(record["id"]):
                self.assertTrue(record.get("hash", "").startswith("sha256:"))
                self.assertTrue(record.get("fragment"))

    def test_no_unresolved_glyphs(self):
        for record in self.records:
            with self.subTest(record["id"]):
                self.assertIsNone(PUA.search(json.dumps(record)))

    def test_credits_and_flavour_are_excluded(self):
        """The denylist did its job: no section is a credit list."""
        titles = {r["title"].upper() for r in self.records}
        for banned in ("CREDITS", "PLAYTESTERS", "S.H.I.E.L.D. BRIEFING",
                       "STRATEGY TIPS", "EXPANSION SYMBOL"):
            with self.subTest(banned):
                self.assertNotIn(banned, titles)

    def test_named_rules_were_actually_found(self):
        """A parser change that stops splitting on bold sub-headings.

        Without them a pack's whole `NEW RULES` section is one blob and every
        rule inside it stops being addressable, which is a silent regression:
        the section records all still exist and the counts barely move.
        """
        named = [r for r in self.records if "." in r["id"].split(":", 2)[2]]
        self.assertGreater(len(named), 250)


class TestReferenceGraph(unittest.TestCase):
    """References are one-way and acyclic, across both tiers."""

    def setUp(self):
        if not (os.path.exists(INDEX) and os.path.exists(RR_INDEX)):
            self.skipTest("rules corpus is not present")
        from tools.rules.refs import load
        self.rules = load(RR_INDEX, INDEX)

    def test_both_tiers_load_together(self):
        self.assertGreater(len(self.rules), 1500)
        self.assertTrue(any(k.startswith("rr:") for k in self.rules))
        self.assertTrue(any(k.startswith("pack:") for k in self.rules))

    def test_every_reference_names_a_rule_that_exists(self):
        for rule_id, record in self.rules.items():
            for target in record.get("references") or []:
                with self.subTest(rule=rule_id, target=target):
                    self.assertIn(target, self.rules)

    def test_the_graph_is_acyclic(self):
        """A cycle is two rules each claiming priority over the other."""
        from tools.rules.refs import cycles
        self.assertEqual(cycles(self.rules), [])

    def test_edges_live_only_in_the_authored_graph(self):
        """Generated indexes carry no reference field at all.

        The indexes are destroyed and rebuilt from the PDFs on every harvest.
        An authored edge stored inside one would be lost on the next refresh,
        silently and completely — so generated and authored data do not share a
        file, and this is the assertion that keeps them apart.
        """
        for path in (INDEX, RR_INDEX):
            with open(path, encoding="utf-8") as handle:
                for record in json.load(handle)["entries"]:
                    with self.subTest(path=path, rule=record["id"]):
                        self.assertNotIn("references", record)

    def test_authored_edges_resolve(self):
        graph = os.path.join("..", "datasets", "rules-graph.json")
        if not os.path.exists(graph):
            self.skipTest("no rules graph present")
        with open(graph, encoding="utf-8") as handle:
            edges = json.load(handle)["edges"]
        self.assertTrue(edges)
        for rule_id, edge in edges.items():
            with self.subTest(rule_id):
                self.assertIn(rule_id, self.rules)
                self.assertTrue(edge.get("why"),
                                "an edge without a reason cannot be reviewed")
                for target in edge["references"]:
                    self.assertIn(target, self.rules)

    def test_no_rule_references_itself(self):
        for rule_id, record in self.rules.items():
            with self.subTest(rule_id):
                self.assertNotIn(rule_id, record.get("references") or [])

    def test_the_reverse_index_is_not_stored(self):
        """Only `references` is authored; back-references are computed.

        A stored reverse index is the same relationship written twice, in two
        files, able to disagree — and once they disagree nothing can say which
        is right.
        """
        for rule_id, record in self.rules.items():
            with self.subTest(rule_id):
                self.assertNotIn("referenced_by", record)
                self.assertNotIn("overridden_by", record)


if __name__ == "__main__":
    unittest.main()
