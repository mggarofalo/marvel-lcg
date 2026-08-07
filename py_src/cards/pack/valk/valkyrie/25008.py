from . import *

# Flight of the Valkyrior

def GetAbilities() -> Sequence['Ability']:

    def flight_of_the_valkyrior(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Response,
            HAS_DEATH_GLOW_FINDER,
            flight_of_the_valkyrior,
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Scheme2),
    ]

