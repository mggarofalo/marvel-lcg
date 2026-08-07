from . import *

# Teleported Away

def GetAbilities() -> Sequence['Ability']:

    def teleported_away(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.DoSchemeInstead(effect)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Mister Sinister"),
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Mister Sinister"),
            teleported_away
        ),
    ]

