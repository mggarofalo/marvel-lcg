from typing import TypeAlias
from core import *
from game.card.face import *
from game.message import *
from cards.paper import Paper

@final
class Evidence(CanAttach, HasAttribute, FinalType):

    SUBTYPE: TypeAlias = Literal["Opportunity", "Motive", "Means"]

    @override
    def __init__(self, paper: 'Paper') -> None:
        self.subtype: Evidence.SUBTYPE

        super().__init__(paper)

        self.RegisterAttribute("Subtype", "subtype", str)

    @override
    def CheckPrintedValue(self):
        assert self.subtype != None
        return super().CheckPrintedValue()

    ################################################################################
    #
    @override
    def OnWhenCardSetup(self, message: 'Message.WhenCardSetup') -> None:
        from game.effect.rule import GameRule
        if self.IsInDeck():
            self.PutIntoPlay("FirstPlayer", GameRule(self))
        return super().OnWhenCardSetup(message)

    @override
    def OnPutIntoPlay(self, message: 'Message.WhenCardPutIntoPlay') -> bool:
        from game.operate.faces import Faces
        world = self.card.world
        if self.card.area == world.area_evidence or \
            Faces.MoveAllTo([self], world.area_evidence, message.by_effect):
            return super().OnPutIntoPlay(message)
        else:
            return False

