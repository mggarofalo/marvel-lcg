from . import *

# * Captain Britain: Brian Braddock

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenAllyWouldTakeConsequentialDamage(
            "This",
            after_attacking_minion=True,
            update_damage=-1,
        ),
        AbilityFactory.WhenAllyWouldTakeConsequentialDamage(
            "This",
            after_thwart_side_scheme=True,
            update_damage=-1,
        ),
    ]

