from . import *

# * Blob

def GetAbilities() -> Sequence['Ability']:

    def blob(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Villain|Minion)
        Unused(this)

        Faces.GiveStatus(message.attacked_targets, "Stunned", effect)

    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            blob,
            target_in_play=True,
        ),
    ]

