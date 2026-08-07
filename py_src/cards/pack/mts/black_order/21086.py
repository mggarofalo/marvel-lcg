from . import *

# * Supergiant

def GetAbilities() -> Sequence['Ability']:

    def supergiant(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([message.attacked], "Stunned", effect)

    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "Character",
            supergiant,
            target_in_play=True,
        ),
    ]

