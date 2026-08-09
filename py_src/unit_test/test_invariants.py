"""What the runtime invariant checker promises, rule by rule.

Self-play is where these rules get *applied*; this is where they get proved. A
correct engine never produces a violation, so a bot game can only ever show that
nothing fired -- it cannot show that a rule would fire if the state went wrong,
and it cannot show that the sentinels are the ones intended. Both are injected
here.

Built against stand-ins rather than a live world, for the reason
`unit_test/test_digest.py` gives: the rules read a small, named set of attributes,
so a fake that provides exactly those fails on the thing being tested instead of
on scenario setup. `game/world/invariants.py` duck-types everything it touches
precisely so this is possible.

Each rule gets both directions -- the legal state it must accept and the broken
one it must reject -- plus its sentinel, because a sentinel nobody tests is a
comment.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from core.errors import EngineIntegrityError
from game.world import invariants
from game.world.invariants import InvariantViolation, Progress, Violation


################################################################################
# Stand-ins for the attributes the rules read


class FakeFlags:
    def __init__(self, *, in_play=False):
        self.is_in_play = in_play


class FakeDeckType:
    def __init__(self, name):
        self.name = name


class FakeArea:
    def __init__(self, name, *, in_play=False, bind_card=None):
        self.deck_type = FakeDeckType(name)
        self.flags = FakeFlags(in_play=in_play)
        self.bind_card = bind_card
        self.cards = []
        self.removed_cards = []

    def Hold(self, *cards):
        for card in cards:
            card.area = self
            self.cards.append(card)
        return self


class FakePaper:
    def __init__(self, card_id):
        self.card_id = card_id


class FakeAbility:
    def __init__(self, when):
        self.when = when


class FakeFaceAbility:
    def __init__(self, abilities):
        self.abilities = list(abilities)


class FakeFace:
    def __init__(self, card_id, *, infinite_health=False, abilities=()):
        self.paper = FakePaper(card_id)
        self.is_infinite_health = infinite_health
        self.ability = FakeFaceAbility(abilities)


class FakeCounter:
    def __init__(self, counters=None):
        self.counters = dict(counters or {})


class FakeToken:
    def __init__(self, tokens=None):
        self.token = dict(tokens or {})


class FakeHealth:
    def __init__(self, health=0, max_health=0):
        self.health = health
        self.max_health = max_health


class FakeComponents:
    """Mirrors `Card.Components`: named sub-objects, some of which own a deck.

    `GetAll()` is how `_CollectAreas` reaches the upgrade and status decks,
    which hang off a card rather than off the world.
    """

    def __init__(self, counter, token, health):
        self.counter = counter
        self.token = token
        self.health = health
        self.inventory = FakeDeckHolder()
        self.status = FakeDeckHolder()

    def GetAll(self):
        return [self.counter, self.token, self.health,
                self.inventory, self.status]


class FakeDeckHolder:
    """A component that owns a deck, the way `Inventory` and `Status` do."""

    def __init__(self):
        self.deck = None


class FakeState:
    def __init__(self, ready=True):
        self.is_ready = ready


class FakeCard:
    def __init__(self, object_id, card_id="01100", *, ready=True, counters=None,
                 tokens=None, health=0, max_health=0, infinite_health=False,
                 abilities=()):
        self.object_id = object_id
        self.face = FakeFace(card_id, infinite_health=infinite_health,
                             abilities=abilities)
        self.state = FakeState(ready)
        self.components = FakeComponents(
            FakeCounter(counters), FakeToken(tokens),
            FakeHealth(health, max_health))
        self.area = None


class FakeObjectManager:
    def __init__(self, cards):
        self.card_dict = {card.object_id: card for card in cards}


class FakeReplay:
    def __init__(self, step_id=0, recorded=0):
        self.current_step_id = step_id
        self.history_inputs = [None] * recorded


class FakeControllerManager:
    def __init__(self, replay):
        self.replay = replay


class FakePhase:
    def __init__(self, state):
        self.state = state


class FakePlayer:
    def __init__(self, player_id, hand_area, hand_size, *, eliminated=False):
        self.player_id = player_id
        self.hand_cards = hand_area
        self.hand_size = hand_size
        self.is_eliminated = eliminated


class FakeScenario:
    pass


class FakeWorld:
    def __init__(self, cards, *, players=(), phase=None, round_id=0, phase_id=0,
                 step_id=0, recorded=0, extra_areas=(), listed_areas=()):
        self.object_manager = FakeObjectManager(cards)
        self.scenario = FakeScenario()
        self.const_seat_order_players = list(players)
        self.phase = FakePhase(phase)
        self.round_id = round_id
        self.phase_id = phase_id
        self.controller_manager = FakeControllerManager(
            FakeReplay(step_id, recorded if recorded else step_id))
        for index, area in enumerate(extra_areas):
            setattr(self, f"area_{index}", area)
        # `world.additional_decks` shape: held in a list, not named directly.
        self.additional_decks = list(listed_areas)


def Rules(violations):
    return sorted(violation.rule for violation in violations)


def Board(*cards, **kwargs):
    """A world holding `cards`, each already placed in an area."""
    return FakeWorld(list(cards), **kwargs)


################################################################################


class TestZones(unittest.TestCase):

    def test_a_card_in_one_zone_is_accepted(self):
        card = FakeCard(1)
        FakeArea("HandsArea").Hold(card)

        self.assertEqual(invariants.Check(Board(card)), [])

    def test_a_card_in_two_zones_is_rejected(self):
        """The headline rule. The digest cannot see this: `_BuildPositionIndex`
        keeps whichever slot it walked last, so the duplicate is recorded once
        and reproduces from the recording perfectly."""
        card = FakeCard(1, "01100")
        hand = FakeArea("HandsArea").Hold(card)
        discard = FakeArea("DiscardPile")
        discard.cards.append(card)
        card.area = hand

        violations = invariants.Check(Board(card, extra_areas=[discard]))

        self.assertEqual(Rules(violations), ["zone/duplicate"])
        self.assertIn("HandsArea", violations[0].detail)
        self.assertIn("DiscardPile", violations[0].detail)

    def test_a_card_in_no_zone_is_rejected(self):
        card = FakeCard(1)
        card.area = FakeArea("RemovedArea")

        self.assertEqual(Rules(invariants.Check(Board(card))), ["zone/absent"])

    def test_a_card_sitting_somewhere_it_does_not_claim_is_rejected(self):
        card = FakeCard(1)
        discard = FakeArea("DiscardPile")
        discard.cards.append(card)
        card.area = FakeArea("HandsArea")

        violations = invariants.Check(Board(card, extra_areas=[discard]))

        self.assertEqual(Rules(violations), ["zone/unclaimed"])
        self.assertIn("DiscardPile", violations[0].detail)
        self.assertIn("HandsArea", violations[0].detail)

    def test_the_removed_list_is_a_zone_of_its_own_not_a_duplicate(self):
        """A detached attachment waits in `removed_cards`. That is one place,
        not two -- the same distinction `digest.SUFFIX_REMOVED` draws."""
        card = FakeCard(1)
        area = FakeArea("UpgradesArea", in_play=True)
        area.removed_cards.append(card)
        card.area = area

        self.assertEqual(invariants.Check(Board(card)), [])

    def test_a_deck_reachable_only_through_a_component_is_still_walked(self):
        """Upgrade and status decks hang off a card, not off the world, and a
        deck that no card claims as its `area` is reachable *only* that way. A
        collector that walked `card.area` alone -- which is all the digest does
        -- would never look inside one, so a card stranded there would read as
        living quietly in its claimed zone."""
        hero = FakeCard(1, "01001a")
        FakeArea("HeroArea", in_play=True).Hold(hero)
        upgrade = FakeCard(2, "01007")
        FakeArea("HandsArea").Hold(upgrade)

        upgrades = FakeArea("UpgradesArea", in_play=True, bind_card=hero)
        upgrades.cards.append(upgrade)          # stale: nothing claims this deck
        hero.components.inventory.deck = upgrades

        violations = invariants.Check(Board(hero, upgrade))

        self.assertEqual(Rules(violations), ["zone/duplicate"])
        self.assertIn("UpgradesArea", violations[0].detail)

    def test_a_deck_held_only_in_a_list_is_still_walked(self):
        """`SetAsideDeck.Create` appends its decks to `world.additional_decks`,
        and once the villain that owns them has advanced that list is the only
        handle on them. A collector that read named attributes alone would stop
        looking inside."""
        card = FakeCard(1, "01110")
        FakeArea("EncounterDeck").Hold(card)
        aside = FakeArea("AsideDeck")
        aside.cards.append(card)

        violations = invariants.Check(
            FakeWorld([card], listed_areas=[aside]))

        self.assertEqual(Rules(violations), ["zone/duplicate"])
        self.assertIn("AsideDeck", violations[0].detail)


class TestIdentity(unittest.TestCase):

    def test_a_card_the_object_manager_never_saw_is_rejected(self):
        """It can change the outcome of a game and never appear in a digest,
        because the digest is built from `card_dict`."""
        known = FakeCard(1)
        area = FakeArea("EncounterDeck").Hold(known)
        stranger = FakeCard(2, "01110")
        stranger.area = area
        area.cards.append(stranger)

        violations = invariants.Check(FakeWorld([known]))

        self.assertIn("identity/unregistered", Rules(violations))

    def test_a_host_the_object_manager_never_saw_is_rejected(self):
        """`digest._Record` writes `area.bind_card.object_id` straight onto the
        wire, so an unregistered host means an unresolvable id in the recording."""
        ghost = FakeCard(99, "01099")
        card = FakeCard(1)
        FakeArea("StatusArea", bind_card=ghost).Hold(card)

        self.assertEqual(Rules(invariants.Check(Board(card))), ["identity/host"])

    def test_a_registered_host_is_accepted(self):
        villain = FakeCard(49, "01095")
        FakeArea("VillainArea", in_play=True).Hold(villain)
        tough = FakeCard(81, "tough")
        FakeArea("StatusArea", bind_card=villain).Hold(tough)

        self.assertEqual(invariants.Check(Board(villain, tough)), [])


class TestNumbers(unittest.TestCase):

    def test_a_negative_counter_is_rejected(self):
        card = FakeCard(1, counters={"counter": -1})
        FakeArea("SupportsArea", in_play=True).Hold(card)

        violations = invariants.Check(Board(card))

        self.assertEqual(Rules(violations), ["counters/negative"])
        self.assertIn("counter = -1", violations[0].detail)

    def test_negative_threat_is_rejected_as_a_token(self):
        """`Scheme2.threat` is `GetTokens('threat')`, so the threat floor is the
        token floor -- one rule, not two that can disagree."""
        scheme = FakeCard(48, "01097b", tokens={"threat": -2})
        FakeArea("MainSchemesArea", in_play=True).Hold(scheme)

        violations = invariants.Check(Board(scheme))

        self.assertEqual(Rules(violations), ["tokens/negative"])
        self.assertIn("threat = -2", violations[0].detail)

    def test_threat_above_the_advance_threshold_is_accepted(self):
        """There is no upper bound: a scheme is not capped at its threshold, it
        advances when it reaches one, and being over it for the moment before
        that resolves is legal play."""
        scheme = FakeCard(48, "01097b", tokens={"threat": 99})
        FakeArea("MainSchemesArea", in_play=True).Hold(scheme)

        self.assertEqual(invariants.Check(Board(scheme)), [])

    def test_health_above_max_in_play_is_rejected(self):
        card = FakeCard(1, "01095", health=15, max_health=14)
        FakeArea("VillainArea", in_play=True).Hold(card)

        violations = invariants.Check(Board(card))

        self.assertEqual(Rules(violations), ["health/over-max"])
        self.assertIn("15", violations[0].detail)
        self.assertIn("14", violations[0].detail)

    def test_an_infinite_health_card_is_exempt_from_the_ceiling(self):
        card = FakeCard(1, "01095", health=1, max_health=0, infinite_health=True)
        FakeArea("VillainArea", in_play=True).Hold(card)

        self.assertEqual(invariants.Check(Board(card)), [])

    def test_negative_max_health_is_rejected(self):
        card = FakeCard(1, "01095", health=-1, max_health=-1)
        FakeArea("VillainArea", in_play=True).Hold(card)

        self.assertEqual(Rules(invariants.Check(Board(card))), ["health/max-negative"])

    def test_negative_health_in_play_is_the_pending_defeat_sentinel(self):
        """`UpdateHealth` writes it and `TakeDamageWithOverkillTarget` then asks
        for a "Simultaneous Overkill" order through `ChoiceOne` -- so the checker
        is looking straight at a unit standing at negative health, legally."""
        card = FakeCard(1, "01095", health=-3, max_health=14)
        FakeArea("VillainArea", in_play=True).Hold(card)

        self.assertEqual(invariants.Check(Board(card)), [])

    def test_negative_health_out_of_play_is_defeat_residue_not_a_violation(self):
        """A minion defeated by 2 overkill lands in the encounter discard pile
        at -2 and stays there: `Card.MoveToArea` resets ready, not health.
        Calibration caught this on the first multiplayer game."""
        card = FakeCard(1, "01120", health=-2, max_health=3)
        FakeArea("EncounterDiscardPile").Hold(card)

        self.assertEqual(invariants.Check(Board(card)), [])


class TestReadyState(unittest.TestCase):

    def test_an_exhausted_card_out_of_play_is_rejected(self):
        """`Card.MoveToArea` calls `ResetReady` on the way out of play exactly so
        this cannot happen."""
        card = FakeCard(1, ready=False)
        FakeArea("DiscardPile").Hold(card)

        violations = invariants.Check(Board(card))

        self.assertEqual(Rules(violations), ["ready/exhausted-out-of-play"])
        self.assertIn("DiscardPile", violations[0].detail)

    def test_an_exhausted_card_in_play_is_ordinary(self):
        card = FakeCard(1, ready=False)
        FakeArea("AlliesArea", in_play=True).Hold(card)

        self.assertEqual(invariants.Check(Board(card)), [])


class TestHandSize(unittest.TestCase):
    """Checked at `PlaceThreat` and nowhere else: that is the first named moment
    after `PlayerPhase.EndPhase` has run the discard step, and no encounter card
    has been dealt yet."""

    def Board(self, hand_size, hand, *, phase, eliminated=False):
        cards = [FakeCard(index + 1, **card) for index, card in enumerate(hand)]
        area = FakeArea("HandsArea").Hold(*cards)
        player = FakePlayer(0, area, hand_size, eliminated=eliminated)
        return FakeWorld(cards, players=[player], phase=phase)

    def Phase(self, name):
        from game.world.phase import Phase
        return getattr(Phase.State, name)

    def test_a_hand_over_the_limit_at_place_threat_is_rejected(self):
        world = self.Board(5, [{}] * 6, phase=self.Phase("PlaceThreat"))

        violations = invariants.Check(world)

        self.assertEqual(Rules(violations), ["hand/over-limit"])
        self.assertIn("6 cards", violations[0].detail)
        self.assertIn("hand size of 5", violations[0].detail)

    def test_a_hand_at_the_limit_is_accepted(self):
        world = self.Board(5, [{}] * 5, phase=self.Phase("PlaceThreat"))

        self.assertEqual(invariants.Check(world), [])

    def test_the_same_hand_mid_turn_is_not_checked(self):
        """Hand size is a limit at particular moments, not a continuous bound --
        a hero draws past it and plays back down."""
        world = self.Board(5, [{}] * 9, phase=self.Phase("PlayerTurn"))

        self.assertEqual(invariants.Check(world), [])

    def test_a_card_that_opts_out_does_not_count(self):
        """"28007" Connection to the Worldmind. The engine asks by sending
        `CheckIfFaceCountHandSize`; a read-only checker reads the trigger class
        off the ability instead."""
        from game.message import Message
        opt_out = {"card_id": "28007",
                   "abilities": [FakeAbility(Message.CheckIfFaceCountHandSize)]}
        world = self.Board(5, [{}] * 5 + [opt_out], phase=self.Phase("PlaceThreat"))

        self.assertEqual(invariants.Check(world), [])

    def test_an_eliminated_player_is_not_checked(self):
        world = self.Board(5, [{}] * 9, phase=self.Phase("PlaceThreat"),
                           eliminated=True)

        self.assertEqual(invariants.Check(world), [])


class TestReplayAgreement(unittest.TestCase):

    def test_a_step_counter_that_matches_the_history_is_accepted(self):
        card = FakeCard(1)
        FakeArea("HandsArea").Hold(card)

        self.assertEqual(invariants.Check(Board(card, step_id=7, recorded=7)), [])

    def test_a_step_counter_that_has_drifted_from_the_history_is_rejected(self):
        """Every saved scene pairs step n with `history_inputs[n]`. Drift means
        the digest recorded against a step was taken at a different moment."""
        card = FakeCard(1)
        FakeArea("HandsArea").Hold(card)

        violations = invariants.Check(Board(card, step_id=7, recorded=5))

        self.assertEqual(Rules(violations), ["replay/step-count"])
        self.assertIn("7", violations[0].detail)
        self.assertIn("5", violations[0].detail)


class TestProgress(unittest.TestCase):

    def Advance(self, progress, step_id, round_id=1, phase_id=1):
        return progress.Advance(step_id, round_id, phase_id)

    def test_a_game_moving_forward_is_accepted(self):
        progress = Progress()
        self.assertEqual(self.Advance(progress, 1, 1, 1), [])
        self.assertEqual(self.Advance(progress, 2, 1, 2), [])
        self.assertEqual(self.Advance(progress, 3, 2, 3), [])

    def test_one_step_backwards_is_the_failed_turn_option_sentinel(self):
        """`PlayerAction.AskChooseAbility` pops the recorded step when a chosen
        turn option fails to resolve, then asks again."""
        progress = Progress()
        self.Advance(progress, 9)

        self.assertEqual(self.Advance(progress, 8), [])

    def test_a_wild_rewind_is_rejected(self):
        progress = Progress()
        self.Advance(progress, 9)

        violations = self.Advance(progress, 2)

        self.assertEqual(Rules(violations), ["progress/step"])

    def test_a_round_going_backwards_is_rejected(self):
        progress = Progress()
        self.Advance(progress, 1, round_id=4)

        self.assertEqual(Rules(self.Advance(progress, 2, round_id=3)),
                         ["progress/round"])

    def test_a_phase_going_backwards_is_rejected(self):
        progress = Progress()
        self.Advance(progress, 1, phase_id=8)

        self.assertEqual(Rules(self.Advance(progress, 2, phase_id=7)),
                         ["progress/phase"])

    def test_a_reset_forgets_the_previous_game(self):
        """`ControllerManager.Setup` resets this, and a load, a replay and an
        undo all come back through it with a step counter that starts again."""
        progress = Progress()
        self.Advance(progress, 40, round_id=6, phase_id=12)
        progress.Reset()

        self.assertEqual(self.Advance(progress, 0, round_id=0, phase_id=0), [])

    def test_progress_rules_are_skipped_without_a_progress_object(self):
        card = FakeCard(1)
        FakeArea("HandsArea").Hold(card)

        self.assertEqual(invariants.Check(Board(card), None), [])


class TestReporting(unittest.TestCase):

    def test_a_violation_names_the_card_the_way_the_digest_diff_does(self):
        card = FakeCard(49, "01095", ready=False)
        FakeArea("DiscardPile").Hold(card)

        violations = invariants.Check(Board(card))

        self.assertEqual(violations[0].subject, "c49 01095")

    def test_the_report_puts_one_rule_on_each_line(self):
        report = invariants.Report([
            Violation("zone/duplicate", "c1 01100", "in 2 places"),
            Violation("tokens/negative", "c2 01097b", "threat = -1"),
        ])

        lines = report.splitlines()
        self.assertEqual(len(lines), 2)
        self.assertIn("zone/duplicate", lines[0])
        self.assertIn("tokens/negative", lines[1])

    def test_violations_come_back_in_a_stable_order(self):
        """Two runs that break the same way must produce the same text, or a
        report cannot be diffed."""
        first = FakeCard(2, ready=False)
        second = FakeCard(1, ready=False)
        FakeArea("DiscardPile").Hold(first, second)

        world = FakeWorld([first, second])

        self.assertEqual([v.subject for v in invariants.Check(world)],
                         ["c1 01100", "c2 01100"])


class TestFlagWiring(unittest.TestCase):
    """Where the on-by-default for the bot device is allowed to come from.

    `Engine.Initialize` forces it, next to the line that forces `EDITOR` off for
    the same device. The obvious alternative -- adding `-check_invariants` to the
    `bot` arg group beside `-device bot` and `-no_editor` -- looks equivalent,
    reads better, and silently breaks the off switch, so it is worth a test
    rather than a comment.
    """

    def test_the_flag_is_not_in_the_bot_arg_group(self):
        """Expanding a group calls `ConfigVariables.InitVariable` for each of its
        keys immediately, stamping `set_from = "CommandLine"`. The real command
        line is applied after that loop, and `SetValue` returns early when
        `set_from` already matches -- so `-bot -no_check_invariants` would set the
        variable to True and then discard the False. Verified by hand: with the
        flag in the group, the run manifest reported `check_invariants: true`
        even with `-no_check_invariants` on the command line."""
        from engine.config import ConfigVariables

        self.assertNotIn("check_invariants", ConfigVariables.group.get("bot", ""))

    def test_a_bool_in_an_arg_group_really_cannot_be_turned_off_again(self):
        """The trap itself, on a throwaway variable, so the reason above is not
        just a story. Any flag put in any group inherits this."""
        from engine.config import ConfigVariables

        name = "invariant_probe_flag"
        group = "invariant_probe_group"
        try:
            flag = ConfigVariables.Bool(name, False)
            ConfigVariables.SetGroupArgs(group, f"-{name}")

            ConfigVariables.ParseArguments([f"-{group}", f"-no_{name}"])
            ConfigVariables.SetupVariables([name])

            # What the caller asked for is False. What they get is True.
            self.assertIs(ConfigVariables.instance_command[name], False)
            self.assertTrue(flag.value)
        finally:
            ConfigVariables.variable_dict.pop(name, None)
            ConfigVariables.group.pop(group, None)
            ConfigVariables.instance_command.pop(name, None)


class TestViolationIsNotSwallowed(unittest.TestCase):

    def test_it_derives_from_the_class_log_oncrash_re_raises(self):
        """`EffectInvoker`, `Message2.Send` and `Engine.EngineRun` all catch
        broadly so one bad card cannot end the game. `Log.OnCrash` re-raises
        `EngineIntegrityError` regardless of the build, which is the only reason
        an abort here actually aborts."""
        self.assertTrue(issubclass(InvariantViolation, EngineIntegrityError))


################################################################################


class TestModuleWiring(unittest.TestCase):
    """The engine-side half: when it runs, what it writes, and what it raises."""

    def Module(self, *, enabled=True, violations=(), is_puzzle=False,
               save_path="./invariants/repro.json"):
        from engine.controller.module import invariants as module

        manager = mock.Mock()
        manager.replay.current_step_id = 12
        # Read off the session, not `Game.scene`: that property asserts rather
        # than returning None, and `TestRun.Run` deletes it between cases.
        manager.game.session.scene.is_puzzle = is_puzzle
        manager.game.session.scene.GetSaveFileName.return_value = "spider-man-rhino-(47)-(1)"
        manager.game.session.SaveScene.return_value = save_path

        invariant_module = module.InvariantModule(manager)

        patches = [
            mock.patch.object(module.CHECK_INVARIANTS, "value", enabled),
            mock.patch.object(module.INVARIANT_FOLDER, "value", "./invariants"),
            mock.patch.object(module, "Log"),
            mock.patch.object(module.FileManager, "MakeDir"),
            mock.patch.object(invariants, "Check", return_value=list(violations)),
        ]
        return module, invariant_module, manager, patches

    def Run(self, invariant_module, patches):
        world = mock.Mock()
        with patches[0], patches[1], patches[2] as log, patches[3], patches[4]:
            try:
                invariant_module.Check(world)
                raised = None
            except InvariantViolation as exc:
                raised = exc
        return raised, log

    def RunRaising(self, exc):
        """A rule that blows up rather than returning violations."""
        from engine.controller.module import invariants as module

        _, invariant_module, manager, patches = self.Module()
        patches[4] = mock.patch.object(invariants, "Check", side_effect=exc)
        return self.Run(invariant_module, patches) + (manager,)

    def test_nothing_runs_when_the_flag_is_off(self):
        module, invariant_module, _, patches = self.Module(
            enabled=False, violations=[Violation("zone/duplicate", "c1", "x")])

        raised, _ = self.Run(invariant_module, patches)

        self.assertIsNone(raised)

    def test_a_clean_world_is_silent(self):
        _, invariant_module, manager, patches = self.Module(violations=[])

        raised, log = self.Run(invariant_module, patches)

        self.assertIsNone(raised)
        manager.game.session.SaveScene.assert_not_called()
        log.Assert.assert_not_called()

    def test_a_violation_dumps_before_it_raises(self):
        """The log line is worth more with a path in it, and worth more still if
        the file is on disk when the process dies."""
        _, invariant_module, manager, patches = self.Module(
            violations=[Violation("zone/duplicate", "c1 01100", "in 2 places")])

        raised, log = self.Run(invariant_module, patches)

        self.assertIsNotNone(raised)
        manager.game.session.SaveScene.assert_called_once()
        reported = log.Assert.call_args[0][1]
        self.assertIn("step 12", reported)
        self.assertIn("zone/duplicate", reported)
        self.assertIn("./invariants/repro.json", reported)

    def test_the_repro_is_saved_deterministically(self):
        """A repro carrying a host fingerprint and a timestamp is not one that
        can be committed or handed to someone else. See MARVEL-27."""
        _, invariant_module, manager, patches = self.Module(
            violations=[Violation("zone/duplicate", "c1 01100", "in 2 places")])

        self.Run(invariant_module, patches)

        _, kwargs = manager.game.session.SaveScene.call_args
        self.assertTrue(kwargs["deterministic"])
        self.assertFalse(kwargs["delete_old"])

    def test_the_repro_name_carries_the_step_and_the_rule(self):
        _, invariant_module, manager, patches = self.Module(
            violations=[Violation("zone/duplicate", "c1 01100", "in 2 places")])

        self.Run(invariant_module, patches)

        path = manager.game.session.SaveScene.call_args[0][0]
        self.assertIn("step12", path)
        self.assertIn("zoneduplicate", path)

    def test_a_puzzle_board_raises_without_a_repro(self):
        """`Scene.Save` refuses a puzzle, and a puzzle is authored rather than
        generated, so there is nothing to reproduce from -- but the violation is
        still real."""
        _, invariant_module, manager, patches = self.Module(
            is_puzzle=True,
            violations=[Violation("zone/duplicate", "c1 01100", "in 2 places")])

        raised, log = self.Run(invariant_module, patches)

        self.assertIsNotNone(raised)
        manager.game.session.SaveScene.assert_not_called()
        self.assertIn("No repro", log.Assert.call_args[0][1])

    def test_a_failed_dump_does_not_replace_the_violation(self):
        _, invariant_module, manager, patches = self.Module(
            violations=[Violation("zone/duplicate", "c1 01100", "in 2 places")])
        manager.game.session.SaveScene.side_effect = OSError("disk full")

        raised, log = self.Run(invariant_module, patches)

        self.assertIsNotNone(raised)
        self.assertIn("zone/duplicate", str(raised))
        self.assertIn("No repro", log.Assert.call_args[0][1])

    def test_the_first_violation_names_the_exception(self):
        _, invariant_module, invariant_manager, patches = self.Module(
            violations=[Violation("zone/duplicate", "c1 01100", "in 2 places"),
                        Violation("tokens/negative", "c2 01097b", "threat = -1")])

        raised, _ = self.Run(invariant_module, patches)

        self.assertIn("zone/duplicate", str(raised))
        self.assertIn("c1 01100", str(raised))

    def test_a_rule_that_raises_fails_the_run_rather_than_going_quiet(self):
        """The sharp edge. `Log.OnCrash` re-raises `EngineIntegrityError` and
        swallows everything else, and `build.py` hardcodes `Build.release`, so a
        plain `AttributeError` out of a rule would be absorbed, play would carry
        on, and the run manifest would still say `check_invariants: true`. A run
        that cannot check must not be able to claim it did."""
        raised, _, _ = self.RunRaising(AttributeError("'Card' object has no attribute 'area'"))

        self.assertIsInstance(raised, InvariantViolation)
        self.assertIn("checker itself failed", str(raised))
        self.assertIn("AttributeError", str(raised))

    def test_a_broken_rule_does_not_pretend_to_have_a_repro(self):
        raised, _, manager = self.RunRaising(AttributeError("boom"))

        self.assertIsNotNone(raised)
        manager.game.session.SaveScene.assert_not_called()

    def test_a_violation_raised_from_inside_the_rules_passes_straight_through(self):
        """Not re-wrapped as a checker failure -- it is the ordinary result."""
        from game.world.invariants import InvariantViolation as Violation_

        raised, _, _ = self.RunRaising(Violation_("zone/duplicate at step 3"))

        self.assertIn("zone/duplicate", str(raised))
        self.assertNotIn("checker itself failed", str(raised))

    def test_a_deleted_scene_costs_the_repro_not_the_violation(self):
        """`TestRun.Run` does `del game.session.scene` between cases, and
        `Game.scene` asserts rather than returning None."""
        _, invariant_module, manager, patches = self.Module(
            violations=[Violation("zone/duplicate", "c1 01100", "in 2 places")])
        del manager.game.session.scene

        raised, log = self.Run(invariant_module, patches)

        self.assertIsNotNone(raised)
        self.assertIn("zone/duplicate", str(raised))
        self.assertIn("No repro", log.Assert.call_args[0][1])

    def test_clean_forgets_the_previous_game(self):
        _, invariant_module, _, _ = self.Module()
        invariant_module.progress.Advance(40, 6, 12)

        invariant_module.Clean()

        self.assertEqual(invariant_module.progress.step_id, -1)


if __name__ == "__main__":
    unittest.main()
