"""The harness's run digest must depend on the engine, not on who was watching.

`RunResult.digest()` folds the object-id counters in alongside the per-step
digests, because allocation order is part of the digest contract a port has to
reproduce: a card handed a different id is a real divergence even when every
field matches.

But `ObjectManager.index_dict` counts more than that. It also counts transient
query objects -- `check_message` above all -- whose ids are never written into a
save or onto the digest wire. Folding those in made the harness assert something
stronger than "the engine is deterministic": it asserted "the engine allocated
the same number of internal query objects", which moves whenever anything asks
the engine a question.

It fired twice in one day, both benign, and cost an investigation each time.
MARVEL-29 stopped headless runs building a `WorldDescriptor`; MARVEL-76 added a
post-condition that asks `GetCountHandSizeFaces()` once more. Neither moved a
per-step digest or a saved byte.

These tests pin both directions, because a fix here is only worth having if the
narrower digest still catches what the wide one caught. See MARVEL-75.
"""

import unittest

from tools.determinism.headless import PERSISTED_ID_CATEGORIES, RunResult, Step


def Result(**index):
    full = {"card": 129, "effect": 146, "message": 879, "check_message": 642,
            "forced_effect": 262, "paying_effect": 80, "choose_effect": 1,
            "deck": 569, "game_area": 1, "player": 2, "scenario": 1}
    full.update(index)
    return RunResult(
        campaign="klaw",
        heroes=["captain_marvel", "she_hulk"],
        seed=999,
        steps=[Step(0, 0, "WhenPlayerChooseAbility", "{}")],
        object_index=full,
    )


class TestWhatTheDigestCovers(unittest.TestCase):

    def test_a_watcher_asking_more_questions_does_not_move_it(self):
        # The exact shape of MARVEL-29 and MARVEL-76: `check_message` moved and
        # nothing else did.
        before = Result(check_message=642).digest()
        after = Result(check_message=678).digest()

        self.assertEqual(before, after)

    def test_no_unpersisted_counter_moves_it(self):
        # Named individually rather than in a loop over the difference, so that
        # adding a category to `ObjectManager` without deciding which side it
        # belongs on shows up as an unpinned name here.
        baseline = Result().digest()

        for name in ("check_message", "forced_effect", "paying_effect",
                     "choose_effect", "deck", "game_area", "player", "scenario"):
            self.assertEqual(Result(**{name: 9999}).digest(), baseline,
                             f"{name} moved the run digest")

    def test_a_card_allocated_a_different_id_still_moves_it(self):
        # The reason the counters are in the digest at all. Losing this would
        # make the narrowing a regression rather than a fix.
        self.assertNotEqual(Result(card=129).digest(), Result(card=130).digest())

    def test_an_effect_or_message_count_still_moves_it(self):
        # Both reach a saved scene: `e` in the effect id, `m` in the event name.
        baseline = Result().digest()

        self.assertNotEqual(Result(effect=147).digest(), baseline)
        self.assertNotEqual(Result(message=880).digest(), baseline)


class TestThePersistedSet(unittest.TestCase):

    def test_it_is_exactly_the_three_ids_a_recording_holds(self):
        # Measured, not chosen: `m`, `e` and `c` are the only id prefixes that
        # appear anywhere in a saved scene, and `card` is the only one on the
        # v2 digest wire. `owner` there is a seat index, not the `player`
        # counter.
        self.assertEqual(set(PERSISTED_ID_CATEGORIES), {"card", "effect", "message"})

    def test_the_reported_subset_matches_the_set(self):
        result = Result()

        self.assertEqual(set(result.persisted_index), set(PERSISTED_ID_CATEGORIES))

    def test_the_full_index_is_still_carried(self):
        # Narrowing what decides a verdict must not throw away what helps
        # diagnose one.
        result = Result()

        self.assertIn("check_message", result.object_index)
        self.assertIn("check_message", result.to_json())


if __name__ == "__main__":
    unittest.main()
