from typing import Final
from core import *
from game.card.face import *
from game.ability import *
from game.message import *

class Buff:
    def __init__(self) -> None:
        self.name: Final = type(self).__name__
        self.Reset()

    @final
    def Reset(self):
        self.by_effects: List['Effect'] = []
        self.text = ""

    ################################################################################
    #
    def OnRoundEnd(self):
        pass

    def OnRecordPlayedFace(self, face: 'CardFace'):
        pass

    ################################################################################
    #
    def SetUIText(self, text: str):
        self.text = text

    @final
    def GetText(self) -> str:
        if self.text:
            return self.text
        return str(len(self.by_effects))

    @final
    def OnGain(self, by_effect: 'Effect'):
        self.by_effects.append(by_effect)

    @final
    def OnLost(self, which_effect: 'Effect'):
        self.by_effects.remove(which_effect)

    def __bool__(self) -> bool:
        return self.by_effects != []

class BuffIsTreatAsIfBlank(Buff):
    """Except for traits"""
    pass

# class BufferCannotTakeDamage(Buffer):
#     def __init__(self) -> None:
#         super().__init__()

#         self.conditions: ConditionsType[Message.WhenUnitWouldTakeDamage]=[]
#         self.is_from_attack: bool|Message.WhenUnitWouldAttack|None=None
#         self.is_damage_more_than: Callable[['Effect'], int]|int|None = None
#         self.damage_from: CardType=None,
#         self.cannot_take_more_than_damage: Callable[['Effect'], int]|int|None = None

#     def CheckConditions(self, message: 'Message.WhenUnitWouldTakeDamage') -> bool:
#         ...

#     def Operate(self, message: 'Message.WhenUnitWouldTakeDamage') -> None:
#         ...

class BuffCannotTakeDamage(Buff):
    pass

class BuffCannotBeConfused(Buff):
    pass

class BuffCannotBeStunned(Buff):
    pass
