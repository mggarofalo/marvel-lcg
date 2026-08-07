from . import *

# * Marvel Girl: Rachel Summers

def GetAbilities() -> Sequence['Ability']:

    def marvel_girl(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        minion = message.target.CastTo(Minion)
        this.RemoveThreatFromSchemes(effect.targets, minion.printed_scheme, effect)


    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.Interrupt,
            "This",
            Minion,
            marvel_girl,
        ).SetTarget(MainScheme),
    ]

