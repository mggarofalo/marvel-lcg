from . import *

# First Hit

def GetAbilities() -> Sequence['Ability']:

    def first_hit(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    def first_hit_interrupt(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            first_hit
        ).SetPlay().SetLabel('attack')
        .SetTarget(Villain),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroAction,
            Minion,
            first_hit_interrupt
        ).SetPlay().SetLabel('attack')
        .SetTarget("Trigger"),
    ]

