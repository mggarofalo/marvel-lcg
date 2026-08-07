from core import *
from game.card.face import *
from game.message import *
from cards.paper import Paper

@final
class Attachment(Asset2, HasModify, HasAmplify, HasHazard, CanCrisis, CanAttacked, CanNoStatus, HasUses, HasAccelerationIcon, HasPermanent, HasMaxPer, HasSetup, EncounterNonVillainCard, FinalType):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)
        return

    @override
    def OnWhenCardRevealed(self, revealed_message: 'Message.WhenCardRevealed') -> None:
        from game.effect.rule import GameRule
        from game.operate.faces import Faces

        player = revealed_message.GetToPlayer()
        if self.PutIntoPlay(player, revealed_message.by_effect):
            super().OnWhenCardRevealed(revealed_message)
        if not self.card.area.flags.is_in_play:
            Faces.DiscardAll([self], GameRule(self))

    @override
    def CanAttachTo(self, face: 'CardFace') -> bool:
        if not super().CanAttachTo(face):
            return False
        # play_abilities= self.FindAbilities(func_name="AttachToWhenEnterPlay")
        # for ability in play_abilities:
        #     assert ability.selector
        #     return ability.selector.CheckFinder(face)
        # return False
        return True

