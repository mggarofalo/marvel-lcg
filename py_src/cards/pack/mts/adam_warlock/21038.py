from . import *

# Karmic Blast

def GetAbilities() -> Sequence['Ability']:

    def karmic_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = 4
        damage += FacesCounter.GetAspectCount(effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards)
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            karmic_blast
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", (1, 4))),
    ]

