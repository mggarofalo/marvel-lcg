from . import *

# * Siryn: Theresa Cassidy

def GetAbilities() -> Sequence['Ability']:

    def siryn(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.Response,
            "This",
            siryn
        ).SetTarget(Minion, canbe_stunned=True),
    ]

