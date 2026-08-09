"""Device for replaying a saved scene with nobody watching.

Selected by `-verify_replays`, which forces `-device verify`. During a replay
every decision is answered from the recording inside `Controller.ChoiceOne`, so
this device is never consulted -- right up until the recording runs out. A scene
saved mid-game holds fewer inputs than the game has decisions, and at that point
`ChoiceOne` stops using the replay and asks the device for a real answer.

An interactive device blocks there forever. That is precisely what `main.py
-test` did: it expanded to `-device`, landed on `KeyDeviceManager`, and the
process sat in a wait no client was ever going to satisfy (MARVEL-28). This
device ends the game instead and counts the decision it refused to invent, so
the verifier can report the scene as incomplete rather than hang.

Answering `"{}"` after setting game over is what makes that safe: `ChoiceOne`
checks `world.is_game_over` before it pushes an operation, so the refusal never
becomes a step in the replay history.
"""

from core import *
from engine.controller import *
from engine.device import *
from engine.log import Log

CATEGORY_NAME = "TEST"

class VerifyDevice(OutputDevice, InputDevice):

    @override
    def __init__(self, controller: 'Controller', manager: 'VerifyDeviceManager') -> None:
        self.manager_verify = manager
        super().__init__(controller, manager)
        self.is_connected = True

    ################################################################################
    #
    @override
    def IsConnect(self) -> bool:
        # Nothing to connect: `WaitConnect()` returns immediately.
        return True

    @override
    def IsSyncReady(self) -> bool:
        # Nothing to render to: `WaitSync()` returns immediately.
        return True

    @override
    def IsInputReady(self) -> bool:
        self.manager_verify.SupplyInput(self)
        return True

    ################################################################################
    #
    @override
    def Render(self) -> None:
        # Headless: no client, no network, nothing to send.
        pass

class VerifyDeviceManager(DeviceManager):

    def __init__(self) -> None:
        super().__init__()

        # Decisions the engine asked for that the recording had no answer to.
        # Non-zero means the scene ran out before the game did, which is a
        # different finding from a digest mismatch and is reported as one.
        self.unanswered_decisions = 0

        Log.Info(CATEGORY_NAME, "Using verify device")

    ################################################################################
    #
    @override
    def CreateDevices(self, controller: 'Controller') -> Tuple['OutputDevice', 'InputDevice']:
        device = VerifyDevice(controller, self)
        return device, device

    @override
    def OnInputTimedOut(self, player_id: int) -> None:
        # Unreachable while `SupplyInput` answers from inside the wait
        # predicate, and overridden anyway for the same reason the bot device
        # does: the base class shrugs and records a decline nobody made, and a
        # verification run that silently invented an input would be comparing
        # the engine against a recording it had just edited. See MARVEL-32.
        raise FabricatedInputError(
            f"Player {player_id} input timed out after {self.timer.max_timeout}s "
            "while verifying a replay. A verification run must not answer a "
            "decision the recording does not contain.")

    ################################################################################
    #
    def BeginCase(self) -> None:
        """Reset per-scene state. Called by `ReplayVerifier` before each case."""
        self.unanswered_decisions = 0

    def SupplyInput(self, device: 'VerifyDevice') -> None:
        """End the game rather than answer a decision the recording lacks."""
        self.unanswered_decisions += 1

        if self.unanswered_decisions == 1:
            step = device.controller.manager.replay.current_step_id
            Log.Warn(CATEGORY_NAME,
                f"The recording ran out at step {step} but the game had not "
                "finished. Ending it here; the scene is incomplete, not divergent.")

        world = device.controller.world
        if world:
            # Checked by `ChoiceOne` immediately after the input comes back, and
            # it returns before `replay.Push`, so nothing is appended to the
            # history and the recorded/replayed step counts still line up.
            world.game_over.SetExit()

        # Same entry point the web server uses for a browser POST. `"{}"` is the
        # empty command; it is never recorded because of the check above.
        self.WhenInput("{}", device.player_id)
