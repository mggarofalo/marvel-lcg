from . import *

# * Winter Soldier: Bucky Barnes

def GetAbilities() -> Sequence['Ability']:

    def winter_soldier(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            "This",
            Enemy,
            winter_soldier
        ).SetTarget(Scheme2),
    ]

