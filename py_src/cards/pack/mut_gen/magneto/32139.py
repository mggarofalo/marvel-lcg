from . import *

# * Magneto

def GetAbilities() -> Sequence['Ability']:

    def magneto_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            magneto_revealed
        ),
        AfterMagnetoAttacksYouPlace1MagnetCounterOnTheMainScheme()
    ]

