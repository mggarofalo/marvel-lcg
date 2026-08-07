from . import *

# * Brawn: Amadeus Cho

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("B"),
            conditions=[
                lambda effect, message:
                    effect.this.IsExhaust()
            ]
        ).LimitOncePerPhase(),
    ]

