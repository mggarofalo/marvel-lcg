from . import *

# * Fabian Cortez

def GetAbilities() -> Sequence['Ability']:

    def fabian_cortez(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()

        if player:
            player.DiscardDeckTopCards(4, effect)

    def fabian_cortez_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardDeckTopCards(4, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            fabian_cortez
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            fabian_cortez_boost
        ),
    ]

