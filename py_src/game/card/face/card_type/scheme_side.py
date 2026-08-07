from core import *
from game.card.face import *
from game.ability import *
from game.message import *
from game.player import *
from cards.paper import Paper

@final
class EncounterSideScheme(SchemeSide2, CanCrisis, HasSetup, HasPermanent, EncounterNonVillainCard, FinalType):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    @override
    def OnBeDefeated(self, would_defeated_message: 'Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated', *, as_asset: bool, ignore_when_defeated: bool):
        if self.permanent:
            pass
        else:
            return super().OnBeDefeated(would_defeated_message, as_asset=as_asset, ignore_when_defeated=ignore_when_defeated)

