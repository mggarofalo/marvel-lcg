from . import *

# Law of Attraction

def GetAbilities() -> Sequence['Ability']:

    def law_of_attraction_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            trait="ENTHRALLED",
            card_type=Minion
        )
        if minion:
            minion.Reveal(player, effect)

    def law_of_attraction_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        PlaceCharmCounter(player, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            law_of_attraction_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            law_of_attraction_boost
        ),
    ]

