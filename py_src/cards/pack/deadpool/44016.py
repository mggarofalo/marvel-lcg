from . import *

# * Lady Deadpool: Wanda Wilson

def GetAbilities() -> Sequence['Ability']:

    def lady_deadpool(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.DefeatUnits(effect.targets, this, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            lady_deadpool
        ).SetTarget(Minion, non_trait="ELITE"),
    ]

