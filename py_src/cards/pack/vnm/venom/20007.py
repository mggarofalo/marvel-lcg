from . import *

# * Project Rebirth 2.0

def GetAbilities() -> Sequence['Ability']:

    def project_rebirth_20(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Draw 1 card",
                lambda targets:
                    Players.DrawUp(targets, 1, effect)
            ).SetTarget("YourIdentity"),
            AbilityFactory.ForChoiceAbility(
                "Heal 3 damage from Flash Thompson",
                lambda targets:
                    this.HealthUnits(targets, 3, effect)
            ).SetTarget("YourIdentity", canbe_heal=True)
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            project_rebirth_20
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

