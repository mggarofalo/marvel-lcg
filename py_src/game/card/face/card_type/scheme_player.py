from core import *
from game.card.face import *
from game.ability import *
from game.ability.factory import *
from game.message import *

@final
class PlayerSideScheme(SchemeSide2, CanCrisis, HasAmplify, HasVictory, ClassCard, FinalType):

    @override
    def GetAbilities(self) -> List['Ability']:
        abilities: List['Ability'] = []
        if not self.ability.Find(func_name="Play"):
            abilities.append(
                AbilityFactory.CanPlayThisSchemeCard()
            )
        return abilities + super().GetAbilities()

    @override
    def OnAfterCardEnterPlay(self, message: 'Message.AfterCardEnterPlay') -> None:
        world = self.card.world
        world.limit_player_side_scheme.CheckLimit()
        return super().OnAfterCardEnterPlay(message)

