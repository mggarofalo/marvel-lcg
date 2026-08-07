from . import *

# Abjuration

def GetAbilities() -> Sequence['Ability']:

    def abjuration(effect: 'Effect', message: 'Message.AfterDamageBePrevented') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Ebony Maw")
        ),
        AbilityFactory.PreventAllDamageTo(
            CardFinder(name="Ebony Maw")
        ),
        AbilityFactory.AfterDamageBePrevented(
            AbilityType.ForcedResponse,
            abjuration,
            prevent_by="This",
            is_from_attack=True,
            damage_more_than=2
        )
    ]

