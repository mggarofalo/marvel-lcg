from core import *

from game.card.face import *
from game.ability import *
from game.ability.condition import *
from game.message import *
from game.player import *

class ConditionHeroForm:

    @staticmethod
    def PlayerInMassForm(card_type: 'CardFinder|Player|Literal["You"]', form: 'Form.FORM_TYPE', effect: 'Effect') -> bool:
        from game.operate.worlds import Worlds
        from game.player import Player

        if card_type == "You":
            initiator = effect.GetInitiator()
        elif isinstance(card_type, Player):
            initiator = card_type
        else:
            face = Worlds.FindCardOnField(
                effect,
                card_type,
                check_face_fn=lambda face:
                    Player.IsType(face.GetControlBy())
            )
            if face:
                initiator = face.GetControlByPlayer()
            else:
                return False
        return initiator.form.IsFormInternal(form)

    @staticmethod
    def YouAreInHeroFrom(effect: 'Effect', trait: "CardFace.TRAITS") -> bool:
        initiator = effect.GetInitiator()
        return initiator.IsInHeroForm(trait)

