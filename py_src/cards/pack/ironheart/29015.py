from . import *

# * Falcon: Joaquin Torres

def GetAbilities() -> Sequence['Ability']:

    def falcon(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitAttackOrThwart(
            AbilityType.HeroResponse,
            "This",
            falcon
        ).SetCost(Cost("Y"))
        .SetTarget("YouControlUnit", trait="CHAMPION", canbe_ready=True, another=True),
    ]

