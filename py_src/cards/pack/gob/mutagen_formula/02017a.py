from . import *

# Unleashing the Mutagen - 1A

def GetAbilities() -> Sequence['Ability']:

    def unleashing_the_mutagen(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            SetupCards.PutIntoPlay(
                effect,
                for_player=player,
                name="Goblin Thrall",
                card_type=Minion
            )

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            unleashing_the_mutagen
        )
    ]

