from . import *

# * The Iliad

def GetAbilities() -> Sequence['Ability']:

    def the_iliad(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Deal 5 damage to an enemy",
                lambda targets:
                    this.DealDamage(targets, 5, effect)
            ).SetTarget(Enemy),
            AbilityFactory.ForChoiceAbility(
                "Remove 4 threat from a scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 4, effect)
            ).SetTarget(Scheme2),
            AbilityFactory.ForChoiceAbility(
                "Heal 3 damage from an identity",
                lambda targets:
                    this.HealthUnits(targets, 3, effect)
            ).SetTarget(Identity, canbe_heal=True),
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_iliad
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'mission'))
        .SetTarget2(Enemy)
        .SetTarget2(Scheme2)
        .SetTarget2(Identity),
    ]

