from . import *

# Bazooka

def GetAbilities() -> Sequence['Ability']:

    def bazooka(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        damage = Worlds.GetIconsNum(effect, ["Crisis", "Acceleration", "Amplify", "Hazard"])
        this.DealDamage(effect.targets, damage, effect, property=AttackProperty(ranged=True))

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            bazooka
        )
        .SetLabel('attack')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Enemy),
    ]

