from typing import Final
from core import *
from engine.device.manager import DeviceManager
from engine.controller import *

class Device:

    def __init__(self, controller: 'Controller', manager: 'DeviceManager') -> None:
        self.controller : Final = controller
        self.manager    : Final = manager
        self.player_id  : Final = controller.player_id
        self.is_connected = False

    def Shutdown(self):
        pass

