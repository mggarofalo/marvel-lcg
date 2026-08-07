from . import *

# Sap Power

def GetAbilities() -> Sequence['Ability']:

    def sap_power(effect: 'Effect', message: 'Message.AfterPlayerTurnEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        hero = player.GetIdentity()
        hero.TakeDamage(this, 1, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.AfterPlayerTurnEnd(
            AbilityType.ForcedResponse,
            "You",
            sap_power
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("YY")),
    ]

