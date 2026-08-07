from . import *

# Looking for Trouble

def GetAbilities() -> Sequence['Ability']:

    def looking_for_trouble(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            looking_for_trouble
        ).SetPlay().SetLabel('thwart')
        .SetTarget(MainScheme)
        .SetCostFunc(CostFunc.DiscardDeckTopUntil("EncounterDeck", Minion, "PutIntoPlayEngagedYou"))
    ]

