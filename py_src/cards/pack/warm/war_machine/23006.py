from . import *

# Missile Launcher

def GetAbilities() -> Sequence['Ability']:

    def missile_launcher(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect, property=AttackProperty(ranged=True))

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            missile_launcher
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter(CardFinder(name="War Machine"), 1, 'ammo'))
        .SetTarget(Enemy),
    ]

