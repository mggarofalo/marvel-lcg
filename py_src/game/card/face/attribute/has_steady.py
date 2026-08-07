from . import *

"""
Steady
A character with the steady keyword can have one additional stun status card and one
additional confuse status card.
That character is not stunned unless they have two stunned status cards,
and is not confused unless they have two confused status cards.
After that character’s activation is replaced by a status card effect,
remove all status cards of that type from that character
"""

class HasSteady(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_steady = 0

        super().__init__(paper)

        self.RegisterAttribute("Steady", "printed_steady")
        self.RegisterInfoDict('steady')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainSteady(self.printed_steady, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def IsSteady(self) -> bool:
        return self.steady > 0

    @final
    @property
    def steady(self) -> int:
        return self.GetKeyword('Steady')

    @final
    def GainSteady(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Steady', by_effect)

