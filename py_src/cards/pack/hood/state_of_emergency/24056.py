from . import *

# Disaster at the Docks

def GetAbilities() -> Sequence['Ability']:

    def disaster_at_the_docks_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        identity.TakeIndirectDamage(this, 3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            disaster_at_the_docks_revealed
        ),
    ]

