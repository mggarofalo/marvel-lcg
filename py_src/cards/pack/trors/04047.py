from . import *

# Skilled Investigator

def GetAbilities() -> Sequence['Ability']:

    def skilled_investigator(effect: 'Effect', message: 'Message.AfterSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        AbilityFactory.AfterSchemeBeDefeated(
            AbilityType.HeroResponse,
            SchemeSide2,
            skilled_investigator
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Initiator"),
    ]

