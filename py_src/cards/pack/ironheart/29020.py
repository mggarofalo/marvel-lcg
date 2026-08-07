from . import *

# R&D Facility

def GetAbilities() -> Sequence['Ability']:

    def r_d_facility(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            target.GainUntilPhaseEnd(
                effect,
                thwart=1,
                attack=1,
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            r_d_facility
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'research'))
        .SetTarget(Friend),
    ]

