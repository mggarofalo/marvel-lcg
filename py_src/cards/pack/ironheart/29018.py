from . import *

# Push Ahead

def GetAbilities() -> Sequence['Ability']:

    def push_ahead(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        value = hero.thwart + hero.attack + hero.defense
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            push_ahead
        ).SetPlay().SetLabel('thwart')
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
        .SetTarget(Scheme2),
    ]

