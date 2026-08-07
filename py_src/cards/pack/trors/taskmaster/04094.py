from . import *

# * Taskmaster

def GetAbilities() -> Sequence['Ability']:

    def taskmaster_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AfterPlayerChangesToHeroFormDiscardTheEncounterDeckAndTakeDamage(),
        AbilityFactory.WhenThisRevealed(
            None,
            taskmaster_revealed
        ),
    ]

