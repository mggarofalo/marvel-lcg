from . import *

# Mass Attack

def GetAbilities() -> Sequence['Ability']:

    def mass_attack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        damage = hero.attack
        for target in effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards:
            ally = target.CastTo(Ally)
            damage += ally.attack

        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mass_attack
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust("YourAlly", share_trait_with_your_hero=True, range=(3, 3)))
        .SetTarget(Enemy),
    ]

