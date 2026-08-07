from . import *

# * Havok: Alex Summers

def GetAbilities() -> Sequence['Ability']:

    def havok(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        face = Worlds.DiscardEncounterTopCard(effect)
        if face:
            boost = FacesCounter.CountTotalBoostIcons([face])
            this.GainForThisActive(effect, message, attack=boost, attack_consequential_damage=boost)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "This",
            havok
        ),
    ]

