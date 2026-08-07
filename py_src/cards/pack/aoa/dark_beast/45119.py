from . import *

# * Dark Beast

def GetAbilities() -> Sequence['Ability']:

    def dark_beast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        RevealRandomSetAsideEnvironmentAndShuffleRest(effect)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)

    return [
        WhenDarkBeastAttacksYouResolveSpecialAbilityOnTheSETTINGEnvironment(),
        AbilityFactory.WhenThisRevealed(
            None,
            dark_beast_revealed
        ),
    ]

