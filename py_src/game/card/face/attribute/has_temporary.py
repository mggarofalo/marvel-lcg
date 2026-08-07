from . import *

class HasTemporary(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_temporary = 0

        super().__init__(paper)

        self.RegisterAttribute("Temporary", "printed_temporary")
        self.RegisterInfoDict('temporary')

    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainTemporary(self.printed_temporary, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def GainTemporary(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Temporary', by_effect)

    @final
    def IsTemporary(self) -> bool:
        return self.temporary > 0

    @final
    @property
    def temporary(self) -> int:
        return self.GetKeyword('Temporary')

    # # A card with temporary must be discarded from play at the end of the round.
    # def ResolvedTemporaryDiscard(self, by_effect: 'Effect'):
    #     from game.operate.faces import Faces
    #     if self.IsInPlay() and self.temporary:
    #         Faces.DiscardAll([self], by_effect)

