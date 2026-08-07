from . import *

# * Daredevil

def GetAbilities() -> Sequence['Ability']:

    def daredevil(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            daredevil
        ).SetTarget(Enemy)
        # .SetDesc('Deal 1 damage to an enemy'),
    ]

