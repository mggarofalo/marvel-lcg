"""The committed rules index is internally consistent.

`tools.rules.harvest` cannot run in CI: it reads a 31 MB copyrighted PDF that
is not in this repository and is not going to be. So unlike every other
fixture here there is no regenerate-and-compare gate, and these tests carry the
whole weight of "the snapshot is not quietly broken".

They are written against the *artefacts* rather than the parser, deliberately.
Every defect they check for is one the parser actually shipped at some point
during MARVEL-154 -- entries absorbed into their neighbours, cross-references
dangling because a compound title was cited by half its name, icon glyphs left
unresolved as private-use characters, `See also` lists split inside a name.
A parser rewrite that reintroduces any of them fails here.
"""

from __future__ import annotations

import json
import os
import re
import unittest

ROOT = os.path.join("..", "datasets", "rules-reference")
ENTRIES = os.path.join(ROOT, "entries")

# Private use area. Nothing here should reach the committed text: every glyph
# the document uses is an icon, and every icon resolves through icons.json.
PUA = re.compile(r'[-]')

FRONT_MATTER = re.compile(r'\A---\n(.*?)\n---\n', re.S)


def load_index() -> dict:
    with open(os.path.join(ROOT, "index.json"), encoding="utf-8") as handle:
        return json.load(handle)


def entry_files() -> list:
    return sorted(name for name in os.listdir(ENTRIES) if name.endswith(".md"))


def read(name: str) -> str:
    with open(os.path.join(ENTRIES, name), encoding="utf-8") as handle:
        return handle.read()


def parse_front_matter(text: str) -> dict:
    """The deliberately small YAML subset `harvest` emits: scalars and flat lists."""
    match = FRONT_MATTER.match(text)
    if match is None:
        return {}
    fields = {}
    for line in match.group(1).split("\n"):
        key, _, value = line.partition(":")
        value = value.strip()
        if value.startswith("["):
            fields[key.strip()] = json.loads(value)
        elif value.startswith('"'):
            fields[key.strip()] = json.loads(value)
        else:
            fields[key.strip()] = value
    return fields


class TestRulesIndex(unittest.TestCase):

    def setUp(self):
        if not os.path.isdir(ENTRIES):
            self.skipTest(f"{ROOT} is not present")
        self.index = load_index()
        self.records = self.index["entries"]
        self.top = [r for r in self.records if r["id"].count(".") == 0]

    def test_index_and_entry_files_agree(self):
        """Every top-level record has a document, and every document a record."""
        from_index = {r["id"].split(":", 1)[1] for r in self.top}
        from_disk = {name[:-3] for name in entry_files()}
        self.assertEqual(from_index, from_disk)

    def test_counts_are_self_consistent(self):
        self.assertEqual(self.index["entry_count"], len(self.top))
        self.assertEqual(self.index["record_count"], len(self.records))

    def test_ids_are_unique(self):
        ids = [r["id"] for r in self.records]
        self.assertEqual(len(ids), len(set(ids)))

    def test_every_citable_record_carries_a_hash_and_a_fragment(self):
        """A record without both cannot be cited or diffed, which is the point."""
        for record in self.records:
            if record.get("redirect"):
                continue
            with self.subTest(record["id"]):
                self.assertTrue(record.get("hash", "").startswith("sha256:"))
                self.assertTrue(record.get("fragment"))

    def test_no_unresolved_icon_glyphs(self):
        """Private-use characters mean the legend missed one."""
        for name in entry_files():
            with self.subTest(name):
                self.assertIsNone(PUA.search(read(name)))
        self.assertIsNone(PUA.search(json.dumps(self.records)))

    def test_icon_tokens_are_all_known(self):
        known = set(load_index()["icons"].values())
        for name in entry_files():
            for token in re.findall(r'\[([a-z-]+)\]', read(name)):
                # Markdown links are `[Title](target.md)`; icons are bare.
                if token in known:
                    continue
                with self.subTest(name=name, token=token):
                    self.assertNotIn(token, known | {"icon"})

    def test_cross_references_resolve(self):
        """A `See also` that points nowhere is the failure mode aliasing fixed."""
        known = {r["id"] for r in self.top}
        for record in self.top:
            for reference in record.get("see_also", []):
                with self.subTest(entry=record["id"], reference=reference):
                    self.assertIn(reference, known)

    def test_only_appendices_are_unresolved(self):
        """The `rr:` tier covers the glossary; appendix references are expected.

        Pinned so that a parser change which starts dropping ordinary
        cross-references shows up here rather than as a quietly smaller corpus.
        """
        for record in self.top:
            for name in record.get("see_also_unresolved", []):
                with self.subTest(entry=record["id"], reference=name):
                    self.assertTrue(name.startswith("Appendix"), name)

    def test_markdown_links_point_at_existing_documents(self):
        known = {name[:-3] for name in entry_files()}
        for name in entry_files():
            for target in re.findall(r'\]\(([^)]+)\.md\)', read(name)):
                with self.subTest(name=name, target=target):
                    self.assertIn(target, known)

    def test_clause_anchors_exist_for_every_clause_record(self):
        """`rr:cost.4` must resolve to a place in `cost.md`, or it is not a citation."""
        for record in self.records:
            if record["id"].count(".") == 0:
                continue
            entry_id, _, suffix = record["id"].partition(".")
            slug = entry_id.split(":", 1)[1]
            anchor = f'<a id="{slug}-{suffix.replace(".", "-")}"></a>'
            with self.subTest(record["id"]):
                self.assertIn(anchor, read(f"{slug}.md"))

    def test_front_matter_matches_the_index(self):
        by_id = {r["id"]: r for r in self.top}
        for name in entry_files():
            fields = parse_front_matter(read(name))
            with self.subTest(name):
                self.assertIn("id", fields)
                record = by_id[fields["id"]]
                self.assertEqual(fields["title"], record["title"])
                self.assertEqual(int(fields["page"]), record["page"])
                if "hash" in fields:
                    self.assertEqual(fields["hash"], record["hash"])

    def test_entries_are_not_empty(self):
        """An entry with no text is one that was absorbed into its neighbour."""
        for record in self.top:
            if record.get("redirect"):
                continue
            with self.subTest(record["id"]):
                self.assertGreater(len(record["fragment"]), 10)

    def test_the_generation_versus_payment_rule_is_citable(self):
        """The rule MARVEL-169 turns on, pinned as a worked example.

        This is the case that motivated the whole dataset: the engine models
        resource generation and cost payment as one step, and nothing in the
        repository could say otherwise. Now something can.
        """
        by_id = {r["id"]: r for r in self.records}
        self.assertIn("rr:cost", by_id)
        cost = [r for r in self.records if r["id"].startswith("rr:cost.")]
        generation = [r for r in cost
                      if "generate" in r["fragment"] and "spends" in r["fragment"]]
        self.assertTrue(generation,
                        "no Cost clause states that paying spends generated resources")


if __name__ == "__main__":
    unittest.main()
