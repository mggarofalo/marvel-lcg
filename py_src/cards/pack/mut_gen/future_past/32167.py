from . import *

# * Bastion

def GetAbilities() -> Sequence['Ability']:

    def bastion_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DealEncounterCard(this, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            bastion_boost
        ),
    ]

