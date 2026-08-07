from . import *

# * Triage: Christopher Muse

def GetAbilities() -> Sequence['Ability']:

    def triage(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            triage
        ).SetTarget(Unit2, trait="X-MEN", canbe_heal=True),
    ]

