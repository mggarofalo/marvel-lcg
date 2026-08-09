"""Card coverage against a real game, not a stand-in.

`test_card_coverage.py` pins the rules. This pins the thing those rules are
useless without: that the names observed while a game runs are the same names
the card dataset uses, and that the three levels really do come apart on a board
the engine actually played.

The tripwire is `test_every_fired_factory_is_in_the_dataset`. Runtime
attribution comes from wrapping `AbilityFactory`; the denominator comes from
`tools/cards/scripts.py` reading `AbilityFactory.<name>` off each card script's
syntax tree. Rename a factory method and regenerate nothing, and the report
would quietly stop counting it as reached -- coverage would look worse, or a
never-fired entry would name a method that no longer exists. This fails instead.

It boots the engine and plays, so it is not in the fast tier. Full-fidelity
end-to-end -- a real policy, a saved scene, a written report -- is
`python main.py -bot -bot_verify`.
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.profile import coverage_report
from engine.profile.card_coverage import CardCoverage

CAMPAIGN = "rhino"
HEROES = ["spider_man"]


def PlayOne(seed):
    """One headless game with coverage recording, returned as a game record.

    `CardsDB.ability_cache` builds a card's abilities once per process and hands
    the same objects to every later copy, so anything created before
    instrumentation carries no attribution for the rest of the run. In a corpus
    run `BotRunner.Run` instruments before the first game; here another test
    module in the same process may already have warmed the cache, so it is
    dropped and rebuilt through the wrappers.
    """
    from cards.database import CardsDB
    from engine import Engine
    from tools.determinism.headless import run_headless

    CardCoverage.Enable()
    CardsDB.ability_cache.clear()

    CardCoverage.BeginGame()
    run_headless(CAMPAIGN, HEROES, seed, max_steps=400)

    world = Engine.game.world
    return CardCoverage.EndGame(
        world, seed=seed, scenario=CAMPAIGN, heroes=HEROES,
        outcome=world.game_over.reason or "Unknown")


class TestCoverageOfAPlayedGame(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.record = PlayOne(1)
        cls.universe = coverage_report.LoadUniverse()

    @classmethod
    def tearDownClass(cls):
        CardCoverage.Disable()

    def test_the_game_exercised_something(self):
        """Everything below is vacuously true of an empty record."""
        self.assertTrue(self.record["cards"]["present"])
        self.assertTrue(self.record["cards"]["entered_play"])
        self.assertTrue(self.record["cards"]["resolved"])
        self.assertTrue(self.record["factories"])

    def test_every_fired_factory_is_in_the_dataset(self):
        """The runtime and static namespaces are the same namespace.

        If this fails, an `AbilityFactory` method was renamed or added without
        `datasets/cards/` being regenerated -- run `python -m tools.cards.extract`.
        """
        unknown = sorted(set(self.record["factories"]) - set(self.universe.factories))

        self.assertEqual(unknown, [], "fired factories the card dataset has never heard of")

    def test_a_card_in_play_was_present(self):
        present = set(self.record["cards"]["present"])
        entered = set(self.record["cards"]["entered_play"])

        self.assertTrue(entered <= present, sorted(entered - present))

    def test_present_and_resolved_come_apart(self):
        """The acceptance criterion, on a real board: most of what a game
        contains never does anything, and the report has to say which."""
        present = set(self.record["cards"]["present"])
        resolved = set(self.record["cards"]["resolved"])

        self.assertTrue(present - resolved, "every present card resolved, which is not a real game")

    def test_most_of_the_factory_tail_went_unreached(self):
        """Not an assertion about quality -- an assertion that the denominator
        is the whole tail rather than the handful this board happens to use. A
        report measuring only what it saw would always read 100%."""
        fired = set(self.record["factories"])

        self.assertLess(len(fired), len(self.universe.factories))

    def test_resolution_is_attributed_to_real_cards(self):
        resolved = set(self.record["cards"]["resolved"])

        self.assertTrue(resolved & set(self.universe.cards))

    def test_the_same_seed_records_the_same_coverage(self):
        """Coverage is an observation of a deterministic engine, so it inherits
        the determinism. A recorder that iterated an unordered set would fail
        here rather than in a corpus six months from now."""
        again = PlayOne(1)

        self.assertEqual(again, self.record)

    def test_the_report_answers_what_was_missed(self):
        document = coverage_report.Build(
            [self.record], generator="test", engine_version="0.0.0",
            universe=self.universe)

        self.assertTrue(document["universe"]["available"])
        self.assertEqual(document["totals"]["factories"]["universe"],
                         len(self.universe.factories))
        self.assertTrue(document["never_fired_factories"])
        self.assertTrue(document["never_exercised_cards"])
        # Ranked, worst first, and stable.
        weights = [entry["cards"] for entry in document["never_fired_factories"]]
        self.assertEqual(weights, sorted(weights, reverse=True))


if __name__ == "__main__":
    unittest.main()
