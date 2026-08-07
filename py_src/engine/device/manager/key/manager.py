from core import *
from engine.controller import *
from engine.device import *
from engine.device.keyboard.key_input import KeyInput
from engine.device.console.console import ConsoleDevice
from engine.log import Log

class KeyDeviceManager(DeviceManager):

    def __init__(self) -> None:
        super().__init__()
        Log.Print(f"Using debug device")

    @override
    def CreateDevices(self, controller: 'Controller') -> Tuple['OutputDevice', 'InputDevice']:
        return ConsoleDevice(controller, self), KeyInput(controller, self)

