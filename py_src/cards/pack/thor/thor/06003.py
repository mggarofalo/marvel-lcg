from . import *

# Defender of the Nine Realms

def GetAbilities() -> Sequence['Ability']:

    def defender_of_the_nine_realms(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            defender_of_the_nine_realms
        ).SetPlay().SetLabel('thwart')
        .SetCostFunc(CostFunc.DiscardDeckTopUntil("EncounterDeck", Minion, "PutIntoPlayEngagedYou"))
        .SetTarget(Scheme2)
    ]

