from core import *
from engine.device.base.input import InputDevice

class KeyInput(InputDevice):

    @override
    def IsInputReady(self) -> bool:
        ask_option = self.manager.ask_options[self.player_id]
        ask_option.input_json = input()
        return True

