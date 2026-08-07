from . import *

# The Power Stone

def GetAbilities() -> Sequence['Ability']:

    def the_power_stone_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Worlds.GetEncounterDeck(effect).ShuffleWithDiscardPile(False, effect)

        def action(player: 'Player'):
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                trait="SPELL",
            )
            if face:
                face.PutIntoPlay(player, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_power_stone_revealed
        ),
    ]

