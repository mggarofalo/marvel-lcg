from typing import final
from engine.device import *
from engine.log import Log

class OutputDevice(Device):

    @final
    def WaitSync(self) -> None:
        game = self.controller.game
        if not game.state.is_running:
            Log.DebugSilent("SYNC", f"WaitSync skip (Game is not running)")
            return
        return self.manager.DoWaitSync(self.player_id, self.IsSyncReady)

    def IsSyncReady(self) -> bool:
        ...

    def Render(self) -> None:
        ...

