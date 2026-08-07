from . import *

# * Longshot

def GetAbilities() -> Sequence['Ability']:

    def longshot(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = message.GetGaveToPlayer()
        this.PutIntoPlay(initiator, effect)
        ThisCardGainSurge(effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True,
        ),
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.WhenThisRevealed(
            None,
            longshot
        ).SetCannotBeCancel(),
    ]

