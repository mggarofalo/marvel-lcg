from . import *

# Self-propelled Glide

def GetAbilities() -> Sequence['Ability']:

    def self_propelled_glide(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        face = FindFinesse(effect)
        size = face.GetCounters('skill')
        message.ReduceDamage(size, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Spider-Woman"),
            self_propelled_glide,
            is_from_attack=True
        ),
    ]

