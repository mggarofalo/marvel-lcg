from . import *

# "Off with His Head!"

def GetAbilities() -> Sequence['Ability']:

    def off_with_his_head_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            minion = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Minion
            )
            if minion:
                minion.Reveal(player, effect)

        Worlds.Enemies.AllMinionActivateAgainstEngagedPlayer(effect, if_no_activate=action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            off_with_his_head_revealed
        ),
    ]

