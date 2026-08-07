from . import *

# The Trickster Tango

def GetAbilities() -> Sequence['Ability']:

    def the_trickster_tango_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.would_atk_message.GainOverKill(effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            stalwart=1
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            the_trickster_tango_boost,
            during_attack=True
        ),
    ]

