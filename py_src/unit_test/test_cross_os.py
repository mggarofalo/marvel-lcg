"""The cross-OS trace comparison has to fail on a real divergence.

`tools/determinism/cross_os.py` is the only gate that can catch Windows and
Linux disagreeing about a digest, and it is structurally easy to get wrong in
the direction that always passes: the two trace files legitimately differ in
their platform metadata, so a naive whole-file diff is useless and a too-narrow
comparison reports green on traces that disagree.

These tests pin what is compared and what is deliberately ignored. They run on
synthetic traces rather than the engine, so they belong to the fast tier -- the
comparison logic is what is under test here, not the engine that feeds it. See
MARVEL-35.
"""

import io
import json
import os
import tempfile
import unittest
from contextlib import redirect_stdout

from tools.determinism.cross_os import COMPARED_FIELDS, main


def make_trace(label, system, *, cases=None, matrix="wide"):
    return {
        "platform": {
            "label": label,
            "system": system,
            "release": "6.8.0" if system == "Linux" else "11",
            "machine": "x86_64",
            "python": "3.13.15",
        },
        "matrix": matrix,
        "max_steps": 400,
        "cases": cases if cases is not None else {"rhino/spider_man/12345": make_case()},
    }


def make_case(*, digest="a" * 64, steps=None, object_index=None, error="",
              game_over=False):
    steps = steps if steps is not None else [
        {"i": 0, "p": 0, "e": "GameSetup", "digest": "d0"},
        {"i": 1, "p": 0, "e": "PlayerTurn", "digest": "d1"},
    ]
    return {
        "campaign": "rhino",
        "heroes": ["spider_man"],
        "seed": 12345,
        "digest": digest,
        "step_count": len(steps),
        "object_index": object_index if object_index is not None else {"card": 7},
        "game_over": game_over,
        "error": error,
        "steps": steps,
    }


class CompareTestCase(unittest.TestCase):
    """Writes trace files to a scratch directory and runs `compare` over them."""

    def setUp(self):
        self.scratch = tempfile.mkdtemp()
        self.addCleanup(self._clean)

    def _clean(self):
        for name in os.listdir(self.scratch):
            os.unlink(os.path.join(self.scratch, name))
        os.rmdir(self.scratch)

    def run_compare(self, *traces):
        paths = []
        for index, trace in enumerate(traces):
            path = os.path.join(self.scratch, f"trace-{index}.json")
            with open(path, "w", encoding="utf-8") as handle:
                json.dump(trace, handle)
            paths.append(path)
        output = io.StringIO()
        with redirect_stdout(output):
            code = main(["compare"] + paths)
        return code, output.getvalue()


class TestAgreeingPlatformsPass(CompareTestCase):

    def test_identical_traces_agree(self):
        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux"),
            make_trace("windows-latest", "Windows"),
        )

        self.assertEqual(code, 0)
        self.assertIn("all cases agree", out)

    def test_platform_metadata_is_expected_to_differ(self):
        # The entire point of the comparison: every field in the platform block
        # differs between runners and none of it is a divergence.
        linux = make_trace("ubuntu-latest", "Linux")
        windows = make_trace("windows-latest", "Windows")
        windows["platform"]["python"] = "3.13.9"
        windows["platform"]["machine"] = "AMD64"

        code, _ = self.run_compare(linux, windows)

        self.assertEqual(code, 0)


class TestADivergenceFails(CompareTestCase):

    def test_a_differing_run_digest_fails(self):
        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux"),
            make_trace("windows-latest", "Windows",
                       cases={"rhino/spider_man/12345": make_case(digest="b" * 64)}),
        )

        self.assertEqual(code, 1)
        self.assertIn("cross-OS divergence", out)

    def test_the_report_names_the_first_divergent_step(self):
        diverged = make_case(digest="b" * 64, steps=[
            {"i": 0, "p": 0, "e": "GameSetup", "digest": "d0"},
            {"i": 1, "p": 0, "e": "PlayerTurn", "digest": "DIFFERENT"},
        ])

        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux"),
            make_trace("windows-latest", "Windows",
                       cases={"rhino/spider_man/12345": diverged}),
        )

        self.assertEqual(code, 1)
        # Step 1 diverged, step 0 did not -- a report that pointed at either the
        # whole trace or the wrong step would not tell you where to look.
        self.assertIn("first divergent step 1", out)
        self.assertIn("PlayerTurn", out)

    def test_a_differing_step_count_fails(self):
        short = make_case(digest="b" * 64, steps=[
            {"i": 0, "p": 0, "e": "GameSetup", "digest": "d0"},
        ])

        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux"),
            make_trace("windows-latest", "Windows",
                       cases={"rhino/spider_man/12345": short}),
        )

        self.assertEqual(code, 1)
        self.assertIn("step counts differ: 2 vs 1", out)

    def test_an_error_on_one_platform_only_fails(self):
        # Both traces can be step-for-step identical up to the point one engine
        # raised. That is still a divergence.
        raised = make_case(error="ValueError: card script blew up")

        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux"),
            make_trace("windows-latest", "Windows",
                       cases={"rhino/spider_man/12345": raised}),
        )

        self.assertEqual(code, 1)
        self.assertIn("error differs", out)

    def test_a_differing_object_index_fails(self):
        # Card id allocation order is part of the digest contract, so two
        # platforms allocating differently is a finding even if every step
        # digest matches.
        reallocated = make_case(object_index={"card": 9})

        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux"),
            make_trace("windows-latest", "Windows",
                       cases={"rhino/spider_man/12345": reallocated}),
        )

        self.assertEqual(code, 1)
        self.assertIn("object_index differs", out)


def digest_document(health):
    """The smallest thing `game.world.digest.Parse` accepts, in canonical form."""
    return json.dumps(
        {"v": 2, "cards": [{
            "id": 103, "card": "01120", "zone": "EngagedEnemiesArea",
            "owner": -1, "index": 0, "host": -1, "face_up": True,
            "fields": {"health": health},
        }]},
        separators=(",", ":"), ensure_ascii=True, sort_keys=False,
    )


class TestTheReportIsReadable(CompareTestCase):
    """A v2 digest is a whole board. Printing two of them is not a report.

    The failure this guards is a CI log that technically contains the answer
    inside forty kilobytes of serialised cards. `digest.Diff` is the engine's
    own differ and names the card and the field instead.
    """

    def diverging_traces(self):
        steps_a = [{"i": 0, "p": 0, "e": "VillainPhase", "digest": digest_document(3)}]
        steps_b = [{"i": 0, "p": 0, "e": "VillainPhase", "digest": digest_document(2)}]
        return (
            make_trace("ubuntu-latest", "Linux", cases={
                "rhino/spider_man/12345": make_case(digest="a" * 64, steps=steps_a)}),
            make_trace("windows-latest", "Windows", cases={
                "rhino/spider_man/12345": make_case(digest="b" * 64, steps=steps_b)}),
        )

    def test_the_report_names_the_card_and_the_field(self):
        code, out = self.run_compare(*self.diverging_traces())

        self.assertEqual(code, 1)
        self.assertIn("c103", out)
        self.assertIn("health", out)
        self.assertIn("3 -> 2", out)

    def test_the_report_does_not_dump_the_whole_board(self):
        _, out = self.run_compare(*self.diverging_traces())

        self.assertNotIn("EngagedEnemiesArea", out)

    def test_an_unparseable_digest_still_produces_a_report(self):
        # Traces are files on disk that can be truncated or predate a format
        # change. The failure path must not itself fail.
        steps_a = [{"i": 0, "p": 0, "e": "VillainPhase", "digest": "not a digest"}]
        steps_b = [{"i": 0, "p": 0, "e": "VillainPhase", "digest": "also not one"}]

        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux", cases={
                "rhino/spider_man/12345": make_case(digest="a" * 64, steps=steps_a)}),
            make_trace("windows-latest", "Windows", cases={
                "rhino/spider_man/12345": make_case(digest="b" * 64, steps=steps_b)}),
        )

        self.assertEqual(code, 1)
        self.assertIn("digest unreadable", out)


class TestNonComparableInputIsRefused(CompareTestCase):

    def test_differing_case_sets_do_not_silently_compare_the_overlap(self):
        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux", cases={
                "rhino/spider_man/12345": make_case(),
                "klaw/captain_marvel/999": make_case(),
            }),
            make_trace("windows-latest", "Windows"),
        )

        self.assertEqual(code, 1)
        self.assertIn("case sets differ", out)

    def test_differing_matrices_are_not_compared(self):
        code, out = self.run_compare(
            make_trace("ubuntu-latest", "Linux", matrix="wide"),
            make_trace("windows-latest", "Windows", matrix="smoke"),
        )

        self.assertEqual(code, 1)
        self.assertIn("different matrices", out)

    def test_one_platform_is_not_a_comparison(self):
        # A CI job whose second runner failed to upload would otherwise report
        # a green cross-OS check having compared nothing.
        path = os.path.join(self.scratch, "only.json")
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(make_trace("ubuntu-latest", "Linux"), handle)

        self.assertEqual(main(["compare", path]), 2)


class TestTheComparedFieldSet(unittest.TestCase):

    def test_the_digest_and_the_fields_that_build_it_are_all_compared(self):
        # Pins the set itself: dropping a field from COMPARED_FIELDS is the
        # exact edit that would make this gate pass on a real divergence.
        self.assertEqual(
            set(COMPARED_FIELDS),
            {"digest", "step_count", "object_index", "game_over", "error"},
        )


if __name__ == "__main__":
    unittest.main()
