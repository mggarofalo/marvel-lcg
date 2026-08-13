"""The parts of `tools/determinism/probe_temp0_order.py` that decide a verdict.

The probe's measurements need a live engine and thousands of puzzle games, and
those are not a unit test. Three pieces of it are pure, and all three can turn a
real finding into a false all-clear if they are wrong:

- `AllTemp0` is what "the prompt was spurious" means. Read it too loosely and a
  batch containing a real card ability is filed as internal bookkeeping.
- `Describe` runs inside a live `SelectForcedEffect`, and the engine's message
  broadcast catches broadly, so an exception raised there is swallowed: the
  batch resolves differently and the probe reports *fewer* batches than there
  were. That happened during MARVEL-95 -- reading a `card_id` attribute a
  `CardFace` does not have shortened a game by one step and reported zero.
- `Shard` splits the card pool across subprocesses. A chunking bug drops cards
  silently, and a scan that missed them looks exactly like a scan that cleared
  them.

Run from `py_src/`.
"""

import unittest

from tools.determinism.probe_temp0_order import AllTemp0, Describe, Shard


class Boom:
    """Anything the probe reads off a candidate raises."""

    def __getattr__(self, name):
        raise RuntimeError(f"no {name} here")


class TestAllTemp0(unittest.TestCase):

    def test_a_batch_of_cleanups_is_internal(self):
        self.assertTrue(AllTemp0({"types": ["AbilityType.Temp0",
                                            "AbilityType.Temp0UI"]}))

    def test_a_mixed_batch_is_not(self):
        self.assertFalse(AllTemp0({"types": ["AbilityType.Temp0",
                                             "AbilityType.ForcedResponse"]}))

    def test_a_batch_of_card_abilities_is_not(self):
        self.assertFalse(AllTemp0({"types": ["AbilityType.ForcedInterrupt",
                                             "AbilityType.ForcedInterrupt"]}))

    def test_an_empty_batch_is_not(self):
        # A batch the probe could not describe must never be counted as a
        # spurious prompt -- that is the direction that hides a finding.
        self.assertFalse(AllTemp0({"types": []}))

    def test_a_type_that_merely_starts_the_same_is_not_a_cleanup(self):
        # `Temp1` and `Temp2` are different abilities on different priorities.
        self.assertFalse(AllTemp0({"types": ["AbilityType.Temp1"]}))
        self.assertFalse(AllTemp0({"types": ["AbilityType.Temp0", "AbilityType.Temp2"]}))


class TestDescribeNeverRaises(unittest.TestCase):

    def test_an_unreadable_candidate_becomes_a_record_not_an_exception(self):
        record = Describe([Boom(), Boom()])

        self.assertEqual(record["count"], 2)
        self.assertIn("error", record)
        # And it must not read as an all-Temp0 batch, or a batch the probe
        # failed to describe would be counted as evidence that the prompt was
        # spurious.
        self.assertFalse(AllTemp0(record))

    def test_the_failure_record_carries_every_key_a_caller_reads(self):
        record = Describe([Boom()])

        for key in ("priority", "types", "names", "cards", "card_ids", "local",
                    "where", "when", "labels", "count"):
            self.assertIn(key, record)


class TestShard(unittest.TestCase):

    def test_every_item_survives_the_split(self):
        items = [str(index) for index in range(97)]
        for count in (1, 2, 5, 8, 96, 97, 200):
            with self.subTest(count=count):
                chunks = Shard(items, count)
                self.assertEqual([x for chunk in chunks for x in chunk], items)

    def test_no_chunk_is_empty(self):
        # An empty chunk becomes a subprocess told to scan the card id "", which
        # fails in a way that reads like a card the engine cannot generate.
        for count in (3, 7, 64):
            with self.subTest(count=count):
                for chunk in Shard([str(index) for index in range(10)], count):
                    self.assertTrue(chunk)

    def test_one_shard_is_the_whole_list(self):
        self.assertEqual(Shard(["a", "b", "c"], 1), [["a", "b", "c"]])


if __name__ == "__main__":
    unittest.main()
