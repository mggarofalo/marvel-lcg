from . import *

# Generation Why?

def GetAbilities() -> Sequence['Ability']:

    def generation_why_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        size = 0
        size += Worlds.FindCardSizeOnField(effect, card_type=Ally)
        size += Worlds.FindCardSizeOnField(effect, CardFinder2("PERSONA", Support))
        def action(player: 'Player'):
            player.DiscardDeckTopCards(size, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            generation_why_revealed
        ),
    ]

