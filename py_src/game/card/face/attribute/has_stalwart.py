from . import *

class HasStalwart(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        """
        A character with stalwart cannot be stunned or confused
        """
        self.printed_stalwart = 0

        super().__init__(paper)

        self.RegisterAttribute("Stalwart", "printed_stalwart")
        self.RegisterInfoDict('stalwart')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainStalwart(self.printed_stalwart, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def IsStalwart(self) -> bool:
        return self.stalwart > 0

    @final
    @property
    def stalwart(self) -> int:
        return self.GetKeyword('Stalwart')

    @final
    def GainStalwart(self, diff: int, by_effect: 'Effect'):
        from game.card.face.base import Unit2
        if diff > 0:
            self.CastTo(Unit2).DiscardStunned(by_effect, rule="All")
            self.CastTo(Unit2).DiscardConfused(by_effect, rule="All")
        self.GainKeyword(diff, 'Stalwart', by_effect)

