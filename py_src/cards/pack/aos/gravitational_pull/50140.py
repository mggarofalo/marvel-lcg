from . import *

# Rule the Skies

def GetAbilities() -> Sequence['Ability']:

    def rule_the_skies_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.GiveBoostCardForThisActivation(message.activating_enemy, 1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("AERIAL", Unit2),
            attack=+1,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            rule_the_skies_boost,
            activating_enemy=CardFinder2("AERIAL")
        ),
    ]

