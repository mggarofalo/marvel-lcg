from typing import TypeAlias
from core import *
from engine.log import Log
from engine.task.condition import Condition
from game.scene import *
from engine.controller import *

CATEGORY_NAME = "GAME_STATE"

class StartState:

    def __init__(self) -> None:
        self.state: 'GameState.START_STATE' = ""

    def Set(self, state: 'GameState.START_STATE'):
        self.state = state

    def Reset(self):
        self.state = ""

    @property
    def is_undo(self) -> bool:
        return self.state == "Undo"

    @property
    def is_new(self) -> bool:
        return self.state == "New"

    @property
    def is_load(self) -> bool:
        return self.state == "Load"

    @property
    def is_replay(self) -> bool:
        return self.state == "Replay"

    @property
    def is_in_testing(self) -> bool:
        return self.state == "InTesting"

    @property
    def is_reset(self) -> bool:
        return self.state == ""

class ExitState:
    def __init__(self) -> None:
        self.state: 'GameState.EXIT_STATE' = ""

    def Set(self, state: 'GameState.EXIT_STATE'):
        self.state = state

    def Reset(self):
        self.state = ""

    @property
    def is_normal_exit(self) -> bool:
        return self.state == ""

    @property
    def is_undo_exit(self) -> bool:
        return self.state == "Undo"

class RunningState:
    def __init__(self) -> None:
        self.is_running: bool = False

    def Set(self, running: bool):
        self.is_running = running

class GameState:
    START_STATE: TypeAlias = Literal['', 'New', 'Undo', 'Load', 'Replay', 'InTesting']
    EXIT_STATE: TypeAlias = Literal['', 'Exit', 'Undo']

    StartState = StartState

    def __init__(self) -> None:
        self.start_state    = StartState()
        self.exit_state     = ExitState()
        self.running_state  = RunningState()

        self.condition = Condition("State")

    def SetStartState(self, start_state: 'GameState.START_STATE'):
        self.start_state.Set(start_state)
        Log.DebugSilent(CATEGORY_NAME, f"{start_state=}")
        self.SetRunningState(False) # will be set True after really start

        if start_state != '':
            self.condition.NotifyAll()

    def ResetStartState(self):
        self.start_state.Reset()

    def WaitUntilGameStart(self):
        def check():
            if self.IsRunningNewGame():
                return True
            return False

        self.condition.Wait(check)

    def IsRunningNewGame(self):
        return not self.start_state.is_reset

    ################################################################################
    #
    def SetRunningState(self, running: bool):
        self.running_state.Set(running)
        Log.DebugSilent("NEW", f"{self.is_running=}")

    ################################################################################
    #
    def SetExitStatus(self, status: 'GameState.EXIT_STATE'):
        self.exit_state.Set(status)

    ################################################################################
    #
    @property
    def is_running(self):
        return self.running_state.is_running

    # @property
    # def game_over(self) -> bool:
    #     return not self.is_running

    # @property
    # def game_start(self) -> bool:
    #     return self.is_running

