"""A clientless run must skip the descriptor without skipping anything else.

`WorldRender.PresentInternal` serialises the whole board into a `WorldDescriptor`
on every present, and exactly one thing reads it: the browser sync in
`GameServerSync.handle_post`. With no client attached that work is thrown away --
measured at 5.9s of a 9.6s six-game bot run, because a present happens on every
message rather than only at a decision.

The risk in skipping it is not that the descriptor goes missing; it is that
something *else* in `PresentInternal` gets skipped along with it. The prompt, the
round id and the game log are bookkeeping the rest of the engine reads, and
dropping any of them would change a headless run's recorded steps. So these tests
pin the split: the descriptor is conditional, everything around it is not.

See MARVEL-29.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.device.manager.base import DeviceManager
from engine.device.manager.bot.manager import BotDeviceManager
from game.world import world_render
from game.world.world_render import WorldRender


class TestWhoNeedsRendering(unittest.TestCase):

    def test_a_device_manager_renders_unless_it_says_otherwise(self):
        # The default is the safe direction: a device that renders and forgets
        # to declare it shows a blank screen, one that does not and forgets is
        # merely slow.
        self.assertTrue(DeviceManager.IsRenderNeeded(mock.Mock()))

    def test_the_headless_bot_does_not(self):
        self.assertFalse(BotDeviceManager.IsRenderNeeded(mock.Mock()))


class PresentTestCase(unittest.TestCase):
    """Drives `PresentInternal` with the world and engine stubbed out."""

    def Render(self, *, render_needed, skip=False, round_id=3):
        world = mock.Mock()
        world.round_id = round_id
        world.const_seat_order_players = []
        world.controller_manager.skip.is_skipping = False

        render = WorldRender(world)
        render.last_round_id = 0

        engine_stub = mock.Mock()
        game = engine_stub.game
        game.controller_manager.device_manager.IsRenderNeeded.return_value = render_needed

        with mock.patch.object(world_render, "ToDescriptor") as to_descriptor:
            with mock.patch.object(world_render, "Log"):
                with mock.patch("engine.Engine", engine_stub):
                    render.PresentInternal("a prompt", "", "an_event", skip=skip)
        return render, to_descriptor, game


class TestTheDescriptorIsSkipped(PresentTestCase):

    def test_a_client_still_gets_a_descriptor(self):
        _, to_descriptor, _ = self.Render(render_needed=True)

        to_descriptor.World.assert_called_once()

    def test_no_client_means_no_descriptor(self):
        _, to_descriptor, _ = self.Render(render_needed=False)

        to_descriptor.World.assert_not_called()

    def test_the_stale_descriptor_is_left_alone_rather_than_emptied(self):
        # Nothing reads it headless, but replacing it with a half-built one
        # would be worse than leaving the last good value in place.
        render, _, _ = self.Render(render_needed=False)

        self.assertIsNotNone(render.descriptor)


class TestEverythingElseStillHappens(PresentTestCase):
    """The bookkeeping around the descriptor is what keeps the run identical."""

    def test_the_prompt_is_recorded(self):
        render, _, _ = self.Render(render_needed=False)

        self.assertEqual(render.prompt, "a prompt")
        self.assertEqual(render.last_prompt, "a prompt")

    def test_the_round_id_advances(self):
        render, _, _ = self.Render(render_needed=False, round_id=7)

        # Read by the `skip` branch to decide whether a round has turned. Left
        # stale, an undo would render or not render at the wrong moment.
        self.assertEqual(render.last_round_id, 7)

    def test_the_render_id_advances(self):
        render, _, _ = self.Render(render_needed=False)

        self.assertEqual(render.last_render_id, 1)

    def test_the_controllers_are_still_synced(self):
        _, _, game = self.Render(render_needed=False)

        # `WaitSync` is how the engine hands control back. Skipping it because
        # nobody is watching would change when decisions happen.
        game.controller_manager.WaitSync.assert_called_once()


class TestTheSkipBranchIsUnchanged(PresentTestCase):

    def test_a_skipped_present_asks_nobody(self):
        # `skip` returns before the descriptor either way, so a clientless run
        # must not start consulting the device manager on that path.
        _, to_descriptor, game = self.Render(render_needed=True, skip=True, round_id=0)

        to_descriptor.World.assert_not_called()
        game.controller_manager.device_manager.IsRenderNeeded.assert_not_called()


if __name__ == "__main__":
    unittest.main()
