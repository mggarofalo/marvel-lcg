from . import *

# Heightened Reflexes

def GetAbilities() -> Sequence['Ability']:

    def heightened_reflexes(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.PreventDamage(2, effect)
        Faces.RemoveCountersOn([this], 1, 'leap', effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Batroc")
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Batroc"),
            heightened_reflexes
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Batroc")
        ),
    ]

