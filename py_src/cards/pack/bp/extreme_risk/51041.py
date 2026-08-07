from . import *

# Playing for Keeps

def GetAbilities() -> Sequence['Ability']:

    def playing_for_keeps(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.GiveBoostCardForThisActivation(1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Identity,
            hand_size=+1,
        ),
        AbilityFactory.WhenEnemyWouldActivate(
            AbilityType.ForcedInterrupt,
            None,
            playing_for_keeps
        ),
    ]

