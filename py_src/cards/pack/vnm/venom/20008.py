from . import *

# * Multi-Gun

def GetAbilities() -> Sequence['Ability']:

    def multi_gun(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        def deal_damage_to_engaged_player(targets: Sequence['CardFace']):
            chose_player = targets[0].GetControlByPlayer()
            units = chose_player.GetEngagedMinions()
            this.DealDamage(units, 1, effect)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Deal 2 damage to an enemy",
                lambda targets:
                    this.DealDamage(targets, 2, effect)
            ).SetTarget(Enemy),
            AbilityFactory.ForChoiceAbility(
                "Choose a player. Deal 1 damage to each minion engaged with that player",
                deal_damage_to_engaged_player
            ).SetTarget("Players"),
            AbilityFactory.ForChoiceAbility(
                "Remove 2 threat from a scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 2, effect)
            ).SetTarget(Scheme2)
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            multi_gun
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget2(Enemy)
        .SetTarget2("Players")
        .SetTarget2(Scheme2),
    ]

