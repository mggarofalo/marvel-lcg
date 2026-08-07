from . import *

# The Dark Riders

def GetAbilities() -> Sequence['Ability']:

    def the_dark_riders_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
            trait="DARK RIDERS",
        )
        if minion:
            minion.Reveal(player, effect)

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("DARK RIDERS", Minion),
            toughness=1,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            the_dark_riders_revealed
        ),
    ]

