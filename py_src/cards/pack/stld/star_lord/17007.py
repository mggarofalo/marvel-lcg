from . import *

# Element Gun

def GetAbilities() -> Sequence['Ability']:

    def element_gun(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.DealDamage(effect.targets, 3, effect, property=AttackProperty(piercing=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            element_gun
        ).SetLabel('attack')
        .SetCost(Cost("1"))
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy)
    ]

