from . import *

# Stun Beam

def GetAbilities() -> Sequence['Ability']:

    def stun_beam(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.GiveStatus(message.attacked_targets, "Stunned", effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder2("SENTINEL", Minion),
            without_another_copy=True,
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "AttachedMinion",
            None,
            stun_beam,
            target_in_play=True,
        ),
    ]

