from . import *

# Infiltration

def GetAbilities() -> Sequence['Ability']:

    def infiltration(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        value = len(faces)
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

        minion = Filter.One(Filter.ByType(faces, Minion), effect)
        if minion:
            minion.PutIntoPlay(initiator, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            infiltration
        ).SetPlay().SetLabel('thwart')
        .SetCostFunc(CostFunc.DiscardDeckTopCards("EncounterDeck", (1, 5)))
        .SetTarget(Scheme2),
    ]

