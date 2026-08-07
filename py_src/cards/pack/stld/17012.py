from . import *

# * Beta Ray Bill

def GetAbilities() -> Sequence['Ability']:

    def beta_ray_bill(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            "This",
            Minion,
            beta_ray_bill,
        ).SetTarget(MainScheme)
    ]

