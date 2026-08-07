from core import *
from engine.device import *
from engine.device.manager.base import AskOptionPayload

class InputDevice(Device):

    @final
    def GetInput(self, data: AskOptionPayload) -> str|None:
        return self.manager.DoGetInput(data, self.player_id, self.IsInputReady)

    def IsInputReady(self) -> bool:
        ...

    @final
    def WaitConnect(self) -> None:
        self.manager.DoWaitConnect(self.player_id, self.IsConnect)
        self.is_connected = True

    def IsConnect(self) -> bool:
        ...

