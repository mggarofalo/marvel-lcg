from . import *

# Master of Illusions

def GetAbilities() -> Sequence['Ability']:

    def master_of_illusions(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = Worlds.DiscardEncounterTopCard(effect)
        if face and Treachery.IsType(face):
            message.PreventDamage("All", effect)
            Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Loki")
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Loki"),
            master_of_illusions,
            is_from_attack=True,
        ),
    ]

