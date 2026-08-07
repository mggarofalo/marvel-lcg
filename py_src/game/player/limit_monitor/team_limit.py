from core import *
from game.ability import *
from game.card.face import *
from game.player import *

class TeamLimit:

    def __init__(self, player: 'Player') -> None:
        self.player = player

    def CheckLimit(self, gain_faces: List['CardFace']) -> bool:
        from game.operate.faces import Faces
        from game.effect.rule import GameRule
        from game.card.card_finder import CardFinder2
        from game.ability.factory import AbilityFactory
        player = self.player

        max_repeat_times = 10
        while True:
            assert max_repeat_times > 0
            max_repeat_times -= 1

            if player.is_eliminated:
                return False
            team_faces = player.GetControlCards2(CardFinder2("TEAM"))

            rule = GameRule(player.GetIdentity())
            if len(team_faces) + len(gain_faces) > 1:
                player.ChooseAbilities(
                    rule,
                    AbilityFactory.ForChoiceAbility(
                        "Team Card Limit",
                        lambda targets:
                            Faces.DiscardAll(targets, rule),
                    ).SetTarget(team_faces)
                )
            else:
                break
        return True

