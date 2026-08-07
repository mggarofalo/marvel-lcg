from . import *

class HasHandSize(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_hand_size = 0

        super().__init__(paper)

        self.RegisterAttribute("HS", "printed_hand_size")
        self.RegisterInfoDict('hand_size')

    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainHandSize(self.printed_hand_size, by_effect)
        return super().OnResetKeywords(by_effect)

    ################################################################################
    #
    @final
    def GainHandSize(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'HS', by_effect)

    @final
    @property
    def hand_size(self):
        return max(0, self.GetKeyword('HS'))

    # def GainHandSizeUntilPhaseEnd(self, value: int, by_effect: 'Effect') -> None:
    #     self.GainUntilPhaseEnd(
    #         by_effect,
    #         hand_size=value,
    #     )

