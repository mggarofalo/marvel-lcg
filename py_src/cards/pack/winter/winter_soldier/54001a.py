from . import *

# * Winter Soldier

def GetAbilities() -> Sequence['Ability']:

    def winter_soldier(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            "You",
            Enemy,
            winter_soldier
        ).SetName("Lethal Protector")
        .SetTarget(Scheme2),
    ]

