from . import *

class HasGuard(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_guard = 0

        super().__init__(paper)

        self.RegisterAttribute("Guard", "printed_guard")
        self.RegisterInfoDict('guard')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainGuard(self.printed_guard, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def IsGuard(self) -> bool:
        return self.guard > 0

    @final
    @property
    def guard(self) -> int:
        return self.GetKeyword('Guard')

    @final
    def GainGuard(self, diff: int, by_effect: 'Effect'):
        from game.card.face.card_type import Minion
        assert Minion.IsType(self)
        self.ClearGuardPatrolEffectTargets()
        self.GainKeyword(diff, 'Guard', by_effect)
        self.SetGuardPatrolEffectedBy(self.IsGuard(), self.IsPatrol())

