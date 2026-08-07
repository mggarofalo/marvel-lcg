from . import *

# Laser Strike

def GetAbilities() -> Sequence['Ability']:

    def laser_strike_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if not player.DiscardControlCards(effect, upgrade=True):
            ThisCardGainSurge(effect)

    def laser_strike_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            laser_strike_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            laser_strike_boost,
            is_undefended_attack=True
        ),
    ]

