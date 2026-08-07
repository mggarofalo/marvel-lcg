from typing import TypeAlias
from core import *

from game.ability import *

class ConditionAbilityType:

    ONE_ABILITY_TYPE: TypeAlias = Literal["AlterEgoAction", "Action", "TriggeredAbility", "Interrupt", "Response", "Special", "ForcedResponse", "WhenRevealed", "ForcedInterrupt", "NonKeyword"]
    ABILITY_TYPE: TypeAlias = ONE_ABILITY_TYPE|List[ONE_ABILITY_TYPE]|None

    @staticmethod
    def CheckWhichAbility(check_rule: 'ABILITY_TYPE', ability: 'Ability') -> bool:
        from game.ability.condition import Condition
        if check_rule == None:
            return True

        def invoke_check(ability_type: 'Condition.ONE_ABILITY_TYPE') -> bool:
            if ability_type == "TriggeredAbility":
                return ability.flags.is_action or \
                    ability.flags.is_interrupt or \
                    ability.flags.is_response or \
                    ability.flags.is_resource
            if ability_type == "ForcedInterrupt":
                return ability.flags.IsType(AbilityType.ForcedInterrupt)
            if ability_type == "Interrupt":
                return ability.flags.is_interrupt
            if ability_type == "Response":
                return ability.flags.is_response
            if ability_type == "Special":
                return ability.flags.is_special
            if ability_type == "Action":
                return ability.flags.is_action
            if ability_type == "ForcedResponse":
                return ability.flags.IsType(AbilityType.ForcedResponse)
            if ability_type == "WhenRevealed":
                return ability.flags.IsType(AbilityType.WhenRevealed)
            if ability_type == "AlterEgoAction":
                return ability.flags.IsType(AbilityType.AlterEgoAction)
            if ability_type == "NonKeyword":
                return ability.flags.IsType(AbilityType.NonKeyword) or \
                    ability.flags.IsType(AbilityType.NonKeywordBold) or \
                    ability.flags.IsType(AbilityType.NonKeywordStar)

        if isinstance(check_rule, str):
            return invoke_check(check_rule)
        else:
            for ability_type in check_rule:
                if invoke_check(ability_type):
                    return True
            return False

