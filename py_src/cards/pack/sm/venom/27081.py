from . import *

# Tooth and Nail

def GetAbilities() -> Sequence['Ability']:

    def tooth_and_nail_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.would_atk_message.GainPiercing(effect)

    return [
        AfterVenomTakesAttackDamageRemoveEqualThreat(),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            tooth_and_nail_boost,
            during_attack=True,
        ),
    ]

