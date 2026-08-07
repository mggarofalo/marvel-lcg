from game.card.face.base import *
from game.card.face import *
from game.card import *
from game.ability import *
from game.message import *
from cards.paper import Paper

class Asset2(CanAttach, CardFace):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    @override
    def OnBeforeFlip(self, by_effect: 'Effect', no_detach: bool=False):
        super().OnBeforeFlip(by_effect, no_detach=no_detach)
        if self.IsInPlay():
            assert self.bind_face != None

    # @override
    # def OnTreatAsIfBlank(self, is_blank: bool):
    #     if self.attached_card and HasModify.IsType(self):
    #         self.Apply(self.attached_card.face, False if is_blank else True, True)
    #     return super().OnTreatAsIfBlank(is_blank)

    @override
    def IsDefeated(self) -> bool:
        return self.bind_face == None or super().IsDefeated()

    # @override
    # def OnAttachUpgrade(self, face: 'CardFace', cause_by_flip: bool):
    #     from game.message import Message
    #     # from game.card.face.attribute.has_modify import HasModify
    #     # self.is_attached = True
    #     # if HasModify.IsType(self):
    #     #     self.Apply(face, True, cause_by_flip)
    #     message = Message.AfterCardGainUpgradeAbility(face, self)
    #     message.Send()
    #     return super().OnAttachUpgrade(face, cause_by_flip)

    # @override
    # def OnUnattachUpgrade(self, face: 'CardFace', by_flip: bool):
    #     from game.message import Message
    #     # from game.card.face.attribute.has_modify import HasModify
    #     # if not by_flip:
    #     # self.is_attached = False
    #     # if HasModify.IsType(self):
    #     #     self.Apply(face, False, by_flip)
    #     message = Message.AfterCardLostUpgradeAbility(face, self, by_flip)
    #     message.Send()
    #     return super().OnUnattachUpgrade(face, by_flip)

    # @override
    # def OnDiscard(self, by_effect: 'Effect'):
    #     face = self.bind_face
    #     if face:
    #         self.UnattachFrom2(face, by_effect)
    #     return super().OnDiscard(by_effect)

    # @override
    # def GetAttachToCard(self) -> 'Card|None':
    #     from game.deck import DeckType
    #     if self.card.area.bind_card:
    #         assert self.IsOnField(), f"{self=}"
    #         assert self.card.area.deck_type == DeckType.UpgradesArea, f"{self=}"
    #         return self.card.area.bind_card
    #     else:
    #         return None

    def CanAttachTo(self, face: 'CardFace') -> bool:
        def check_max_per_unit():
            this = self.CastTo(HasMaxPer)
            return face.GetInventoryDeck().FindCardSize(CardFinder(name=this.name, card_type=type(this))) < this.max_per_unit
        return check_max_per_unit()

