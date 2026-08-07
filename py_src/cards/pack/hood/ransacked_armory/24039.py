from . import *

# Jetpack

def GetAbilities() -> Sequence['Ability']:

    def jetpack(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = Worlds.DiscardEncounterTopCard(effect)
        if face:
            value = FacesCounter.CountTotalBoostIcons([face])
        else:
            value = 0
        message.ReduceDamage(value, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            most_remaining_hp=True,
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "AttachedMinion",
            jetpack,
            is_from_attack=True
        ),
    ]

