from . import *

# Turn the Tide

def GetAbilities() -> Sequence['Ability']:

    def turn_the_tide(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.AfterUnitThwartAndRemoveLastThreat(
            AbilityType.Response,
            "YourHero",
            turn_the_tide,
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

