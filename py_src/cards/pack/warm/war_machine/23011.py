from . import *

# Full Auto

def GetAbilities() -> Sequence['Ability']:

    def full_auto(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 8, effect, property=AttackProperty(overkill=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            full_auto
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Counter(CardFinder(name="War Machine"), 4, 'ammo'))
        .SetTarget(Enemy),
    ]

