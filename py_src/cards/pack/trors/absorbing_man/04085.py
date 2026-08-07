from . import *

# Stall Tactics

def GetAbilities() -> Sequence['Ability']:

    def stall_tactics_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            value = scheme.GetCounters('delay') // 2
        else:
            value = 0
        if value > 0 and scheme:
            this.PlaceThreatOnSchemes([scheme], value, effect)
        else:
            ThisCardGainSurge(effect)

    def stall_tactics_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        scheme = Worlds.FindMainScheme(this)
        if scheme and scheme.GetCounters('delay') >= 5:
            player.GetIdentity().TakeIndirectDamage(this, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            stall_tactics_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            stall_tactics_boost
        ),
    ]

