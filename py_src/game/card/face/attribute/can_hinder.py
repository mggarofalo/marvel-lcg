from . import *

class HasHinder(HasAttribute):

    @override
    def __init__(self, paper: 'Paper') -> None:
        # When a card with the hinder X keyword enters play, place
        # X threat on that card. (X is the value next to the hinder
        # keyword.)
        self.printed_hinder = 0

        super().__init__(paper)

        self.RegisterAttribute("Hinder", "printed_hinder")

    @override
    def CheckPrintedValue(self):
        assert self.printed_hinder >= 0
        return super().CheckPrintedValue()

    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainHinder(self.printed_hinder, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def GainHinder(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Hinder', by_effect)

    @property
    def hinder(self) -> int:
        return self.GetKeyword('Hinder')

class CanHinder(HasHinder, CanPlaceToken):

    @override
    def OnWouldEnterPlay(self, into_area: 'Deck') -> bool:
        self.ResolveHinder()
        return super().OnWouldEnterPlay(into_area)

    # @override
    # def OnFlip(self, by_effect: 'Effect', origin_face: 'CardFace|None'):
    #     self.GainHinder(self.printed_hinder, by_effect)
    #     return super().OnFlip(by_effect, origin_face)

    @override
    def OnAfterFlip(self, by_effect: 'Effect', call_reveal: bool = False):
        super().OnAfterFlip(by_effect, call_reveal)
        if self.IsInPlay():
            self.ResolveHinder()

    ################################################################################
    #
    @final
    def ResolveHinder(self):
        from game.effect.rule import ResetKeyword
        if self.hinder:
            self.PlaceTokensInternal(self.hinder, 'threat', ResetKeyword(self))

