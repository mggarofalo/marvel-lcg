from game.effect import *
from game.element.resources import Resources

class ResourcesPool:

    def __init__(self) -> None:
        self.Reset()

    ################################################################################
    # resource_pool
    def Gain(self, res: 'Resources', by_effect: 'Effect') -> None:
        self.resource_pool += res

    def Reset(self) -> None:
        self.resource_pool = Resources("0")

    def Get(self) -> 'Resources':
        return self.resource_pool

    def Set(self, res: 'Resources'): # Debug only
        self.resource_pool = res

