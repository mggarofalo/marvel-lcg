from core import *
from engine.controller import *
from game.scene.replay import *
from game.world import *
from game.effect import *

CATEGORY_NAME = "CONSOLE"

class Console:

    def __init__(self, manager: 'ControllerManager') -> None:
        self.commands: Dict[str, Callable[..., str|None]] = {}
        self.debug_break: bool = False
        self.manager = manager
        self.debug_cmds: str = ""
        self.effect_list: Sequence['Effect'] = []

    def SetLastEffectList(self, effect_list: Sequence['Effect']):
        self.effect_list = effect_list

    # def RegisterCommand(self, command_name: str, command_function: Callable[..., str|None]):
    #     self.commands[command_name] = command_function

    # def ExecuteCommand(self, command_line: str) -> bool:
    #     parts = command_line.split()
    #     command_name = parts[0]
    #     args = parts[1:]

    #     if command_name in self.commands:
    #         try:
    #             self.commands[command_name](*args)
    #         except Exception as exc:
    #             Log.FailedTrace(CATEGORY_NAME, exc, no_take_as_error=True)
    #         return True
    #     else:
    #         Log.Warn(CATEGORY_NAME, f"Command '{command_name}' not found.")
    #         return False

    def Clean(self):
        self.debug_cmds = ""

    def TryBreak(self, world: 'World') -> bool:
        if not self.debug_break:
            return False
        self.debug_break = False

        return (True, world.cheat.DebugBreak())[0]

    def SetCommand(self, debug_cmd: str, world: 'World|None') -> bool:
        from game.cheat.cheat import Cheat

        if not debug_cmd:
            return False

        if debug_cmd == "/break":
            self.debug_break = True
        elif Cheat.TryPreDebugExec(debug_cmd, world):
            # Log.Notify.Show("CHEAT", debug_cmd)
            return False
        else:
            if self.debug_cmds:
                self.debug_cmds += "\n" + debug_cmd
            else:
                self.debug_cmds = debug_cmd
        return True

    def DebugExec(self, debug_cmds: List[str], world: 'World'):
        world.cheat.DebugExec(debug_cmds)

    def Execute(self, message_str: str, world: 'World', effect_list: Sequence['Effect']) -> bool:
        from game.test import Test

        if not self.debug_cmds:
            return False

        self.SetLastEffectList(effect_list)

        from game.world.world import Phase
        if not world.is_game_started:
            if world.phase.state != Phase.State.ResolveMulligans:
                self.manager.skip.SetSkipTo(1)
                self.manager.skip.SetIsSkipping(True)
                return False

        replay = self.manager.replay

        if not Test.IsInTesting() and \
            replay.replay_step_id >= self.manager.skip.skip_to - 1:
            self.manager.skip.SetIsSkipping(False)

        debug_cmd_text = self.debug_cmds
        action = OperationDescriptor(
            replay.current_step_id,
            message_str,
            CommandDescriptor.FromDebugCommand(debug_cmd_text),
            replay.calculated_crc[0])
        replay.Push(action)

        debug_cmds = self.debug_cmds
        self.debug_cmds = ""
        if world.scene.is_puzzle:
            from game.puzzle import PuzzleHelper
            PuzzleHelper.Exec([debug_cmds], world)
        else:
            self.DebugExec([debug_cmds], world)
        return True

