from . import *

# Targeted Strike

def GetAbilities() -> Sequence['Ability']:

    def targeted_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            targeted_strike
        ).SetPlay().SetLabel('thwart')
        .SetCostFunc(CostFunc.Counter(CardFinder(name="War Machine"), 1, 'ammo'))
        .SetTarget(Scheme2)
    ]

