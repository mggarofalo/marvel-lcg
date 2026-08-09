"""What card coverage claims, and what it refuses to claim.

The number this produces decides whether the corpus is worth generating more of,
so the properties worth pinning are the ones that would make it flatter than it
should be: attributing an ability to the wrong factory, counting a card as
exercised because it was in a deck, counting a replayed game twice, or ranking
the unreached list in an order that is not stable between runs.

Recording is tested against stand-ins rather than a booted engine. The recorder
reads a small, named set of attributes off an ability and a card face; a fake
that provides exactly those fails on the rule being tested instead of on
scenario setup. End-to-end agreement with a real game is
`unit_test/test_card_coverage_play.py`.
"""

import json
import os
import tempfile
import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.profile import coverage_report
from engine.profile.card_coverage import FACTORY_MARK, CardCoverage
from tools.coverage import report as report_cli


################################################################################
# Stand-ins for the handful of attributes the recorder reads


class FakePaper:
    def __init__(self, card_id):
        self.card_id = card_id


class FakeAbilityType:
    def __init__(self, name):
        self.name = name


class FakeAbility:
    def __init__(self, *, card_id="", factory="", when=None, ability_type="Response"):
        self.paper = FakePaper(card_id) if card_id else None
        self.factory = factory
        self.when = when
        self.type = FakeAbilityType(ability_type)


class FakeFace:
    def __init__(self, card_id, *, stage=0):
        self.paper = FakePaper(card_id)
        self.printed_stage = stage


class FakeCard:
    def __init__(self, *card_ids):
        self.printed_faces = [FakeFace(card_id) for card_id in card_ids]


class FakeObjectManager:
    def __init__(self, cards):
        self.card_dict = cards


class FakeRule:
    mode_heroic = 0
    mode_skirmish = False
    mode_campaign = False


class FakeCampaign:
    expert = False
    challenges = []


class FakeScene:
    campaign = FakeCampaign()
    rules = []


class FakeWorld:
    def __init__(self, cards=None, players=1, surviving=None):
        self.object_manager = FakeObjectManager(cards or {})
        self.const_seat_order_players = list(range(players))
        # `world.players`, which shrinks as heroes are eliminated.
        self.players = list(range(players if surviving is None else surviving))
        self.scene = FakeScene()
        self.rule = FakeRule()

    @property
    def const_players(self):
        return self.players[:]


class MessageA:
    pass


class MessageB:
    pass


class CoverageTestCase(unittest.TestCase):
    """Every test runs against a clean recorder and leaves one behind."""

    def setUp(self):
        self.addCleanup(CardCoverage.Disable)
        CardCoverage.Disable()
        CardCoverage.is_enable = True
        CardCoverage.BeginGame()


################################################################################
# Recording


class TestRecording(CoverageTestCase):

    def test_disabled_records_nothing(self):
        CardCoverage.Disable()
        CardCoverage.BeginGame()

        CardCoverage.RecordCardEnteredPlay(FakeFace("01001a"))
        CardCoverage.RecordAbilityResolved(FakeAbility(card_id="01001a", factory="X"))

        self.assertFalse(CardCoverage.is_recording)
        self.assertEqual(CardCoverage.entered_play, {})
        self.assertEqual(CardCoverage.resolved, {})

    def test_entered_play_counts_each_arrival(self):
        CardCoverage.RecordCardEnteredPlay(FakeFace("01001a"))
        CardCoverage.RecordCardEnteredPlay(FakeFace("01001a"))
        CardCoverage.RecordCardEnteredPlay(FakeFace("01002"))

        self.assertEqual(CardCoverage.entered_play, {"01001a": 2, "01002": 1})

    def test_only_staged_faces_record_a_stage(self):
        CardCoverage.RecordCardEnteredPlay(FakeFace("01094", stage=1))
        CardCoverage.RecordCardEnteredPlay(FakeFace("01095", stage=2))
        CardCoverage.RecordCardEnteredPlay(FakeFace("01002"))

        self.assertEqual(CardCoverage.stages, {"01094": 1, "01095": 2})

    def test_resolved_records_card_factory_trigger_and_type(self):
        CardCoverage.RecordAbilityResolved(FakeAbility(
            card_id="01001a", factory="WhenThisRevealed",
            when=MessageA, ability_type="ForcedResponse"))

        self.assertEqual(CardCoverage.resolved, {"01001a": 1})
        self.assertEqual(CardCoverage.factories, {"WhenThisRevealed": 1})
        self.assertEqual(CardCoverage.triggers, {"MessageA": 1})
        self.assertEqual(CardCoverage.ability_types, {"ForcedResponse": 1})

    def test_an_ability_belonging_to_no_card_still_counts_its_trigger(self):
        """Rule and statistics abilities are built directly, not by a factory,
        and belong to no card -- but they are engine behaviour a port has to
        reproduce, so the trigger is worth counting."""
        CardCoverage.RecordAbilityResolved(FakeAbility(when=MessageA, ability_type="Rule"))

        self.assertEqual(CardCoverage.resolved, {})
        self.assertEqual(CardCoverage.factories, {})
        self.assertEqual(CardCoverage.triggers, {"MessageA": 1})

    def test_a_union_trigger_is_named_by_its_sorted_members(self):
        self.assertEqual(CardCoverage.TriggerName(MessageB | MessageA), "MessageA|MessageB")
        self.assertEqual(CardCoverage.TriggerName(MessageA), "MessageA")
        self.assertEqual(CardCoverage.TriggerName(None), "")

    def test_begin_game_clears_the_previous_game(self):
        CardCoverage.RecordCardEnteredPlay(FakeFace("01001a"))
        CardCoverage.BeginGame()

        self.assertEqual(CardCoverage.entered_play, {})

    def test_end_game_closes_the_window(self):
        """`-bot_verify` replays a finished game through the same engine paths.
        If the window stayed open every verified game would count twice."""
        CardCoverage.RecordCardEnteredPlay(FakeFace("01001a"))
        CardCoverage.EndGame(FakeWorld(), seed=1, scenario="rhino",
                             heroes=["spider_man"], outcome="won")

        CardCoverage.RecordCardEnteredPlay(FakeFace("01002"))

        self.assertFalse(CardCoverage.is_recording)
        self.assertEqual(CardCoverage.entered_play, {"01001a": 1})


class TestGameRecord(CoverageTestCase):

    def test_present_covers_both_printed_faces_sorted_and_deduped(self):
        world = FakeWorld({
            3: FakeCard("01002"),
            1: FakeCard("01001a", "01001b"),
            2: FakeCard("01002"),
        })

        self.assertEqual(CardCoverage.PresentCardIds(world),
                         ["01001a", "01001b", "01002"])

    def test_record_carries_the_run_context(self):
        world = FakeWorld({1: FakeCard("01001a")}, players=3)
        CardCoverage.RecordCardEnteredPlay(FakeFace("01001a"))
        CardCoverage.RecordAbilityResolved(FakeAbility(
            card_id="01001a", factory="WhenThisRevealed", when=MessageA))

        record = CardCoverage.EndGame(world, seed=7, scenario="klaw",
                                      heroes=["she_hulk"], outcome="The Villain Was Defeated")

        self.assertEqual(record["seed"], 7)
        self.assertEqual(record["scenario"], "klaw")
        self.assertEqual(record["heroes"], ["she_hulk"])
        self.assertEqual(record["player_count"], 3)
        self.assertEqual(record["outcome"], "The Villain Was Defeated")
        self.assertEqual(record["modes"], {"heroic": 0, "skirmish": False, "campaign": False})
        self.assertEqual(record["cards"]["present"], ["01001a"])
        self.assertEqual(record["cards"]["entered_play"], {"01001a": 1})
        self.assertEqual(record["cards"]["resolved"], {"01001a": 1})
        self.assertEqual(record["factories"], {"WhenThisRevealed": 1})

    def test_player_count_is_what_the_game_was_set_up_with(self):
        """`world.players` shrinks as heroes are eliminated, so reading it at
        game end reports a two-handed game that ended badly as a solo game --
        and the corpus looks like it covered a player count it never played."""
        world = FakeWorld(players=3, surviving=1)

        record = CardCoverage.EndGame(world, seed=1, scenario="klaw",
                                      heroes=["she_hulk", "thor", "hulk"],
                                      outcome="All players were eliminated")

        self.assertEqual(record["player_count"], 3)

    def test_a_card_can_be_present_without_being_exercised(self):
        """The whole point of the metric: a card sitting in a deck proves nothing."""
        world = FakeWorld({1: FakeCard("01001a"), 2: FakeCard("01050")})
        CardCoverage.RecordAbilityResolved(FakeAbility(card_id="01001a", when=MessageA))

        record = CardCoverage.EndGame(world, seed=1, scenario="rhino",
                                      heroes=["spider_man"], outcome="lost")

        self.assertIn("01050", record["cards"]["present"])
        self.assertNotIn("01050", record["cards"]["resolved"])
        self.assertNotIn("01050", record["cards"]["entered_play"])

    def test_record_collections_are_sorted(self):
        for card_id in ["03005", "01002", "02001"]:
            CardCoverage.RecordCardEnteredPlay(FakeFace(card_id))

        record = CardCoverage.EndGame(FakeWorld(), seed=1, scenario="rhino",
                                      heroes=["spider_man"], outcome="lost")

        self.assertEqual(list(record["cards"]["entered_play"]),
                         ["01002", "02001", "03005"])


################################################################################
# Factory attribution


class Inner:
    @staticmethod
    def Shared(ability):
        return ability


class FakeFactory(Inner):
    """Shaped like `AbilityFactory`: static methods spread over a mixin."""

    NOT_A_METHOD = 3

    @staticmethod
    def Direct(ability):
        return ability

    @staticmethod
    def Wrapping(ability):
        return FakeFactory.Shared(ability)

    @staticmethod
    def Several(*abilities):
        return list(abilities)

    @staticmethod
    def NotAnAbility():
        return "selector"

    @classmethod
    def NotStatic(cls):
        return None


class Stamped:
    def __init__(self):
        self.factory = ""


class TestInstrumentation(unittest.TestCase):

    def setUp(self):
        # `InstrumentClass` mutates the class it is given, so each test gets its
        # own subclass rather than a shared one carrying wrappers forward.
        self.factory = type("Factory", (FakeFactory,), {})

    def test_the_returned_ability_is_stamped(self):
        CardCoverage.InstrumentClass(self.factory, Stamped)

        self.assertEqual(self.factory.Direct(Stamped()).factory, "Direct")

    def test_a_nested_call_attributes_to_the_outermost_factory(self):
        """A card script names one method; that method may call others. The name
        the script named is the one `tools/cards/scripts.py` records statically,
        so it has to be the one recorded at runtime or the two cannot be
        subtracted from each other."""
        CardCoverage.InstrumentClass(self.factory, Stamped)

        self.assertEqual(self.factory.Wrapping(Stamped()).factory, "Wrapping")
        self.assertEqual(self.factory.Shared(Stamped()).factory, "Shared")

    def test_every_ability_in_a_returned_list_is_stamped(self):
        CardCoverage.InstrumentClass(self.factory, Stamped)

        abilities = self.factory.Several(Stamped(), Stamped())

        self.assertEqual([ability.factory for ability in abilities],
                         ["Several", "Several"])

    def test_a_non_ability_result_passes_through(self):
        CardCoverage.InstrumentClass(self.factory, Stamped)

        self.assertEqual(self.factory.NotAnAbility(), "selector")

    def test_only_public_static_methods_are_wrapped(self):
        CardCoverage.InstrumentClass(self.factory, Stamped)

        self.assertIsNone(getattr(self.factory.NotStatic, FACTORY_MARK, None))
        self.assertEqual(self.factory.NOT_A_METHOD, 3)

    def test_instrumenting_twice_does_not_stack_wrappers(self):
        first = CardCoverage.InstrumentClass(self.factory, Stamped)
        wrapped = self.factory.Direct
        second = CardCoverage.InstrumentClass(self.factory, Stamped)

        self.assertEqual(first, second)
        self.assertIs(self.factory.Direct, wrapped)

    def test_find_static_method_walks_the_mro_and_rejects_the_rest(self):
        self.assertIsNotNone(CardCoverage.FindStaticMethod(self.factory, "Shared"))
        self.assertIsNone(CardCoverage.FindStaticMethod(self.factory, "NotStatic"))
        self.assertIsNone(CardCoverage.FindStaticMethod(self.factory, "NOT_A_METHOD"))
        self.assertIsNone(CardCoverage.FindStaticMethod(self.factory, "Absent"))


################################################################################
# The universe


def WriteDataset(folder, cards):
    path = os.path.join(folder, "cards.json")
    with open(path, "w", encoding="utf-8") as handle:
        json.dump({"dataset_version": 1, "cards": cards}, handle)
    return path


def Card(card_id, factories, *, name="", pack="core", script=True):
    entry = {"card_id": card_id, "name": name, "pack": pack, "engine": {}}
    if script:
        entry["engine"]["script"] = {
            "path": f"cards/pack/{pack}/{card_id}.py",
            "ability_factories": list(factories),
        }
    return entry


class TestUniverse(unittest.TestCase):

    def setUp(self):
        self.folder = tempfile.mkdtemp()
        self.addCleanup(lambda: __import__("shutil").rmtree(self.folder, ignore_errors=True))

    def test_only_cards_with_a_script_are_in_the_universe(self):
        """A card with no script has no card-specific ability to exercise.
        Counting it as unreached would put a permanent floor under the miss rate."""
        path = WriteDataset(self.folder, [
            Card("01001a", ["Alpha"]),
            Card("01094", [], script=False),
        ])

        universe = coverage_report.LoadUniverse(path)

        self.assertEqual(universe.cards, ["01001a"])
        self.assertEqual(universe.factories, ["Alpha"])

    def test_factory_weight_is_how_many_scripts_name_it(self):
        path = WriteDataset(self.folder, [
            Card("a", ["Alpha", "Beta"]),
            Card("b", ["Alpha"]),
            Card("c", ["Alpha"]),
        ])

        universe = coverage_report.LoadUniverse(path)

        self.assertEqual(universe.factory_cards, {"Alpha": 3, "Beta": 1})

    def test_a_missing_dataset_is_refused_by_name(self):
        with self.assertRaises(coverage_report.DatasetMissing):
            coverage_report.LoadUniverse(os.path.join(self.folder, "absent.json"))

    def test_an_unreadable_dataset_is_refused(self):
        path = os.path.join(self.folder, "cards.json")
        with open(path, "w", encoding="utf-8") as handle:
            handle.write("{not json")

        with self.assertRaises(coverage_report.DatasetMissing):
            coverage_report.LoadUniverse(path)

    def test_a_dataset_without_cards_is_refused(self):
        path = os.path.join(self.folder, "cards.json")
        with open(path, "w", encoding="utf-8") as handle:
            json.dump({"dataset_version": 1}, handle)

        with self.assertRaises(coverage_report.DatasetMissing):
            coverage_report.LoadUniverse(path)


################################################################################
# The report


def Game(**overrides):
    game = {
        "seed": 1,
        "scenario": "rhino",
        "heroes": ["spider_man"],
        "player_count": 1,
        "expert": False,
        "challenges": [],
        "rules": [],
        "modes": {"heroic": 0, "skirmish": False, "campaign": False},
        "outcome": "won",
        "cards": {"present": [], "entered_play": {}, "resolved": {}},
        "stages": {},
        "factories": {},
        "triggers": {},
        "ability_types": {},
    }
    game.update(overrides)
    return game


class TestAccumulate(unittest.TestCase):

    def test_present_counts_games_not_copies(self):
        """Three copies of a card in one deck is one game's worth of presence."""
        totals = coverage_report.Accumulate([
            Game(cards={"present": ["a", "b"], "entered_play": {}, "resolved": {}}),
            Game(cards={"present": ["a"], "entered_play": {}, "resolved": {}}),
        ])

        self.assertEqual(totals.present, {"a": 2, "b": 1})

    def test_counts_add_across_games(self):
        totals = coverage_report.Accumulate([
            Game(factories={"Alpha": 2}),
            Game(factories={"Alpha": 3, "Beta": 1}),
        ])

        self.assertEqual(totals.factories, {"Alpha": 5, "Beta": 1})


class TestRanking(unittest.TestCase):

    def setUp(self):
        self.folder = tempfile.mkdtemp()
        self.addCleanup(lambda: __import__("shutil").rmtree(self.folder, ignore_errors=True))
        self.path = WriteDataset(self.folder, [
            Card("a", ["Alpha", "Beta"], name="Ay"),
            Card("b", ["Alpha"], name="Bee"),
            Card("c", ["Gamma"], name="Cee"),
            Card("d", ["Beta", "Gamma"], name="Dee"),
        ])
        self.universe = coverage_report.LoadUniverse(self.path)

    def test_unreached_triggers_are_ranked_by_how_many_cards_need_them(self):
        ranked = coverage_report.NeverFiredFactories(self.universe, ["Alpha"])

        self.assertEqual([entry["factory"] for entry in ranked], ["Beta", "Gamma"])
        self.assertEqual([entry["cards"] for entry in ranked], [2, 2])

    def test_a_fired_trigger_is_not_listed(self):
        ranked = coverage_report.NeverFiredFactories(
            self.universe, ["Alpha", "Beta", "Gamma"])

        self.assertEqual(ranked, [])

    def test_unexercised_cards_are_ranked_by_what_they_would_unlock(self):
        never_fired = ["Beta", "Gamma"]
        ranked = coverage_report.NeverExercisedCards(self.universe, [], never_fired)

        # "d" registers both unreached triggers; "a" one of two; "b" none.
        self.assertEqual([entry["card_id"] for entry in ranked], ["d", "a", "c", "b"])
        self.assertEqual([entry["score"] for entry in ranked], [2, 1, 1, 0])
        self.assertEqual(ranked[0]["unfired"], ["Beta", "Gamma"])

    def test_a_resolved_card_is_not_listed(self):
        ranked = coverage_report.NeverExercisedCards(self.universe, ["a", "d"], ["Beta"])

        self.assertEqual([entry["card_id"] for entry in ranked], ["b", "c"])

    def test_present_is_not_exercised(self):
        """A card in a deck all game is still unexercised. Feeding `present`
        here instead of `resolved` is exactly the mistake the metric exists to
        stop, so the input is named and the ranking follows it."""
        ranked = coverage_report.NeverExercisedCards(self.universe, [], [])

        self.assertEqual(len(ranked), 4)


class TestBuild(unittest.TestCase):

    def setUp(self):
        self.folder = tempfile.mkdtemp()
        self.addCleanup(lambda: __import__("shutil").rmtree(self.folder, ignore_errors=True))
        self.universe = coverage_report.LoadUniverse(WriteDataset(self.folder, [
            Card("a", ["Alpha"]),
            Card("b", ["Beta"]),
        ]))

    def Build(self, games, universe=None, universe_error=""):
        return coverage_report.Build(
            games, generator="test", engine_version="0.0.0",
            universe=universe, universe_error=universe_error)

    def test_totals_separate_present_from_resolved(self):
        document = self.Build([Game(
            cards={"present": ["a", "b"], "entered_play": {"a": 1}, "resolved": {"a": 2}},
            factories={"Alpha": 2},
        )], universe=self.universe)

        self.assertEqual(document["totals"]["cards"],
                         {"present": 2, "entered_play": 1, "resolved": 1,
                          "universe": 2, "resolved_in_universe": 1})
        self.assertEqual(document["totals"]["factories"], {"fired": 1, "universe": 2})

    def test_a_card_outside_the_universe_does_not_inflate_the_ratio(self):
        """Vanilla minions and the engine's `rule_*` pseudo-cards resolve
        abilities but have no script, so they are not in the denominator. They
        must not be in the numerator either."""
        document = self.Build([Game(
            cards={"present": ["a"], "entered_play": {}, "resolved": {"a": 1, "rule_a": 4}},
        )], universe=self.universe)

        self.assertEqual(document["totals"]["cards"]["resolved"], 2)
        self.assertEqual(document["totals"]["cards"]["resolved_in_universe"], 1)

    def test_without_a_universe_the_report_says_so(self):
        """Emitting empty ranked lists would read as "nothing was missed"."""
        document = self.Build([Game()], universe=None, universe_error="no dataset")

        self.assertEqual(document["universe"], {"available": False, "reason": "no dataset"})
        self.assertNotIn("never_fired_factories", document)
        self.assertNotIn("never_exercised_cards", document)

    def test_reached_records_what_the_games_were_configured_as(self):
        document = self.Build([
            Game(scenario="rhino", heroes=["spider_man"], player_count=1),
            Game(scenario="klaw", heroes=["she_hulk", "thor"], player_count=2,
                 expert=True, modes={"heroic": 2, "skirmish": False, "campaign": True}),
        ], universe=self.universe)

        reached = document["reached"]
        self.assertEqual(reached["scenarios"], {"klaw": 1, "rhino": 1})
        self.assertEqual(reached["heroes"], {"she_hulk": 1, "spider_man": 1, "thor": 1})
        self.assertEqual(reached["player_counts"], {"1": 1, "2": 1})
        self.assertEqual(reached["difficulty"]["expert"], 1)
        self.assertEqual(reached["difficulty"]["standard"], 1)
        self.assertEqual(reached["difficulty"]["heroic"], {"0": 1, "2": 1})
        self.assertEqual(reached["difficulty"]["campaign"], 1)

    def test_stages_count_the_games_that_reached_them(self):
        document = self.Build([
            Game(stages={"01094": 1, "01095": 2}),
            Game(stages={"01094": 1}),
        ], universe=self.universe)

        self.assertEqual(document["reached"]["stages"], {
            "01094": {"stage": 1, "games": 2},
            "01095": {"stage": 2, "games": 1},
        })

    def test_the_document_is_json_serialisable_and_key_ordered(self):
        document = self.Build([Game(
            factories={"Beta": 1, "Alpha": 1},
        )], universe=self.universe)

        json.dumps(document)
        self.assertEqual(list(document["counts"]["factories"]), ["Alpha", "Beta"])

    def test_summarize_survives_a_report_with_no_universe(self):
        text = coverage_report.Summarize(self.Build([Game()], universe=None))

        self.assertIn("games:", text)


class TestRunArtefactPath(unittest.TestCase):
    """Where the per-run report lands.

    A bare filename would be resolved against the working directory, which for
    a development checkout is `py_src/` -- so a run that saved nothing used to
    drop a coverage report into the repository.
    """

    def setUp(self):
        from engine.device.manager.bot.runner import BotRunner

        self.runner = BotRunner
        original = BotRunner.save_folder
        self.addCleanup(lambda: setattr(BotRunner, "save_folder", original))

    def test_nothing_is_written_when_no_scene_was_saved(self):
        self.runner.save_folder = ""

        self.assertIsNone(self.runner.RunFilePath("bot-coverage"))

    def test_the_artefact_lands_beside_the_scenes(self):
        self.runner.save_folder = "./replays"

        path = self.runner.RunFilePath("bot-coverage")

        self.assertIsNotNone(path)
        self.assertTrue(os.path.basename(path).startswith("bot-coverage-"))
        self.assertEqual(os.path.dirname(path).replace("\\", "/"), "./replays")


class TestMerge(unittest.TestCase):

    def setUp(self):
        self.folder = tempfile.mkdtemp()
        self.addCleanup(lambda: __import__("shutil").rmtree(self.folder, ignore_errors=True))
        self.dataset = WriteDataset(self.folder, [
            Card("a", ["Alpha"]),
            Card("b", ["Beta"]),
        ])

    def WriteRun(self, name, games):
        path = os.path.join(self.folder, name)
        with open(path, "w", encoding="utf-8") as handle:
            json.dump({"engine_version": "1.2.3", "games": games}, handle)
        return path

    def test_games_of_concatenates_every_run(self):
        documents = [{"games": [Game(seed=1)]}, {"games": [Game(seed=2)]}, {}]

        self.assertEqual([game["seed"] for game in coverage_report.GamesOf(documents)],
                         [1, 2])

    def test_merging_reranks_from_the_raw_counts(self):
        """A trigger unreached in every run individually may still be reached by
        the corpus. Merging the ranked lists instead of the observations would
        report it as missing."""
        self.WriteRun("bot-coverage-one.json", [Game(factories={"Alpha": 1})])
        self.WriteRun("bot-coverage-two.json", [Game(factories={"Beta": 1})])

        document = report_cli.Merge(
            report_cli.Expand([self.folder]), dataset=self.dataset)

        self.assertEqual(document["never_fired_factories"], [])
        self.assertEqual(document["totals"]["games"], 2)

    def test_a_directory_expands_to_its_coverage_artefacts(self):
        self.WriteRun("bot-coverage-one.json", [Game()])
        self.WriteRun("not-coverage.json", [Game()])

        found = report_cli.Expand([self.folder])

        self.assertEqual([os.path.basename(path) for path in found],
                         ["bot-coverage-one.json"])

    def test_a_path_that_matches_nothing_is_kept_so_the_caller_is_named(self):
        absent = os.path.join(self.folder, "absent.json")

        self.assertEqual(report_cli.Expand([absent]), [os.path.normpath(absent)])

    def test_mixed_engine_versions_are_reported_rather_than_hidden(self):
        self.WriteRun("bot-coverage-one.json", [Game()])
        path = os.path.join(self.folder, "bot-coverage-two.json")
        with open(path, "w", encoding="utf-8") as handle:
            json.dump({"engine_version": "9.9.9", "games": [Game()]}, handle)

        document = report_cli.Merge(
            report_cli.Expand([self.folder]), dataset=self.dataset)

        self.assertEqual(document["engine_version"], "1.2.3+9.9.9")


if __name__ == "__main__":
    unittest.main()
