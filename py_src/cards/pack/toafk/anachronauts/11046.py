from . import *

# Kang's Chosen

def GetAbilities() -> Sequence['Ability']:

    def kangs_chosen_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
            trait="TEMPORAL",
        )
        if minion:
            minion.Reveal(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            kangs_chosen_revealed
        ),
    ]

