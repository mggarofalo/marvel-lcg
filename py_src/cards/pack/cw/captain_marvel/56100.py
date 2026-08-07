from . import *

# Cosmic Flight

def GetAbilities() -> Sequence['Ability']:

    def cosmic_flight(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.ReduceDamageTo(3, effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Captain Marvel"),
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Captain Marvel"),
            cosmic_flight,
            is_from_attack=True,
            more_than_damage=3,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

