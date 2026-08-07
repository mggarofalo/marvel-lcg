from . import *

# * Precinct HQ

def GetAbilities() -> Sequence['Ability']:

    def precinct_hq(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        size = 1 + len(initiator.GetEngagedMinions())
        this.RemoveThreatFromSchemes(effect.targets, size, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            precinct_hq
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2),
    ]

