from . import *

# * Rictor: Julio Richter

def GetAbilities() -> Sequence['Ability']:

    def rictor(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.Response,
            "This",
            rictor,
        ).SetTarget("VillainAndYouEngagedMinion"),
    ]

