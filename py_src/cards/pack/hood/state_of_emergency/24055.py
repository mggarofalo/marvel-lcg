from . import *

# Feisty Heist

def GetAbilities() -> Sequence['Ability']:

    def feisty_heist_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardHandCards((1, 1), effect, highest_cost=True)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            feisty_heist_revealed
        ),
    ]

