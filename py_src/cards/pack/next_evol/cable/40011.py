from . import *

# Plasma Rifle

def GetAbilities() -> Sequence['Ability']:

    def plasma_rifle(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        damage = Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=SchemeSide2))
        damage = min(4, damage)
        this.DealDamage(effect.targets, damage, effect, property=AttackProperty(ranged=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            plasma_rifle
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("Y"))
        .SetTarget(Enemy),
    ]

