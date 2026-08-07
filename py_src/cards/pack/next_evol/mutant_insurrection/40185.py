from . import *

# * Dragoness

def GetAbilities() -> Sequence['Ability']:

    def dragoness(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        value = FacesCounter.GetPrintedResourcesCount(player.hand_cards.Get(), "Y")

        this.GainForThisActive(
            effect,
            message.would_message,
            scheme=value,
            attack=value,
        )


    return [
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            dragoness
        ),
    ]

