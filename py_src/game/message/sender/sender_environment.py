from . import *
from typing import Final

class SenderEnvironment:

    ################################################################################
    # Send this message when a card allow applying effects
    class TryApplyingEnvironment(Message2):
        def __init__(self, faces: List['CardFace']):
            self.faces = faces # Check these cards
            if faces:
                super().__init__(world=faces[0].card.world)

    class AfterCardApply_Text(TextMessage):
        def __init__(self, faces: List['CardFace'], effect: 'Effect', diff: int, *, no_present: bool=False) -> 'None':
            from game.message import Message
            super().__init__(world=effect.world)

            self.effect: Final = effect
            faces = [x for x in faces if x.IsFaceUp()]

            if diff == 0 or not faces:
                return

            if not no_present:
                if diff > 0:
                    text = TransText("{faces} apply {effect}", faces=faces, effect=effect.this)
                    self.Present(text, "", effect.this, *faces)
                else:
                    Message.AfterCardUnApply_Text(faces, effect)

    # Use in render
    class AfterCardUnApply_Text(TextMessage):
        def __init__(self, faces: List['CardFace'], effect: 'Effect') -> 'None':
            super().__init__(world=effect.world)
            self.effect: Final = effect
            text = TransText("{faces} unapply {effect}", faces=faces, effect=effect.this)
            self.Present(text, "", effect.this, *faces)

    # class AfterCardApplyEnvironment_Text(TriggerFaceMessage, TextMessage):
    #     def __init__(self, face: 'CardFace', effect: 'Effect') -> 'None':
    #         # self.trigger: Final = face
    #         self.effect: Final = effect
    #         # text = TransText("{face} apply {effect}")
    #         # self.Present(text, "activate", face, effect.this)

    # class AfterUnitUnApplyEnvironment_Text(TriggerFaceMessage, TextMessage):
    #     def __init__(self, face: 'CardFace', effect: 'Effect') -> 'None':
    #         # self.trigger: Final = face
    #         self.effect: Final = effect
    #         # text = TransText("{face} unapply {effect}")
    #         # self.Present(text, "activate", face, effect.this)

