from . import *

# Fusillade

def GetAbilities() -> Sequence['Ability']:

    def fusillade(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        # initiator = effect.GetInitiator()

        # exhaust_effect = initiator.ChooseAbilities(
        #     effect,
        #     AbilityFactory.ForChoiceAbility(
        #         "",
        #         lambda targets:
        #             None
        #     ).SetTarget2(Select.From("YouControlUpgrade", trait="WEAPON"))
        #     .SetCostFunc(CostFunc.Exhaust("This"))
        #     .SetDefault(),
        # )

        # assert exhaust_effect
        this.DealDamage(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            fusillade,
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust(
            card_type=Upgrade,
            trait="WEAPON",
            from_where=["YouControlCards"]))
        .SetTarget(Enemy)
    ]

