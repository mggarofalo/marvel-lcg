from core import *
from game.card.face import *
from game.deck import *
from game.ability import *
from game.ability.factory import *
from game.message import *
from game.player import *
from cards.paper import Paper

@final
class Support(HasUses, HasPermanent, HasSetup, ClassCard, FinalType):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    @override
    def GetAbilities(self) -> List['Ability']:
        abilities: List['Ability'] = []
        if not self.ability.Find(func_name="Play"):
            abilities.append(
                AbilityFactory.CanPlayThisSupportCard()
            )
        return abilities + super().GetAbilities()

    @override
    def OnPutIntoPlay(self, message: 'Message.WhenCardPutIntoPlay') -> 'bool':
        from game.operate.faces import Faces
        if Faces.MoveAllTo([self], message.GetToPlayer().supports, message.by_effect):
            return super().OnPutIntoPlay(message)
        else:
            return False

    @override
    def OnWouldEnterPlay(self, into_area: 'Deck') -> bool:
        if super().OnWouldEnterPlay(into_area):
            if self.HasTrait("TEAM"):
                if into_area.GetOwner().IsPlayer():
                    player = into_area.GetOwnerPlayer()
                    if not player.limit_team.CheckLimit([self]):
                        return False
            return True
        return False

    @property
    @override
    def bind_face(self) -> 'Hero|AlterEgo|None':
        player = self.card.area.GetOwner()
        if isinstance(player, Player):
            return player.GetIdentity()
        return None

