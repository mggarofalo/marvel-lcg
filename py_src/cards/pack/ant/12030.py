from . import *

# Moment of Triumph

def GetAbilities() -> Sequence['Ability']:

    def moment_of_triumph(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = message.excess_damage
        this.HealthUnits(effect.targets, value, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.HeroResponse,
            "You",
            Enemy,
            moment_of_triumph,
            has_excess_damage=True,
        ).SetPlay().SetLabel()
        .SetTarget("YourHero", canbe_heal=True),
    ]

