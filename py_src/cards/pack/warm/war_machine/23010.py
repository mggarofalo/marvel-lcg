from . import *

# Scorched Earth

def GetAbilities() -> Sequence['Ability']:

    def scorched_earth(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            scorched_earth
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Counter(CardFinder(name="War Machine"), 3, 'ammo'))
        .SetTarget(Enemy, range="All")
    ]

