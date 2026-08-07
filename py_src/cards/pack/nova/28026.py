from . import *

# Yaw and Roll

def GetAbilities() -> Sequence['Ability']:

    def yaw_and_roll(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)


    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.HeroResponse,
            "YourHero",
            yaw_and_roll
        ).SetPlay(only_if_your_identity_has_trait="AERIAL").SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

