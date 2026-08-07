from . import *

# Teamwork Makes the Dream Work

def GetAbilities() -> Sequence['Ability']:

    def teamwork_makes_the_dream_work_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        if Worlds.IsExpert(effect):
            message.GainBoostIcon(2, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            teamwork_makes_the_dream_work_boost
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Enemy,
            scheme=1,
            attack=1,
        ),
    ]

