from core import *
from game.effect import *
from game.card.face import *
from game.player import *

class AllyLimit:

    def __init__(self, player: 'Player', ally_limit: int) -> None:
        self.player = player
        self.ally_limit = ally_limit
        self.curr_ally = 0

    def Get(self):
        return self.ally_limit

    def Increase(self, diff: int, by_effect: 'Effect'):
        self.ally_limit += diff
        if diff < 0:
            self.CheckLimit([])
        # After the hero flip, it will clean all `effect_by`
        # self.GetIdentity().SetEffectedByEffect(diff, by_effect)

    def CheckLimit(self, gain_faces: List['CardFace']) -> bool:
        from game.card.face.card_face import CardFace
        from game.effect.rule import GameRule
        from game.operate.faces import Faces
        from game.card.card_finder import CardFinder
        from game.message import Message
        from game.ability.factory import AbilityFactory

        player = self.player

        max_repeat_times = 10
        while True:
            assert max_repeat_times > 0
            max_repeat_times -= 1

            if player.is_eliminated:
                return False
            if player.world.is_game_over:
                return False

            not_count_allies: List[CardFace] = []
            for ally in player.allies.Get() + gain_faces:
                message = Message.CheckIfAllyCountLimit(ally)
                message.Send()
                if message.not_count:
                    not_count_allies.append(ally)

            self.curr_ally = player.allies.GetSize() - len(not_count_allies) + len(gain_faces)
            exceed_num = self.curr_ally - self.ally_limit

            if exceed_num > 0:
                rule = GameRule(player.GetIdentity())

                allies = player.allies.Get()
                for face in allies:
                    if face in not_count_allies or face in gain_faces:
                        allies.remove(face)

                player.ChooseAbilities(
                    rule,
                    AbilityFactory.ForChoiceAbility(
                        "Ally Limit",
                        lambda targets:
                            Faces.DiscardAll(targets, rule),
                    ).SetTarget(allies, finder=CardFinder(canbe_discard=True))
                )
            else:
                break
        return True

