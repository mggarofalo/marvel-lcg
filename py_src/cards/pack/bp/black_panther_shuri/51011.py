from . import *

# Panther Claws

def GetAbilities() -> Sequence['Ability']:

    def panther_claws(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        damage = 2
        piercing = False

        deal_effect = initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard this card to deal 3 additional damage to that enemy",
            ).SetCostFunc(CostFunc.Discard("This"))
        )

        if deal_effect:
            damage += 3
            piercing = True

        this.DealDamage(effect.targets, damage, effect, property=AttackProperty(piercing=piercing))


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            panther_claws
        ).SetLabel('attack')
        .SetTarget(Enemy),
    ]

