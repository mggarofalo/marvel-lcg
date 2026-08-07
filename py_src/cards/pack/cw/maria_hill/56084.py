from . import *

# Life Model Decoy

def GetAbilities() -> Sequence['Ability']:

    def life_model_decoy(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.PreventDamage("All", effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "Leader|Minion"
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            stalwart=1
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            life_model_decoy,
            is_from_attack=True
        ),
    ]

