from . import *

# Sky Cycle

def GetAbilities() -> Sequence['Ability']:

    def sky_cycle(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("AVENGER", Ally)
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            sky_cycle,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Attached", canbe_ready=True),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            trait="AERIAL",
        )
    ]

