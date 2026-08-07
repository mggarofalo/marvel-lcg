from . import *

# * Chimera

def GetAbilities() -> Sequence['Ability']:

    def chimera(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        value = FacesCounter.GetPrintedResourcesCount(player.GetControlCards(), "B")

        message.trigger.TemporaryGain(
            effect,
            message.would_message,
            scheme=value,
            attack=value,
        )

    return [
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            chimera
        ),
    ]

