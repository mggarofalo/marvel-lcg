from . import *

# * Kurt's Chapel

def GetAbilities() -> Sequence['Ability']:

    def kurts_chapel(effect: 'Effect', message: 'Message.AfterUnitRecovery') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Kurt Wagner"),
            recover=1,
        ),
        AbilityFactory.AfterUnitMakeRecovery(
            AbilityType.AlterEgoResponse,
            "You",
            kurts_chapel
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Players"),
    ]

