from . import *

# Violent Tendencies

def GetAbilities() -> Sequence['Ability']:

    def violent_tendencies(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = this.GetBindFace()
        Faces.GiveFacedownBoostCards([villain], 1, effect)

        if message.deal_damage >= 3:
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            "AttachedVillain",
            violent_tendencies,
            is_from_attack=True,
        )
    ]

