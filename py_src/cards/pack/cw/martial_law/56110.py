from . import *

# Rapid Response

def GetAbilities() -> Sequence['Ability']:

    def rapid_response_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion
        )
        if minion:
            if player.IsAlterEgo():
                this.PlaceThreatOnSchemes("MainScheme", minion.scheme, effect)
            if player.IsInHeroForm():
                this.DealDamage([identity], minion.attack, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            rapid_response_revealed
        ),
    ]

