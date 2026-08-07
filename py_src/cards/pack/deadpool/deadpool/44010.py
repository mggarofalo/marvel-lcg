from . import *

# Deadpool's Katana

def GetAbilities() -> Sequence['Ability']:

    def deadpools_katana(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetIdentity()
        hero.DealDamage(effect.targets, 2, effect, property=AttackProperty(piercing=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            deadpools_katana
        ).SetLabel('attack')
        .SetTarget(Enemy)
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.TakeDamage(1, "YourIdentity")),
    ]

