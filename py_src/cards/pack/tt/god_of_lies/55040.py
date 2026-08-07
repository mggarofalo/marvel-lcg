from . import *

# * Minotaur

def GetAbilities() -> Sequence['Ability']:

    def minotaur_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        faces = Worlds.GetEncounterDiscardPileCards(effect, CardFinder(card_type=Treachery))
        if len(faces) >= 5:
            this.DoActivate(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            minotaur_revealed
        ),
        WhenDefeatedPlaceShatterCountersOnTheAvatarOfLokivillain(2)
    ]

