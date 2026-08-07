"""In-process device for the headless bot.

Both halves of the device contract are trivially ready: there is no client to
connect and nothing to sync with, so `WaitConnect()` and `WaitSync()` return on
their first predicate check and never block.

`IsInputReady` is the interesting one. It is called by
`DeviceManager.DoGetInput` from inside `Condition.wait_for`'s predicate — the
same slot `KeyInput` uses to read a keypress. Instead of reading stdin it asks
the policy and hands the answer back through `DeviceManager.WhenInput`, which is
exactly the call the web server makes when a browser posts to `/post`.
"""

from core import *
from engine.controller import *
from engine.device import *

class BotDevice(OutputDevice, InputDevice):

    @override
    def __init__(self, controller: 'Controller', manager: 'BotDeviceManager') -> None:
        self.manager_bot = manager
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
        self.manager_bot.SupplyInput(self)
        return True

    ################################################################################
    #
    @override
    def Render(self) -> None:
        # Headless: no client, no network, nothing to send.
        pass
