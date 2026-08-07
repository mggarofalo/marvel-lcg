from . import *

class Health(Component):
    @override
    def __init__(self, card: 'Card') -> None:
        self.health: int = 0
        self.max_health: int = 0

        super().__init__(card)

    @override
    def OnParentReset(self):
        self.health = 0
        self.max_health = 0
        return super().OnParentReset()

    ################################################################################
    # 
    def SetHealth(self, health: int):
        self.health = health
    def AddHealth(self, health: int):
        self.health += health
    def GetHealth(self) -> int:
        return self.health

    def SetMaxHealth(self, health: int):
        self.max_health = health
    def AddMaxHealth(self, health: int):
        self.max_health += health
    def GetMaxHealth(self) -> int:
        return self.max_health

