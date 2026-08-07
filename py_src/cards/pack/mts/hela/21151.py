from . import *

# The Wastes of Niffleheim

def GetAbilities() -> Sequence['Ability']:

    def the_wastes_of_niffleheim_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        damage = 1 + GetSideSchemesNumInVictoryDisplay(effect)
        player.GetIdentity().TakeIndirectDamage(this, damage, effect)


    def the_wastes_of_niffleheim_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        value = GetSideSchemesNumInVictoryDisplay(effect)
        message.GainBoostIcon(value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_wastes_of_niffleheim_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            the_wastes_of_niffleheim_boost
        ),
    ]

