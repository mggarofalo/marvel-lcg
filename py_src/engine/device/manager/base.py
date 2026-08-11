from core import *
from engine.device import *
from engine.controller import *
from engine.log import Log

CATEGORY_NAME = "DEVICE_MANAGER"

class FabricatedInputError(EngineIntegrityError):
    """A wall-clock timeout was about to be recorded as a player's decision.

    When the input wait expires, `DoGetInput` returns the untouched `"{}"` and
    `Controller.ChoiceOne` parses that as effect id 0 -- a decline. For an
    interactive session that is a defensible fallback for a client that went
    away. For a headless generation run it is a fabricated input written into
    the corpus, which would then fail to reproduce on a faster machine.

    Raised by device managers that must not fabricate. It derives from
    `EngineIntegrityError` because the engine's play-time handlers otherwise
    swallow it: `ChoiceOne` runs under `EffectInvoker` and `Message2.Send`,
    both of which catch broadly and report through `Log.OnCrash`. See
    MARVEL-32.
    """

@dataclass
class AskOptionPayload:
    options_json    : str # json
    ability_type    : str
    event_name      : str
    prompt_text     : str
    show_cancel     : bool
    replay_input    : str
    input_json      : str = field(default="{}") # client input result

class DeviceManager:

    def __init__(self) -> None:
        from engine.device.manager.timer import Timer
        from engine.device.manager.notifier import SynchronizationNotifier
        self.timer = Timer()

        self.asking_players: List[int] = [] # 0,1,2,3
        self.ask_options: Dict[int, AskOptionPayload] = {}

        # How many times a timed-out wait has handed back an unanswered input.
        # Raising from `OnInputTimedOut` is the primary defence, but an
        # exception can be absorbed by a handler that was written to protect
        # against a broken card, so record the fact as well as raising: a
        # counter cannot be swallowed. `since_game` is reset per game by the
        # bot device, `total` never is. See MARVEL-32.
        self.fabricated_inputs_since_game = 0
        self.fabricated_inputs_total = 0

        self.notify = SynchronizationNotifier()

        self.controllers: List['Controller'] = []

    ################################################################################
    #
    def CreateDevices(self, controller: 'Controller') -> Tuple['OutputDevice', 'InputDevice']:
        ...

    def AddController(self, controller: 'Controller'):
        self.controllers.append(controller)

    def IsRenderNeeded(self) -> bool:
        """Whether anything will read the `WorldDescriptor` this step builds.

        `WorldRender.PresentInternal` serialises the entire board into a
        descriptor on every present, and exactly one thing consumes it:
        `GameServerSync.handle_post` hands it to a browser. A manager with no
        client attached answers False and the engine skips the construction.

        Default True, and deliberately so -- a device that renders and forgets
        to say so is a blank screen, while one that does not render and forgets
        is only slow. Overriding this is a claim that nothing, now or later,
        reads `world.render.descriptor` under this manager.

        It is not a correctness switch. Nothing the descriptor touches reaches
        game state or the digest (`game/world/digest.py` reads the world), so
        answering False must leave every recorded step byte-identical. See
        MARVEL-29.
        """
        return True

    ################################################################################
    #
    def OnNewGame(self):
        Log.DebugSilent(CATEGORY_NAME, "DeviceManager new game")
        self.ExitWait()
        self.notify.RefreshExitWait()

    def OnRestart(self):
        Log.DebugSilent(CATEGORY_NAME, "DeviceManager restart")
        self.ExitWait()

    def OnShutdown(self):
        Log.DebugSilent(CATEGORY_NAME, "DeviceManager Shutdown")
        self.ExitWait()

    ################################################################################
    #
    def ExitWait(self):
        self.asking_players = []
        self.notify.ExitWait()

    def WhenInput(self, post_json: str, player_id: int):
        self.asking_players.remove(player_id)
        self.ask_options[player_id].options_json = ""
        self.ask_options[player_id].input_json = post_json
        self.notify.WhenInput()

    def AfterSync(self):
        self.notify.RefreshExitWait()

    ################################################################################
    #
    def DoWaitConnect(self, player_id: int, check: Callable[[], bool]):
        def check_fn():
            if self.notify.should_exit_wait:
                return True
            return check()

        self.notify.connect.Wait(check_fn, None)
        # Log.Info(CATEGORY_NAME, f"[Client] Player {self.player_id} Connect")
        return

    def DoGetInput(self, data: 'AskOptionPayload', player_id: int, check: Callable[[], bool]):
        from core.lib import Time

        self.notify.RefreshExitWait()
        self.ask_options[player_id] = data
        self.asking_players.append(player_id)
        self.notify.has_client_input = False

        wait = self.timer.max_timeout
        if wait <= 0:
            wait = None
        self.timer.start_time = Time.GetTime()

        def check_fn():
            if self.notify.should_exit_wait:
                return True
            if self.asking_players == []:
                return True
            ask_option = self.ask_options[player_id]
            if ask_option.input_json != "{}":
                return True

            return check()

        no_time_out = self.notify.input.Wait(check_fn, wait)
        if not no_time_out:
            self.asking_players.remove(player_id)

        self.timer.start_time = None
        self.notify.has_client_input = False

        if player_id in self.asking_players:
            self.asking_players.remove(player_id)
            # When anyone has inputted, process it first
            return None

        input_json = self.ask_options[player_id].input_json
        if not no_time_out and input_json == "{}":
            # Nothing was ever posted, and this is what the step will record.
            self.fabricated_inputs_since_game += 1
            self.fabricated_inputs_total += 1
            self.OnInputTimedOut(player_id)
        return input_json

    def OnInputTimedOut(self, player_id: int) -> None:
        """The input wait expired with nothing posted.

        The caller is about to return the untouched `"{}"`, which
        `Controller.ChoiceOne` records as a decline the player never made.
        This is the only wall-clock value in the engine that can reach game
        state, so it is worth saying out loud. Device managers that must not
        fabricate an input override this and raise `FabricatedInputError`.
        See MARVEL-32.
        """
        Log.Warn(CATEGORY_NAME,
            f"Player {player_id} did not answer within {self.timer.max_timeout}s; "
            f"recording a decline they did not make")

    def DoWaitSync(self, player_id: int, check: Callable[[], bool]):
        from core.lib import Time
        Log.DebugSilent("SYNC", f"WaitSync start")
        wait = self.timer.max_timeout
        if wait <= 0:
            wait = None
        self.timer.start_time = Time.GetTime()

        def check_fn():
            if self.notify.should_exit_wait:
                Log.DebugSilent("SYNC", f"WaitSync Exit: Force")
                return True
            return check()

        self.notify.sync.Wait(check_fn, wait)

        self.timer.start_time = None
        self.notify.has_client_input = False
        Log.DebugSilent("SYNC", f"WaitSync end")

