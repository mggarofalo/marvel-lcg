"""Published shards round-trip to the exact bytes the manifest describes.

The shards are the **only** published artefact — `mggarofalo/marvel-lcg-corpus`
holds them and nothing else — so "can they be expanded, and is what comes out
the corpus that was frozen" is the whole question about whether the oracle is
usable by anybody.

Both halves of that were broken when `tools.corpus.expand` was written, and
both failures were quiet:

  * `Shard` stored each scene's *parsed document*. The manifest hashes bytes on
    disk, and scenes are not written as canonical JSON, so re-serialising lost
    ~0.3% of each scene's length to separator and key-order differences. Every
    expanded scene would have failed its hash, looking like corruption rather
    than like a lossy format.
  * `RootHash` hashes `path\\tsha256\\n` lines *in the order given*, and
    `Collect` sorts before hashing while expansion walks shards. Every scene
    matched individually and the root hash did not.
"""

from __future__ import annotations

import json
import os
import shutil
import tempfile
import unittest

from tools.corpus import freeze
from tools.corpus.expand import expand, read_shard


SCENE = {
    # `players` and `campaign` are what `freeze.IsScene` recognises a scene by.
    "players": [{"identity": "spider_man"}],
    "campaign": {"name": "Rhino"},
    "rules": [],
    # Key order and spacing here are deliberately *not* canonical: this is what
    # makes the round trip a real test rather than a tautology.
    "steps": [{"b": 2, "a": 1}],
}


class TestExpand(unittest.TestCase):

    def setUp(self):
        self.tmp = tempfile.mkdtemp()
        self.addCleanup(shutil.rmtree, self.tmp, ignore_errors=True)
        self.corpus = os.path.join(self.tmp, "corpus")
        os.makedirs(os.path.join(self.corpus, "case-1"))
        self.scene = os.path.join(self.corpus, "case-1", "scene.json")
        # Indented, insertion-ordered: not what `json.dumps(sort_keys=True,
        # separators=(",",":"))` would produce.
        with open(self.scene, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(SCENE, handle, indent=2)

        self.manifest_path = os.path.join(self.tmp, "manifest.json")
        manifest = freeze.Build(self.corpus)
        with open(self.manifest_path, "w", encoding="utf-8") as handle:
            json.dump(manifest, handle)
        self.manifest = manifest
        self.shards = os.path.join(self.tmp, "shards")
        freeze.Shard(self.corpus, manifest, self.shards)

    def test_the_scene_is_not_canonical_json(self):
        """Otherwise this whole test suite proves nothing."""
        with open(self.scene, "rb") as handle:
            raw = handle.read()
        canonical = json.dumps(SCENE, sort_keys=True,
                               separators=(",", ":")).encode("utf-8")
        self.assertNotEqual(raw, canonical)

    def test_a_shard_carries_text_not_a_document(self):
        shard = os.path.join(self.shards, "rhino.json.gz")
        for contents in read_shard(shard).values():
            self.assertIsInstance(contents, str)

    def test_expansion_reproduces_the_exact_bytes(self):
        out = os.path.join(self.tmp, "out")
        self.assertEqual(expand(self.shards, out,
                                manifest_path=self.manifest_path), 0)
        with open(self.scene, "rb") as handle:
            original = handle.read()
        with open(os.path.join(out, "case-1", "scene.json"), "rb") as handle:
            expanded = handle.read()
        self.assertEqual(original, expanded)

    def test_a_tampered_shard_is_rejected(self):
        """The gate has to be able to fail."""
        import gzip
        shard = os.path.join(self.shards, "rhino.json.gz")
        bundle = read_shard(shard)
        key = next(iter(bundle))
        bundle[key] = bundle[key].replace('"rules": []', '"rules": ["tampered"]')
        with gzip.GzipFile(shard, "wb", compresslevel=9, mtime=0) as handle:
            handle.write(json.dumps(bundle).encode("utf-8"))
        out = os.path.join(self.tmp, "tampered")
        self.assertEqual(expand(self.shards, out,
                                manifest_path=self.manifest_path), 1)

    def test_a_missing_scene_fails_the_root_hash(self):
        """Per-file hashes cannot notice an omission; the root hash must."""
        os.makedirs(os.path.join(self.corpus, "case-2"))
        second = os.path.join(self.corpus, "case-2", "scene.json")
        with open(second, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(SCENE, handle, indent=2)
        manifest = freeze.Build(self.corpus)
        with open(self.manifest_path, "w", encoding="utf-8") as handle:
            json.dump(manifest, handle)
        # Shards still describe only the first scene.
        out = os.path.join(self.tmp, "short")
        self.assertEqual(expand(self.shards, out,
                                manifest_path=self.manifest_path), 1)

    def test_expansion_without_a_manifest_still_works(self):
        out = os.path.join(self.tmp, "bare")
        self.assertEqual(expand(self.shards, out), 0)
        self.assertTrue(os.path.exists(
            os.path.join(out, "case-1", "scene.json")))


if __name__ == "__main__":
    unittest.main()
