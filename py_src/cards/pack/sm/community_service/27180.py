from . import *

# Rubble Rescue

def GetAbilities() -> Sequence['Ability']:

    def rubble_rescue(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.UseATKInsteadTHW(effect)


    return [
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.Interrupt,
            "Character",
            "This",
            rubble_rescue,
            is_basic_thwart=True,
        ),
    ]

