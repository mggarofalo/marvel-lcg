from . import *

"""
During step three of the villain phase, for each hazard icon on cards in play, deal one player one additional card (not one card per player). Additional cards are dealt in player order (first additional card to the first player, the second to the second player, etc.).
"""

class HasHazard(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        # For each side scheme in play with the hazard icon,
        # an additional encounter card is dealt during step three of the villain phase.
        # Additional cards are dealt in player order
        self.printed_hazard = 0

        super().__init__(paper)

        self.RegisterAttribute("Hazard", "printed_hazard")
        self.RegisterInfoDict('hazard')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainHazard(self.printed_hazard, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    @property
    def hazard(self) -> int:
        return self.GetKeyword('Hazard')

    @final
    def GainHazard(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Hazard', by_effect)

    @final
    def SetIgnoreHazard(self, diff: int, by_effect: 'Effect'):
        self.SetIgnoreKeyword(diff, 'Hazard', by_effect)

