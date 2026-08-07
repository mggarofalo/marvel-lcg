from . import *

# Beast Mode

def GetAbilities() -> Sequence['Ability']:

    def beast_mode(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.IncreaseDamage(1, effect)

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(card_type=Friend, is_stunned=True),
            beast_mode,
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(card_type=Friend, is_confused=True),
            beast_mode,
        )
    ]

