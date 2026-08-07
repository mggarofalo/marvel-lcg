from . import *

# Attack on Knowhere

def GetAbilities() -> Sequence['Ability']:

    def attack_on_knowhere_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                trait="SPELL",
            )
            if face:
                face.PutIntoPlay(player, effect)
        Players.ForEachPlayer(effect, action)

        Worlds.GetEncounterDeck(effect).ShuffleWithDiscardPile(False, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            attack_on_knowhere_revealed
        ),
    ]

