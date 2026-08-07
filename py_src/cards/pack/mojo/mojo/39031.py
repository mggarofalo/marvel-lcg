from . import *

# Undercover Mojo

def GetAbilities() -> Sequence['Ability']:

    def undercover_mojo(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.SetBeInstead(effect)

        num = message.will_take_damage
        this.RemoveThreatFromSchemes([this], num, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            Villain,
            undercover_mojo
        ),
    ]

