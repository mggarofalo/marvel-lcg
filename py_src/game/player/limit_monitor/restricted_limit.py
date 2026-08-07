from core import *
from game.ability import *
from game.card.face import *
from game.player import *
from game.message import *

class RestrictedLimit:

    def __init__(self, player: 'Player', restricted_limit: int) -> None:
        self.player = player
        self.restricted_limit = restricted_limit
        self.additional_restricted_limit: Dict['Effect', 'CardFinder'] = {}
        self.curr_restricted = 0

    def Get(self):
        return self.restricted_limit + len(self.additional_restricted_limit)

    # CanControlAdditionalRestrictedUpgrade
    def Increase(self, diff: int, by_effect: 'Effect', finder: 'CardFinder'):
        from game.message import Message
        from game.operate.run_at import RunAt

        if diff == 1:
            self.additional_restricted_limit |= {by_effect: finder}
        elif diff == -1:
            del self.additional_restricted_limit[by_effect]
            message = by_effect.bind_message

            if type(message) is Message.WhenCardFlip:
                def action():
                    self.CheckLimit([])

                RunAt.AfterEventEnd(by_effect, message, action)

            elif type(message) is Message.AfterCardLostUpgradeAbility and message.by_flip:
                # If the ability is from an item, We don't check when identity change form (or name flip)
                # It will check in the previous `if` statement
                pass
            else:
                self.CheckLimit([])
        else:
            assert False, f"{diff=}"

    def CheckLimit(self, gain_faces: List['CardFace']) -> bool:
        from game.card.card_finder import CardFinder
        from game.card.face.card_type import Upgrade
        from game.operate.faces import Faces
        from game.effect.rule import GameRule
        from game.ability.factory import AbilityFactory
        # from game.selector import Select
        player = self.player

        max_repeat_times = 10
        gain_faces_restricted = 0
        for face in gain_faces:
            if Upgrade.IsType(face):
                gain_faces_restricted += face.restricted

        while True:
            assert max_repeat_times > 0
            max_repeat_times -= 1

            if player.is_eliminated:
                return False

            if player.world.is_game_over:
                return False

            faces = [x for x in player.GetIdentity().GetInventoryDeck().Get() if Upgrade.IsType(x) and x.restricted]
            has_restricted = 0
            for face in faces:
                has_restricted += face.restricted

            # TODO: Clean
            additional = 0
            visited_face: List['CardFace'] = []
            for effect in self.additional_restricted_limit:
                for face in faces + gain_faces:
                    if face in visited_face:
                        continue
                    card_finder = self.additional_restricted_limit[effect]
                    if card_finder.Check(face):
                        additional += 1
                        visited_face.append(face)
                        break

            self.curr_restricted = has_restricted + gain_faces_restricted
            exceed_num = self.curr_restricted - self.restricted_limit - additional

            if exceed_num > 0:

                rule = GameRule(player.GetIdentity())

                def action(targets: Sequence['CardFace']):
                    Faces.DiscardAll(targets, rule)

                target_faces = CardFinder(canbe_discard=True).Checks(faces)
                do_effects = player.ChooseAbilities(
                    rule,
                    AbilityFactory.ForChoiceAbility(
                        "Restricted Limit",
                        action,
                    ).SetTarget(target_faces)
                )
                if do_effects == []:
                    return False
            else:
                break
        return True

