from . import *

# * Scorpion

def GetAbilities() -> Sequence['Ability']:

    def scorpion(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus(message.attacked_targets, "Stunned", effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            scorpion
        ),
    ]

