"""What the five fixture `--check` gates call stale (`tools/fixtures.py`).

The gates regenerate a checked-in file and compare:

    python -m tools.rng.emit_vectors --check       datasets/rng/vectors.json
    python -m tools.digest.emit_vectors --check    datasets/digest/vectors.json
    python -m tools.digest.emit_escaping --check   datasets/digest/escaping.json
    python -m tools.events.emit_vocabulary --check datasets/events/vocabulary.json
    python -m tools.cards.extract --check          datasets/cards/*.json

Before MARVEL-73 they disagreed about what a difference was, and the
disagreement was invisible until a Windows clone with `core.autocrlf=true`
failed one of the three on files nobody had touched. So the question is asked
in one place now, and this is where that place is tested.

Every test here is a mutation: a file that ought to fail, failing. A gate is
only worth having if something specific gets past it and something specific
does not, and an accept-only test would pass against a `Compare` that returned
`FRESH` unconditionally -- which is exactly the shape of the bug being guarded
against.

No engine bootstrap: `tools/fixtures.py` and `tools/cards/` are stdlib-only.

    python -m unittest unit_test.test_fixture_staleness
"""

import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from tools import fixtures
from tools.cards import engine, extract

# A file shaped like the fixtures: JSON, LF line endings, trailing newline.
RENDERED = json.dumps({"cases": [1, 2, 3], "seed": 42}, indent=2,
                      sort_keys=True) + "\n"


def Write(path: Path, text: str, newline: str = "\n") -> Path:
    path.write_bytes(text.replace("\n", newline).encode("utf-8"))
    return path


class TestCompare(unittest.TestCase):
    """The one definition of "stale", exercised through each of its verdicts."""

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.root = Path(self._tmp.name)
        self.path = self.root / "vectors.json"

    def test_the_file_a_generator_wrote_is_fresh(self):
        Write(self.path, RENDERED)
        self.assertEqual(fixtures.Compare(RENDERED, self.path), fixtures.FRESH)

    def test_a_file_that_is_not_there_is_missing(self):
        self.assertEqual(fixtures.Compare(RENDERED, self.path), fixtures.MISSING)

    def test_a_changed_number_is_stale(self):
        """Mutation. The plainest kind of staleness there is."""
        Write(self.path, RENDERED.replace("42", "43"))
        self.assertEqual(fixtures.Compare(RENDERED, self.path), fixtures.STALE)

    def test_a_reordered_key_is_stale(self):
        """Mutation. Same parsed document, different file.

        This is the case that says the comparison is over bytes and not over
        `json.loads`. The fixtures are read by a C# port and reviewed as
        diffs, and both of those see key order.
        """
        reordered = json.dumps({"seed": 42, "cases": [1, 2, 3]}, indent=2) + "\n"
        self.assertEqual(json.loads(reordered), json.loads(RENDERED))
        Write(self.path, reordered)
        self.assertEqual(fixtures.Compare(RENDERED, self.path), fixtures.STALE)

    def test_a_reflowed_layout_is_stale(self):
        """Mutation. `cards.json` is one card per line on purpose."""
        Write(self.path, json.dumps(json.loads(RENDERED), sort_keys=True) + "\n")
        self.assertEqual(fixtures.Compare(RENDERED, self.path), fixtures.STALE)

    def test_a_missing_trailing_newline_is_stale(self):
        Write(self.path, RENDERED.rstrip("\n"))
        self.assertEqual(fixtures.Compare(RENDERED, self.path), fixtures.STALE)

    def test_crlf_line_endings_are_a_failure_of_their_own_kind(self):
        """The MARVEL-73 case: still a failure, reported as what it is.

        A verdict, not a pass. The repair is to re-normalise the checkout, and
        a gate that said "stale" here would send the contributor to regenerate
        a file that has nothing wrong with its content.
        """
        Write(self.path, RENDERED, newline="\r\n")
        self.assertEqual(fixtures.Compare(RENDERED, self.path),
                         fixtures.LINE_ENDINGS)
        self.assertNotEqual(fixtures.Compare(RENDERED, self.path),
                            fixtures.FRESH)

    def test_a_crlf_file_that_is_also_stale_is_reported_stale(self):
        """CRLF must never mask a real change. Content is decided first."""
        Write(self.path, RENDERED.replace("42", "43"), newline="\r\n")
        self.assertEqual(fixtures.Compare(RENDERED, self.path), fixtures.STALE)

    def test_every_verdict_has_a_summary_and_a_repair(self):
        for verdict in (fixtures.MISSING, fixtures.STALE, fixtures.LINE_ENDINGS):
            with self.subTest(verdict=verdict):
                self.assertIn(verdict, fixtures.SUMMARY)
                message = fixtures.Explain(verdict, self.path, "python -m tool")
                self.assertIn(str(self.path), message)
        # The CRLF message must not tell anyone to regenerate and stop there.
        crlf = fixtures.Explain(fixtures.LINE_ENDINGS, self.path, "python -m tool")
        self.assertIn("core.autocrlf", crlf)


class TestCardDatasetCheck(unittest.TestCase):
    """`extract.Check`, the gate `tools.cards.extract --check` runs."""

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.directory = Path(self._tmp.name)
        self.outputs = {extract.CARDS_FILE: RENDERED,
                        extract.SUMMARY_FILE: RENDERED}
        for name, content in self.outputs.items():
            Write(self.directory / name, content)

    def test_a_faithful_copy_is_not_stale(self):
        self.assertEqual(extract.Check(self.outputs, self.directory), [])

    def test_a_mutated_file_is_named_with_its_verdict(self):
        Write(self.directory / extract.SUMMARY_FILE, RENDERED.replace("42", "99"))
        self.assertEqual(extract.Check(self.outputs, self.directory),
                         [(extract.SUMMARY_FILE, fixtures.STALE)])

    def test_a_deleted_file_is_reported_missing(self):
        (self.directory / extract.CARDS_FILE).unlink()
        self.assertEqual(extract.Check(self.outputs, self.directory),
                         [(extract.CARDS_FILE, fixtures.MISSING)])

    def test_a_crlf_checkout_is_reported_as_line_endings(self):
        """The exact condition that failed on Windows and nowhere else."""
        for name, content in self.outputs.items():
            Write(self.directory / name, content, newline="\r\n")
        self.assertEqual(
            sorted(extract.Check(self.outputs, self.directory)),
            sorted((name, fixtures.LINE_ENDINGS) for name in self.outputs))


class TestProvenanceHash(unittest.TestCase):
    """`engine.Sha256`, which is what actually broke on Windows.

    The header of `datasets/cards/cards.json` records the SHA-256 of each
    engine file the dataset was built from. Hashing raw bytes made those two
    hex strings a property of the checkout: on a CRLF working tree the
    regenerated header disagreed with the committed one and the dataset was
    called stale with nothing semantic changed.
    """

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.root = Path(self._tmp.name)

    def Source(self, name: str, text: str, newline: str = "\n") -> Path:
        return Write(self.root / name, text, newline)

    def test_the_same_content_hashes_the_same_under_either_checkout(self):
        text = '{\n  "core": [\n    {"card_id": "01002"}\n  ]\n}\n'
        self.assertEqual(engine.Sha256(self.Source("lf.json", text)),
                         engine.Sha256(self.Source("crlf.json", text, "\r\n")))

    def test_a_content_change_still_moves_the_hash(self):
        """Mutation. Normalising newlines must not have made the hash blind.

        Checked on both a CRLF and an LF copy, because a normalisation that
        threw away too much would show up as a collision on one and not the
        other.
        """
        original = '{\n  "core": [\n    {"card_id": "01002"}\n  ]\n}\n'
        changed = original.replace("01002", "01003")
        for newline in ("\n", "\r\n"):
            with self.subTest(newline=repr(newline)):
                self.assertNotEqual(
                    engine.Sha256(self.Source("a.json", original, newline)),
                    engine.Sha256(self.Source("b.json", changed, newline)))

    def test_whitespace_inside_the_file_still_moves_the_hash(self):
        """Only line terminators are normalised, not whitespace generally."""
        original = '{\n  "core": []\n}\n'
        self.assertNotEqual(
            engine.Sha256(self.Source("a.json", original)),
            engine.Sha256(self.Source("b.json", original.replace("  ", "    "))))

    def test_an_lf_file_hashes_to_its_own_sha256(self):
        """The value on an LF checkout is unchanged by the normalisation.

        Which is why `datasets/cards/cards.json` did not have to be
        regenerated when MARVEL-73 landed, and why anyone can still verify the
        header with `sha256sum` on a normal clone.
        """
        text = '{\n  "core": []\n}\n'
        path = self.Source("lf.json", text)
        self.assertEqual(engine.Sha256(path),
                         hashlib.sha256(text.encode("utf-8")).hexdigest())


class TestTheGatesAgree(unittest.TestCase):
    """The point of the issue: one definition, used by all three gates.

    Stated as a test because "they agree" is otherwise a claim about five
    files that will be edited separately. Import-level, so it survives a
    rewrite of any of the three that keeps calling `tools.fixtures`.
    """

    def test_all_five_gates_take_their_verdict_from_tools_fixtures(self):
        from tools.digest import emit_escaping
        from tools.digest import emit_vectors as digest_vectors
        from tools.events import emit_vocabulary
        from tools.rng import emit_vectors as rng_vectors

        for module in (rng_vectors, digest_vectors, emit_escaping,
                       emit_vocabulary, extract):
            with self.subTest(module=module.__name__):
                self.assertIs(module.fixtures, fixtures)


if __name__ == "__main__":
    unittest.main()
