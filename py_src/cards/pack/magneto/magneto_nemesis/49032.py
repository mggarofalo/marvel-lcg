from . import *

# Angry Acolyte

def GetAbilities() -> Sequence['Ability']:

    def angry_acolyte_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action():
            minion = Worlds.DiscardEncounterCardsUntil(
                effect,
                trait="ACOLYTE",
                card_type=Minion
            )
            if minion:
                player = message.GetToPlayer()
                minion.Reveal(player, effect)

        Worlds.Enemies.AllMinionActivateAgainstEngagedPlayer(effect, trait="ACOLYTE", if_no_activate=action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            angry_acolyte_revealed
        ),
    ]

