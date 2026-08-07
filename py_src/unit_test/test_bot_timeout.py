"""A wall-clock timeout must never become a recorded player decision.

`DeviceManager.DoGetInput` waits on a condition bounded by
`timer.max_timeout`. When that expires it returns the untouched `"{}"`, and
`Controller.ChoiceOne` parses that as effect id 0 -- a decline. `max_timeout`
comes from `NewGameDescriptor.timeout` and defaults to 0 (disabled), so this is
dormant in practice, but it is the only wall-clock value in the engine that can
reach game state.

A corpus generated on a loaded machine could therefore contain fabricated
declines that fail to reproduce on a faster one: a corrupt oracle that looks
fine until it has wasted days of C# port debugging.

See MARVEL-32 and `docs/determinism-audit.md` (F1).
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.lib import Ver

# The run manifest records the engine version, which is a parsed file rather
# than a constant.
Ver.Initialize()

from engine.device.manager.base import AskOptionPayload, DeviceManager, FabricatedInputError
from engine.device.manager.bot.manager import BotDeviceManager
from engine.device.manager.bot.runner import BotRunner


def MakePayload():
    return AskOptionPayload(
        options_json="[]",
        ability_type="Normal",
        event_name="WhenPlayerInTurn",
        prompt_text="",
        show_cancel=True,
        replay_input="{}",
    )


class RecordingDeviceManager(DeviceManager):
    """A device manager with no client, so every input wait runs to timeout."""

    def __init__(self):
        super().__init__()
        self.timed_out_for = []

    def OnInputTimedOut(self, player_id):
        self.timed_out_for.append(player_id)


class FakeSession:
    def __init__(self, timeout):
        self.timeout = timeout


class FakeGame:
    def __init__(self, timeout=0):
        self.session = FakeSession(timeout)


class FakeTimer:
    def __init__(self, max_timeout):
        self.max_timeout = max_timeout


class FakeDeviceManager:
    def __init__(self, max_timeout=0, policy_name="first"):
        self.timer = FakeTimer(max_timeout)
        self.policy = type("Policy", (), {"name": policy_name})()


class TestTimeoutIsNotSilent(unittest.TestCase):

    def test_a_timed_out_wait_reports_itself(self):
        manager = RecordingDeviceManager()
        manager.timer.UpdateMaxTimeout(0.05)

        result = manager.DoGetInput(MakePayload(), 0, lambda: False)

        # This is the value `Controller.ChoiceOne` would record as a decline.
        self.assertEqual(result, "{}")
        self.assertEqual(manager.timed_out_for, [0])

    def test_an_answered_wait_reports_nothing(self):
        manager = RecordingDeviceManager()
        manager.timer.UpdateMaxTimeout(0.05)

        answered = []

        def AnswerOnFirstCheck():
            # `WhenInput` is the same entry point the web server calls when a
            # browser posts its selection, and the bot device calls from here.
            if not answered:
                answered.append(True)
                manager.WhenInput('{"id": 7}', 0)
            return False

        result = manager.DoGetInput(MakePayload(), 0, AnswerOnFirstCheck)

        self.assertEqual(result, '{"id": 7}')
        self.assertEqual(manager.timed_out_for, [])

    def test_the_bot_refuses_to_fabricate(self):
        # Constructing a `BotDeviceManager` builds a policy from config; the
        # override under test only needs the timer.
        stub = FakeDeviceManager(max_timeout=30)

        with self.assertRaises(FabricatedInputError):
            BotDeviceManager.OnInputTimedOut(stub, 0)


class TestGenerationRefusesATimeout(unittest.TestCase):

    def test_the_descriptor_asks_for_no_timeout(self):
        # Not the guard, but the thing the guard exists to keep honest.
        self.assertEqual(BotRunner.BuildDescriptor(1).timeout, 0)

    def test_zero_on_both_sides_is_accepted(self):
        self.assertTrue(
            BotRunner.CheckNoTimeout(FakeGame(0), FakeDeviceManager(0), "test"))

    def test_a_requested_timeout_is_refused(self):
        # The descriptor was overridden somewhere upstream.
        self.assertFalse(
            BotRunner.CheckNoTimeout(FakeGame(30), FakeDeviceManager(0), "test"))

    def test_a_resolved_timeout_is_refused(self):
        # The descriptor said 0 but config layering put something else into the
        # timer -- which is the value `DoGetInput` actually waits on.
        self.assertFalse(
            BotRunner.CheckNoTimeout(FakeGame(0), FakeDeviceManager(30), "test"))


class TestRunManifest(unittest.TestCase):

    def test_the_resolved_timeout_is_recorded(self):
        manifest = BotRunner.BuildManifest(FakeGame(0), FakeDeviceManager(0), [])

        self.assertEqual(manifest["timeout"], {"requested": 0.0, "resolved": 0.0})

    def test_a_non_zero_timeout_would_be_visible_after_the_fact(self):
        # The point of recording it: a corpus generated under a timeout can be
        # identified later, from the manifest alone.
        manifest = BotRunner.BuildManifest(FakeGame(30), FakeDeviceManager(30), [])

        self.assertEqual(manifest["timeout"], {"requested": 30.0, "resolved": 30.0})

    def test_games_are_listed_with_their_seeds(self):
        played = [{"seed": 7, "steps": 12, "decisions": 12, "outcome": "x", "file": "a.json"}]

        manifest = BotRunner.BuildManifest(FakeGame(0), FakeDeviceManager(0), played)

        self.assertEqual([game["seed"] for game in manifest["games"]], [7])

    def test_the_manifest_reads_no_clock_and_no_host(self):
        # It sits beside byte-reproducible scenes (MARVEL-27); it must not be
        # the thing that reintroduces a per-run difference.
        one = BotRunner.BuildManifest(FakeGame(0), FakeDeviceManager(0), [])
        other = BotRunner.BuildManifest(FakeGame(0), FakeDeviceManager(0), [])

        self.assertEqual(one, other)


if __name__ == "__main__":
    unittest.main()
