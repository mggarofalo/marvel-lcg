from . import *

# A Sound Thrashing

def GetAbilities() -> Sequence['Ability']:

    def a_sound_thrashing_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minions = Worlds.GetOnFieldMinions(effect)
        minions = CardFinder(name="Creeping Willow").Checks(minions)

        attcked = False

        for minion in minions:
            attacking_player = minion.GetEngagedPlayer()
            def action(atk_message: 'Message.AfterUnitAttackEnd'):
                nonlocal attcked
                if attacking_player == player:
                    attcked = True
            minion.DoAttackYou(attacking_player, effect,
                operation_end=action
            )

        if not attcked:
            minion = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Creeping Willow",
                card_type=Minion
            )

            if minion:
                minion.Reveal(None, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            a_sound_thrashing_revealed
        ),
    ]

