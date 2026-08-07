from . import *

# Cosmic Awareness

def GetAbilities() -> Sequence['Ability']:

    def cosmic_awareness(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 3
        value += FacesCounter.GetAspectCount(effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards)
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            cosmic_awareness
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", (1, 4))),
    ]

