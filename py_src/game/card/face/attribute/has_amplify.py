from . import *

class HasAmplify(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        """
        AMPLIFY ICON
        The amplify icon represents various forces that are empowering or bolstering the villain. When a boost card is turned faceup during an enemy activation, add one additional boost icon to that card for each amplify icon in play.
        """
        self.printed_amplify = 0

        super().__init__(paper)

        self.RegisterAttribute("Amplify", "printed_amplify")
        self.RegisterInfoDict('amplify')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainAmplify(self.printed_amplify, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    @property
    def amplify(self) -> int:
        return self.GetKeyword('Amplify')

    @final
    def GainAmplify(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Amplify', by_effect)

    @final
    def SetIgnoreAmplify(self, diff: int, by_effect: 'Effect'):
        self.SetIgnoreKeyword(diff, 'Amplify', by_effect)

    @final
    def ResolveAmplify(self, face: 'EncounterNonVillainCard'):
        from game.effect.rule import GameRule
        if self.amplify:
            face.GainBoostIcons(self.amplify, GameRule(self))

