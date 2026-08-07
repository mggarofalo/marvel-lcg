from . import *

# Repulsor Beam

def GetAbilities() -> Sequence['Ability']:

    def repulsor_beam(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            repulsor_beam
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Counter(CardFinder(name="War Machine"), 1, 'ammo'))
        .SetTarget(Enemy),
    ]

