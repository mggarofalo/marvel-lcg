from . import *

# Psionic Enhancement

def GetAbilities() -> Sequence['Ability']:

    def psionic_enhancement_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.GiveActivatingEnemyAdditionalBoostCard(1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            psionic_enhancement_boost
        ),
    ]

