from . import *

class Counter(Component):
    @override
    def __init__(self, card: 'Card') -> None:
        self.counters: Dict['CardFace.COUNTER', int] = {}

        super().__init__(card)

    @override
    def OnParentReset(self):
        self.counters = {}
        return super().OnParentReset()

    ################################################################################
    # Counter
    def GetAllCounters(self) -> int:
        value = 0
        for key in self.counters:
            value += self.counters[key]
        return value
    def SetCounters(self, counters: int, name: 'CardFace.COUNTER'):
        if name in self.counters:
            self.counters[name] = counters
        else:
            self.counters |= {name: counters}
    def GetCounters(self, name: 'CardFace.COUNTER') -> int:
        if name in self.counters:
            return self.counters[name]
        else:
            return 0

    def PlaceCounters(self, counters: int, name: 'CardFace.COUNTER') -> bool:
        self.SetCounters(counters + self.GetCounters(name), name)
        return True
    def RemoveCounters(self, counters: Literal["All"]|int, name: 'CardFace.COUNTER', forced: bool) -> bool:
        if name in self.counters:
            if counters == "All":
                self.counters[name] = 0
                return True
            elif forced or self.counters[name] >= counters:
                self.counters[name] -= counters
                if self.counters[name] < 0:
                    self.counters[name] = 0
                return True
            return False
        else:
            return False

    def GetCounterNames(self) -> List['CardFace.COUNTER']:
        return list(self.counters.keys())

