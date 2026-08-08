"""Headless `bot` device manager.

Selected with `python main.py -device bot`. Modelled on `KeyDeviceManager`: it
adds no servers, no sockets and no threads of its own. The one thing it does add
is a policy object that answers `Controller.ChoiceOne` in place of a human.

Decisions are delivered through `DeviceManager.WhenInput` — the same entry point
`GameServerSync.handle_post` uses for a browser client — so `ChoiceOne` runs its
normal validation, digest and `replay.Push` path and bot replays come out
structurally identical to human ones.
"""

from core import *
from engine.config import ConfigVariables
from engine.controller import *
from engine.device import *
from engine.log import Log
from engine.device.manager.bot.command import BotCommand
from engine.device.manager.bot.device import BotDevice
from engine.device.manager.bot.policies import BotPolicyFactory
from engine.device.manager.bot.policy import BotDecision, BotOptionParser, BotPolicy

CATEGORY_NAME = "BOT"

# Backstop so a policy that cannot make progress ends the game instead of
# spinning forever. A normal game is a few hundred steps.
BOT_MAX_STEPS = ConfigVariables.Int('bot_max_steps', 20000)

class BotDeviceManager(DeviceManager):

    def __init__(self, policy: 'BotPolicy|None'=None) -> None:
        super().__init__()

        self.policy: 'BotPolicy' = policy if policy != None else BotPolicyFactory.Create()

        self.decision_count = 0
        self.stopped_on_max_steps = False

        # (player_id, step_id) of the decision currently being answered, and how
        # many answers the engine has already rejected for it.
        self.attempt_key: Tuple[int, int] = (-1, -1)
        self.attempt = 0

        Log.Info(CATEGORY_NAME, f"Using bot device (policy: {self.policy.name})")

    ################################################################################
    #
    @override
    def CreateDevices(self, controller: 'Controller') -> Tuple['OutputDevice', 'InputDevice']:
        device = BotDevice(controller, self)
        return device, device

    @override
    def OnInputTimedOut(self, player_id: int) -> None:
        # The base class shrugs and records a decline. A generation run cannot:
        # the fabricated input would land in the corpus and then fail to
        # reproduce on a machine that answered in time. `BotRunner` refuses to
        # start with a non-zero timeout, so reaching this means the guard was
        # bypassed. See MARVEL-32.
        raise FabricatedInputError(
            f"Player {player_id} input timed out after {self.timer.max_timeout}s. "
            "A headless run must not record a decision the policy did not make.")

    ################################################################################
    #
    def SetPolicy(self, policy: 'BotPolicy') -> None:
        self.policy = policy

    def BeginGame(self, seed: int) -> None:
        """Reset per-game bot state. Called by `BotRunner` before each game."""
        self.decision_count = 0
        self.stopped_on_max_steps = False
        self.attempt_key = (-1, -1)
        self.attempt = 0
        self.fabricated_inputs_since_game = 0
        self.policy.OnGameStart(seed)

    ################################################################################
    #
    def SupplyInput(self, device: 'BotDevice') -> None:
        """Answer the decision `device`'s controller is currently blocked on."""
        player_id = device.player_id
        controller = device.controller
        replay = controller.manager.replay
        step_id = replay.current_step_id

        ask_option = self.ask_options[player_id]

        if self.StopIfOverMaxSteps(device, step_id):
            self.WhenInput(BotCommand.ToJson(BotCommand.Cancel()), player_id)
            return

        key = (player_id, step_id)
        if key != self.attempt_key:
            self.attempt_key = key
            self.attempt = 0
        else:
            self.attempt += 1

        decision = BotDecision(
            player_id       = player_id,
            step_id         = step_id,
            attempt         = self.attempt,
            event_name      = ask_option.event_name,
            ability_type    = ask_option.ability_type,
            prompt_text     = ask_option.prompt_text,
            can_cancel      = ask_option.show_cancel,
            options         = BotOptionParser.Parse(ask_option.options_json),
            replay_input    = ask_option.replay_input,
            world           = controller.world,
        )

        command = self.policy.Choose(decision)
        self.decision_count += 1

        Log.DebugSilent("DEVICE_MANAGER",
            f"[Bot] p{player_id} #{step_id}.{self.attempt} {decision.event_name}: {command}")

        # Same call the web server makes when a client posts its selection.
        self.WhenInput(BotCommand.ToJson(command), player_id)

    ################################################################################
    #
    def StopIfOverMaxSteps(self, device: 'BotDevice', step_id: int) -> bool:
        max_steps = BOT_MAX_STEPS.value
        if max_steps <= 0 or step_id < max_steps:
            return False

        if not self.stopped_on_max_steps:
            self.stopped_on_max_steps = True
            Log.Warn(CATEGORY_NAME, f"Reached bot_max_steps ({max_steps}), ending the game")
            world = device.controller.world
            if world:
                world.game_over.SetExit()
        return True
