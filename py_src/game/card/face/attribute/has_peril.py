from . import *

class HasPeril(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        """
        While a player is resolving a card with the peril keyword, that player cannot consult other players, and other players cannot play cards or trigger abilities.
        """
        self.printed_peril: int = 0

        super().__init__(paper)

        self.RegisterAttribute("Peril", "printed_peril")
        self.RegisterInfoDict('peril')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainPeril(self.printed_peril, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def IsPeril(self) -> bool:
        return self.peril > 0

    @final
    @property
    def peril(self) -> int:
        return self.GetKeyword('Peril')

    @final
    def GainPeril(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Peril', by_effect)

