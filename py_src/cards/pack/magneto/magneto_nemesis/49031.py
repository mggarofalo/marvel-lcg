from . import *

# * Frenzy

def GetAbilities() -> Sequence['Ability']:

    def frenzy(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player:
            player.DiscardDeckTopCards(2, effect)

    def frenzy_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardDeckTopCards(4, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            frenzy
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            frenzy_boost
        ),
    ]

