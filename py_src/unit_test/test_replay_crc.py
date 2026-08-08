"""`InputModule.GetReplayOperation` decides whether a digest mismatch is fatal.

The digest is the project's oracle: `replay.py` compares the recorded per-card
digest against the recalculated one on every replayed step, and the boolean it
returns is what stops a replay that has diverged. That boolean was wrong.

`all(x for x in diff_ids if x in CRC_IGNORE_IDS.value)` filters `diff_ids` down
to the ignorable ids and then tests *those* for truthiness, which is not the
question. `crc_ignore_ids` defaults to `[]`, so the filtered sequence was empty
and `all(<empty>)` was `True` -- every mismatch accepted.

See MARVEL-43 and `docs/state-digest-contract.md` (D4).

The bug was survivable only because the paths that matter return earlier:
`-bot_verify` sets `Test.is_in_test` before replaying, and the `IsInTesting`
branch rejects at `replay.py:186`. Live play and any non-test replay fell
through. Both halves are pinned here.

    python -m unittest unit_test.test_replay_crc
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported. That is the whole bootstrap this needs.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.controller.module.replay import (CRC_IGNORE_IDS, InputModule,
                                             IsIgnorableMismatch)
from game.test import Test


class IgnoreList:
    """Sets `crc_ignore_ids` for the duration of a block.

    `ConfigVariables` is process-global, so a test that left it set would leak
    into whatever ran next.
    """

    def __init__(self, ids) -> None:
        self.ids = ids

    def __enter__(self):
        self.original = CRC_IGNORE_IDS.value
        CRC_IGNORE_IDS.value = self.ids
        return self

    def __exit__(self, *exc) -> None:
        CRC_IGNORE_IDS.value = self.original


class InTesting:
    """Sets `Test.is_in_test`, restoring it afterwards."""

    def __init__(self, value: bool) -> None:
        self.value = value

    def __enter__(self):
        self.original = Test.is_in_test
        Test.is_in_test = self.value
        return self

    def __exit__(self, *exc) -> None:
        Test.is_in_test = self.original


class TestTheVerdict(unittest.TestCase):
    """The decision itself, with no replay machinery around it."""

    def test_a_mismatch_is_rejected_when_nothing_is_ignorable(self):
        # The headline bug: this returned True for every mismatch, because the
        # default empty ignore list made the filtered sequence empty.
        with IgnoreList([]):
            self.assertFalse(IsIgnorableMismatch([12]))
            self.assertFalse(IsIgnorableMismatch([12, 34, 56]))

    def test_a_mismatch_is_accepted_when_every_differing_id_is_ignorable(self):
        with IgnoreList([12, 34]):
            self.assertTrue(IsIgnorableMismatch([12]))
            self.assertTrue(IsIgnorableMismatch([12, 34]))
            self.assertTrue(IsIgnorableMismatch([34, 12]))

    def test_one_unignorable_id_rejects_the_whole_mismatch(self):
        # Also wrong before, and not fixed by populating the ignore list: a
        # single ignorable id in the diff made `all` true over a one-element
        # sequence, whatever else had moved.
        with IgnoreList([12, 34]):
            self.assertFalse(IsIgnorableMismatch([12, 99]))
            self.assertFalse(IsIgnorableMismatch([99, 12, 34]))

    def test_card_id_zero_is_ignorable_like_any_other(self):
        # The old expression yielded the id itself, and 0 is falsy, so an
        # explicitly ignored id 0 was rejected.
        with IgnoreList([0]):
            self.assertTrue(IsIgnorableMismatch([0]))

    def test_no_differing_ids_is_vacuously_acceptable(self):
        # Reachable only if the digest strings differ while the parsed
        # per-card dicts agree. Recording the behaviour, not endorsing it.
        with IgnoreList([]):
            self.assertTrue(IsIgnorableMismatch([]))


class FakeConsole:
    debug_cmds = None


class FakeControllerManager:
    def __init__(self, version: str) -> None:
        self.console = FakeConsole()
        self.game = self
        self.scene = self
        self.version = version


class FakeScene:
    pass


class TestTheReplayVerdict(unittest.TestCase):
    """The same decision reached through `GetReplayOperation`.

    Enough of a replay to get into the mismatch branch: a recorded digest that
    disagrees with the recalculated one on exactly the ids each test names.
    """

    VERSION = "0.5.9.204"

    def setUp(self) -> None:
        from engine import Engine
        self.original_game = getattr(Engine, "game", None)
        Engine.game = FakeControllerManager(self.VERSION)
        Engine.game.controller_manager = Engine.game

    def tearDown(self) -> None:
        from engine import Engine
        Engine.game = self.original_game

    def MakeModule(self, recorded: dict, calculated: dict) -> InputModule:
        class FakeOperation:
            def __init__(self, crc: str) -> None:
                self.crc = crc

        module = InputModule(FakeControllerManager(self.VERSION))
        module.replay_inputs = [FakeOperation(repr(recorded))]
        module.replay_step_id = 0
        module.current_step_id = 0
        # Slots 1 and 2 are always empty in practice (MARVEL-45); slot 0 is the
        # one `GetReplayOperation` parses for a non-`0.5.9.4` scene.
        module.calculated_crc = [repr(calculated), "", ""]
        return module

    def Verdict(self, recorded: dict, calculated: dict) -> bool:
        _, read_ok = self.MakeModule(recorded, calculated).GetReplayOperation(False)
        return read_ok

    def test_a_matching_digest_is_accepted(self):
        with IgnoreList([]), InTesting(False):
            self.assertTrue(self.Verdict({1: 10, 2: 20}, {1: 10, 2: 20}))

    def test_a_mismatch_outside_test_mode_is_now_rejected(self):
        # This is the bug as a user would have met it: a replay of a diverged
        # game continuing silently.
        with IgnoreList([]), InTesting(False):
            self.assertFalse(self.Verdict({1: 10, 2: 20}, {1: 10, 2: 99}))

    def test_a_mismatch_outside_test_mode_is_accepted_when_ignorable(self):
        with IgnoreList([2]), InTesting(False):
            self.assertTrue(self.Verdict({1: 10, 2: 20}, {1: 10, 2: 99}))

    def test_a_mismatch_outside_test_mode_is_rejected_when_only_partly_ignorable(self):
        with IgnoreList([2]), InTesting(False):
            self.assertFalse(self.Verdict({1: 10, 2: 20}, {1: 77, 2: 99}))

    def test_test_mode_still_rejects_even_an_ignorable_mismatch(self):
        # The acceptance criterion that behaviour on the `IsInTesting` path is
        # unchanged: it returns earlier and does not consult the ignore list.
        with IgnoreList([2]), InTesting(True):
            self.assertFalse(self.Verdict({1: 10, 2: 20}, {1: 10, 2: 99}))

    def test_a_puzzle_skips_the_check_entirely(self):
        with IgnoreList([]), InTesting(False):
            module = self.MakeModule({1: 10}, {1: 99})
            _, read_ok = module.GetReplayOperation(True)
            self.assertTrue(read_ok)

    def test_check_crc_false_skips_the_check_entirely(self):
        # `EventManager` replays some steps with `check_crc=False`.
        with IgnoreList([]), InTesting(False):
            module = self.MakeModule({1: 10}, {1: 99})
            _, read_ok = module.GetReplayOperation(False, check_crc=False)
            self.assertTrue(read_ok)


if __name__ == "__main__":
    unittest.main()
