from . import *

# Tabula Rasa 16

def GetAbilities() -> Sequence['Ability']:

    def tabula_rasa_16_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        this.AttachTo2(player.GetIdentity(), effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Identity,
            treat_as_blank=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("BB")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            tabula_rasa_16_boost
        ),
    ]

