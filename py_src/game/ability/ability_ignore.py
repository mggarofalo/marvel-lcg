from typing import Final
from core import *
from game.ability import *
from game.effect import *

class AbilityIgnore:

    def __init__(self, ability: 'Ability') -> None:
        from game.card.face.card_face import CardFace

        self.ability: Final = ability
        self.keyword: Dict['CardFace.ABILITY_IGNORE_KEY', Callable[['Effect'], bool]] = {
            'Guard': lambda effect: False,
            'Patrol': lambda effect: False,
            'Crisis': lambda effect: False,
        }
        self.out_of_play = self.ability.flags.is_rule or self.ability.flags.is_temp
        self.game_area = False

    @property
    def be_removed(self) -> bool:
        if self.ability.flags.is_rule or self.ability.flags.is_temp:
            return True
        if self.ability.flags.is_status:
            return True
        if self.ability.ignore.out_of_play:
            return True
        return False

    @property
    def treat_as_if_blank(self) -> bool:
        if self.ability.flags.is_rule or self.ability.flags.is_temp:
            return True
        if self.ability.flags.is_ask:
            return True
        if self.ability.flags.is_choose_ability or self.ability.flags.is_basic_power:
            return True
        if self.ability.func_names == set(['Change Form']):
            return True
        return False

