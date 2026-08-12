"""Content-addressing a corpus, and what the hash deliberately does not cover.

From the freeze onward the corpus is immutable and every later phase validates
against it, so the question that matters is not "does it hash" but **what is
inside the identity and what is only recorded beside it**. See MARVEL-18.

The root hash covers the scene files and nothing else. That is a decision with a
measured reason: since MARVEL-34 a per-run `bot-manifest-*.json` records the
resolved config, which includes `bot_save_folder` -- an absolute path. Two
byte-identical corpora generated into different directories have different run
manifests, so hashing them would make the corpus's identity depend on where the
generator was standing. Provenance (Python version, platform, freeze date) is
fenced out for the same reason, one step further: it is exactly what cannot be
reproduced.
"""

import gzip
import json
import os
import shutil
import tempfile
import unittest

from tools.corpus import freeze


def Scene(name="Rhino", inputs=1):
    return {
        "version": "0.5.9.205",
        "players": [{"hero": "spider_man"}],
        "campaign": {"name": name},
        "inputs": [{"digest": "abcd1234"}] * inputs,
    }


class Corpus:
    """A corpus tree on disk, built the way `tools/corpus/generate.py` builds one."""

    def __init__(self, root):
        self.root = root

    def Scene(self, folder, name, document=None):
        target = os.path.join(self.root, folder)
        os.makedirs(target, exist_ok=True)
        path = os.path.join(target, name)
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(document if document != None else Scene(), handle)
        return path

    def Raw(self, folder, name, document):
        target = os.path.join(self.root, folder)
        os.makedirs(target, exist_ok=True)
        path = os.path.join(target, name)
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(document, handle)
        return path


class Base(unittest.TestCase):

    def setUp(self):
        self.root = tempfile.mkdtemp(prefix="freeze-corpus-")
        self.addCleanup(shutil.rmtree, self.root, True)
        # A separate tree for everything a test *writes*. Not `self.root/..`:
        # that resolves to the shared system temp directory, so every test in
        # the file wrote its shards and manifests to one folder and read each
        # other's leftovers.
        self.out = tempfile.mkdtemp(prefix="freeze-out-")
        self.addCleanup(shutil.rmtree, self.out, True)
        self.corpus = Corpus(self.root)

    def Out(self, *parts):
        return os.path.join(self.out, *parts)


class TestWhatCounts(Base):

    def test_a_scene_is_collected(self):
        self.corpus.Scene("00000-rhino-1", "a.json")

        entries = freeze.Collect(self.root)

        self.assertEqual([entry.path for entry in entries],
                         ["00000-rhino-1/a.json"])

    def test_run_artefacts_are_not_scenes(self):
        self.corpus.Scene("00000-rhino-1", "a.json")
        self.corpus.Raw("00000-rhino-1", "bot-manifest-rhino-1.json", {"generator": "bot"})
        self.corpus.Raw("00000-rhino-1", "bot-coverage-rhino-1.json", {"games": []})
        self.corpus.Raw(".", "corpus-manifest.json", {"tool": "corpus-generate"})

        self.assertEqual(len(freeze.Collect(self.root)), 1)

    def test_a_json_file_that_is_not_a_scene_is_skipped(self):
        # Every field of `Scene` has a default, so loading a manifest as one
        # *succeeds* and yields an empty game. The filter has to be on content.
        self.corpus.Raw("00000-rhino-1", "notes.json", {"anything": 1})

        self.assertEqual(freeze.Collect(self.root), [])

    def test_the_tree_is_walked_to_the_bottom(self):
        self.corpus.Scene("00000-rhino-1", "a.json")
        self.corpus.Scene("00001-klaw-5", "b.json")

        self.assertEqual(len(freeze.Collect(self.root)), 2)

    def test_entries_are_sorted_so_the_root_hash_is_stable(self):
        self.corpus.Scene("z", "z.json")
        self.corpus.Scene("a", "a.json")

        self.assertEqual([entry.path for entry in freeze.Collect(self.root)],
                         ["a/a.json", "z/z.json"])


class TestTheRootHash(Base):

    def Hash(self):
        return freeze.RootHash(freeze.Collect(self.root))

    def test_the_same_corpus_hashes_the_same(self):
        self.corpus.Scene("00000-rhino-1", "a.json")

        self.assertEqual(self.Hash(), self.Hash())

    def test_changing_a_scene_changes_it(self):
        path = self.corpus.Scene("00000-rhino-1", "a.json")
        before = self.Hash()
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(Scene(inputs=2), handle)

        self.assertNotEqual(self.Hash(), before)

    def test_renaming_a_scene_changes_it(self):
        # The path is part of the corpus. A hash over content alone could not
        # tell a corpus with two identical scenes from one with a single scene.
        path = self.corpus.Scene("00000-rhino-1", "a.json")
        before = self.Hash()
        os.rename(path, os.path.join(os.path.dirname(path), "b.json"))

        self.assertNotEqual(self.Hash(), before)

    def test_a_run_artefact_changing_does_not(self):
        # The measured reason: `bot-manifest-*.json` records the resolved
        # config, which includes `bot_save_folder`. Two byte-identical corpora
        # in different directories differ there, and the identity of a corpus
        # must not depend on where the generator was standing.
        self.corpus.Scene("00000-rhino-1", "a.json")
        before = self.Hash()
        self.corpus.Raw("00000-rhino-1", "bot-manifest-rhino-1.json",
                        {"config": {"values": {"bot_save_folder": "/somewhere/else/"}}})

        self.assertEqual(self.Hash(), before)


class TestProvenanceIsOutsideTheHash(Base):

    def test_two_freezes_agree_on_everything_but_provenance(self):
        self.corpus.Scene("00000-rhino-1", "a.json")

        first = freeze.Build(self.root, dated="2026-01-01")
        second = freeze.Build(self.root, dated="2026-12-31")

        self.assertEqual(first["root_sha256"], second["root_sha256"])
        del first["provenance"], second["provenance"]
        self.assertEqual(first, second)

    def test_the_freeze_date_is_recorded(self):
        self.corpus.Scene("00000-rhino-1", "a.json")

        manifest = freeze.Build(self.root, dated="2026-08-12")

        self.assertEqual(manifest["provenance"]["frozen"], "2026-08-12")

    def test_the_engine_commit_is_recorded(self):
        self.corpus.Scene("00000-rhino-1", "a.json")

        sha = freeze.Build(self.root)["provenance"]["engine_git_sha"]

        # None outside a checkout is a legitimate answer, so pin the shape.
        if sha != None:
            self.assertEqual(len(sha), 40)


class TestQuarantineIsExcluded(Base):
    """A non-reproducing replay must not enter the corpus, or vanish."""

    def Quarantine(self, *files):
        folder = self.Out("quarantine")
        os.makedirs(folder, exist_ok=True)
        with open(os.path.join(folder, "quarantine.json"), "w",
                  encoding="utf-8") as handle:
            json.dump({"cases": [{"file": name} for name in files]}, handle)
        return folder

    def test_a_quarantined_scene_is_left_out(self):
        self.corpus.Scene("00000-rhino-1", "good.json")
        self.corpus.Scene("00001-rhino-2", "bad.json")
        folder = self.Quarantine("bad.json")

        manifest = freeze.Build(self.root, quarantine=folder)

        self.assertEqual(manifest["scenes"], 1)
        self.assertEqual(manifest["excluded"]["quarantined"], ["bad.json"])

    def test_an_empty_quarantine_excludes_nothing(self):
        self.corpus.Scene("00000-rhino-1", "good.json")
        folder = self.Quarantine()

        self.assertEqual(freeze.Build(self.root, quarantine=folder)["scenes"], 1)

    def test_no_quarantine_file_is_not_an_error(self):
        self.corpus.Scene("00000-rhino-1", "good.json")

        self.assertEqual(freeze.Quarantined(self.root), [])


class TestCheck(Base):

    def Freeze(self):
        path = self.Out("manifest.json")
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(freeze.Build(self.root), handle)
        return path

    def test_an_untouched_corpus_matches(self):
        self.corpus.Scene("00000-rhino-1", "a.json")

        self.assertEqual(freeze.Check(self.root, self.Freeze()), [])

    def test_a_changed_scene_is_reported_by_name(self):
        path = self.corpus.Scene("00000-rhino-1", "a.json")
        manifest = self.Freeze()
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(Scene(inputs=9), handle)

        problems = freeze.Check(self.root, manifest)

        self.assertTrue(any("changed: 00000-rhino-1/a.json" in p for p in problems))
        self.assertTrue(any("root hash" in p for p in problems))

    def test_a_deleted_scene_is_reported(self):
        path = self.corpus.Scene("00000-rhino-1", "a.json")
        manifest = self.Freeze()
        os.remove(path)

        self.assertTrue(any(p.startswith("missing:")
                            for p in freeze.Check(self.root, manifest)))

    def test_an_added_scene_is_reported(self):
        self.corpus.Scene("00000-rhino-1", "a.json")
        manifest = self.Freeze()
        self.corpus.Scene("00001-rhino-2", "b.json")

        self.assertTrue(any(p.startswith("added:")
                            for p in freeze.Check(self.root, manifest)))

    def test_every_problem_is_reported_not_just_the_first(self):
        # "one file changed" and "everything changed" want different responses.
        self.corpus.Scene("00000-rhino-1", "a.json")
        self.corpus.Scene("00001-rhino-2", "b.json")
        manifest = self.Freeze()
        os.remove(os.path.join(self.root, "00000-rhino-1", "a.json"))
        os.remove(os.path.join(self.root, "00001-rhino-2", "b.json"))

        problems = freeze.Check(self.root, manifest)

        self.assertEqual(len([p for p in problems if p.startswith("missing:")]), 2)

    def test_an_unreadable_manifest_is_reported_rather_than_raised(self):
        self.assertTrue(freeze.Check(self.root, "/nonexistent/manifest.json"))

    def test_a_manifest_from_a_newer_tool_is_refused(self):
        path = self.Out("future.json")
        with open(path, "w", encoding="utf-8") as handle:
            json.dump({"manifest_version": freeze.MANIFEST_VERSION + 1}, handle)

        problems = freeze.Check(self.root, path)

        self.assertEqual(len(problems), 1)
        self.assertIn("manifest version", problems[0])


class TestShards(Base):
    """MARVEL-4: gzipped, one file per scenario, so CI can fetch a subset."""

    def test_one_shard_per_scenario(self):
        self.corpus.Scene("00000-rhino-1", "a.json", Scene(name="Rhino"))
        self.corpus.Scene("00001-klaw-2", "b.json", Scene(name="Klaw"))

        manifest = freeze.Build(self.root)

        self.assertEqual(sorted(manifest["shards"]), ["Klaw", "Rhino"])

    def test_the_scenario_comes_from_the_scene_not_its_filename(self):
        # A shard boundary parsed out of a filename would move the first time
        # the naming changed.
        self.corpus.Scene("00000-misleading-1", "not-a-scenario-name.json",
                          Scene(name="Ultron"))

        self.assertEqual(list(freeze.Build(self.root)["shards"]), ["Ultron"])

    def test_shards_rebuild_byte_identically(self):
        # A gzip header carries a timestamp. A shard that changed only because
        # it was rebuilt on Tuesday is not an immutable artefact.
        self.corpus.Scene("00000-rhino-1", "a.json")
        manifest = freeze.Build(self.root)
        first = self.Out("shards-a")
        second = self.Out("shards-b")

        freeze.Shard(self.root, manifest, first)
        freeze.Shard(self.root, manifest, second)

        with open(os.path.join(first, "rhino.json.gz"), "rb") as handle:
            a = handle.read()
        with open(os.path.join(second, "rhino.json.gz"), "rb") as handle:
            b = handle.read()
        self.assertEqual(a, b)

    def test_a_shard_holds_the_scenes_keyed_by_corpus_path(self):
        self.corpus.Scene("00000-rhino-1", "a.json")
        manifest = freeze.Build(self.root)
        out = self.Out("shards")

        freeze.Shard(self.root, manifest, out)

        with gzip.open(os.path.join(out, "rhino.json.gz"), "rt",
                       encoding="utf-8") as handle:
            bundle = json.load(handle)
        self.assertEqual(list(bundle), ["00000-rhino-1/a.json"])
        self.assertEqual(bundle["00000-rhino-1/a.json"]["campaign"]["name"], "Rhino")

    def test_a_scenario_name_with_awkward_characters_still_makes_a_filename(self):
        self.corpus.Scene("00000-x-1", "a.json", Scene(name="Mutagen Formula!"))
        manifest = freeze.Build(self.root)
        out = self.Out("shards")

        freeze.Shard(self.root, manifest, out)

        self.assertEqual(os.listdir(out), ["mutagen_formula_.json.gz"])


class TestArtefactsAreRecorded(Base):

    def test_the_coverage_report_is_hashed_beside_the_scenes(self):
        # MARVEL-18 asks for it as a first-class artefact: future phases need to
        # know what the corpus does *not* cover.
        self.corpus.Scene("00000-rhino-1", "a.json")
        self.corpus.Raw("00000-rhino-1", "bot-coverage-rhino-1.json",
                        {"games": [], "never_exercised_cards": []})

        artefacts = freeze.Build(self.root)["artefacts"]

        self.assertIn("00000-rhino-1/bot-coverage-rhino-1.json", artefacts)
        self.assertEqual(len(artefacts["00000-rhino-1/bot-coverage-rhino-1.json"]), 64)


if __name__ == "__main__":
    unittest.main()
