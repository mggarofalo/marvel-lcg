from . import *

# Under Fire

def GetAbilities() -> Sequence['Ability']:

    def under_fire(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Worlds.RevealEncounterDeckTopCard(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            under_fire
        )
    ]
