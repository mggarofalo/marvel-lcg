from . import *

# * Venom

def GetAbilities() -> Sequence['Ability']:

    def venom_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            PlaceFaceDownBoostCardsOnPlayer(player, 2, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            venom_revealed
        ),
        VengeanceRetribution(3)
    ]

