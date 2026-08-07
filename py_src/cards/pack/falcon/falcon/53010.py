from . import *

# Battlefield Awareness

def GetAbilities() -> Sequence['Ability']:

    def battlefield_awareness(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        value = FacesCounter.CountTotalBoostStarAndBoost(faces)
        message.GainValue(value, effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.HeroInterrupt,
            CardFinder(name="Falcon"),
            battlefield_awareness
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DiscardDeckTopCards("EncounterDeck", 1))
        .SetTarget("Trigger"),
    ]

