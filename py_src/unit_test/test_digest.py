"""What the v2 state digest promises, and what a replay does when it is broken.

The digest is a wire format (`docs/state-digest-v2.md`), so the properties worth
pinning are the ones a C# port could get subtly wrong and still look right:
the canonical serialisation, the order things appear in, what "absent" means,
and where a card sits when it is not in the ordinary part of its area.

Building a record is tested against stand-ins rather than a live world. The
builder reads a small, named set of attributes off a card; a fake that provides
exactly those is a sharper test than a booted engine, because it fails on the
thing being tested instead of on scenario setup. End-to-end agreement between a
recording and a replay is `python main.py -bot -bot_verify`, and cross-language
agreement is `datasets/digest/vectors.json`.
"""

import json
import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.controller.module import replay as replay_module
from engine.controller.module.replay import IsIgnorableMismatch, InputModule
from game.world import digest


################################################################################
# Stand-ins for the handful of attributes the builder reads


class FakeFlags:
    def __init__(self, *, in_play=False, status=False, boost=False):
        self.is_in_play = in_play
        self.is_status_area = status
        self.is_boost_area = boost


class FakeDeckType:
    def __init__(self, name):
        self.name = name


class FakeArea:
    def __init__(self, name, *, flags=None, bind_card=None):
        self.deck_type = FakeDeckType(name)
        self.flags = flags or FakeFlags()
        self.bind_card = bind_card
        self.cards = []
        self.removed_cards = []


class FakePaper:
    def __init__(self, card_id):
        self.card_id = card_id


class FakeFace:
    def __init__(self, card_id, fields=None):
        self.paper = FakePaper(card_id)
        self._fields = fields or {}

    def GetStateFields(self):
        return dict(self._fields)


class FakeOwner:
    def __init__(self, player_id, is_scenario=False):
        self.player_id = player_id
        self.is_scenario = is_scenario


SCENARIO = FakeOwner(0, is_scenario=True)


class FakeState:
    def __init__(self, face_up):
        self.is_face_up = face_up


class FakeCard:
    def __init__(self, object_id, card_id, area, *, fields=None,
                 face_up=True, owner=SCENARIO, controller=None):
        self.object_id = object_id
        self.face = FakeFace(card_id, fields)
        self.area = area
        self.state = FakeState(face_up)
        self._owner = owner
        self._controller = controller

    def IsOnField(self):
        return self.area.flags.is_in_play

    def GetController(self):
        return self._controller

    def GetOwner(self):
        return self._owner


class FakeObjectManager:
    def __init__(self, cards):
        self.card_dict = {card.object_id: card for card in cards}


class FakeWorld:
    def __init__(self, cards):
        self.object_manager = FakeObjectManager(cards)


def Document(cards):
    return digest.BuildDocument(FakeWorld(cards))


def Records(cards):
    return {record["id"]: record for record in Document(cards)["cards"]}


################################################################################


class TestSerialisation(unittest.TestCase):

    def test_canonical_form_has_no_whitespace(self):
        area = FakeArea("HandsArea")
        card = FakeCard(1, "01001b", area)
        area.cards.append(card)

        text = digest.Serialize(Document([card]))

        self.assertNotIn(" ", text)
        self.assertNotIn("\n", text)

    def test_per_card_keys_appear_in_the_contract_order(self):
        area = FakeArea("HandsArea")
        card = FakeCard(1, "01001b", area)
        area.cards.append(card)

        text = digest.Serialize(Document([card]))
        positions = [text.index(f'"{key}"') for key in digest.CARD_KEYS]

        self.assertEqual(positions, sorted(positions))

    def test_fields_are_ordered_by_code_point_not_by_insertion(self):
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(1, "01094", area, fields={"health": 14, "attack": 5, "Z": 1})
        area.cards.append(card)

        emitted = list(Records([card])[1]["fields"])

        self.assertEqual(emitted, sorted(emitted))
        self.assertEqual(emitted, ["Z", "attack", "health"])

    def test_non_ascii_field_names_are_escaped_the_same_way_everywhere(self):
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(1, "01094", area, fields={"t_CAFÉ": 1})
        area.cards.append(card)

        text = digest.Serialize(Document([card]))

        self.assertIn("t_CAF\\u00c9", text)
        self.assertNotIn("É", text)

    def test_cards_are_emitted_in_ascending_id_order(self):
        area = FakeArea("EncounterDeck")
        cards = [FakeCard(object_id, "01100", area) for object_id in (9, 2, 40)]
        area.cards.extend(cards)

        ids = [record["id"] for record in Document(cards)["cards"]]

        self.assertEqual(ids, [2, 9, 40])

    def test_empty_digest_constant_matches_what_the_builder_produces(self):
        self.assertEqual(digest.EMPTY_DIGEST, digest.Serialize(Document([])))

    def test_round_trips_through_parse(self):
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(1, "01094", area, fields={"health": 14})
        area.cards.append(card)

        text = digest.Serialize(Document([card]))

        self.assertEqual(digest.Parse(text), json.loads(text))

    def test_parse_refuses_something_that_is_not_a_digest(self):
        """Everything unreadable must be `ValueError`, because that is what the
        replay comparison catches -- a corrupt corpus file has to come back as a
        rejected step rather than as an exception through the replay loop."""
        for bad in (
            '{"1":27,"9":-2}',              # a v1 value
            'not json at all',             # not JSON
            '[]',                          # not an object
            '{"v":2}',                     # no cards
            '{"v":2,"cards":{}}',          # cards is not an array
            '{"v":2,"cards":[3]}',         # a record is not an object
            '{"v":2,"cards":[{"card":"01094"}]}',   # a record has no id
            '{"v":2,"cards":[{"id":"49"}]}',        # id is not an integer
        ):
            with self.subTest(bad=bad):
                with self.assertRaises(ValueError):
                    digest.Parse(bad)

    def test_diff_reports_a_truncated_record_rather_than_failing_on_it(self):
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(49, "01094", area, fields={"health": 14})
        area.cards.append(card)

        ids, report = digest.Diff('{"v":2,"cards":[{"id":49}]}',
                                  digest.Serialize(Document([card])))

        self.assertEqual(ids, [49])
        self.assertIn("c49", report)

    def test_fingerprint_follows_the_text(self):
        self.assertEqual(digest.Fingerprint("a"), digest.Fingerprint("a"))
        self.assertNotEqual(digest.Fingerprint("a"), digest.Fingerprint("b"))


class TestPosition(unittest.TestCase):

    def test_index_records_the_position_within_the_zone(self):
        area = FakeArea("PlayerDeck")
        cards = [FakeCard(object_id, "01100", area) for object_id in (1, 2, 3)]
        area.cards.extend(cards)

        records = Records(cards)

        self.assertEqual([records[i]["index"] for i in (1, 2, 3)], [0, 1, 2])

    def test_deck_order_is_visible_rather_than_summarised_as_top_and_bottom(self):
        """v1 said `-3` for the top and `-4` for the bottom and nothing else, so
        a shuffle that left both in place was invisible to it."""
        area = FakeArea("EncounterDeck")
        cards = [FakeCard(object_id, "01100", area) for object_id in (1, 2, 3, 4)]
        area.cards.extend(cards)

        before = Records(cards)
        area.cards[1], area.cards[2] = area.cards[2], area.cards[1]
        after = Records(cards)

        self.assertNotEqual(before[2], after[2])
        self.assertEqual(after[2]["index"], 2)
        self.assertEqual(after[3]["index"], 1)

    def test_a_detached_card_is_in_a_zone_of_its_own(self):
        area = FakeArea("UpgradesArea", flags=FakeFlags(in_play=True))
        detached = FakeCard(1, "01099", area)
        area.removed_cards.append(detached)

        record = Records([detached])[1]

        self.assertEqual(record["zone"], "UpgradesArea" + digest.SUFFIX_REMOVED)
        self.assertEqual(record["index"], 0)

    def test_a_card_in_neither_list_is_marked_rather_than_guessed_at(self):
        area = FakeArea("RemovedArea")
        stray = FakeCard(1, "01100", area)

        record = Records([stray])[1]

        self.assertEqual(record["zone"], "RemovedArea" + digest.SUFFIX_ABSENT)
        self.assertEqual(record["index"], -1)

    def test_host_names_the_card_an_attachment_or_status_hangs_off(self):
        villain_area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        villain = FakeCard(49, "01094", villain_area)
        villain_area.cards.append(villain)

        status_area = FakeArea("StatusArea", flags=FakeFlags(status=True),
                               bind_card=villain)
        tough = FakeCard(81, "tough", status_area)
        status_area.cards.append(tough)

        records = Records([villain, tough])

        self.assertEqual(records[81]["host"], 49)
        self.assertEqual(records[49]["host"], -1)


class TestCoverage(unittest.TestCase):

    def test_every_card_is_present_including_the_first_one_allocated(self):
        """v1 dropped id 0 by number, which excluded whatever card happened to
        be created first rather than a card identified by what it is."""
        area = FakeArea("RuleArea", flags=FakeFlags(in_play=True))
        cards = [FakeCard(object_id, "rule_a", area) for object_id in (0, 1)]
        area.cards.extend(cards)

        self.assertEqual(sorted(Records(cards)), [0, 1])

    def test_a_boost_card_carries_its_state(self):
        """v1 could not see boost cards at all, though their icons decide how
        much damage a villain activation deals."""
        area = FakeArea("BoostingArea", flags=FakeFlags(boost=True))
        card = FakeCard(72, "01188", area, fields={"boost_const": 2})
        area.cards.append(card)

        self.assertEqual(Records([card])[72]["fields"], {"boost_const": 2})

    def test_a_card_out_of_play_carries_its_fields_too(self):
        # MARVEL-59. v1 interrogated only cards in play, v2 kept that boundary
        # and added boost areas, and this removes it: the zone a card is in no
        # longer decides whether the oracle can see its state. Otherwise a card
        # modified on its way into a deck is invisible until it comes back.
        area = FakeArea("PlayerDeck")
        card = FakeCard(1, "01100", area, fields={"attack": 3})
        area.cards.append(card)

        record = Records([card])[1]

        self.assertEqual(record["fields"], {"attack": 3})
        self.assertEqual(record["zone"], "PlayerDeck")

    def test_every_zone_is_interrogated_alike(self):
        # The rule is now "every card", so no zone may be special-cased.
        for zone, flags in (
            ("PlayerDeck", FakeFlags()),
            ("HandsArea", FakeFlags()),
            ("EncounterDiscardPile", FakeFlags()),
            ("VictoryDisplay", FakeFlags()),
            ("RemovedArea", FakeFlags()),
            ("VillainArea", FakeFlags(in_play=True)),
            ("StatusArea", FakeFlags(status=True)),
            ("BoostingArea", FakeFlags(boost=True)),
        ):
            area = FakeArea(zone, flags=flags)
            card = FakeCard(1, "01100", area, fields={"attack": 3})
            area.cards.append(card)

            self.assertEqual(Records([card])[1]["fields"], {"attack": 3}, zone)

    def test_a_card_with_no_state_still_reports_an_empty_object(self):
        # `{}` now means "this card has no registered fields", not "the digest
        # declined to look". A port must not conflate them.
        area = FakeArea("PlayerDeck")
        card = FakeCard(1, "rule_a", area, fields={})
        area.cards.append(card)

        self.assertEqual(Records([card])[1]["fields"], {})

    def test_a_face_down_card_is_labelled_rather_than_hidden(self):
        """The digest is an engine-internal oracle, never a client payload, so
        it records the truth and says that the card is face down."""
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(1, "01094", area, fields={"health": 14}, face_up=False)
        area.cards.append(card)

        record = Records([card])[1]

        self.assertFalse(record["face_up"])
        self.assertEqual(record["fields"], {"health": 14})


class TestOwner(unittest.TestCase):

    def test_scenario_owned_cards_report_minus_one(self):
        area = FakeArea("EncounterDeck")
        card = FakeCard(1, "01100", area, owner=SCENARIO)
        area.cards.append(card)

        self.assertEqual(Records([card])[1]["owner"], -1)

    def test_a_player_owned_card_reports_that_player(self):
        area = FakeArea("HandsArea")
        card = FakeCard(1, "01100", area, owner=FakeOwner(1))
        area.cards.append(card)

        self.assertEqual(Records([card])[1]["owner"], 1)

    def test_control_beats_ownership_for_a_card_in_play(self):
        area = FakeArea("AlliesArea", flags=FakeFlags(in_play=True))
        card = FakeCard(1, "01100", area, owner=FakeOwner(0),
                        controller=FakeOwner(1))
        area.cards.append(card)

        self.assertEqual(Records([card])[1]["owner"], 1)


class TestDiff(unittest.TestCase):

    def Two(self, before_fields, after_fields):
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(49, "01094", area, fields=before_fields)
        area.cards.append(card)
        recorded = digest.Serialize(Document([card]))
        card.face = FakeFace("01094", after_fields)
        current = digest.Serialize(Document([card]))
        return digest.Diff(recorded, current)

    def test_a_changed_field_is_named_with_both_values(self):
        ids, report = self.Two({"health": 14}, {"health": 12})

        self.assertEqual(ids, [49])
        self.assertIn("health", report)
        self.assertIn("14 -> 12", report)

    def test_the_report_names_the_card_not_only_its_object_id(self):
        _, report = self.Two({"health": 14}, {"health": 12})

        self.assertIn("c49", report)
        self.assertIn("01094", report)

    def test_a_field_that_disappears_reads_as_absent(self):
        _, report = self.Two({"health": 14, "t_BRUTE": 1}, {"health": 14})

        self.assertIn("t_BRUTE", report)
        self.assertIn("1 -> -", report)

    def test_offsetting_changes_are_reported_rather_than_cancelling(self):
        """The whole point. Under v1 both cards summed to the same integer and
        the mismatch table printed no row at all."""
        ids, report = self.Two({"attack": 5, "t_BRUTE": 1},
                               {"attack": 6, "t_BRUTE": 0})

        self.assertEqual(ids, [49])
        self.assertIn("attack", report)
        self.assertIn("t_BRUTE", report)

    def test_a_card_present_on_only_one_side_is_called_out(self):
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(49, "01094", area)
        area.cards.append(card)

        ids, report = digest.Diff(digest.EMPTY_DIGEST, digest.Serialize(Document([card])))

        self.assertEqual(ids, [49])
        self.assertIn("only in the current state", report)

    def test_a_move_between_zones_is_reported(self):
        source = FakeArea("HandsArea")
        card = FakeCard(9, "01100", source)
        source.cards.append(card)
        recorded = digest.Serialize(Document([card]))

        target = FakeArea("DiscardPile")
        card.area = target
        target.cards.append(card)
        ids, report = digest.Diff(recorded, digest.Serialize(Document([card])))

        self.assertEqual(ids, [9])
        self.assertIn("HandsArea -> DiscardPile", report)

    def test_identical_digests_yield_no_ids(self):
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(49, "01094", area, fields={"health": 14})
        area.cards.append(card)
        text = digest.Serialize(Document([card]))

        self.assertEqual(digest.Diff(text, text), ([], ""))


class TestMismatchVerdict(unittest.TestCase):
    """MARVEL-43: a divergence must not be accepted by default.

    Carried over from `unit_test/test_replay_crc.py`, which pinned the same rule
    against the v1 digest. The rule is unchanged; only the digest it is asked
    about is. One case is deliberately different -- see the last test.
    """

    def test_a_mismatch_is_rejected_when_nothing_is_ignorable(self):
        # The headline bug: this returned True for every mismatch, because the
        # default empty ignore list made the filtered sequence empty.
        self.assertFalse(IsIgnorableMismatch([12], []))
        self.assertFalse(IsIgnorableMismatch([12, 34, 56], []))

    def test_a_mismatch_is_accepted_when_every_differing_id_is_ignorable(self):
        self.assertTrue(IsIgnorableMismatch([12], [12, 34]))
        self.assertTrue(IsIgnorableMismatch([12, 34], [12, 34]))
        self.assertTrue(IsIgnorableMismatch([34, 12], [12, 34]))

    def test_one_unignorable_id_rejects_the_whole_mismatch(self):
        self.assertFalse(IsIgnorableMismatch([12, 99], [12, 34]))
        self.assertFalse(IsIgnorableMismatch([99, 12, 34], [12, 34]))

    def test_card_id_zero_is_ignorable_like_any_other(self):
        # The old expression yielded the id itself, and 0 is falsy, so an
        # explicitly ignored id 0 was rejected.
        self.assertTrue(IsIgnorableMismatch([0], [0]))

    def test_no_differing_ids_is_rejected_rather_than_vacuously_accepted(self):
        """Deliberately changed from the v1 rule, which accepted this.

        Under v1 it was barely reachable and `test_replay_crc.py` recorded the
        behaviour without endorsing it. Under v2 it is reachable and means
        something definite: the two digest strings differed while no card record
        did, so the difference is in the envelope and no card id can explain it.
        """
        self.assertFalse(IsIgnorableMismatch([], []))
        self.assertFalse(IsIgnorableMismatch([], [12, 34]))


class TestReplayComparison(unittest.TestCase):
    """The glue between a recorded digest and the verdict a replay step returns.

    `-bot_verify` proves the matching case end to end on a real game. What it
    cannot prove is the failing case, because a correct engine never produces
    one -- so the divergence is injected here.
    """

    def Module(self, recorded, calculated, *, scene_version="9.9.9"):
        step = mock.Mock()
        step.digest = recorded

        manager = mock.Mock()
        manager.game.scene.version = scene_version

        module = InputModule(manager)
        module.SetReplayInputs([step])
        module.calculated_digest = calculated
        return module, step

    def Run(self, module, *, is_puzzle=False, check_crc=True,
            in_testing=False, ignore=()):
        """`GetReplayOperation` with the engine singletons stubbed out."""
        engine_stub = mock.Mock()
        engine_stub.game.controller_manager.console.debug_cmds = ""
        engine_stub.in_unit_test = False
        test_stub = mock.Mock()
        test_stub.IsInTesting.return_value = in_testing

        with mock.patch.object(replay_module, "Log") as log:
            with mock.patch.object(replay_module.DIGEST_IGNORE_IDS, "value", list(ignore)):
                with mock.patch("engine.Engine", engine_stub):
                    with mock.patch("game.test.Test", test_stub):
                        result = module.GetReplayOperation(is_puzzle, check_crc=check_crc)
        return result, log

    def Digest(self, fields):
        area = FakeArea("VillainArea", flags=FakeFlags(in_play=True))
        card = FakeCard(49, "01094", area, fields=fields)
        area.cards.append(card)
        return digest.Serialize(Document([card]))

    def test_an_agreeing_digest_passes(self):
        text = self.Digest({"health": 14})
        module, step = self.Module(text, text)

        (operation, ok), _ = self.Run(module)

        self.assertIs(operation, step)
        self.assertTrue(ok)

    def test_a_divergence_is_rejected_and_named(self):
        module, _ = self.Module(self.Digest({"health": 14}),
                                self.Digest({"health": 12}))

        (_, ok), log = self.Run(module)

        self.assertFalse(ok)
        reported = log.Assert.call_args[0][1]
        self.assertIn("c49", reported)
        self.assertIn("health", reported)
        self.assertIn("14 -> 12", reported)

    def test_offsetting_changes_are_rejected_rather_than_cancelling(self):
        """v1's sum made this pair identical, so the step passed silently."""
        module, _ = self.Module(self.Digest({"attack": 5, "t_BRUTE": 1}),
                                self.Digest({"attack": 6, "t_BRUTE": 0}))

        (_, ok), _ = self.Run(module)

        self.assertFalse(ok)

    def test_an_unreadable_scene_version_explains_rather_than_raising(self):
        """The version string came out of a file, and this is a warning helper."""
        module, step = self.Module("", self.Digest({"health": 14}),
                                   scene_version="not-a-version")

        (operation, ok), log = self.Run(module)

        self.assertIs(operation, step)
        self.assertTrue(ok)
        self.assertIn("unreadable scene version", log.Warn.call_args[0][1])

    def test_a_scene_older_than_v2_replays_on_its_inputs_with_a_warning(self):
        module, step = self.Module("", self.Digest({"health": 14}),
                                   scene_version="0.5.9.201")

        (operation, ok), log = self.Run(module)

        self.assertIs(operation, step)
        self.assertTrue(ok)
        self.assertIn("predates the v2 digest", log.Warn.call_args[0][1])

    def test_an_unreadable_recorded_digest_is_rejected_not_ignored(self):
        module, _ = self.Module("not json at all", self.Digest({"health": 14}))

        (_, ok), log = self.Run(module)

        self.assertFalse(ok)
        self.assertIn("unreadable digest", log.Assert.call_args[0][1])

    def test_an_unreadable_digest_is_rejected_even_with_an_ignore_list(self):
        """It yields no differing ids, so `all(...)` over them would be vacuously
        true and the ignore list would wave a corrupt recording through."""
        module, _ = self.Module("not json at all", self.Digest({"health": 14}))

        (_, ok), _ = self.Run(module, ignore=[49])

        self.assertFalse(ok)

    def test_a_mismatch_is_accepted_when_the_differing_card_is_ignorable(self):
        module, _ = self.Module(self.Digest({"health": 14}),
                                self.Digest({"health": 12}))

        (_, ok), _ = self.Run(module, ignore=[49])

        self.assertTrue(ok)

    def test_test_mode_rejects_even_an_ignorable_mismatch(self):
        """`-bot_verify` sets `Test.is_in_test`, and that path does not consult
        the ignore list at all. Unchanged from the v1 behaviour."""
        module, _ = self.Module(self.Digest({"health": 14}),
                                self.Digest({"health": 12}))

        (_, ok), _ = self.Run(module, in_testing=True, ignore=[49])

        self.assertFalse(ok)

    def test_a_puzzle_skips_the_check_entirely(self):
        module, step = self.Module(self.Digest({"health": 14}),
                                   self.Digest({"health": 12}))

        (operation, ok), _ = self.Run(module, is_puzzle=True)

        self.assertIs(operation, step)
        self.assertTrue(ok)

    def test_check_crc_false_skips_the_check_entirely(self):
        """`EventManager`'s fast-undo lookahead replays with `check_crc=False`."""
        module, step = self.Module(self.Digest({"health": 14}),
                                   self.Digest({"health": 12}))

        (operation, ok), _ = self.Run(module, check_crc=False)

        self.assertIs(operation, step)
        self.assertTrue(ok)


if __name__ == "__main__":
    unittest.main()
