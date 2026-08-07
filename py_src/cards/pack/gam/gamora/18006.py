from . import *

# Decisive Blow

def GetAbilities() -> Sequence['Ability']:

    def decisive_blow(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        damage = 4
        if HavePlayedEventThisTurn(initiator, "THWART"):
            damage = 7
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            decisive_blow
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

