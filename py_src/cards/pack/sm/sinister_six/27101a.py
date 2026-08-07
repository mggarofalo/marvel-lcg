from . import *

# Sinister Beatdown

def GetAbilities() -> Sequence['Ability']:

    def sinister_beatdown_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = message.GetToPlayer()

        villain = ChooseSetAsideVillainAtRandom(effect)
        if villain:
            villain.PutIntoPlay(player, effect)
            villain.SetActive(effect)

        if not villain or Worlds.IsExpert(effect):
            first_player = Worlds.GetFirstPlayer(effect)
            first_player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sinister_beatdown_revealed
        ),
    ]

