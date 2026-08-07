from . import *

"""
MODIFIERS

The game constantly checks and (if necessary) updates the count of any variable quantity that is being modified. Any time a new modifier is applied or removed, the entire quantity is recalculated from the start, considering the unmodified base value and all active modifiers.
* The calculation of a value treats all modifiers as being applied simultaneously. However, while performing the calculation, all additive and subtractive modifiers are calculated before doubling and/or halving modifiers are calculated.
* If a value is “set” to a specific number, the set modifier overrides all non-set modifiers. If multiple set modifiers are in conflict, the most recen
* After all active modifiers have been taken into account, if a value is below zero, it is treated as zero: a card cannot have “negative” icons, attributes, traits, cost, or keywords.
* Fractional values are rounded up after all modifiers have been applied
"""

class HasModify(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.modify_atk = 0
        self.modify_sch = 0
        self.modify_thw = 0

        super().__init__(paper)

        self.RegisterAttribute("ATK+", "modify_atk")
        self.RegisterAttribute("SCH+", "modify_sch")
        self.RegisterAttribute("THW+", "modify_thw")

    @override
    def GetAbilities(self) -> List['Ability']:
        def get_value(num: int) -> int|None:
            if num == 0:
                return None
            else:
                return num
        if self.modify_atk != 0 or self.modify_sch != 0 or self.modify_thw != 0:
            abilities = AbilityFactory.GiveKeywordToAttached(
                attack=get_value(self.modify_atk),
                scheme=get_value(self.modify_sch),
                thwart=get_value(self.modify_thw),
            )
        else:
            abilities = []
        return abilities + super().GetAbilities()

