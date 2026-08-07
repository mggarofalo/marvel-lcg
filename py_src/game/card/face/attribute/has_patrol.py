from . import *

class HasPatrol(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_patrol = 0

        super().__init__(paper)

        self.RegisterAttribute("Patrol", "printed_patrol")
        self.RegisterInfoDict('patrol')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainPatrol(self.printed_patrol, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def IsPatrol(self) -> bool:
        return self.patrol > 0

    @final
    @property
    def patrol(self) -> int:
        return self.GetKeyword('Patrol')

    @final
    def GainPatrol(self, diff: int, by_effect: 'Effect'):
        from game.card.face.card_type import Minion
        assert Minion.IsType(self)
        self.ClearGuardPatrolEffectTargets()
        self.GainKeyword(diff, 'Patrol', by_effect)
        self.SetGuardPatrolEffectedBy(self.IsGuard(), self.IsPatrol())

