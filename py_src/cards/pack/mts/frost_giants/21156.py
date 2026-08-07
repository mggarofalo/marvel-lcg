from . import *

# * Laufey

def GetAbilities() -> Sequence['Ability']:

    def laufey(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([message.attacked], "Stunned", effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            laufey,
            target_in_play=True
        ),
    ]

