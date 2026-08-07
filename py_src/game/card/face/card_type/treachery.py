from core import *
from game.card.face import *
from game.deck import *
from game.ability import *
from game.message import *
from game.player import *
from cards.paper import Paper

@final
class Treachery(EncounterNonVillainCard, FinalType):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    def ResetTreachery(self):
        pass

    @override
    def OnWhenCardWouldReveal(self, message: 'Message.WhenCardWouldReveal') -> bool:
        self.ResetTreachery()
        return super().OnWhenCardWouldReveal(message)

    @override
    def OnWhenCardRevealed(self, revealed_message: 'Message.WhenCardRevealed') -> None:
        from game.effect.rule import GameRule
        from game.operate.faces import Faces

        if not self.card.world.rule.fix_treachery:
            if not revealed_message.reveal_message.from_area.flags.is_boost_area:
                self.ResolveIncite()
                Faces.DiscardAll([self], GameRule(self))

        super().OnWhenCardRevealed(revealed_message)

        if self.card.world.rule.fix_treachery:
            if self.card.area.flags.is_revealing:
                Faces.DiscardAll([self], GameRule(self))

