from . import *

class CanAttach(CardFace):

    @override
    def __init__(self, paper: 'Paper') -> None:
        self.refers_to_the_villain_refers_to_attached: bool|None = None
        super().__init__(paper)

    @override
    def OnBeforeFlip(self, by_effect: 'Effect', no_detach: bool=False):
        super().OnBeforeFlip(by_effect, no_detach=no_detach)
        if self.bind_face and self.IsInPlay():
            self.DetachFrom2(self.GetBindFace(), by_effect)

    @override
    def OnAfterFlip(self, by_effect: 'Effect', call_reveal: bool=False):
        super().OnAfterFlip(by_effect, call_reveal=call_reveal)
        # Fix "32145b"
        if self.IsFaceUp() and self.IsInPlay() and self.card.area.flags.is_upgrades:
            assert self.card.area.bind_card
            self.AttachTo2(self.card.area.bind_card.face, by_effect)

    @override
    def OnLeavedPlayEnd(self, by_effect: 'Effect'):
        face = self.bind_face
        if face:
            self.DetachFrom2(face, by_effect)
        return super().OnLeavedPlayEnd(by_effect)

    ################################################################################
    #
    def OnAttachTo(self, face: 'CardFace', cause_by_flip: bool):
        from game.message import Message
        self.card.state.is_attached = True
        message = Message.AfterCardGainUpgradeAbility(face, self)
        message.Send()

    def OnDetachFrom(self, face: 'CardFace', by_flip: bool):
        from game.message import Message
        message = Message.AfterCardLostUpgradeAbility(face, self, by_flip)
        message.Send()
        self.card.state.is_attached = False

    ################################################################################
    # Upgrade / Inventory
    # If a card uses the phrase “attach to”, it must be attached to (placed beneath and slightly overlapped by) the specified game element as it enters play
    # @final
    def AttachTo2(self, face: 'CardFace', by_effect: 'Effect') -> bool:
        from game.message import Message
        from game.effect.rule import GameRule
        from game.operate.faces import Faces

        if not face.card.IsOnField():
            return False
        self.refers_to_the_villain_refers_to_attached = None

        message = Message.WhenCardWouldAttachTo(self, face, by_effect)
        message.Send()
        if message.is_be_instead:
            return False

        if self.bind_face == face:
            self.card.area.Remove(self.card)
            self.card.area.Append(self.card)
            return True

        if self.bind_face:
            self.DetachFrom2(self.bind_face, by_effect)

        if Faces.MoveAllTo([self], face.GetInventoryDeck(), GameRule(self)) or \
            ( \
                self in face.GetInventoryDeck().GetAll() and \
                self.bind_face == None # Fix "21004"
            ):
            self.card.area.UnMarkAsRemoved(self.card)
            self.OnAttachTo(face, False)
            gain_message = Message.AfterCardAttachTo(face, self, message)
            gain_message.Send()
            return True
        else:
            Faces.DiscardAll([self], by_effect)
        return False

    # You need to handle where this card to go after it detach
    @final
    def DetachFrom2(self, face: 'CardFace', by_effect: 'Effect') -> bool:
        from game.message import Message
        self.refers_to_the_villain_refers_to_attached = None
        # from game.operate.faces import Faces
        # from game.ability.rule import GameRule
        if not face.card.IsOnField():
            return False
        if self.card in self.card.area.removed_cards:
            return False
        if self.bind_face == face:
            self.OnDetachFrom(face, False)
            self.card.area.MarkAsRemoved(self.card)
            # Faces.MoveAllToProcessingArea([self], GameRule(self))
            # We must remove `self` from face's upgrade area before send this event!
            lost_message = Message.AfterCardDetachFrom(face, self)
            lost_message.Send()
            return True
        return False

    @property
    def attached_face(self) -> 'CardFace|None':
        if not self.card.state.is_attached:
            return None
        return self.bind_face

    @property
    @override
    def bind_face(self) -> 'CardFace|None':
        # if self.card.is_leaving_play:
        #     return None
        if self.card.area.flags.is_encounter_deck:
            return None
        if self.card.area.flags.is_encounter_discard_pile:
            return None
        if self.card.area.bind_card == None:
            return None
        return self.card.area.bind_card.face

    @property
    def is_refers_to_the_villain_refers_to_attached(self) -> bool:
        if self.refers_to_the_villain_refers_to_attached == None:
            from game.message import Message
            message = Message.CheckIfRefersToTheVillainRefersToAttached(self)
            message.Send()
            self.refers_to_the_villain_refers_to_attached = message.refers_to
        return self.refers_to_the_villain_refers_to_attached

