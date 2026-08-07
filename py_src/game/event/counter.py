from core import *
from game.message import *

class EventCounter:
    def __init__(self) -> None:
        self.event_size: Dict[Type['Message2'], int] = {}

    def Add(self, event: Type['Message2']):
        if event not in self.event_size:
            self.event_size[event] = 0
        self.event_size[event] += 1

    def GetSize(self, event: 'Type[Message2]') -> int:
        if event in self.event_size:
            return self.event_size[event]
        return 0

    def GetAllSize(self) -> int:
        size = 0
        for event in self.event_size:
            size += self.event_size[event]
        return size

