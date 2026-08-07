from core import *

class PlayerFlag:

    def __init__(self) -> None:
        self.Reset()

    FLAG = Literal[
        "MAGIK_ONCE_PER_PHASE",
        "YOU_TAKE_THE_FIRST_TURN_DURING_THE_PLAYER_PHASE",
    ]

    ################################################################################
    #
    def Reset(self):
        self.flags: Dict[str, int] = {}

    def Set(self, flag: FLAG, value: int):
        self.flags[flag] = value

    def Get(self, flag: FLAG) -> int:
        if flag in self.flags:
            return self.flags[flag]
        return 0

    def Update(self, flag: FLAG, value: int):
        value += self.Get(flag)
        self.Set(flag, value)

