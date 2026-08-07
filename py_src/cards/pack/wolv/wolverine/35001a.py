from . import *

# * Wolverine

def GetAbilities() -> Sequence['Ability']:

    def wolverine(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)

    return [
        AbilityFactory.AfterPlayerPhaseBegin(
            AbilityType.Response,
            wolverine
        ).SetTarget("YourHero", canbe_heal=True)
        .SetName("Healing Factor")
    ]

