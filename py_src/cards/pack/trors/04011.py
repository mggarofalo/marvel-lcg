from . import *

# * Hawkeye

def GetAbilities() -> Sequence['Ability']:

    def hawkeye(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        damage = FacesCounter.GetPrintedResourcesIcon(faces, initiator.hand_cards)

        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            hawkeye
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("YourHandCards"))
        .SetTarget(Enemy)
        # .SetDesc('Exhaust this ally and discard 1 card from your hand → deal X damage to an enemy')
    ]

