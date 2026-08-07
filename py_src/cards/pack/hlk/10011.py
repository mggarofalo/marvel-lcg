from . import *

# * Brawn

def GetAbilities() -> Sequence['Ability']:

    def brawn(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)
        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.Response,
            "This",
            brawn
        ).SetTarget(Scheme2)
    ]

