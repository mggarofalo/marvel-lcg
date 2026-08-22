"""Tests for the MarvelCDB rulings snapshot and its harvester (MARVEL-143).

Two layers, following `test_card_dataset.py`. Most tests build a synthetic
snapshot so one rule can be stated and checked in isolation; a handful run
against the real repository, because what actually matters is that the vendored
file is internally consistent and still lands on cards the dataset has.

Everything here is stdlib-only and offline. `tools/cards/harvest_faq.py` is the
one module that shells out to `marvelcdb`, and the tests that cover it replace
`_Run` rather than calling it -- a unit suite that needs the network is a unit
suite that goes red when a community-run site has a bad afternoon.

    python -m unittest unit_test.test_card_rulings
"""

import json
import tempfile
import unittest
from pathlib import Path

from tools.cards import harvest_faq, rulings

REPO = Path(".")
SNAPSHOT = REPO / rulings.SNAPSHOT_DIR
DATASET = REPO / rulings.CARD_DATASET


def WriteSnapshot(root: Path, entries, queried=None, **overrides) -> Path:
    payload = {
        "version": 1,
        "harvested": "2026-08-22",
        "source": "https://marvelcdb.com",
        "harvester": "marvelcdb v0.1.0",
        "queried": sorted(queried if queried is not None
                          else [e["code"] for e in entries]),
        "entries": entries,
    }
    payload.update(overrides)
    root.mkdir(parents=True, exist_ok=True)
    path = root / rulings.FAQ_FILE
    path.write_text(json.dumps(payload), encoding="utf-8")
    return path


def Entry(code: str, text: str = "A ruling.") -> dict:
    return {"code": code, "html": f"<p>{text}</p>", "text": text,
            "updated": {"date": "2020-08-28 03:35:33.000000",
                        "timezone_type": 3, "timezone": "UTC"}}


################################################################################
# Reading the snapshot


class TestLoad(unittest.TestCase):

    def test_a_missing_snapshot_is_absent_not_an_error(self):
        """A clone that has never harvested still has to run everything."""
        with tempfile.TemporaryDirectory() as tmp:
            data = rulings.Load(Path(tmp) / "nothing-here")
        self.assertFalse(data.Loaded())
        self.assertEqual(data.rulings, {})
        self.assertEqual(data.queried, set())

    def test_reads_entries_and_the_queried_set(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            WriteSnapshot(root, [Entry("01001a")], queried=["01001a", "01050"])
            data = rulings.Load(root)
        self.assertTrue(data.Loaded())
        self.assertEqual(set(data.rulings), {"01001a"})
        self.assertEqual(data.rulings["01001a"].text, "A ruling.")
        self.assertEqual(data.harvested, "2026-08-22")
        self.assertEqual(data.harvester, "marvelcdb v0.1.0")

    def test_asked_separates_no_ruling_from_never_asked(self):
        """The distinction `queried` exists to preserve."""
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            WriteSnapshot(root, [Entry("01001a")], queried=["01001a", "01050"])
            data = rulings.Load(root)
        # Asked and answered.
        self.assertTrue(data.Asked("01001a"))
        # Asked, and the answer was "nothing" -- not the same as unknown.
        self.assertTrue(data.Asked("01050"))
        self.assertNotIn("01050", data.rulings)
        # Never asked.
        self.assertFalse(data.Asked("99999"))

    def test_a_file_without_entries_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / rulings.FAQ_FILE
            path.write_text(json.dumps({"version": 1}), encoding="utf-8")
            with self.assertRaises(ValueError):
                rulings.Load(Path(tmp))

    def test_an_entry_without_a_code_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            WriteSnapshot(root, [{"text": "orphan"}], queried=["01001a"])
            with self.assertRaises(ValueError):
                rulings.Load(root)

    def test_a_repeated_code_keeps_the_first_and_is_reported(self):
        """Upstream serves `05005` twice. Neither copy may vanish silently."""
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            WriteSnapshot(root, [Entry("05005", "first"), Entry("05005", "second")],
                          queried=["05005"])
            data = rulings.Load(root)
        self.assertEqual(data.rulings["05005"].text, "first")
        self.assertEqual(data.duplicate_codes, ["05005"])

    def test_a_ruling_nobody_asked_for_is_rejected(self):
        """Entries outside `queried` mean the file is not one harvest's output.

        Tolerating it would let a hand-edited snapshot report rulings whose
        provenance nothing records.
        """
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            WriteSnapshot(root, [Entry("01001a")], queried=["01050"])
            with self.assertRaises(ValueError):
                rulings.Load(root)


class TestFaces(unittest.TestCase):
    """Mapping a MarvelCDB code onto the card ids this repository uses."""

    CARDS = {"01001a", "01001b", "01050", "01097a", "01097b",
             "01144a", "01144b", "01144c"}

    def test_a_code_the_dataset_has_maps_to_itself(self):
        self.assertEqual(rulings.Faces("01050", self.CARDS), ["01050"])

    def test_a_whole_card_code_fans_out_to_its_printed_faces(self):
        """Site `01097` is `01097a` + `01097b` here -- 76 codes are like this.

        A ruling is about the card. Which face prints the sentence being asked
        about is a printing detail, so both faces get it.
        """
        self.assertEqual(rulings.Faces("01097", self.CARDS),
                         ["01097a", "01097b"])

    def test_three_part_cards_are_not_truncated_to_two(self):
        self.assertEqual(rulings.Faces("01144", self.CARDS),
                         ["01144a", "01144b", "01144c"])

    def test_a_code_matching_nothing_maps_to_nothing(self):
        self.assertEqual(rulings.Faces("99999", self.CARDS), [])

    def test_a_face_id_is_preferred_over_fanning_out(self):
        """`01001a` exists on both sides, so it must not also collect `01001ab`."""
        self.assertEqual(rulings.Faces("01001a", self.CARDS), ["01001a"])


class TestByCard(unittest.TestCase):

    CARDS = {"01001a", "01050", "01097a", "01097b"}

    def _Data(self, entries, queried=None):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            WriteSnapshot(root, entries, queried=queried)
            return rulings.Load(root)

    def test_one_ruling_reaches_every_face(self):
        data = self._Data([Entry("01097", "Main scheme timing.")])
        by_card = rulings.ByCard(data, self.CARDS)
        self.assertEqual(set(by_card), {"01097a", "01097b"})
        self.assertEqual(by_card["01097b"][0].text, "Main scheme timing.")
        # The ruling keeps the code MarvelCDB served it under.
        self.assertEqual(by_card["01097b"][0].code, "01097")

    def test_unmappable_codes_are_reported_not_dropped(self):
        data = self._Data([Entry("01050"), Entry("99999")])
        self.assertEqual(rulings.Unmapped(data, self.CARDS), ["99999"])
        self.assertNotIn("99999", rulings.ByCard(data, self.CARDS))

    def test_a_face_counts_as_asked_under_its_whole_card_code(self):
        """`01097b` was covered by the harvest, which asked about `01097`."""
        data = self._Data([], queried=["01097"])
        self.assertTrue(rulings.WasAsked(data, "01097b"))
        self.assertFalse(rulings.WasAsked(data, "02001b"))


################################################################################
# Harvesting


class FakeCli:
    """Stands in for `harvest_faq._Run`, recording what it was asked."""

    def __init__(self, results):
        self.results = results
        self.calls = []

    def __call__(self, args):
        self.calls.append(list(args))
        return self.results.pop(0)


class TestParsing(unittest.TestCase):

    def test_a_single_result_is_an_object_not_an_array(self):
        """The CLI's shape changes with the count -- see the marvelcdb skill."""
        self.assertEqual(harvest_faq._Entries('{"code": "01001a"}'),
                         [{"code": "01001a"}])

    def test_several_results_are_an_array(self):
        self.assertEqual(
            harvest_faq._Entries('[{"code": "01001a"}, {"code": "01135"}]'),
            [{"code": "01001a"}, {"code": "01135"}])

    def test_empty_stdout_means_the_batch_found_nothing(self):
        self.assertEqual(harvest_faq._Entries("   \n"), [])

    def test_stderr_reports_which_codes_have_no_ruling(self):
        stderr = "no FAQ entries for 01050\nno FAQ entries for 01051\n"
        self.assertEqual(harvest_faq._Empty(stderr), ["01050", "01051"])

    def test_other_stderr_lines_are_not_read_as_answers(self):
        """stderr also carries advisories. Only the exact form counts."""
        stderr = ("warning: cache is stale\n"
                  "no FAQ entries for 01050\n"
                  "could not find no FAQ entries for anything\n")
        self.assertEqual(harvest_faq._Empty(stderr), ["01050"])


class TestAccounting(unittest.TestCase):
    """The rule that stands in for trusting the exit code."""

    def setUp(self):
        self._real = harvest_faq._Run

    def tearDown(self):
        harvest_faq._Run = self._real

    def test_every_code_accounted_for_is_a_success_even_at_exit_one(self):
        """A batch holding an unknown code exits 1 with complete, valid JSON."""
        harvest_faq._Run = FakeCli([
            (1, '{"code": "01001a"}', "no FAQ entries for 01050\n"),
        ])
        entries = harvest_faq.Batch(["01001a", "01050"])
        self.assertEqual([e["code"] for e in entries], ["01001a"])

    def test_a_code_in_neither_stream_fails_the_run(self):
        """The whole point: a lost answer must not become "no ruling"."""
        harvest_faq._Run = FakeCli([(0, '{"code": "01001a"}', "")])
        with self.assertRaises(harvest_faq.HarvestError) as caught:
            harvest_faq.Batch(["01001a", "01050"])
        self.assertIn("01050", str(caught.exception))

    def test_quiet_is_never_passed(self):
        """`-q` suppresses the lines the accounting depends on."""
        harvest_faq._Run = FakeCli([(0, "", "no FAQ entries for 01050\n")])
        harvest_faq.Batch(["01050"])
        self.assertNotIn("-q", harvest_faq._Run.calls[0])

    def test_an_unknown_code_stops_the_harvest(self):
        harvest_faq._Run = FakeCli([(4, "", "")])
        with self.assertRaises(harvest_faq.HarvestError):
            harvest_faq.Batch(["99999"])

    def test_being_offline_stops_the_harvest(self):
        harvest_faq._Run = FakeCli([(5, "", "")])
        with self.assertRaises(harvest_faq.HarvestError):
            harvest_faq.Batch(["01050"])

    def test_unparseable_json_stops_the_harvest(self):
        harvest_faq._Run = FakeCli([(0, "not json", "")])
        with self.assertRaises(harvest_faq.HarvestError):
            harvest_faq.Batch(["01050"])


class TestRender(unittest.TestCase):

    def test_the_same_harvest_renders_the_same_bytes(self):
        args = ([Entry("01135"), Entry("01001a")], ["01001a", "01050", "01135"],
                "2026-08-22", "marvelcdb v0.1.0")
        self.assertEqual(harvest_faq.Render(*args), harvest_faq.Render(*args))

    def test_entries_and_queried_are_sorted_regardless_of_input_order(self):
        payload = harvest_faq.Render(
            [Entry("01135"), Entry("01001a")], ["01135", "01001a"],
            "2026-08-22", "marvelcdb v0.1.0")
        parsed = json.loads(payload)
        self.assertEqual([e["code"] for e in parsed["entries"]],
                         ["01001a", "01135"])
        self.assertEqual(parsed["queried"], ["01001a", "01135"])

    def test_one_entry_per_line_so_a_refresh_diffs_readably(self):
        payload = harvest_faq.Render(
            [Entry("01001a"), Entry("01135")], ["01001a", "01135"],
            "2026-08-22", "marvelcdb v0.1.0")
        bodies = [l for l in payload.splitlines() if l.startswith('{"code"')]
        self.assertEqual(len(bodies), 2)

    def test_unknown_upstream_fields_survive_the_round_trip(self):
        """The snapshot mirrors MarvelCDB; it does not curate it."""
        entry = Entry("01001a")
        entry["something_new"] = "kept"
        parsed = json.loads(harvest_faq.Render(
            [entry], ["01001a"], "2026-08-22", "marvelcdb v0.1.0"))
        self.assertEqual(parsed["entries"][0]["something_new"], "kept")

    def test_what_it_renders_is_what_the_reader_accepts(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            harvest_faq.Write(
                root / rulings.FAQ_FILE,
                harvest_faq.Render([Entry("01001a")], ["01001a", "01050"],
                                   "2026-08-22", "marvelcdb v0.1.0"))
            data = rulings.Load(root)
        self.assertEqual(set(data.rulings), {"01001a"})
        self.assertEqual(data.queried, {"01001a", "01050"})


################################################################################
# The real repository


@unittest.skipUnless((SNAPSHOT / rulings.FAQ_FILE).exists(),
                     "no harvested snapshot in this working tree")
class TestVendoredSnapshot(unittest.TestCase):
    """The checked-in file, which is vendored rather than regenerable offline.

    `datasets/cards/` gets a byte-for-byte `--check` gate because a machine can
    rebuild it. This one cannot be rebuilt without a network, so what guards it
    is consistency: it must parse, its rulings must be ones the harvest asked
    for, and they must still land on cards this repository has.
    """

    @classmethod
    def setUpClass(cls):
        cls.data = rulings.Load(SNAPSHOT)
        cls.card_ids = rulings.CardIds(DATASET)

    def test_it_parses_and_is_not_empty(self):
        """A floor, not a measurement.

        The exact count is the tool's answer, not this file's -- asserting it
        would mean editing a test every refresh, and a test nobody can leave
        alone is one people stop reading. What this catches is a truncated or
        half-written harvest, so the floor sits far below any real one and
        never needs revisiting: rulings only accumulate.
        """
        self.assertTrue(self.data.Loaded())
        self.assertGreater(len(self.data.rulings), 25)
        self.assertGreater(len(self.data.queried), 4000)

    def test_the_harvest_date_and_tool_are_recorded(self):
        """Without these the snapshot cannot be dated -- see UPSTREAM.md."""
        self.assertRegex(self.data.harvested, r"^\d{4}-\d{2}-\d{2}$")
        self.assertTrue(self.data.harvester)

    def test_every_ruling_was_asked_for(self):
        self.assertEqual(set(self.data.rulings) - self.data.queried, set())

    def test_every_ruling_lands_on_a_card_this_repository_has(self):
        """Drift between the snapshot and the card dataset, made visible.

        A non-empty result is not necessarily a bug -- MarvelCDB is ahead of the
        pinned MarvelSDB snapshot by two packs (MARVEL-144) -- but it should be
        a deliberate, named state rather than a silent one.
        """
        unmapped = rulings.Unmapped(self.data, self.card_ids)
        self.assertEqual(
            unmapped, [],
            f"{len(unmapped)} ruling(s) match no card id: {unmapped[:10]}. "
            f"Either the card dataset needs a refresh (MARVEL-144) or the "
            f"snapshot does.")

    def test_rulings_reach_cards_by_id(self):
        by_card = rulings.ByCard(self.data, self.card_ids)
        self.assertGreater(len(by_card), 25)
        self.assertLessEqual(len(by_card), len(self.card_ids))

    def test_the_spiderman_ruling_is_present_and_readable(self):
        """The worked example the whole dataset is justified by."""
        by_card = rulings.ByCard(self.data, self.card_ids)
        self.assertIn("01001a", by_card)
        self.assertIn("initiate", by_card["01001a"][0].text)


class TestCoverageIntegration(unittest.TestCase):
    """`tools.spec.coverage` must survive both states of the snapshot."""

    def test_rulings_load_without_the_engine(self):
        from tools.spec import coverage
        cards = [{"card_id": card_id} for card_id in sorted(rulings.CardIds(DATASET))]
        by_card = coverage.Rulings(cards)
        self.assertIsInstance(by_card, dict)
        if (SNAPSHOT / rulings.FAQ_FILE).exists():
            self.assertGreater(len(by_card), 25)
        else:
            self.assertEqual(by_card, {})

    def test_a_card_with_a_ruling_is_flagged_in_a_work_list_row(self):
        from tools.spec import coverage
        self.assertEqual(coverage._Flags({"rulings": 1, "quarantined": 0}),
                         "  RULING")
        self.assertEqual(coverage._Flags({"rulings": 0, "quarantined": 0}), "")
        self.assertEqual(coverage._Flags({"rulings": 2, "quarantined": 1}),
                         "  QUARANTINED  RULING")
