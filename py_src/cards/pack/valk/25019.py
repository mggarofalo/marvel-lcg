from . import *

# Smash the Problem

def GetAbilities() -> Sequence['Ability']:

    def smash_the_problem(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetHero().attack
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            smash_the_problem
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetCostFunc(CostFunc.Exhaust("YourHero")),
    ]

