from . import *

class HasVictory(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_victory: int|None = None

        super().__init__(paper)

        self.RegisterAttribute("Victory", "printed_victory")
        # self.RegisterInfoDict('victory')

    @override
    def GetInfoDict(self) -> Dict[str, int]:
        return {
            'victory': self.printed_victory if self.printed_victory != None else 0,
        } | super().GetInfoDict()

    @override
    def OnBeDefeated(self, would_defeated_message: 'Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated', *, as_asset: bool, ignore_when_defeated: bool):
        from game.card.face.base import Villain
        from game.effect.rule import GameRule
        from game.operate.faces import Faces
        # if self.victory and not Villain.IsType(self) and self.IsThisFaceUp():
        if self.victory and self.IsThisFaceUp():
            do_move = True
            if Villain.IsType(self):
                next_villain = self.card.world.scenario.GetNextVillain(self)
                if(not next_villain or not next_villain.IsName(self.name, check_all_face=True)):
                    pass
                else:
                    do_move = False
            if do_move:
                Faces.AddToVictoryDisplay([self], GameRule(self))
                # self.card.MoveToArea(self.card.world.victory_display, GameRule(self))
        return super().OnBeDefeated(would_defeated_message, as_asset=as_asset, ignore_when_defeated=ignore_when_defeated)

    # @override
    # def OnResetKeywords(self, by_effect: 'Effect'):
    #     from game.ability.rule import ResetKeyword
    #     if self.printed_victory != None:
    #         self.GainVictory(self.printed_victory, ResetKeyword(self))
    #     return super().OnResetKeywords(by_effect)

    # def GainVictory(self, diff: int, by_effect: 'Effect'):
    #     self.GainKeyword(diff, 'Victory', by_effect)

    @final
    @property
    def victory(self) -> bool:
        return self.printed_victory != None

